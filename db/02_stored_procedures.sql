/* ============================================================
   預存程序 — 全程參數化，杜絕 SQL Injection
     1) usp_MonthlyRevenue_Upsert            寫入（依主鍵 upsert，匯入可重跑）
     2) usp_MonthlyRevenue_GetByCompanyCode  以公司代號查詢
     3) usp_Company_Search                   關鍵字搜尋公司（自動完成）
     4) usp_DailyQuote_Upsert                每日行情寫入（依主鍵 upsert）
     5) usp_Quote_Ranking                    買賣投報排行（每元當日報酬／近月變量）
   ============================================================ */
USE TwseRevenue;
GO

/* -------- 寫入：以複合主鍵判斷，存在則更新、否則新增 --------
   採 upsert 而非單純 INSERT 的理由：每月資料會重複匯入/補正，
   upsert 讓匯入具冪等性（idempotent），重跑不會因主鍵衝突而中斷。 */
IF OBJECT_ID(N'dbo.usp_MonthlyRevenue_Upsert', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_MonthlyRevenue_Upsert;
GO
CREATE PROCEDURE dbo.usp_MonthlyRevenue_Upsert
    @CompanyCode           NVARCHAR(10),
    @DataYearMonth         INT,
    @ReportDate            INT,
    @CompanyName           NVARCHAR(60),
    @Industry              NVARCHAR(60)  = NULL,
    @CurrentMonthRevenue   BIGINT        = NULL,
    @LastMonthRevenue      BIGINT        = NULL,
    @LastYearMonthRevenue  BIGINT        = NULL,
    @MoMPercent            DECIMAL(18,6) = NULL,
    @YoYPercent            DECIMAL(18,6) = NULL,
    @CumCurrentRevenue     BIGINT        = NULL,
    @CumLastYearRevenue    BIGINT        = NULL,
    @CumDiffPercent        DECIMAL(18,6) = NULL,
    @Remark                NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.MonthlyRevenue WITH (HOLDLOCK) AS tgt
    USING (SELECT @CompanyCode AS CompanyCode, @DataYearMonth AS DataYearMonth) AS src
       ON  tgt.CompanyCode   = src.CompanyCode
       AND tgt.DataYearMonth = src.DataYearMonth
    WHEN MATCHED THEN UPDATE SET
        ReportDate           = @ReportDate,
        CompanyName          = @CompanyName,
        Industry             = @Industry,
        CurrentMonthRevenue  = @CurrentMonthRevenue,
        LastMonthRevenue     = @LastMonthRevenue,
        LastYearMonthRevenue = @LastYearMonthRevenue,
        MoMPercent           = @MoMPercent,
        YoYPercent           = @YoYPercent,
        CumCurrentRevenue    = @CumCurrentRevenue,
        CumLastYearRevenue   = @CumLastYearRevenue,
        CumDiffPercent       = @CumDiffPercent,
        Remark               = @Remark,
        UpdatedAt            = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT
        (CompanyCode, DataYearMonth, ReportDate, CompanyName, Industry,
         CurrentMonthRevenue, LastMonthRevenue, LastYearMonthRevenue, MoMPercent, YoYPercent,
         CumCurrentRevenue, CumLastYearRevenue, CumDiffPercent, Remark)
        VALUES
        (@CompanyCode, @DataYearMonth, @ReportDate, @CompanyName, @Industry,
         @CurrentMonthRevenue, @LastMonthRevenue, @LastYearMonthRevenue, @MoMPercent, @YoYPercent,
         @CumCurrentRevenue, @CumLastYearRevenue, @CumDiffPercent, @Remark);
END
GO

/* -------- 查詢：以公司代號取回各月資料，最新月份在前 -------- */
IF OBJECT_ID(N'dbo.usp_MonthlyRevenue_GetByCompanyCode', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_MonthlyRevenue_GetByCompanyCode;
GO
CREATE PROCEDURE dbo.usp_MonthlyRevenue_GetByCompanyCode
    @CompanyCode NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CompanyCode, DataYearMonth, ReportDate, CompanyName, Industry,
           CurrentMonthRevenue, LastMonthRevenue, LastYearMonthRevenue,
           MoMPercent, YoYPercent,
           CumCurrentRevenue, CumLastYearRevenue, CumDiffPercent, Remark
    FROM   dbo.MonthlyRevenue
    WHERE  CompanyCode = @CompanyCode      -- 參數化比對，非字串拼接
    ORDER BY DataYearMonth DESC;
END
GO

/* -------- 搜尋：依關鍵字列出符合的公司（自動完成用） --------
   keyword 可為「代號的一部分」或「名稱的一部分」（皆為包含比對）；
   每間公司取最新月一筆，最多 20 筆。
   全程參數化；另把 LIKE 萬用字元（% _ [）轉義，避免使用者輸入改變比對語意。
   排序：代號完全命中 > 代號前綴 > 名稱前綴 > 代號包含 > 名稱包含，
   讓「打完整代號（2330）」精準浮在最前，「打代號片段／公司名」也都列得出。 */
IF OBJECT_ID(N'dbo.usp_Company_Search', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_Company_Search;
GO
CREATE PROCEDURE dbo.usp_Company_Search
    @Keyword NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- 轉義 LIKE 萬用字元（用中括號轉義法，免 ESCAPE 子句）：[ → [[]、% → [%]、_ → [_]
    DECLARE @kw NVARCHAR(200) =
        REPLACE(REPLACE(REPLACE(@Keyword, N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]');

    ;WITH latest AS (
        SELECT CompanyCode, CompanyName, Industry,
               ROW_NUMBER() OVER (PARTITION BY CompanyCode ORDER BY DataYearMonth DESC) AS rn
        FROM   dbo.MonthlyRevenue
    )
    SELECT TOP (20) CompanyCode, CompanyName, Industry
    FROM   latest
    WHERE  rn = 1
      AND  (CompanyCode LIKE N'%' + @kw + N'%' OR CompanyName LIKE N'%' + @kw + N'%')
    ORDER BY
        CASE WHEN CompanyCode = @Keyword              THEN 0   -- 代號完全命中（打 2330）最優先
             WHEN CompanyCode LIKE @kw + N'%'         THEN 1   -- 代號前綴（打 23）
             WHEN CompanyName LIKE @kw + N'%'         THEN 2   -- 名稱前綴（打「台積」）
             WHEN CompanyCode LIKE N'%' + @kw + N'%'  THEN 3   -- 代號包含（打 33 → 2330）
             ELSE 4 END,                                       -- 名稱包含（打「積」）
        CompanyCode;
END
GO

/* -------- 每日行情寫入：依 (代號+交易日) upsert，匯入可重跑 -------- */
IF OBJECT_ID(N'dbo.usp_DailyQuote_Upsert', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DailyQuote_Upsert;
GO
CREATE PROCEDURE dbo.usp_DailyQuote_Upsert
    @CompanyCode NVARCHAR(10),
    @TradeDate   INT,
    @CompanyName NVARCHAR(60)  = NULL,
    @OpenPrice   DECIMAL(18,4) = NULL,
    @HighPrice   DECIMAL(18,4) = NULL,
    @LowPrice    DECIMAL(18,4) = NULL,
    @ClosePrice  DECIMAL(18,4) = NULL,
    @Change      DECIMAL(18,4) = NULL,
    @TradeVolume BIGINT        = NULL
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.DailyQuote WITH (HOLDLOCK) AS tgt
    USING (SELECT @CompanyCode AS CompanyCode, @TradeDate AS TradeDate) AS src
       ON  tgt.CompanyCode = src.CompanyCode
       AND tgt.TradeDate   = src.TradeDate
    WHEN MATCHED THEN UPDATE SET
        CompanyName = @CompanyName, OpenPrice = @OpenPrice, HighPrice = @HighPrice,
        LowPrice = @LowPrice, ClosePrice = @ClosePrice, Change = @Change,
        TradeVolume = @TradeVolume, UpdatedAt = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT
        (CompanyCode, TradeDate, CompanyName, OpenPrice, HighPrice, LowPrice, ClosePrice, Change, TradeVolume)
        VALUES
        (@CompanyCode, @TradeDate, @CompanyName, @OpenPrice, @HighPrice, @LowPrice, @ClosePrice, @Change, @TradeVolume);
END
GO

/* -------- 買賣投報排行 --------
   以「每日每元當日報酬率 = 漲跌 ÷ 昨收(=收盤-漲跌)」為基礎，
   在已餵入的近期間（一個月每日）彙總每檔的：
     PeriodReturnPct  期間累計報酬率 = (期末收盤 − 期初收盤) ÷ 期初收盤
     VolatilityPct    日報酬波動度（標準差）＝「變量」
     AvgDailyRetPct   平均日報酬
     LastDayRetPct    最近一日每元報酬
   依 @Sort 排序（預設累計報酬率，變量最大則選 volatility），全程參數化、LIKE 轉義。
   篩選：@Keyword（代號/名稱包含）、@Codes（逗號分隔代碼清單，可空）。 */
IF OBJECT_ID(N'dbo.usp_Quote_Ranking', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_Quote_Ranking;
GO
CREATE PROCEDURE dbo.usp_Quote_Ranking
    @Keyword  NVARCHAR(100) = NULL,
    @Codes    NVARCHAR(MAX) = NULL,   -- 逗號分隔代碼清單；NULL/空 = 不限
    @Sort     NVARCHAR(20)  = NULL,   -- return | volatility | avg | daily；NULL = return
    @Top      INT           = 30,
    @MaxPrice DECIMAL(18,4) = NULL    -- 小資可負擔：最近收盤每股價上限；NULL = 不限
AS
BEGIN
    SET NOCOUNT ON;
    IF @Top IS NULL OR @Top < 1 SET @Top = 30;
    IF @Top > 200 SET @Top = 200;

    DECLARE @kw NVARCHAR(200) =
        REPLACE(REPLACE(REPLACE(ISNULL(@Keyword, N''), N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]');
    DECLARE @hasKw BIT = CASE WHEN LTRIM(RTRIM(ISNULL(@Keyword, N''))) = N'' THEN 0 ELSE 1 END;
    DECLARE @hasCodes BIT = CASE WHEN LTRIM(RTRIM(ISNULL(@Codes, N''))) = N'' THEN 0 ELSE 1 END;

    ;WITH q AS (
        SELECT CompanyCode, CompanyName, TradeDate, ClosePrice, Change,
               CASE WHEN (ClosePrice - Change) <> 0 THEN Change / (ClosePrice - Change) * 100 END AS DailyRetPct,
               ROW_NUMBER() OVER (PARTITION BY CompanyCode ORDER BY TradeDate ASC)  AS rnFirst,
               ROW_NUMBER() OVER (PARTITION BY CompanyCode ORDER BY TradeDate DESC) AS rnLast
        FROM   dbo.DailyQuote
    ),
    agg AS (
        SELECT CompanyCode,
               MAX(CompanyName) AS CompanyName,
               COUNT(*)         AS Days,
               -- 只有第一/最後一筆的 rn=1，MIN 會略過 NULL 取到該值
               MIN(CASE WHEN rnFirst = 1 THEN ClosePrice END) AS FirstClose,
               MIN(CASE WHEN rnLast  = 1 THEN ClosePrice END) AS LastClose,
               MIN(CASE WHEN rnLast  = 1 THEN TradeDate  END) AS LastDate,
               MIN(CASE WHEN rnLast  = 1 THEN DailyRetPct END) AS LastDayRetPct,
               AVG(DailyRetPct)   AS AvgDailyRetPct,
               STDEV(DailyRetPct) AS VolatilityPct
        FROM   q
        GROUP BY CompanyCode
    )
    SELECT TOP (@Top)
           CompanyCode, CompanyName, Days, FirstClose, LastClose, LastDate,
           CAST(CASE WHEN FirstClose <> 0 THEN (LastClose - FirstClose) / FirstClose * 100 END AS DECIMAL(18,6)) AS PeriodReturnPct,
           CAST(AvgDailyRetPct   AS DECIMAL(18,6)) AS AvgDailyRetPct,
           CAST(VolatilityPct    AS DECIMAL(18,6)) AS VolatilityPct,   -- STDEV 回傳 float，轉 decimal 與其他欄位一致
           CAST(LastDayRetPct    AS DECIMAL(18,6)) AS LastDayRetPct
    FROM   agg
    WHERE  (@hasKw = 0 OR CompanyCode LIKE N'%' + @kw + N'%' OR CompanyName LIKE N'%' + @kw + N'%')
      AND  (@hasCodes = 0 OR CompanyCode IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@Codes, N',')))
      AND  (@MaxPrice IS NULL OR LastClose <= @MaxPrice)   -- 小資可負擔：濾掉每股價超過上限者
    ORDER BY
        CASE @Sort
            WHEN 'volatility' THEN VolatilityPct
            WHEN 'avg'        THEN AvgDailyRetPct
            WHEN 'daily'      THEN LastDayRetPct
            ELSE CASE WHEN FirstClose <> 0 THEN (LastClose - FirstClose) / FirstClose * 100 END  -- return（預設）
        END DESC,
        CompanyCode;
END
GO

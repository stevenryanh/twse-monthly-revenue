/* ============================================================
   預存程序 — 全程參數化，杜絕 SQL Injection
     1) usp_MonthlyRevenue_Upsert            寫入（依主鍵 upsert，匯入可重跑）
     2) usp_MonthlyRevenue_GetByCompanyCode  以公司代號查詢
     3) usp_Company_Search                   關鍵字搜尋公司（自動完成）
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

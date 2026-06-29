/* ============================================================
   預存程序 — 全程參數化，杜絕 SQL Injection
     1) usp_MonthlyRevenue_Upsert            寫入（依主鍵 upsert，匯入可重跑）
     2) usp_MonthlyRevenue_GetByCompanyCode  以公司代號查詢
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

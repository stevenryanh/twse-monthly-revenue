/* ============================================================
   上市公司每月營業收入彙總表 — 資料庫結構 (MSSQL)
   資料來源：政府資料開放平臺 / 臺灣證券交易所 OpenAPI t187ap05_L
   設計重點：
     1. 複合主鍵 (公司代號 + 資料年月)：一間公司每月恰一筆，為天然唯一鍵。
     2. 欄位完全對標來源 14 欄；民國年月日以整數保真儲存，不在入庫時失真。
     3. 僅透過參數化預存程序存取，從資料層杜絕 SQL Injection。
   ============================================================ */
IF DB_ID(N'TwseRevenue') IS NULL
    EXEC(N'CREATE DATABASE TwseRevenue;');
GO
USE TwseRevenue;
GO

IF OBJECT_ID(N'dbo.MonthlyRevenue', N'U') IS NOT NULL
    DROP TABLE dbo.MonthlyRevenue;
GO

CREATE TABLE dbo.MonthlyRevenue
(
    CompanyCode            NVARCHAR(10)   NOT NULL,  -- 公司代號（部分代號含英文，故用字串）
    DataYearMonth          INT            NOT NULL,  -- 資料年月（民國 YYYMM，例 11505）
    ReportDate             INT            NOT NULL,  -- 出表日期（民國 YYYMMDD，例 1150617）
    CompanyName            NVARCHAR(60)   NOT NULL,  -- 公司名稱
    Industry               NVARCHAR(60)   NULL,      -- 產業別
    CurrentMonthRevenue    BIGINT         NULL,      -- 營業收入-當月營收（仟元）
    LastMonthRevenue       BIGINT         NULL,      -- 營業收入-上月營收
    LastYearMonthRevenue   BIGINT         NULL,      -- 營業收入-去年當月營收
    MoMPercent             DECIMAL(18,6)  NULL,      -- 營業收入-上月比較增減(%)
    YoYPercent             DECIMAL(18,6)  NULL,      -- 營業收入-去年同月增減(%)
    CumCurrentRevenue      BIGINT         NULL,      -- 累計營業收入-當月累計營收
    CumLastYearRevenue     BIGINT         NULL,      -- 累計營業收入-去年累計營收
    CumDiffPercent         DECIMAL(18,6)  NULL,      -- 累計營業收入-前期比較增減(%)
    Remark                 NVARCHAR(200)  NULL,      -- 備註
    CreatedAt              DATETIME2(0)   NOT NULL CONSTRAINT DF_MonthlyRevenue_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt              DATETIME2(0)   NOT NULL CONSTRAINT DF_MonthlyRevenue_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_MonthlyRevenue PRIMARY KEY CLUSTERED (CompanyCode, DataYearMonth)
);
GO

/* 次要索引：主鍵叢集索引已最佳化「以公司代號查詢」；
   此非叢集索引服務「以資料年月查整月所有公司」的另一條查詢路徑，
   並以 INCLUDE 形成覆蓋索引，避免回表。 */
CREATE NONCLUSTERED INDEX IX_MonthlyRevenue_YearMonth
    ON dbo.MonthlyRevenue (DataYearMonth)
    INCLUDE (CompanyName, Industry, CurrentMonthRevenue);
GO

/* ============================================================
   每日收盤行情（買賣股票投報用）
   來源：證交所 STOCK_DAY_ALL（當日全市場）/ STOCK_DAY（單檔月歷史）。
   用途：以「每元當日報酬（漲跌÷昨收）」在近期間（一個月）衡量買賣投報與其變量。
   主鍵 (代號 + 交易日)：一檔每日恰一筆；交易日以民國 YYYMMDD 整數保真儲存。
   ============================================================ */
IF OBJECT_ID(N'dbo.DailyQuote', N'U') IS NOT NULL
    DROP TABLE dbo.DailyQuote;
GO

CREATE TABLE dbo.DailyQuote
(
    CompanyCode  NVARCHAR(10)   NOT NULL,  -- 證券代號
    TradeDate    INT            NOT NULL,  -- 交易日（民國 YYYMMDD，例 1150629）
    CompanyName  NVARCHAR(60)   NULL,      -- 證券名稱（行情來源附帶）
    OpenPrice    DECIMAL(18,4)  NULL,      -- 開盤價
    HighPrice    DECIMAL(18,4)  NULL,      -- 最高價
    LowPrice     DECIMAL(18,4)  NULL,      -- 最低價
    ClosePrice   DECIMAL(18,4)  NULL,      -- 收盤價
    Change       DECIMAL(18,4)  NULL,      -- 漲跌價差（相對昨收，帶正負）
    TradeVolume  BIGINT         NULL,      -- 成交股數
    CreatedAt    DATETIME2(0)   NOT NULL CONSTRAINT DF_DailyQuote_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt    DATETIME2(0)   NOT NULL CONSTRAINT DF_DailyQuote_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_DailyQuote PRIMARY KEY CLUSTERED (CompanyCode, TradeDate)
);
GO

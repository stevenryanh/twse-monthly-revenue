/* ============================================================
   DB 層 smoke test：驗證預存程序、冪等性、查詢結果
   （以台泥 1101 / 11505 真實樣本資料）
   ============================================================ */
USE TwseRevenue;
GO

PRINT N'--- 1) 第一次寫入 ---';
EXEC dbo.usp_MonthlyRevenue_Upsert
     @CompanyCode=N'1101', @DataYearMonth=11505, @ReportDate=1150617,
     @CompanyName=N'台泥', @Industry=N'水泥工業',
     @CurrentMonthRevenue=12612013, @LastMonthRevenue=12213195, @LastYearMonthRevenue=12619495,
     @MoMPercent=3.265468, @YoYPercent=-0.059289,
     @CumCurrentRevenue=58084626, @CumLastYearRevenue=60273039, @CumDiffPercent=-3.630832,
     @Remark=N'-';
GO

PRINT N'--- 2) 同主鍵再寫入一次（當月營收改 99999999），驗證冪等：不應變兩筆 ---';
EXEC dbo.usp_MonthlyRevenue_Upsert
     @CompanyCode=N'1101', @DataYearMonth=11505, @ReportDate=1150617,
     @CompanyName=N'台泥', @Industry=N'水泥工業',
     @CurrentMonthRevenue=99999999, @LastMonthRevenue=12213195, @LastYearMonthRevenue=12619495,
     @MoMPercent=3.265468, @YoYPercent=-0.059289,
     @CumCurrentRevenue=58084626, @CumLastYearRevenue=60273039, @CumDiffPercent=-3.630832,
     @Remark=N'更新測試';
GO

PRINT N'--- 3) 以公司代號查詢 ---';
EXEC dbo.usp_MonthlyRevenue_GetByCompanyCode @CompanyCode=N'1101';
GO

PRINT N'--- 4) 總筆數（冪等成功應為 1） ---';
SELECT COUNT(*) AS 總筆數 FROM dbo.MonthlyRevenue;
GO

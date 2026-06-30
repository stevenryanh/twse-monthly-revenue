using TwseRevenue.Application.Contracts;
using TwseRevenue.Domain.Entities;

namespace TwseRevenue.Application.Mapping;

/// <summary>
/// Entity ↔ DTO 的手寫映射（依團隊家規，不引入 AutoMapper）。
/// 欄位數固定、轉換直白，手寫比反射映射更可讀、可除錯、零額外相依。
/// </summary>
public static class RevenueMapping
{
    public static MonthlyRevenueDto ToDto(this MonthlyRevenue e) => new(
        e.CompanyCode, e.DataYearMonth, e.ReportDate, e.CompanyName, e.Industry,
        e.CurrentMonthRevenue, e.LastMonthRevenue, e.LastYearMonthRevenue,
        e.MoMPercent, e.YoYPercent,
        e.CumCurrentRevenue, e.CumLastYearRevenue, e.CumDiffPercent, e.Remark);

    public static CompanySummaryDto ToDto(this CompanySummary e) =>
        new(e.CompanyCode, e.CompanyName, e.Industry);

    public static MonthlyRevenue ToEntity(this CreateRevenueRequest r) => new()
    {
        CompanyCode = r.CompanyCode,
        DataYearMonth = r.DataYearMonth,
        ReportDate = r.ReportDate,
        CompanyName = r.CompanyName,
        Industry = r.Industry,
        CurrentMonthRevenue = r.CurrentMonthRevenue,
        LastMonthRevenue = r.LastMonthRevenue,
        LastYearMonthRevenue = r.LastYearMonthRevenue,
        MoMPercent = r.MoMPercent,
        YoYPercent = r.YoYPercent,
        CumCurrentRevenue = r.CumCurrentRevenue,
        CumLastYearRevenue = r.CumLastYearRevenue,
        CumDiffPercent = r.CumDiffPercent,
        Remark = r.Remark,
    };
}

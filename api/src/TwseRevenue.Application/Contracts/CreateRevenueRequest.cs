namespace TwseRevenue.Application.Contracts;

/// <summary>新增營收資料的輸入契約。</summary>
public sealed record CreateRevenueRequest(
    string CompanyCode,
    int DataYearMonth,
    int ReportDate,
    string CompanyName,
    string? Industry,
    long? CurrentMonthRevenue,
    long? LastMonthRevenue,
    long? LastYearMonthRevenue,
    decimal? MoMPercent,
    decimal? YoYPercent,
    long? CumCurrentRevenue,
    long? CumLastYearRevenue,
    decimal? CumDiffPercent,
    string? Remark);

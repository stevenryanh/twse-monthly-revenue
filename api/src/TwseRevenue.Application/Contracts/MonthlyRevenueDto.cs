namespace TwseRevenue.Application.Contracts;

/// <summary>對外輸出契約（查詢結果）。與 Entity 解耦，避免資料層欄位外洩。</summary>
public sealed record MonthlyRevenueDto(
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

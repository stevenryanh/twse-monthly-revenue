namespace TwseRevenue.Application.Contracts;

/// <summary>買賣投報排行結果（對外輸出契約）。報酬基礎：每元當日報酬率 = 漲跌 ÷ 昨收。</summary>
public sealed record QuoteRankingDto(
    string CompanyCode,
    string? CompanyName,
    int Days,
    decimal? FirstClose,
    decimal? LastClose,
    int? LastDate,
    decimal? PeriodReturnPercent,
    decimal? AvgDailyReturnPercent,
    decimal? VolatilityPercent,
    decimal? LastDayReturnPercent);

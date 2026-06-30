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
    decimal? LastDayReturnPercent,
    decimal? RiskAdjustedReturn,
    decimal? CycleDays = null,            // 波段平均週期（天）；僅 sort=swingscore 時計算
    decimal? PricePositionPercent = null, // 目前價在區間位置（0=近低、100=近高）
    decimal? SwingScore = null);          // 易入手波段分 =（報酬÷波動）÷週期×離低點程度；越高越好

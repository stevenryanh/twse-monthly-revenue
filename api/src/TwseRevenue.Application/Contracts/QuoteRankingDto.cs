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
    string? NextTurnKind = null,          // 下一轉折推估：trough（即將見底）/ peak（即將見頂）
    int? EstDaysToNextTurn = null,        // 推估到下一轉折還有幾個交易日
    string? EntryTiming = null,           // 進場時機標籤：即將見底 / 偏高·觀望 / 還會跌·觀望 / 即將見頂·宜收手 / 普通
    decimal? SwingScore = null);          // 易入手波段分；越高越好（含即將見底前瞻權重）

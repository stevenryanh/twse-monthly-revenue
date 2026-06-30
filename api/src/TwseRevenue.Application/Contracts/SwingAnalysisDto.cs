namespace TwseRevenue.Application.Contracts;

/// <summary>序列上的一個交易日點（供前端畫線）。</summary>
public sealed record SwingPointDto(int TradeDate, decimal? Close);

/// <summary>偵測到的波段轉折點。Kind：peak（波峰）/ trough（波谷）。</summary>
public sealed record SwingMarkerDto(int TradeDate, decimal? Close, string Kind);

/// <summary>
/// 個股波段分析（啟發式，僅供參考、非投資建議）。
/// 以近期每日收盤偵測波峰/波谷，量出平均週期與振幅，據此推估下一轉折的時間與價位。
/// </summary>
public sealed record SwingAnalysisDto(
    string CompanyCode,
    string? CompanyName,
    int Days,                              // 序列天數
    IReadOnlyList<SwingPointDto> Points,   // 每日收盤序列（由舊到新）
    IReadOnlyList<SwingMarkerDto> Swings,  // 偵測到的波峰/波谷
    decimal? LastClose,                    // 最新收盤
    decimal? RecentHigh,                   // 區間高
    decimal? RecentLow,                    // 區間低
    decimal? PricePositionPercent,         // 目前價在區間位置（0=近低、100=近高）
    decimal? Ma5,                          // 5 日均
    decimal? Ma20,                         // 20 日均
    decimal? Rsi14,                        // 14 日 RSI
    decimal? AvgCycleDays,                 // 平均波段週期（峰到峰，交易日）
    decimal? AvgAmplitudePercent,          // 平均波段振幅（%）
    string? NextTurnKind,                  // 下一轉折推估：peak / trough / null
    int? EstDaysToNextTurn,                // 推估還有幾個交易日到下一轉折
    decimal? EstTargetLow,                 // 推估目標價區間（低）
    decimal? EstTargetHigh,                // 推估目標價區間（高）
    string Note);                          // 說明與免責

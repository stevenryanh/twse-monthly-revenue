namespace TwseRevenue.Domain.Entities;

/// <summary>
/// 買賣投報排行的單檔彙總（近期間每日行情算出）。
/// 報酬以「每元當日報酬率」為基礎：漲跌 ÷ 昨收。
/// </summary>
public sealed class QuoteRanking
{
    public string CompanyCode { get; init; } = default!;   // 證券代號
    public string? CompanyName { get; init; }              // 證券名稱
    public int Days { get; init; }                         // 期間天數（已餵入的交易日數）
    public decimal? FirstClose { get; init; }              // 期初收盤
    public decimal? LastClose { get; init; }               // 期末收盤
    public int? LastDate { get; init; }                    // 最近交易日（民國 YYYMMDD）
    public decimal? PeriodReturnPercent { get; init; }     // 期間累計報酬率（%）
    public decimal? AvgDailyReturnPercent { get; init; }   // 平均日報酬（%）
    public decimal? VolatilityPercent { get; init; }       // 日報酬波動度＝變量（標準差，%）
    public decimal? LastDayReturnPercent { get; init; }    // 最近一日每元報酬（%）
    public decimal? RiskAdjustedReturn { get; init; }      // 報酬/風險比（期間報酬÷波動，類 Sharpe）
}

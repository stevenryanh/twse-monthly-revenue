namespace TwseRevenue.Domain.Entities;

/// <summary>
/// 每日收盤行情（買賣股票投報用）。交易日以民國 YYYMMDD 整數保真；價格為元。
/// </summary>
public sealed class DailyQuote
{
    public string CompanyCode { get; init; } = default!;   // 證券代號
    public int TradeDate { get; init; }                    // 交易日（民國 YYYMMDD）
    public string? CompanyName { get; init; }              // 證券名稱
    public decimal? OpenPrice { get; init; }               // 開盤價
    public decimal? HighPrice { get; init; }               // 最高價
    public decimal? LowPrice { get; init; }                // 最低價
    public decimal? ClosePrice { get; init; }              // 收盤價
    public decimal? Change { get; init; }                  // 漲跌價差（相對昨收，帶正負）
    public long? TradeVolume { get; init; }                // 成交股數
}

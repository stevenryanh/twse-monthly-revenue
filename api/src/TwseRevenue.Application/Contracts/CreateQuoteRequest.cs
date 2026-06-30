namespace TwseRevenue.Application.Contracts;

/// <summary>寫入每日行情的輸入契約（依主鍵 upsert，匯入可重跑）。</summary>
public sealed record CreateQuoteRequest(
    string CompanyCode,
    int TradeDate,
    string? CompanyName,
    decimal? OpenPrice,
    decimal? HighPrice,
    decimal? LowPrice,
    decimal? ClosePrice,
    decimal? Change,
    long? TradeVolume);

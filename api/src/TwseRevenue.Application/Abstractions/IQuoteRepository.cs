using TwseRevenue.Domain.Entities;

namespace TwseRevenue.Application.Abstractions;

/// <summary>
/// 每日行情資料存取介面。實作於 Infrastructure 層（透過參數化預存程序存取）。
/// </summary>
public interface IQuoteRepository
{
    /// <summary>寫入單日行情（依主鍵 upsert，匯入可重跑）。</summary>
    Task UpsertAsync(DailyQuote quote, CancellationToken ct);

    /// <summary>
    /// 買賣投報排行：以近期間每日行情彙總每檔報酬指標並排序。
    /// keyword（代號/名稱包含）、codes（逗號分隔代碼清單）皆可選；sort 指排序指標。
    /// </summary>
    Task<IReadOnlyList<QuoteRanking>> RankAsync(
        string? keyword, string? codes, string? sort, int top, decimal? maxPrice, string? dir, CancellationToken ct);
}

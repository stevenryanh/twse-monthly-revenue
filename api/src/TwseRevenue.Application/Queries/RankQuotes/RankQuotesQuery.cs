using MediatR;
using TwseRevenue.Application.Contracts;

namespace TwseRevenue.Application.Queries.RankQuotes;

/// <summary>
/// 買賣投報排行：把（條件中的）股票依每元當日報酬在近期間的指標排序。
/// Keyword/Codes/MaxPrice 為篩選（MaxPrice＝小資可負擔的每股價上限）；
/// Sort：return（累計報酬，預設）| volatility（變量）| avg | daily。
/// </summary>
public sealed record RankQuotesQuery(string? Keyword, string? Codes, string? Sort, int Top, decimal? MaxPrice, string? Dir)
    : IRequest<IReadOnlyList<QuoteRankingDto>>;

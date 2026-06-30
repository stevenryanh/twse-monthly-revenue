using MediatR;
using Microsoft.Extensions.Logging;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Analysis;
using TwseRevenue.Application.Contracts;
using TwseRevenue.Application.Logging;
using TwseRevenue.Application.Mapping;

namespace TwseRevenue.Application.Queries.RankQuotes;

// 驗證已上移至 RankQuotesValidator（由 ValidationBehavior 在此 handler 前執行）。
public sealed class RankQuotesHandler
    : IRequestHandler<RankQuotesQuery, IReadOnlyList<QuoteRankingDto>>
{
    private const int DefaultTop = 30;
    private const int MaxTop = 200;
    private const int SwingPool = 120;   // 易入手波段分：先取較大候選池再逐檔算週期

    private readonly IQuoteRepository _repository;
    private readonly ILogger<RankQuotesHandler> _logger;

    public RankQuotesHandler(IQuoteRepository repository, ILogger<RankQuotesHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<QuoteRankingDto>> Handle(
        RankQuotesQuery request, CancellationToken cancellationToken)
    {
        var top = request.Top <= 0 ? DefaultTop : Math.Min(request.Top, MaxTop);

        if (string.Equals(request.Sort, "swingscore", StringComparison.OrdinalIgnoreCase))
            return await RankBySwingScoreAsync(request, top, cancellationToken);

        var rows = await _repository.RankAsync(
            request.Keyword, request.Codes, request.Sort, top, request.MaxPrice, request.Dir, cancellationToken);

        _logger.LogInformation(TwseLogCodes.Quote.Ranked +
            " 買賣投報排行完成 - Sort={Sort}, Keyword={Keyword}, Count={Count}",
            request.Sort, request.Keyword, rows.Count);

        return rows.Select(r => r.ToDto()).ToList();
    }

    /// <summary>
    /// 易入手波段分 =（報酬÷波動）÷ 波段週期 ×（離低點程度）。
    /// 同時偏好「高報酬、低風險、短週期、目前接近低檔」——又快、又穩、又划算、進場點佳。
    /// 屬歷史統計外推,僅供參考、非投資建議。
    /// </summary>
    private async Task<IReadOnlyList<QuoteRankingDto>> RankBySwingScoreAsync(
        RankQuotesQuery request, int top, CancellationToken ct)
    {
        // 先取（總預算內的）候選池，含報酬/風險比，再逐檔以每日序列算波段週期與區間位置。
        var pool = await _repository.RankAsync(
            request.Keyword, request.Codes, "return", SwingPool, request.MaxPrice, "desc", ct);
        var codes = pool.Select(p => p.CompanyCode).ToList();
        var seriesByCode = await _repository.GetSeriesForCodesAsync(codes, ct);

        var scored = new List<QuoteRankingDto>();
        foreach (var p in pool)
        {
            decimal? cycle = null, pos = null, score = null;
            string? nextKind = null, entryLabel = null;
            int? estDays = null;
            if (seriesByCode.TryGetValue(p.CompanyCode, out var series))
            {
                var a = SwingAnalyzer.Analyze(p.CompanyCode, series);
                cycle = a.AvgCycleDays;
                pos = a.PricePositionPercent;
                nextKind = a.NextTurnKind;
                estDays = a.EstDaysToNextTurn;
                entryLabel = SwingAnalyzer.EntryTimingLabel(a);

                // 易入手波段分 =（報酬÷波動）÷週期 × 進場時機權重（離低點×即將見底×下檔有限）
                if (p.RiskAdjustedReturn is > 0 && cycle is > 0)
                {
                    var entry = SwingAnalyzer.EntryTimingFactor(a);
                    score = Math.Round(p.RiskAdjustedReturn.Value / cycle.Value * entry, 4);
                }
            }
            scored.Add(p.ToDto() with
            {
                CycleDays = cycle,
                PricePositionPercent = pos,
                NextTurnKind = nextKind,
                EstDaysToNextTurn = estDays,
                EntryTiming = entryLabel,
                SwingScore = score,
            });
        }

        var ranked = scored
            .Where(d => d.SwingScore.HasValue)
            .OrderByDescending(d => d.SwingScore!.Value)
            .Take(top)
            .ToList();

        _logger.LogInformation(TwseLogCodes.Quote.Ranked +
            " 易入手波段分排行完成 - Pool={Pool}, Count={Count}", pool.Count, ranked.Count);

        return ranked;
    }
}

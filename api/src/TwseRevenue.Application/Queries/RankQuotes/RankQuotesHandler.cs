using MediatR;
using Microsoft.Extensions.Logging;
using TwseRevenue.Application.Abstractions;
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
        var rows = await _repository.RankAsync(
            request.Keyword, request.Codes, request.Sort, top, cancellationToken);

        _logger.LogInformation(TwseLogCodes.Quote.Ranked +
            " 買賣投報排行完成 - Sort={Sort}, Keyword={Keyword}, Count={Count}",
            request.Sort, request.Keyword, rows.Count);

        return rows.Select(r => r.ToDto()).ToList();
    }
}

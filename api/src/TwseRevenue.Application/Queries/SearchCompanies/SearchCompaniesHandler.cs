using MediatR;
using Microsoft.Extensions.Logging;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Contracts;
using TwseRevenue.Application.Logging;
using TwseRevenue.Application.Mapping;

namespace TwseRevenue.Application.Queries.SearchCompanies;

// 輸入驗證已上移至 SearchCompaniesValidator（由 ValidationBehavior 在此 handler 前執行）。
public sealed class SearchCompaniesHandler
    : IRequestHandler<SearchCompaniesQuery, IReadOnlyList<CompanySummaryDto>>
{
    private readonly IRevenueRepository _repository;
    private readonly ILogger<SearchCompaniesHandler> _logger;

    public SearchCompaniesHandler(IRevenueRepository repository, ILogger<SearchCompaniesHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CompanySummaryDto>> Handle(
        SearchCompaniesQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.SearchCompaniesAsync(request.Keyword, cancellationToken);

        _logger.LogInformation(TwseLogCodes.Revenue.CompaniesSearched +
            " 公司搜尋完成 - Keyword={Keyword}, Count={Count}",
            request.Keyword, rows.Count);

        return rows.Select(r => r.ToDto()).ToList();
    }
}

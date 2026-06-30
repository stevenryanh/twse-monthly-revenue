using MediatR;
using Microsoft.Extensions.Logging;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Contracts;
using TwseRevenue.Application.Logging;
using TwseRevenue.Application.Mapping;

namespace TwseRevenue.Application.Queries.GetRevenueByCompanyCode;

// 輸入驗證已上移至 GetRevenueByCompanyCodeValidator（由 ValidationBehavior 在此 handler 前執行）。
public sealed class GetRevenueByCompanyCodeHandler
    : IRequestHandler<GetRevenueByCompanyCodeQuery, IReadOnlyList<MonthlyRevenueDto>>
{
    private readonly IRevenueRepository _repository;
    private readonly ILogger<GetRevenueByCompanyCodeHandler> _logger;

    public GetRevenueByCompanyCodeHandler(IRevenueRepository repository, ILogger<GetRevenueByCompanyCodeHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MonthlyRevenueDto>> Handle(
        GetRevenueByCompanyCodeQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetByCompanyCodeAsync(request.CompanyCode, cancellationToken);

        _logger.LogInformation(TwseLogCodes.Revenue.Queried +
            " 營收查詢完成 - CompanyCode={CompanyCode}, Count={Count}",
            request.CompanyCode, rows.Count);

        return rows.Select(r => r.ToDto()).ToList();
    }
}

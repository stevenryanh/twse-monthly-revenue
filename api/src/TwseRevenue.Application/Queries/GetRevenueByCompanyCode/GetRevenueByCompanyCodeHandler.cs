using MediatR;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Contracts;
using TwseRevenue.Application.Errors;
using TwseRevenue.Application.Mapping;

namespace TwseRevenue.Application.Queries.GetRevenueByCompanyCode;

public sealed class GetRevenueByCompanyCodeHandler
    : IRequestHandler<GetRevenueByCompanyCodeQuery, IReadOnlyList<MonthlyRevenueDto>>
{
    private readonly IRevenueRepository _repository;

    public GetRevenueByCompanyCodeHandler(IRevenueRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<MonthlyRevenueDto>> Handle(
        GetRevenueByCompanyCodeQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyCode))
            throw new ValidationException("公司代號必填。");

        var rows = await _repository.GetByCompanyCodeAsync(request.CompanyCode, cancellationToken);
        return rows.Select(r => r.ToDto()).ToList();
    }
}

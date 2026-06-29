using MediatR;
using TwseRevenue.Application.Contracts;

namespace TwseRevenue.Application.Queries.GetRevenueByCompanyCode;

/// <summary>以公司代號查詢各月營收。</summary>
public sealed record GetRevenueByCompanyCodeQuery(string CompanyCode)
    : IRequest<IReadOnlyList<MonthlyRevenueDto>>;

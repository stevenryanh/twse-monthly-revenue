using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Errors;

namespace TwseRevenue.Application.Queries.GetRevenueByCompanyCode;

/// <summary>查詢輸入驗證。規則集中於此，由 ValidationBehavior 在 handler 前執行。</summary>
public sealed class GetRevenueByCompanyCodeValidator : IValidator<GetRevenueByCompanyCodeQuery>
{
    public void Validate(GetRevenueByCompanyCodeQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.CompanyCode))
            throw new ValidationException("公司代號必填。");
    }
}

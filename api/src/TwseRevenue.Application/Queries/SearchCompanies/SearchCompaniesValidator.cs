using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Errors;

namespace TwseRevenue.Application.Queries.SearchCompanies;

/// <summary>搜尋輸入驗證。規則集中於此，由 ValidationBehavior 在 handler 前執行。</summary>
public sealed class SearchCompaniesValidator : IValidator<SearchCompaniesQuery>
{
    public void Validate(SearchCompaniesQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Keyword))
            throw new ValidationException("搜尋關鍵字必填。");
        if (query.Keyword.Length > 100)
            throw new ValidationException("搜尋關鍵字長度不可超過 100。");
    }
}

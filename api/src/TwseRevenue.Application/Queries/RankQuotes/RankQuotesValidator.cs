using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Errors;

namespace TwseRevenue.Application.Queries.RankQuotes;

/// <summary>排行查詢驗證。由 ValidationBehavior 在 handler 前執行。</summary>
public sealed class RankQuotesValidator : IValidator<RankQuotesQuery>
{
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase) { "return", "volatility", "avg", "daily", "sharpe", "swingscore" };

    public void Validate(RankQuotesQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Sort) && !AllowedSorts.Contains(query.Sort))
            throw new ValidationException("排序方式不正確（可用：return、volatility、avg、daily）。");
        if (!string.IsNullOrEmpty(query.Keyword) && query.Keyword.Length > 100)
            throw new ValidationException("搜尋關鍵字長度不可超過 100。");
        if (query.MaxPrice is < 0)
            throw new ValidationException("可負擔每股價上限不可為負。");
        if (!string.IsNullOrWhiteSpace(query.Dir) &&
            !query.Dir.Equals("asc", StringComparison.OrdinalIgnoreCase) &&
            !query.Dir.Equals("desc", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("排序方向不正確（可用：asc、desc）。");
    }
}

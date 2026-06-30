using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Errors;

namespace TwseRevenue.Application.Queries.GetSwingAnalysis;

/// <summary>波段分析輸入驗證。由 ValidationBehavior 在 handler 前執行。</summary>
public sealed class GetSwingAnalysisValidator : IValidator<GetSwingAnalysisQuery>
{
    public void Validate(GetSwingAnalysisQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.CompanyCode))
            throw new ValidationException("證券代號必填。");
    }
}

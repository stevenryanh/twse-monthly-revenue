using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Errors;

namespace TwseRevenue.Application.Commands.CreateRevenue;

/// <summary>新增營收的輸入驗證。規則集中於此，由 ValidationBehavior 在 handler 前執行。</summary>
public sealed class CreateRevenueValidator : IValidator<CreateRevenueCommand>
{
    public void Validate(CreateRevenueCommand command)
    {
        var d = command.Data;

        if (string.IsNullOrWhiteSpace(d.CompanyCode) || d.CompanyCode.Length > 10)
            throw new ValidationException("公司代號必填，且長度不可超過 10。");
        if (string.IsNullOrWhiteSpace(d.CompanyName))
            throw new ValidationException("公司名稱必填。");
        if (d.DataYearMonth is < 10000 or > 99999)
            throw new ValidationException("資料年月格式不正確（應為民國 YYYMM，例 11505）。");
    }
}

using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Errors;

namespace TwseRevenue.Application.Commands.CreateQuote;

/// <summary>每日行情寫入驗證。由 ValidationBehavior 在 handler 前執行。</summary>
public sealed class CreateQuoteValidator : IValidator<CreateQuoteCommand>
{
    public void Validate(CreateQuoteCommand command)
    {
        var d = command.Data;

        if (string.IsNullOrWhiteSpace(d.CompanyCode) || d.CompanyCode.Length > 10)
            throw new ValidationException("證券代號必填，且長度不可超過 10。");
        if (d.TradeDate is < 1000101 or > 9991231)
            throw new ValidationException("交易日格式不正確（應為民國 YYYMMDD，例 1150629）。");
    }
}

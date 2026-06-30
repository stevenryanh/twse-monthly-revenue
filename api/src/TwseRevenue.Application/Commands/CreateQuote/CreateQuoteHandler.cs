using MediatR;
using Microsoft.Extensions.Logging;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Logging;
using TwseRevenue.Application.Mapping;

namespace TwseRevenue.Application.Commands.CreateQuote;

// 驗證已上移至 CreateQuoteValidator；handler 只把已驗證輸入映射為 Entity 後 upsert。
public sealed class CreateQuoteHandler : IRequestHandler<CreateQuoteCommand, Unit>
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<CreateQuoteHandler> _logger;

    public CreateQuoteHandler(IQuoteRepository repository, ILogger<CreateQuoteHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Unit> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        var entity = request.Data.ToEntity();
        await _repository.UpsertAsync(entity, cancellationToken);

        _logger.LogInformation(TwseLogCodes.Quote.Upserted +
            " 行情已寫入 - CompanyCode={CompanyCode}, TradeDate={TradeDate}",
            entity.CompanyCode, entity.TradeDate);

        return Unit.Value;
    }
}

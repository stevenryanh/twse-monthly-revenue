using MediatR;
using Microsoft.Extensions.Logging;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Logging;
using TwseRevenue.Application.Mapping;

namespace TwseRevenue.Application.Commands.CreateRevenue;

// 輸入驗證已上移至 CreateRevenueValidator（由 ValidationBehavior 在此 handler 前執行）。
// handler 只負責「協調」：把已驗證的輸入映射為 Entity 後寫入。
public sealed class CreateRevenueHandler : IRequestHandler<CreateRevenueCommand, Unit>
{
    private readonly IRevenueRepository _repository;
    private readonly ILogger<CreateRevenueHandler> _logger;

    public CreateRevenueHandler(IRevenueRepository repository, ILogger<CreateRevenueHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Unit> Handle(CreateRevenueCommand request, CancellationToken cancellationToken)
    {
        var entity = request.Data.ToEntity();
        await _repository.UpsertAsync(entity, cancellationToken);

        _logger.LogInformation(TwseLogCodes.Revenue.Upserted +
            " 營收已寫入 - CompanyCode={CompanyCode}, DataYearMonth={DataYearMonth}",
            entity.CompanyCode, entity.DataYearMonth);

        return Unit.Value;
    }
}

using MediatR;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Errors;
using TwseRevenue.Application.Mapping;

namespace TwseRevenue.Application.Commands.CreateRevenue;

public sealed class CreateRevenueHandler : IRequestHandler<CreateRevenueCommand, Unit>
{
    private readonly IRevenueRepository _repository;

    public CreateRevenueHandler(IRevenueRepository repository) => _repository = repository;

    public async Task<Unit> Handle(CreateRevenueCommand request, CancellationToken cancellationToken)
    {
        var d = request.Data;

        if (string.IsNullOrWhiteSpace(d.CompanyCode) || d.CompanyCode.Length > 10)
            throw new ValidationException("公司代號必填，且長度不可超過 10。");
        if (string.IsNullOrWhiteSpace(d.CompanyName))
            throw new ValidationException("公司名稱必填。");
        if (d.DataYearMonth is < 10000 or > 99999)
            throw new ValidationException("資料年月格式不正確（應為民國 YYYMM，例 11505）。");

        await _repository.UpsertAsync(d.ToEntity(), cancellationToken);
        return Unit.Value;
    }
}

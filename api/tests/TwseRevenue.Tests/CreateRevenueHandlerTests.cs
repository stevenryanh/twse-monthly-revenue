using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Commands.CreateRevenue;
using TwseRevenue.Application.Contracts;
using TwseRevenue.Domain.Entities;
using Xunit;

namespace TwseRevenue.Tests;

// 驗證已上移至 ValidationBehavior / CreateRevenueValidator（見 CreateRevenueValidatorTests），
// 故 handler 只負責「有效輸入 → 寫入」的協調。
public class CreateRevenueHandlerTests
{
    private static CreateRevenueRequest ValidRequest(string code = "2330") => new(
        code, 11505, 1150617, "台積電", "半導體業",
        263713000, 320000000, 210000000, -17.5m, 25.5m,
        1200000000, 900000000, 33.3m, "-");

    [Fact]
    public async Task 有效輸入_應呼叫Upsert一次()
    {
        var repo = new Mock<IRevenueRepository>();
        var handler = new CreateRevenueHandler(repo.Object, NullLogger<CreateRevenueHandler>.Instance);

        await handler.Handle(new CreateRevenueCommand(ValidRequest()), CancellationToken.None);

        repo.Verify(r => r.UpsertAsync(
            It.Is<MonthlyRevenue>(m => m.CompanyCode == "2330" && m.CompanyName == "台積電"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

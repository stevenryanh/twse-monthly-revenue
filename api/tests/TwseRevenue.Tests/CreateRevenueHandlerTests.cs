using Moq;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Commands.CreateRevenue;
using TwseRevenue.Application.Contracts;
using TwseRevenue.Application.Errors;
using TwseRevenue.Domain.Entities;
using Xunit;

namespace TwseRevenue.Tests;

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
        var handler = new CreateRevenueHandler(repo.Object);

        await handler.Handle(new CreateRevenueCommand(ValidRequest()), CancellationToken.None);

        repo.Verify(r => r.UpsertAsync(
            It.Is<MonthlyRevenue>(m => m.CompanyCode == "2330" && m.CompanyName == "台積電"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678901")] // 11 碼，超過 10
    public async Task 公司代號不合法_應拋驗證例外且不寫入(string code)
    {
        var repo = new Mock<IRevenueRepository>();
        var handler = new CreateRevenueHandler(repo.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateRevenueCommand(ValidRequest(code)), CancellationToken.None));

        repo.Verify(r => r.UpsertAsync(It.IsAny<MonthlyRevenue>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(9999)]    // < 10000
    [InlineData(100000)]  // > 99999
    public async Task 資料年月超出民國YYYMM範圍_應拋驗證例外(int yearMonth)
    {
        var repo = new Mock<IRevenueRepository>();
        var handler = new CreateRevenueHandler(repo.Object);
        var request = ValidRequest() with { DataYearMonth = yearMonth };

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateRevenueCommand(request), CancellationToken.None));
    }
}

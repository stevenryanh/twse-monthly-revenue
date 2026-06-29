using Moq;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Errors;
using TwseRevenue.Application.Queries.GetRevenueByCompanyCode;
using TwseRevenue.Domain.Entities;
using Xunit;

namespace TwseRevenue.Tests;

public class GetRevenueByCompanyCodeHandlerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task 公司代號空白_應拋驗證例外(string code)
    {
        var repo = new Mock<IRevenueRepository>();
        var handler = new GetRevenueByCompanyCodeHandler(repo.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new GetRevenueByCompanyCodeQuery(code), CancellationToken.None));
    }

    [Fact]
    public async Task 有資料_應映射為DTO並保留關鍵欄位()
    {
        var entity = new MonthlyRevenue
        {
            CompanyCode = "1101",
            DataYearMonth = 11505,
            ReportDate = 1150617,
            CompanyName = "台泥",
            Industry = "水泥工業",
            CurrentMonthRevenue = 12612013,
            MoMPercent = 3.265468m,
            Remark = "-",
        };
        var repo = new Mock<IRevenueRepository>();
        repo.Setup(r => r.GetByCompanyCodeAsync("1101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { entity });
        var handler = new GetRevenueByCompanyCodeHandler(repo.Object);

        var result = await handler.Handle(new GetRevenueByCompanyCodeQuery("1101"), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal("1101", dto.CompanyCode);
        Assert.Equal("台泥", dto.CompanyName);
        Assert.Equal(12612013, dto.CurrentMonthRevenue);
        Assert.Equal(3.265468m, dto.MoMPercent);
    }
}

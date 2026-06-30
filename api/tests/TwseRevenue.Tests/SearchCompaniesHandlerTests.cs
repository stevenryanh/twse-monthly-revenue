using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Queries.SearchCompanies;
using TwseRevenue.Domain.Entities;
using Xunit;

namespace TwseRevenue.Tests;

// 關鍵字必填的驗證已上移至 SearchCompaniesValidator（見對應 ValidatorTests）。
public class SearchCompaniesHandlerTests
{
    [Fact]
    public async Task 有相符公司_應映射為DTO並保留代號名稱產業()
    {
        var entity = new CompanySummary { CompanyCode = "2330", CompanyName = "台積電", Industry = "半導體業" };
        var repo = new Mock<IRevenueRepository>();
        repo.Setup(r => r.SearchCompaniesAsync("台積", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { entity });
        var handler = new SearchCompaniesHandler(repo.Object, NullLogger<SearchCompaniesHandler>.Instance);

        var result = await handler.Handle(new SearchCompaniesQuery("台積"), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal("2330", dto.CompanyCode);
        Assert.Equal("台積電", dto.CompanyName);
        Assert.Equal("半導體業", dto.Industry);
    }

    [Fact]
    public async Task 無相符_應回空清單()
    {
        var repo = new Mock<IRevenueRepository>();
        repo.Setup(r => r.SearchCompaniesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CompanySummary>());
        var handler = new SearchCompaniesHandler(repo.Object, NullLogger<SearchCompaniesHandler>.Instance);

        var result = await handler.Handle(new SearchCompaniesQuery("不存在"), CancellationToken.None);

        Assert.Empty(result);
    }
}

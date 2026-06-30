using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Queries.RankQuotes;
using TwseRevenue.Domain.Entities;
using Xunit;

namespace TwseRevenue.Tests;

public class RankQuotesHandlerTests
{
    [Fact]
    public async Task 有結果_應映射為DTO並保留報酬指標()
    {
        var ranking = new QuoteRanking
        {
            CompanyCode = "2330",
            CompanyName = "台積電",
            Days = 21,
            FirstClose = 1000m,
            LastClose = 1100m,
            LastDate = 1150630,
            PeriodReturnPercent = 10m,
            AvgDailyReturnPercent = 0.5m,
            VolatilityPercent = 2.3m,
            LastDayReturnPercent = 1.7m,
        };
        var repo = new Mock<IQuoteRepository>();
        repo.Setup(r => r.RankAsync(null, null, "return", It.IsAny<int>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ranking });
        var handler = new RankQuotesHandler(repo.Object, NullLogger<RankQuotesHandler>.Instance);

        var result = await handler.Handle(new RankQuotesQuery(null, null, "return", 30, null), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal("2330", dto.CompanyCode);
        Assert.Equal(10m, dto.PeriodReturnPercent);
        Assert.Equal(2.3m, dto.VolatilityPercent);
    }

    [Fact]
    public async Task Top非正_應夾為預設30()
    {
        var repo = new Mock<IQuoteRepository>();
        repo.Setup(r => r.RankAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<QuoteRanking>());
        var handler = new RankQuotesHandler(repo.Object, NullLogger<RankQuotesHandler>.Instance);

        await handler.Handle(new RankQuotesQuery(null, null, null, 0, null), CancellationToken.None);

        repo.Verify(r => r.RankAsync(null, null, null, 30, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}

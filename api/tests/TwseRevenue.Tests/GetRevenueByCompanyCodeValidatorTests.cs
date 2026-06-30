using TwseRevenue.Application.Errors;
using TwseRevenue.Application.Queries.GetRevenueByCompanyCode;
using Xunit;

namespace TwseRevenue.Tests;

public class GetRevenueByCompanyCodeValidatorTests
{
    private static readonly GetRevenueByCompanyCodeValidator Sut = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void 公司代號空白_應拋驗證例外(string code)
    {
        Assert.Throws<ValidationException>(() => Sut.Validate(new GetRevenueByCompanyCodeQuery(code)));
    }

    [Fact]
    public void 有效代號_不應拋例外()
    {
        Sut.Validate(new GetRevenueByCompanyCodeQuery("2330"));
    }
}

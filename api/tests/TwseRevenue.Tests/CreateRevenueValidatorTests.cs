using TwseRevenue.Application.Commands.CreateRevenue;
using TwseRevenue.Application.Contracts;
using TwseRevenue.Application.Errors;
using Xunit;

namespace TwseRevenue.Tests;

// 驗證規則的單元測試（規則所在的單一接縫）。實際請求中由 ValidationBehavior 在 handler 前觸發，
// 故「驗證失敗即不寫入」由 pipeline 順序保證（驗證丟例外 → handler 不會被呼叫）。
public class CreateRevenueValidatorTests
{
    private static readonly CreateRevenueValidator Sut = new();

    private static CreateRevenueRequest ValidRequest(string code = "2330") => new(
        code, 11505, 1150617, "台積電", "半導體業",
        263713000, 320000000, 210000000, -17.5m, 25.5m,
        1200000000, 900000000, 33.3m, "-");

    private static void Act(CreateRevenueRequest req) => Sut.Validate(new CreateRevenueCommand(req));

    [Fact]
    public void 有效輸入_不應拋例外()
    {
        Act(ValidRequest()); // 不丟例外即通過
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678901")] // 11 碼，超過 10
    public void 公司代號不合法_應拋驗證例外(string code)
    {
        Assert.Throws<ValidationException>(() => Act(ValidRequest(code)));
    }

    [Theory]
    [InlineData(9999)]    // < 10000
    [InlineData(100000)]  // > 99999
    public void 資料年月超出民國YYYMM範圍_應拋驗證例外(int yearMonth)
    {
        Assert.Throws<ValidationException>(() => Act(ValidRequest() with { DataYearMonth = yearMonth }));
    }
}

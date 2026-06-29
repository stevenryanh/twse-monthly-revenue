using TwseRevenue.Application.Contracts;
using TwseRevenue.Application.Mapping;
using Xunit;

namespace TwseRevenue.Tests;

public class RevenueMappingTests
{
    [Fact]
    public void Request轉Entity再轉Dto_欄位應完整一致()
    {
        var request = new CreateRevenueRequest(
            "2330", 11505, 1150617, "台積電", "半導體業",
            263713000, 320000000, 210000000, -17.5m, 25.5m,
            1200000000, 900000000, 33.3m, "-");

        var dto = request.ToEntity().ToDto();

        Assert.Equal(request.CompanyCode, dto.CompanyCode);
        Assert.Equal(request.DataYearMonth, dto.DataYearMonth);
        Assert.Equal(request.ReportDate, dto.ReportDate);
        Assert.Equal(request.CompanyName, dto.CompanyName);
        Assert.Equal(request.Industry, dto.Industry);
        Assert.Equal(request.CurrentMonthRevenue, dto.CurrentMonthRevenue);
        Assert.Equal(request.MoMPercent, dto.MoMPercent);
        Assert.Equal(request.YoYPercent, dto.YoYPercent);
        Assert.Equal(request.CumDiffPercent, dto.CumDiffPercent);
        Assert.Equal(request.Remark, dto.Remark);
    }

    [Fact]
    public void Null欄位映射_應保持Null()
    {
        var request = new CreateRevenueRequest(
            "2330", 11505, 1150617, "台積電",
            null, null, null, null, null, null, null, null, null, null);

        var dto = request.ToEntity().ToDto();

        Assert.Null(dto.Industry);
        Assert.Null(dto.CurrentMonthRevenue);
        Assert.Null(dto.MoMPercent);
        Assert.Null(dto.Remark);
    }
}

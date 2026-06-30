using Microsoft.AspNetCore.Http;
using TwseRevenue.Api.Middleware;
using Xunit;

namespace TwseRevenue.Tests;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task 無傳入ID_應產生並寫入回應標頭()
    {
        var sut = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var ctx = new DefaultHttpContext();

        await sut.Invoke(ctx);

        var id = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(id));
    }

    [Fact]
    public async Task 有傳入ID_應沿用()
    {
        var sut = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationIdMiddleware.HeaderName] = "abc123";

        await sut.Invoke(ctx);

        Assert.Equal("abc123", ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }
}

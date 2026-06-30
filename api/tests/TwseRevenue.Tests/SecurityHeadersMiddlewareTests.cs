using Microsoft.AspNetCore.Http;
using TwseRevenue.Api.Middleware;
using Xunit;

namespace TwseRevenue.Tests;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task 應加上安全標頭並呼叫next()
    {
        var nextCalled = false;
        var sut = new SecurityHeadersMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = new DefaultHttpContext();

        await sut.Invoke(ctx);

        Assert.True(nextCalled);
        Assert.Equal("nosniff", ctx.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", ctx.Response.Headers["X-Frame-Options"].ToString());
        Assert.Equal("no-referrer", ctx.Response.Headers["Referrer-Policy"].ToString());
    }
}

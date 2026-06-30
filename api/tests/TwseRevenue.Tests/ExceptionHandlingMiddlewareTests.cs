using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TwseRevenue.Api.Middleware;
using TwseRevenue.Application.Errors;
using Xunit;

namespace TwseRevenue.Tests;

public class ExceptionHandlingMiddlewareTests
{
    private static DefaultHttpContext NewContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static string ReadBody(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        return new StreamReader(ctx.Response.Body, Encoding.UTF8).ReadToEnd();
    }

    [Fact]
    public async Task 驗證例外_應回400與訊息()
    {
        var sut = new ExceptionHandlingMiddleware(
            _ => throw new ValidationException("代號必填"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var ctx = NewContext();

        await sut.Invoke(ctx);

        Assert.Equal(400, ctx.Response.StatusCode);
        Assert.Contains("代號必填", ReadBody(ctx));
    }

    [Fact]
    public async Task 未預期例外_應回500且不外洩細節但帶traceId()
    {
        var sut = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("敏感內部細節不可外洩"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var ctx = NewContext();

        await sut.Invoke(ctx);

        var body = ReadBody(ctx);
        Assert.Equal(500, ctx.Response.StatusCode);
        Assert.Contains("伺服器內部錯誤", body);
        Assert.DoesNotContain("敏感內部細節", body); // 不外洩例外訊息
        Assert.Contains("traceId", body);
    }

    [Fact]
    public async Task 正常無例外_應原樣放行()
    {
        var sut = new ExceptionHandlingMiddleware(
            ctx => { ctx.Response.StatusCode = 200; return Task.CompletedTask; },
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var ctx = NewContext();

        await sut.Invoke(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
    }
}

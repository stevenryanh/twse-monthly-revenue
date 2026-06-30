using MediatR;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Behaviors;
using TwseRevenue.Application.Errors;
using Xunit;

namespace TwseRevenue.Tests;

public class ValidationBehaviorTests
{
    private sealed record Req(string Value);

    private sealed class ThrowingValidator : IValidator<Req>
    {
        public void Validate(Req instance) => throw new ValidationException("bad");
    }

    private sealed class RecordingValidator : IValidator<Req>
    {
        public bool Called { get; private set; }
        public void Validate(Req instance) => Called = true;
    }

    [Fact]
    public async Task 無驗證器_直接放行()
    {
        var sut = new ValidationBehavior<Req, string>(Array.Empty<IValidator<Req>>());
        var nextCalled = false;
        RequestHandlerDelegate<string> next = () => { nextCalled = true; return Task.FromResult("ok"); };

        var result = await sut.Handle(new Req("x"), next, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task 驗證通過_應呼叫驗證器與next()
    {
        var validator = new RecordingValidator();
        var sut = new ValidationBehavior<Req, string>(new IValidator<Req>[] { validator });
        var nextCalled = false;
        RequestHandlerDelegate<string> next = () => { nextCalled = true; return Task.FromResult("ok"); };

        await sut.Handle(new Req("x"), next, CancellationToken.None);

        Assert.True(validator.Called);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task 驗證失敗_應拋例外且不呼叫next()
    {
        var sut = new ValidationBehavior<Req, string>(new IValidator<Req>[] { new ThrowingValidator() });
        var nextCalled = false;
        RequestHandlerDelegate<string> next = () => { nextCalled = true; return Task.FromResult("ok"); };

        await Assert.ThrowsAsync<ValidationException>(() => sut.Handle(new Req("x"), next, CancellationToken.None));

        Assert.False(nextCalled);
    }
}

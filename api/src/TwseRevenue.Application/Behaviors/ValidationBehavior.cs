using MediatR;
using TwseRevenue.Application.Abstractions;

namespace TwseRevenue.Application.Behaviors;

/// <summary>
/// MediatR pipeline 行為：在任何 handler 執行前，跑完該 Request 的所有 <see cref="IValidator{T}"/>。
/// 驗證是橫切關注點 —— 集中在 pipeline 這一個接縫，而非散落各 handler。
/// 沒有對應 validator 的 Request（如純查詢）自動跳過。
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        foreach (var validator in _validators)
            validator.Validate(request);

        return next();
    }
}

using Serilog.Context;

namespace TwseRevenue.Api.Middleware;

/// <summary>
/// 為每個請求建立關聯 ID（CorrelationId）：沿用上游傳入的 <c>X-Correlation-ID</c>，否則新生一個。
/// 寫回回應標頭供前端/呼叫端回報，並推入 Serilog LogContext，
/// 讓該請求內所有日誌（含 UseSerilogRequestLogging 的完成日誌與例外日誌）都帶同一 ID，便於稽核與追蹤。
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var correlationId =
            context.Request.Headers.TryGetValue(HeaderName, out var incoming) && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString()
                : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await _next(context);
    }
}

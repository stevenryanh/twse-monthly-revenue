using System.Text.Json;
using TwseRevenue.Application.Errors;
using TwseRevenue.Application.Logging;

namespace TwseRevenue.Api.Middleware;

/// <summary>
/// 全域例外處理（由 MVC ExceptionFilter 重構為 middleware，涵蓋 MVC 以外的管線錯誤）。
/// 驗證錯誤 → 400 並回明確訊息；其餘 → 500 不外洩內部細節、回傳 traceId 供回報，並以結構化 log code 完整記錄。
/// 無實例本地狀態，可水平擴充。
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, TwseLogCodes.Errors.ValidationFailed +
                " 輸入驗證失敗 - Method={Method}, Path={Path}, Reason={Reason}",
                context.Request.Method, context.Request.Path.Value, ex.Message);
            await WriteAsync(context, StatusCodes.Status400BadRequest, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            var traceId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
            if (string.IsNullOrEmpty(traceId))
                traceId = context.TraceIdentifier;

            _logger.LogError(ex, TwseLogCodes.Errors.Unhandled +
                " 未處理的例外 - Method={Method}, Path={Path}, CorrelationId={CorrelationId}",
                context.Request.Method, context.Request.Path.Value, traceId);
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                new { error = "伺服器內部錯誤", traceId });
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, object body)
    {
        if (context.Response.HasStarted)
            return; // 回應已開始則無法改寫，避免拋出二次例外

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}

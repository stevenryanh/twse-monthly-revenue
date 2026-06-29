using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TwseRevenue.Application.Errors;

namespace TwseRevenue.Api.Filters;

/// <summary>
/// 全域例外處理（題目加分項：action filter 統一錯誤處理 + Log）。
/// 驗證錯誤 → 400 並回明確訊息；其餘 → 500 且不外洩內部細節，但完整記錄到 Log。
/// </summary>
public sealed class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) => _logger = logger;

    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ValidationException ve:
                _logger.LogWarning(ve, "輸入驗證失敗：{Message}", ve.Message);
                context.Result = new BadRequestObjectResult(new { error = ve.Message });
                break;

            default:
                _logger.LogError(context.Exception, "未處理的例外");
                context.Result = new ObjectResult(new { error = "伺服器內部錯誤" }) { StatusCode = 500 };
                break;
        }

        context.ExceptionHandled = true;
    }
}

namespace TwseRevenue.Api.Middleware;

/// <summary>
/// 統一加上基礎安全回應標頭。未來的標頭政策（CSP、HSTS、Permissions-Policy…）
/// 都落在這一個接縫，不必散落各 Controller。
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task Invoke(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";   // 禁止 MIME 嗅探
        headers["X-Frame-Options"] = "DENY";              // 禁止被內嵌（防點擊劫持）
        headers["Referrer-Policy"] = "no-referrer";       // 不外洩來源網址
        return _next(context);
    }
}

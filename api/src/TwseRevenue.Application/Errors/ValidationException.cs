namespace TwseRevenue.Application.Errors;

/// <summary>
/// 應用層輸入驗證失敗。由 Api 的 GlobalExceptionFilter 轉為 400 Bad Request。
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}

namespace TwseRevenue.Application.Abstractions;

/// <summary>
/// 輸入驗證契約。每個需要驗證的 Request 對應一個實作；由 ValidationBehavior 在 handler 執行前統一觸發。
/// 這是「未來輸入/資安驗證政策」的單一著陸點：規則集中於此，不散落在各 handler。
/// 失敗時丟 <see cref="Errors.ValidationException"/>（由 GlobalExceptionFilter 轉 400）。
/// 刻意手寫、零額外相依（比照本專案不引入 AutoMapper/FluentValidation 的取捨）。
/// </summary>
public interface IValidator<in T>
{
    void Validate(T instance);
}

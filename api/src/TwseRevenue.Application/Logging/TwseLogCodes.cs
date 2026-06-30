namespace TwseRevenue.Application.Logging;

/// <summary>
/// 結構化日誌代碼（精神比照 團隊 的 結構化日誌代碼）。格式：{Level}{Layer}{Seq}。
/// Level：I=Info、W=Warning、E=Error、D=Debug。
/// Layer：1=Api/Presentation、3=Application、5=Infrastructure、6=CrossCutting。
/// 用法：logger.LogXxx(TwseLogCodes.X.Y + " 訊息 - Field={Field}", value);
/// 讓日誌可分類、可 grep、跨服務一致；新增功能時只在此擴充碼，呼叫端維持同一格式。
/// </summary>
public static class TwseLogCodes
{
    /// <summary>CrossCutting（layer 6）— 全域例外處理。</summary>
    public static class Errors
    {
        public const string ValidationFailed = "W601";
        public const string Unhandled        = "E602";
    }

    /// <summary>Application（layer 3）— 營收用例。</summary>
    public static class Revenue
    {
        public const string Upserted = "I301"; // 寫入（upsert）成功
        public const string Queried  = "I302"; // 查詢完成（含回傳筆數）
    }
}

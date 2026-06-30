namespace TwseRevenue.Application.Logging;

/// <summary>
/// 結構化日誌代碼（每條 log 掛一個代碼，便於分類/grep/跨服務一致）。格式：{Level}{Layer}{Seq}。
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
        public const string ValidationFailed = "W601"; // 輸入驗證失敗（回 400，訊息含 Method/Path/Reason）
        public const string Unhandled        = "E602"; // 未處理的例外（回 500，不外洩內部細節、帶 traceId）
    }

    /// <summary>Application（layer 3）— 營收用例。</summary>
    public static class Revenue
    {
        public const string Upserted          = "I301"; // 營收資料寫入（upsert）成功，含 CompanyCode/DataYearMonth
        public const string Queried           = "I302"; // 營收查詢完成，含 CompanyCode/回傳筆數 Count
        public const string CompaniesSearched = "I303"; // 公司關鍵字搜尋完成，含 Keyword/回傳筆數 Count
    }

    /// <summary>Application（layer 3）— 買賣投報（每日行情）用例。</summary>
    public static class Quote
    {
        public const string Upserted      = "I304"; // 每日行情寫入（upsert）成功，含 CompanyCode/TradeDate
        public const string Ranked        = "I305"; // 買賣投報排行完成，含 Sort/Keyword/回傳筆數 Count
        public const string SwingAnalyzed = "I306"; // 波段分析完成，含 CompanyCode/序列天數/轉折數
    }
}

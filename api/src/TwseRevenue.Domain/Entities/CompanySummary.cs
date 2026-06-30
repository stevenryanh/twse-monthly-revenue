namespace TwseRevenue.Domain.Entities;

/// <summary>
/// 公司摘要（代號 + 名稱 + 產業別）。供「邊輸入邊列出」的搜尋／自動完成使用，
/// 為營收明細的輕量投影，不含逐月數據。
/// </summary>
public sealed class CompanySummary
{
    public string CompanyCode { get; init; } = default!;   // 公司代號
    public string CompanyName { get; init; } = default!;   // 公司名稱
    public string? Industry { get; init; }                 // 產業別
}

namespace TwseRevenue.Application.Contracts;

/// <summary>公司搜尋結果（自動完成用）。與 Entity 解耦的對外輸出契約。</summary>
public sealed record CompanySummaryDto(
    string CompanyCode,
    string CompanyName,
    string? Industry);

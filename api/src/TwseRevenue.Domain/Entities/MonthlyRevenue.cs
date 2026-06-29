namespace TwseRevenue.Domain.Entities;

/// <summary>
/// 上市公司每月營業收入（對標證交所 OpenAPI t187ap05_L 的 14 個欄位）。
/// 民國年月日以整數保真儲存；金額為仟元；增減為百分比。
/// </summary>
public sealed class MonthlyRevenue
{
    public string CompanyCode { get; init; } = default!;   // 公司代號
    public int DataYearMonth { get; init; }                // 資料年月（民國 YYYMM）
    public int ReportDate { get; init; }                   // 出表日期（民國 YYYMMDD）
    public string CompanyName { get; init; } = default!;   // 公司名稱
    public string? Industry { get; init; }                 // 產業別
    public long? CurrentMonthRevenue { get; init; }        // 當月營收
    public long? LastMonthRevenue { get; init; }           // 上月營收
    public long? LastYearMonthRevenue { get; init; }       // 去年當月營收
    public decimal? MoMPercent { get; init; }              // 上月比較增減(%)
    public decimal? YoYPercent { get; init; }              // 去年同月增減(%)
    public long? CumCurrentRevenue { get; init; }          // 當月累計營收
    public long? CumLastYearRevenue { get; init; }         // 去年累計營收
    public decimal? CumDiffPercent { get; init; }          // 前期比較增減(%)
    public string? Remark { get; init; }                   // 備註
}

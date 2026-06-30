using System;
using TwseRevenue.Application.Analysis;
using TwseRevenue.Domain.Entities;
using Xunit;

namespace TwseRevenue.Tests;

public class SwingAnalyzerTests
{
    private static DailyQuote Q(int date, decimal close) =>
        new() { CompanyCode = "9999", TradeDate = date, CompanyName = "測試", ClosePrice = close };

    [Fact]
    public async Task 資料不足_應回說明且無推估()
    {
        var series = new[] { Q(1150601, 10m), Q(1150602, 11m), Q(1150603, 12m) };

        var r = SwingAnalyzer.Analyze("9999", series);

        Assert.Equal(3, r.Days);
        Assert.Empty(r.Swings);
        Assert.Null(r.NextTurnKind);
        Assert.Contains("資料不足", r.Note, StringComparison.Ordinal);
        await Task.CompletedTask;
    }

    [Fact]
    public void 明顯波形_應偵測到波峰波谷並推估下一轉折()
    {
        // 上下震盪：明顯的峰(14、15)與谷(10)
        decimal[] closes =
        {
            10, 11, 12, 13, 14, 13, 12, 11, 10, 11,
            12, 13, 14, 15, 14, 13, 12, 11, 10, 11, 12,
        };
        var series = closes.Select((c, i) => Q(1150601 + i, c)).ToList();

        var r = SwingAnalyzer.Analyze("9999", series);

        Assert.Equal(closes.Length, r.Days);
        Assert.Contains(r.Swings, s => s.Kind == "peak");
        Assert.Contains(r.Swings, s => s.Kind == "trough");
        Assert.NotNull(r.NextTurnKind);
        Assert.NotNull(r.EstDaysToNextTurn);
        Assert.True(r.EstDaysToNextTurn >= 1);
        Assert.NotNull(r.AvgCycleDays);
        Assert.NotNull(r.RecentHigh);
        Assert.NotNull(r.RecentLow);
    }
}

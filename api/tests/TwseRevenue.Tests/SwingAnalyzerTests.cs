using System;
using System.Collections.Generic;
using TwseRevenue.Application.Analysis;
using TwseRevenue.Application.Contracts;
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

    // 建構波段分析結果（只填進場時機關心的欄位，其餘給合理預設）
    private static SwingAnalysisDto Dto(decimal pos, string nextKind, int days,
        decimal lastClose, decimal tLow, decimal tHigh, decimal cycle) =>
        new("9999", "測試", 60,
            new List<SwingPointDto>(), new List<SwingMarkerDto>(),
            lastClose, lastClose + 10, lastClose - 10, pos,
            null, null, null, cycle, 5m, nextKind, days, tLow, tHigh, "note");

    [Fact]
    public void 進場標籤_即將見底近低下檔有限_標即將見底()
    {
        var a = Dto(pos: 22m, nextKind: "trough", days: 1, lastClose: 24m, tLow: 23.6m, tHigh: 24.0m, cycle: 8m);
        Assert.Equal("即將見底", SwingAnalyzer.EntryTimingLabel(a));
    }

    [Fact]
    public void 進場標籤_位置高_標觀望()
    {
        var a = Dto(pos: 85m, nextKind: "trough", days: 1, lastClose: 28m, tLow: 27m, tHigh: 28m, cycle: 8m);
        Assert.Equal("偏高·觀望", SwingAnalyzer.EntryTimingLabel(a));
    }

    [Fact]
    public void 進場標籤_即將見頂_標宜收手()
    {
        var a = Dto(pos: 60m, nextKind: "peak", days: 2, lastClose: 30m, tLow: 31m, tHigh: 32m, cycle: 8m);
        Assert.Equal("即將見頂·宜收手", SwingAnalyzer.EntryTimingLabel(a));
    }

    [Fact]
    public void 進場權重_近低即將見底_高於_近高()
    {
        var good = Dto(pos: 20m, nextKind: "trough", days: 1, lastClose: 24m, tLow: 23.8m, tHigh: 24.1m, cycle: 8m);
        var high = Dto(pos: 85m, nextKind: "trough", days: 1, lastClose: 28m, tLow: 26m, tHigh: 27m, cycle: 8m);
        Assert.True(SwingAnalyzer.EntryTimingFactor(good) > SwingAnalyzer.EntryTimingFactor(high));
    }
}

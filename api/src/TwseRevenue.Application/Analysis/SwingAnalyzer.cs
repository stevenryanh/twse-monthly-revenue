using TwseRevenue.Application.Contracts;
using TwseRevenue.Domain.Entities;

namespace TwseRevenue.Application.Analysis;

/// <summary>
/// 啟發式波段分析（純函式，無外部相依，易測）。
/// 以近期每日收盤偵測波峰/波谷（區域極值法），量出平均週期與振幅，
/// 據此推估下一轉折「還有幾個交易日」與「目標價區間」。
/// 屬統計外推、僅供參考——不是未來價格的預測，更非投資建議。
/// </summary>
public static class SwingAnalyzer
{
    private const int Window = 3;          // 區域極值視窗（±N 日）
    private const string Disclaimer =
        "啟發式推估，依近期波段週期與振幅外推，僅供參考、非投資建議；市場可能不延續過往節奏。";

    public static SwingAnalysisDto Analyze(string companyCode, IReadOnlyList<DailyQuote> series)
    {
        // 取有收盤價的點，序列已由舊到新
        var pts = series.Where(q => q.ClosePrice.HasValue)
                        .Select(q => (date: q.TradeDate, close: q.ClosePrice!.Value))
                        .ToList();
        var name = series.Select(q => q.CompanyName).LastOrDefault(n => n != null);
        var points = pts.Select(p => new SwingPointDto(p.date, p.close)).ToList();

        if (pts.Count < 2 * Window + 2)
        {
            return new SwingAnalysisDto(
                companyCode, name, pts.Count, points,
                Array.Empty<SwingMarkerDto>(),       // Swings
                pts.Count > 0 ? pts[^1].close : null, // LastClose
                null, null, null,                     // RecentHigh/Low/Position
                null, null, null,                     // Ma5/Ma20/Rsi14
                null, null,                           // AvgCycleDays/AvgAmplitude
                null, null, null, null,               // NextTurnKind/EstDays/TargetLo/TargetHi
                "資料不足以分析波段（建議以 import-quotes.py 餵 3–6 個月）。");
        }

        // 區域極值偵測 + 交替整理（峰谷相間，同類取更極端者）
        var swings = DetectSwings(pts);

        var closes = pts.Select(p => p.close).ToList();
        var last = closes[^1];
        var high = closes.Max();
        var low = closes.Min();
        decimal? pos = high > low ? Math.Round((last - low) / (high - low) * 100m, 2) : null;

        var ma5 = Average(closes, 5);
        var ma20 = Average(closes, 20);
        var rsi = Rsi(closes, 14);

        // 週期（半週期＝相鄰轉折距離）與振幅
        decimal? avgHalf = null, avgAmp = null;
        if (swings.Count >= 2)
        {
            var gaps = new List<int>();
            var amps = new List<decimal>();
            for (int i = 1; i < swings.Count; i++)
            {
                gaps.Add(swings[i].index - swings[i - 1].index);
                var a = swings[i - 1].close;
                var b = swings[i].close;
                var baseP = Math.Min(a, b);
                if (baseP > 0) amps.Add(Math.Abs(a - b) / baseP * 100m);
            }
            if (gaps.Count > 0) avgHalf = (decimal)gaps.Average(g => (double)g);
            if (amps.Count > 0) avgAmp = Math.Round(amps.Average(), 2);
        }

        // 推估下一轉折
        string? nextKind = null;
        int? estDays = null;
        decimal? targetLo = null, targetHi = null;
        if (swings.Count >= 2 && avgHalf is > 0 && avgAmp is > 0)
        {
            var lastSwing = swings[^1];
            nextKind = lastSwing.kind == "peak" ? "trough" : "peak";
            var sinceLast = (closes.Count - 1) - lastSwing.index;
            estDays = Math.Max(1, (int)Math.Round((double)avgHalf.Value) - sinceLast);

            // 目標：自最近轉折按平均振幅外推；給 ±20% 振幅的不確定帶
            var amp = avgAmp.Value / 100m;
            var pivot = lastSwing.close;
            decimal target = nextKind == "peak" ? pivot * (1 + amp) : pivot * (1 - amp);
            var band = pivot * amp * 0.2m;
            targetLo = Math.Round(target - band, 2);
            targetHi = Math.Round(target + band, 2);
        }

        var markers = swings.Select(s => new SwingMarkerDto(pts[s.index].date, s.close, s.kind)).ToList();

        return new SwingAnalysisDto(companyCode, name, pts.Count, points, markers,
            last, high, low, pos, ma5, ma20, rsi,
            avgHalf is null ? null : Math.Round(avgHalf.Value * 2, 1),  // 報告全週期（峰到峰）
            avgAmp, nextKind, estDays, targetLo, targetHi, Disclaimer);
    }

    private static List<(int index, decimal close, string kind)> DetectSwings(
        List<(int date, decimal close)> pts)
    {
        var raw = new List<(int index, decimal close, string kind)>();
        for (int i = Window; i < pts.Count - Window; i++)
        {
            var c = pts[i].close;
            bool isPeak = true, isTrough = true;
            for (int j = i - Window; j <= i + Window; j++)
            {
                if (j == i) continue;
                if (pts[j].close >= c) isPeak = false;
                if (pts[j].close <= c) isTrough = false;
            }
            if (isPeak) raw.Add((i, c, "peak"));
            else if (isTrough) raw.Add((i, c, "trough"));
        }

        // 交替整理：連續同類只保留更極端者（峰取高、谷取低）
        var cleaned = new List<(int index, decimal close, string kind)>();
        foreach (var s in raw)
        {
            if (cleaned.Count == 0 || cleaned[^1].kind != s.kind)
            {
                cleaned.Add(s);
            }
            else
            {
                var prev = cleaned[^1];
                bool replace = s.kind == "peak" ? s.close > prev.close : s.close < prev.close;
                if (replace) cleaned[^1] = s;
            }
        }
        return cleaned;
    }

    private static decimal? Average(List<decimal> xs, int n)
    {
        if (xs.Count < n) return null;
        return Math.Round(xs.Skip(xs.Count - n).Average(), 2);
    }

    private static decimal? Rsi(List<decimal> closes, int n)
    {
        if (closes.Count < n + 1) return null;
        decimal gain = 0, loss = 0;
        for (int i = closes.Count - n; i < closes.Count; i++)
        {
            var diff = closes[i] - closes[i - 1];
            if (diff >= 0) gain += diff; else loss -= diff;
        }
        if (gain + loss == 0) return 50m;
        var rs = loss == 0 ? decimal.MaxValue : gain / loss;
        var rsi = loss == 0 ? 100m : 100m - 100m / (1 + rs);
        return Math.Round(rsi, 2);
    }
}

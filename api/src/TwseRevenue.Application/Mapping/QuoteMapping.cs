using TwseRevenue.Application.Contracts;
using TwseRevenue.Domain.Entities;

namespace TwseRevenue.Application.Mapping;

/// <summary>每日行情的手寫映射（與 RevenueMapping 同家規，不引入 AutoMapper）。</summary>
public static class QuoteMapping
{
    public static DailyQuote ToEntity(this CreateQuoteRequest r) => new()
    {
        CompanyCode = r.CompanyCode,
        TradeDate = r.TradeDate,
        CompanyName = r.CompanyName,
        OpenPrice = r.OpenPrice,
        HighPrice = r.HighPrice,
        LowPrice = r.LowPrice,
        ClosePrice = r.ClosePrice,
        Change = r.Change,
        TradeVolume = r.TradeVolume,
    };

    public static QuoteRankingDto ToDto(this QuoteRanking e) => new(
        e.CompanyCode, e.CompanyName, e.Days, e.FirstClose, e.LastClose, e.LastDate,
        e.PeriodReturnPercent, e.AvgDailyReturnPercent, e.VolatilityPercent, e.LastDayReturnPercent,
        e.RiskAdjustedReturn);
}

using System.Data;
using Microsoft.Data.SqlClient;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Domain.Entities;

namespace TwseRevenue.Infrastructure.Persistence;

/// <summary>
/// 每日行情存取：與 RevenueRepository 同樣「參數化呼叫預存程序」，
/// CommandText 僅為 sp 名稱，所有輸入皆走 SqlParameter，杜絕 SQL Injection。
/// </summary>
public sealed class QuoteRepository : IQuoteRepository
{
    private readonly ISqlConnectionFactory _factory;

    public QuoteRepository(ISqlConnectionFactory factory) => _factory = factory;

    public async Task UpsertAsync(DailyQuote q, CancellationToken ct)
    {
        await using var conn = _factory.Create();
        await using var cmd = new SqlCommand("dbo.usp_DailyQuote_Upsert", conn)
        {
            CommandType = CommandType.StoredProcedure,
        };
        cmd.Parameters.Add(new SqlParameter("@CompanyCode", SqlDbType.NVarChar, 10) { Value = q.CompanyCode });
        cmd.Parameters.Add(new SqlParameter("@TradeDate", SqlDbType.Int) { Value = q.TradeDate });
        cmd.Parameters.Add(new SqlParameter("@CompanyName", SqlDbType.NVarChar, 60) { Value = (object?)q.CompanyName ?? DBNull.Value });
        // DECIMAL(18,4)：明設 Precision/Scale，避免 SqlParameter 預設 Scale=0 截斷小數。
        cmd.Parameters.Add(new SqlParameter("@OpenPrice", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = (object?)q.OpenPrice ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@HighPrice", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = (object?)q.HighPrice ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@LowPrice", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = (object?)q.LowPrice ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@ClosePrice", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = (object?)q.ClosePrice ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Change", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = (object?)q.Change ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@TradeVolume", SqlDbType.BigInt) { Value = (object?)q.TradeVolume ?? DBNull.Value });

        await conn.OpenAsync(ct);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<QuoteRanking>> RankAsync(
        string? keyword, string? codes, string? sort, int top, decimal? maxPrice, CancellationToken ct)
    {
        await using var conn = _factory.Create();
        await using var cmd = new SqlCommand("dbo.usp_Quote_Ranking", conn)
        {
            CommandType = CommandType.StoredProcedure,
        };
        cmd.Parameters.Add(new SqlParameter("@Keyword", SqlDbType.NVarChar, 100) { Value = (object?)keyword ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Codes", SqlDbType.NVarChar, -1) { Value = (object?)codes ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Sort", SqlDbType.NVarChar, 20) { Value = (object?)sort ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Top", SqlDbType.Int) { Value = top });
        cmd.Parameters.Add(new SqlParameter("@MaxPrice", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = (object?)maxPrice ?? DBNull.Value });

        await conn.OpenAsync(ct);
        var list = new List<QuoteRanking>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(new QuoteRanking
            {
                CompanyCode = reader.GetString(reader.GetOrdinal("CompanyCode")),
                CompanyName = NullableString(reader, "CompanyName"),
                Days = reader.GetInt32(reader.GetOrdinal("Days")),
                FirstClose = NullableDecimal(reader, "FirstClose"),
                LastClose = NullableDecimal(reader, "LastClose"),
                LastDate = NullableInt32(reader, "LastDate"),
                PeriodReturnPercent = NullableDecimal(reader, "PeriodReturnPct"),
                AvgDailyReturnPercent = NullableDecimal(reader, "AvgDailyRetPct"),
                VolatilityPercent = NullableDecimal(reader, "VolatilityPct"),
                LastDayReturnPercent = NullableDecimal(reader, "LastDayRetPct"),
            });
        return list;
    }

    private static string? NullableString(SqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    private static int? NullableInt32(SqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetInt32(i);
    }

    private static decimal? NullableDecimal(SqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetDecimal(i);
    }
}

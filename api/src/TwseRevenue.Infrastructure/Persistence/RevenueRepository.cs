using System.Data;
using Microsoft.Data.SqlClient;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Domain.Entities;

namespace TwseRevenue.Infrastructure.Persistence;

/// <summary>
/// 以「參數化呼叫預存程序」存取資料，從應用層杜絕 SQL Injection
/// （CommandText 僅為 sp 名稱，所有輸入皆走 SqlParameter，絕無字串拼接）。
/// </summary>
public sealed class RevenueRepository : IRevenueRepository
{
    private readonly ISqlConnectionFactory _factory;

    public RevenueRepository(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<MonthlyRevenue>> GetByCompanyCodeAsync(string companyCode, CancellationToken ct)
    {
        await using var conn = _factory.Create();
        await using var cmd = new SqlCommand("dbo.usp_MonthlyRevenue_GetByCompanyCode", conn)
        {
            CommandType = CommandType.StoredProcedure,
        };
        cmd.Parameters.Add(new SqlParameter("@CompanyCode", SqlDbType.NVarChar, 10) { Value = companyCode });

        await conn.OpenAsync(ct);
        var list = new List<MonthlyRevenue>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));
        return list;
    }

    public async Task UpsertAsync(MonthlyRevenue r, CancellationToken ct)
    {
        await using var conn = _factory.Create();
        await using var cmd = new SqlCommand("dbo.usp_MonthlyRevenue_Upsert", conn)
        {
            CommandType = CommandType.StoredProcedure,
        };
        cmd.Parameters.Add(new SqlParameter("@CompanyCode", SqlDbType.NVarChar, 10) { Value = r.CompanyCode });
        cmd.Parameters.Add(new SqlParameter("@DataYearMonth", SqlDbType.Int) { Value = r.DataYearMonth });
        cmd.Parameters.Add(new SqlParameter("@ReportDate", SqlDbType.Int) { Value = r.ReportDate });
        cmd.Parameters.Add(new SqlParameter("@CompanyName", SqlDbType.NVarChar, 60) { Value = r.CompanyName });
        cmd.Parameters.Add(new SqlParameter("@Industry", SqlDbType.NVarChar, 60) { Value = (object?)r.Industry ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@CurrentMonthRevenue", SqlDbType.BigInt) { Value = (object?)r.CurrentMonthRevenue ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@LastMonthRevenue", SqlDbType.BigInt) { Value = (object?)r.LastMonthRevenue ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@LastYearMonthRevenue", SqlDbType.BigInt) { Value = (object?)r.LastYearMonthRevenue ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@MoMPercent", SqlDbType.Decimal) { Value = (object?)r.MoMPercent ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@YoYPercent", SqlDbType.Decimal) { Value = (object?)r.YoYPercent ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@CumCurrentRevenue", SqlDbType.BigInt) { Value = (object?)r.CumCurrentRevenue ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@CumLastYearRevenue", SqlDbType.BigInt) { Value = (object?)r.CumLastYearRevenue ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@CumDiffPercent", SqlDbType.Decimal) { Value = (object?)r.CumDiffPercent ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Remark", SqlDbType.NVarChar, 200) { Value = (object?)r.Remark ?? DBNull.Value });

        await conn.OpenAsync(ct);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static MonthlyRevenue Map(SqlDataReader r) => new()
    {
        CompanyCode = r.GetString(r.GetOrdinal("CompanyCode")),
        DataYearMonth = r.GetInt32(r.GetOrdinal("DataYearMonth")),
        ReportDate = r.GetInt32(r.GetOrdinal("ReportDate")),
        CompanyName = r.GetString(r.GetOrdinal("CompanyName")),
        Industry = NullableString(r, "Industry"),
        CurrentMonthRevenue = NullableInt64(r, "CurrentMonthRevenue"),
        LastMonthRevenue = NullableInt64(r, "LastMonthRevenue"),
        LastYearMonthRevenue = NullableInt64(r, "LastYearMonthRevenue"),
        MoMPercent = NullableDecimal(r, "MoMPercent"),
        YoYPercent = NullableDecimal(r, "YoYPercent"),
        CumCurrentRevenue = NullableInt64(r, "CumCurrentRevenue"),
        CumLastYearRevenue = NullableInt64(r, "CumLastYearRevenue"),
        CumDiffPercent = NullableDecimal(r, "CumDiffPercent"),
        Remark = NullableString(r, "Remark"),
    };

    private static string? NullableString(SqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    private static long? NullableInt64(SqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetInt64(i);
    }

    private static decimal? NullableDecimal(SqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetDecimal(i);
    }
}

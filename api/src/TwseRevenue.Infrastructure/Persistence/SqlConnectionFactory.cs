using Microsoft.Data.SqlClient;

namespace TwseRevenue.Infrastructure.Persistence;

public interface ISqlConnectionFactory
{
    SqlConnection Create();
}

/// <summary>集中建立 SqlConnection，連線字串由組態注入，不散落於各處。</summary>
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString) => _connectionString = connectionString;

    public SqlConnection Create() => new(_connectionString);
}

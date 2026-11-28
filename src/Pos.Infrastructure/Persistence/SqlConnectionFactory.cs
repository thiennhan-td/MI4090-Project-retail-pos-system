using Microsoft.Data.SqlClient;

namespace Pos.Infrastructure.Persistence;

public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = new SqlConnectionStringBuilder(connectionString).ConnectionString;
    }

    public SqlConnection CreateConnection() => new(_connectionString);
}

using Npgsql;
using System.Data;

namespace TicketingASP.Data;

/// <summary>
/// Factory for creating PostgreSQL database connections
/// </summary>
public interface IDbConnectionFactory
{
    NpgsqlConnection CreateConnection();
    Task<NpgsqlConnection> CreateOpenConnectionAsync();
}

/// <summary>
/// Implementation of database connection factory for PostgreSQL
/// </summary>
public class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public PostgresConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<NpgsqlConnection> CreateOpenConnectionAsync()
    {
        var connection = CreateConnection();
        await connection.OpenAsync();
        return connection;
    }
}

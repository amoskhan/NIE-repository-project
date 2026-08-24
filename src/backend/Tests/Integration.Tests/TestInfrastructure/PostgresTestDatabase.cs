using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Integration.Tests;

internal sealed class PostgresTestDatabase : IAsyncDisposable
{
    private readonly string _adminConnectionString;
    private readonly string _databaseName;

    private PostgresTestDatabase(string adminConnectionString, string databaseName)
    {
        _adminConnectionString = adminConnectionString;
        _databaseName = databaseName;
    }

    /// <summary>
    /// Skips the calling test when no PostgreSQL admin connection is configured.
    /// A developer without local services gets a skip rather than a hard failure,
    /// while CI always sets the variable so nothing is silently skipped there.
    /// </summary>
    public static string RequireAdminConnectionString()
    {
        var adminConnectionString = Environment.GetEnvironmentVariable("NIE_TEST_POSTGRES_ADMIN_CONNECTION");
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(adminConnectionString),
            "Set NIE_TEST_POSTGRES_ADMIN_CONNECTION to run PostgreSQL integration tests.");
        return adminConnectionString!;
    }

    public static async Task<PostgresTestDatabase> CreateAsync(CancellationToken cancellationToken)
    {
        var adminConnectionString = RequireAdminConnectionString();
        var databaseName = $"nie_integration_{Guid.CreateVersion7():N}";
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);
        return new PostgresTestDatabase(adminConnectionString, databaseName);
    }

    public MainDbContext CreateContext()
    {
        var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = _databaseName,
        };
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .Options;
        return new MainDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }
}

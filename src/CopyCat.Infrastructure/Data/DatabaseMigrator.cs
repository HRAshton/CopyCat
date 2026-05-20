using System.Data.Common;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace CopyCat.Infrastructure.Data;

/// <summary>
/// Applies pending Entity Framework Core migrations.
/// </summary>
public static class DatabaseMigrator
{
    private const long MigrationLockKey = 6_420_245_104_008_119_107;

    /// <summary>
    /// Applies all pending migrations to the configured database.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when migrations have been applied.</returns>
    public static async Task ApplyMigrationsAsync(
        CopyCatDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        bool shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await AcquireMigrationLockAsync(connection, cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        finally
        {
            await ReleaseMigrationLockAsync(connection, cancellationToken);

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task AcquireMigrationLockAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            return;
        }

        await using NpgsqlCommand command = new("SELECT pg_advisory_lock(@key);", npgsqlConnection);
        command.Parameters.AddWithValue("key", MigrationLockKey);
        _ = await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ReleaseMigrationLockAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            return;
        }

        await using NpgsqlCommand command = new("SELECT pg_advisory_unlock(@key);", npgsqlConnection);
        command.Parameters.AddWithValue("key", MigrationLockKey);
        _ = await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

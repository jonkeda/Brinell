using Microsoft.Data.Sqlite;

namespace Brinell.Samples.Todo.Infrastructure.Data;

/// <summary>
/// The app's SQLite file, and the migrations that shape it.
/// </summary>
/// <remarks>
/// <para>
/// The schema version is SQLite's <c>user_version</c>. Each migration runs in its own
/// transaction and moves the version by one, so a crash mid-upgrade leaves the file at the last
/// complete version rather than half-way.
/// </para>
/// <para>
/// Test fixtures seed a database with exactly this class, so a seeded file can never have a
/// schema the app does not.
/// </para>
/// </remarks>
public sealed class TodoDatabase(string path)
{
    /// <summary>The newest schema version.</summary>
    public const int LatestVersion = 2;

    private static readonly string[] Migrations =
    [
        // v1: the todos table.
        """
        CREATE TABLE todos (
            id          TEXT PRIMARY KEY,
            title       TEXT NOT NULL,
            notes       TEXT NULL,
            due_date    TEXT NULL,
            status      TEXT NOT NULL,
            created_at  TEXT NOT NULL,
            updated_at  TEXT NOT NULL,
            sync_state  TEXT NOT NULL
        );
        """,

        // v2: tombstones, so a delete survives a restart until the server has been told.
        """
        ALTER TABLE todos ADD COLUMN is_deleted INTEGER NOT NULL DEFAULT 0;
        """,
    ];

    private readonly SemaphoreSlim _migrationGate = new(1, 1);
    private bool _migrated;

    /// <summary>The file's path.</summary>
    public string Path { get; } = path;

    /// <summary>Opens a connection, migrating the file to <see cref="LatestVersion"/> the first time.</summary>
    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        if (!_migrated)
        {
            await MigrateAsync(LatestVersion, cancellationToken).ConfigureAwait(false);
        }

        return await OpenRawAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Migrates the file up to <paramref name="targetVersion"/>, creating it if needed.
    /// </summary>
    /// <remarks>A lower target than the file's version does nothing: there are no down-migrations.</remarks>
    public async Task MigrateAsync(int targetVersion = LatestVersion, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(targetVersion, LatestVersion);

        await _migrationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var connection = await OpenRawAsync(cancellationToken).ConfigureAwait(false);
            var version = await ReadVersionAsync(connection, cancellationToken).ConfigureAwait(false);

            for (var next = version + 1; next <= targetVersion; next++)
            {
                await using var transaction = (SqliteTransaction)await connection
                    .BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

                await ExecuteAsync(connection, transaction, Migrations[next - 1], cancellationToken).ConfigureAwait(false);
                await ExecuteAsync(connection, transaction, $"PRAGMA user_version = {next};", cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            _migrated = targetVersion == LatestVersion || version >= LatestVersion;
        }
        finally
        {
            _migrationGate.Release();
        }
    }

    /// <summary>The file's schema version; 0 for a new file.</summary>
    public async Task<int> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenRawAsync(cancellationToken).ConfigureAwait(false);
        return await ReadVersionAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases pooled connections, so the file can be deleted or copied. Test clean-up.
    /// </summary>
    public static void ReleaseFileHandles() => SqliteConnection.ClearAllPools();

    private async Task<SqliteConnection> OpenRawAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString());

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private static async Task<int> ReadVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}

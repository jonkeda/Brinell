using System.Globalization;
using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Services;
using Microsoft.Data.Sqlite;

namespace Brinell.Samples.Todo.Infrastructure.Data;

/// <summary>
/// <see cref="ITodoRepository"/> over <see cref="TodoDatabase"/>.
/// </summary>
/// <remarks>
/// Times are stored as round-trip UTC strings and dates as <c>yyyy-MM-dd</c>, so the file reads
/// the same on every device and in every culture (TOD.03.2).
/// </remarks>
public sealed class SqliteTodoRepository(TodoDatabase database) : ITodoRepository
{
    private const string Columns = "id, title, notes, due_date, status, created_at, updated_at, sync_state, is_deleted";

    /// <inheritdoc />
    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM todos;";

        var items = new List<TodoItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(Read(reader));
        }

        return items;
    }

    /// <inheritdoc />
    public async Task<TodoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM todos WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Read(reader) : null;
    }

    /// <inheritdoc />
    public async Task SaveAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            INSERT INTO todos ({Columns})
            VALUES ($id, $title, $notes, $due, $status, $created, $updated, $sync, $deleted)
            ON CONFLICT(id) DO UPDATE SET
                title = excluded.title,
                notes = excluded.notes,
                due_date = excluded.due_date,
                status = excluded.status,
                created_at = excluded.created_at,
                updated_at = excluded.updated_at,
                sync_state = excluded.sync_state,
                is_deleted = excluded.is_deleted;
            """;

        command.Parameters.AddWithValue("$id", item.Id.ToString("D"));
        command.Parameters.AddWithValue("$title", item.Title);
        command.Parameters.AddWithValue("$notes", (object?)item.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$due", item.DueDate is { } due ? due.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : DBNull.Value);
        command.Parameters.AddWithValue("$status", item.Status.ToString());
        command.Parameters.AddWithValue("$created", WriteTime(item.CreatedAt));
        command.Parameters.AddWithValue("$updated", WriteTime(item.UpdatedAt));
        command.Parameters.AddWithValue("$sync", item.SyncState.ToString());
        command.Parameters.AddWithValue("$deleted", item.IsDeleted ? 1 : 0);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PurgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM todos WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static TodoItem Read(SqliteDataReader reader) => new()
    {
        Id = Guid.Parse(reader.GetString(0)),
        Title = reader.GetString(1),
        Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
        DueDate = reader.IsDBNull(3) ? null : DateOnly.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        Status = Enum.Parse<TodoStatus>(reader.GetString(4)),
        CreatedAt = ReadTime(reader.GetString(5)),
        UpdatedAt = ReadTime(reader.GetString(6)),
        SyncState = Enum.Parse<SyncState>(reader.GetString(7)),
        IsDeleted = reader.GetInt64(8) != 0,
    };

    private static string WriteTime(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ReadTime(string value)
        => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}

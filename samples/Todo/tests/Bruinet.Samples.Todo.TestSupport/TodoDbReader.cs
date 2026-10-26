using System.Globalization;
using Bruinet.Samples.Todo.Core.Models;
using Microsoft.Data.Sqlite;

namespace Bruinet.Samples.Todo.TestSupport;

/// <summary>
/// Reads the app's database file directly, read-only, for checks of what the app stored.
/// </summary>
/// <remarks>
/// Deliberately not the app's <c>SqliteTodoRepository</c>: a check that read through the code it
/// checks could pass because of the same bug. Read after a UI sentinel (the row is on screen), so
/// the app's write has landed.
/// </remarks>
public static class TodoDbReader
{
    /// <summary>Every todo in the file at <paramref name="databasePath"/>, tombstones included.</summary>
    public static IReadOnlyList<TodoItem> Todos(string databasePath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, title, notes, due_date, status, created_at, updated_at, sync_state, is_deleted FROM todos;";

        using var reader = command.ExecuteReader();
        var todos = new List<TodoItem>();
        while (reader.Read())
        {
            todos.Add(new TodoItem
            {
                Id = Guid.Parse(reader.GetString(0)),
                Title = reader.GetString(1),
                Notes = reader.IsDBNull(2) ? null : reader.GetString(2),
                DueDate = reader.IsDBNull(3) ? null : DateOnly.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Status = Enum.Parse<TodoStatus>(reader.GetString(4)),
                CreatedAt = DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                UpdatedAt = DateTimeOffset.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                SyncState = Enum.Parse<SyncState>(reader.GetString(7)),
                IsDeleted = reader.GetInt64(8) != 0,
            });
        }

        return todos;
    }
}

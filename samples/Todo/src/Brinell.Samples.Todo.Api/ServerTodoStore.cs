using System.Globalization;
using Brinell.Samples.Todo.Contracts;
using Microsoft.Data.Sqlite;

namespace Brinell.Samples.Todo.Api;

/// <summary>
/// The server's todos, in the server's own SQLite file.
/// </summary>
/// <remarks>
/// Deliberately separate from the app's storage code: the app and the server share only the wire
/// format (<see cref="TodoDto"/>), as they would if different teams wrote them.
/// </remarks>
public sealed class ServerTodoStore
{
    private readonly string _connectionString;

    /// <summary>Opens (and creates, if needed) the file at <paramref name="path"/>.</summary>
    public ServerTodoStore(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS todos (
                id          TEXT PRIMARY KEY,
                title       TEXT NOT NULL,
                notes       TEXT NULL,
                due_date    TEXT NULL,
                status      TEXT NOT NULL,
                created_at  TEXT NOT NULL,
                updated_at  TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    /// <summary>Every todo, oldest first.</summary>
    public IReadOnlyList<TodoDto> GetAll()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, title, notes, due_date, status, created_at, updated_at FROM todos ORDER BY created_at;";

        var todos = new List<TodoDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            todos.Add(Read(reader));
        }

        return todos;
    }

    /// <summary>One todo, or <c>null</c>.</summary>
    public TodoDto? Get(Guid id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, title, notes, due_date, status, created_at, updated_at FROM todos WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));

        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    /// <summary>
    /// Stores <paramref name="todo"/> unless the stored version is newer, and returns what is
    /// stored: last writer wins on <see cref="TodoDto.UpdatedAt"/>.
    /// </summary>
    public TodoDto Upsert(TodoDto todo)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO todos (id, title, notes, due_date, status, created_at, updated_at)
            VALUES ($id, $title, $notes, $due, $status, $created, $updated)
            ON CONFLICT(id) DO UPDATE SET
                title = excluded.title,
                notes = excluded.notes,
                due_date = excluded.due_date,
                status = excluded.status,
                updated_at = excluded.updated_at
            WHERE excluded.updated_at >= todos.updated_at;
            """;

        command.Parameters.AddWithValue("$id", todo.Id.ToString("D"));
        command.Parameters.AddWithValue("$title", todo.Title);
        command.Parameters.AddWithValue("$notes", (object?)todo.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$due", todo.DueDate is { } due ? due.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : DBNull.Value);
        command.Parameters.AddWithValue("$status", todo.Status.ToString());
        command.Parameters.AddWithValue("$created", WriteTime(todo.CreatedAt));
        command.Parameters.AddWithValue("$updated", WriteTime(todo.UpdatedAt));
        command.ExecuteNonQuery();

        return Get(todo.Id)!;
    }

    /// <summary>Deletes a todo; <c>false</c> when there was none.</summary>
    public bool Delete(Guid id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM todos WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        return command.ExecuteNonQuery() > 0;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static TodoDto Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        reader.GetString(1),
        reader.IsDBNull(2) ? null : reader.GetString(2),
        reader.IsDBNull(3) ? null : DateOnly.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        Enum.Parse<TodoStatusDto>(reader.GetString(4)),
        ReadTime(reader.GetString(5)),
        ReadTime(reader.GetString(6)));

    // Fixed-width UTC, so the SQL comparison in Upsert orders times correctly as text.
    private static string WriteTime(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);

    private static DateTimeOffset ReadTime(string value)
        => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
}

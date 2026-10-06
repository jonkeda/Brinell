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

            CREATE TABLE IF NOT EXISTS users (
                id                  TEXT PRIMARY KEY,
                name                TEXT NOT NULL,
                email               TEXT NULL,
                status              TEXT NOT NULL,
                allowed_until       TEXT NULL,
                role                TEXT NOT NULL,
                hours_per_week      INTEGER NOT NULL,
                hourly_rate_cents   INTEGER NOT NULL,
                start_minutes       INTEGER NOT NULL,
                receives_reminders  INTEGER NOT NULL,
                workload            INTEGER NOT NULL,
                notes               TEXT NULL,
                updated_at          TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private const string UserColumns =
        "id, name, email, status, allowed_until, role, hours_per_week, hourly_rate_cents, start_minutes, receives_reminders, workload, notes, updated_at";

    /// <summary>Every user, by name.</summary>
    public IReadOnlyList<UserDto> GetUsers()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {UserColumns} FROM users ORDER BY name COLLATE NOCASE;";

        var users = new List<UserDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            users.Add(ReadUser(reader));
        }

        return users;
    }

    /// <summary>One user, or <c>null</c>.</summary>
    public UserDto? GetUser(Guid id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {UserColumns} FROM users WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadUser(reader) : null;
    }

    /// <summary>
    /// Stores <paramref name="user"/> as sent and returns what is stored. The server is the
    /// authority for users, so the last write simply wins.
    /// </summary>
    public UserDto UpsertUser(UserDto user)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            INSERT INTO users ({UserColumns})
            VALUES ($id, $name, $email, $status, $until, $role, $hours, $rate, $start, $reminders, $workload, $notes, $updated)
            ON CONFLICT(id) DO UPDATE SET
                name = excluded.name,
                email = excluded.email,
                status = excluded.status,
                allowed_until = excluded.allowed_until,
                role = excluded.role,
                hours_per_week = excluded.hours_per_week,
                hourly_rate_cents = excluded.hourly_rate_cents,
                start_minutes = excluded.start_minutes,
                receives_reminders = excluded.receives_reminders,
                workload = excluded.workload,
                notes = excluded.notes,
                updated_at = excluded.updated_at;
            """;

        command.Parameters.AddWithValue("$id", user.Id.ToString("D"));
        command.Parameters.AddWithValue("$name", user.Name.Trim());
        command.Parameters.AddWithValue("$email", (object?)user.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", user.Status.ToString());
        command.Parameters.AddWithValue("$until", user.AllowedUntil is { } until ? until.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : DBNull.Value);
        command.Parameters.AddWithValue("$role", user.Role.ToString());
        command.Parameters.AddWithValue("$hours", user.HoursPerWeek);
        command.Parameters.AddWithValue("$rate", decimal.ToInt64(user.HourlyRate * 100m));
        command.Parameters.AddWithValue("$start", (int)user.StartOfDay.ToTimeSpan().TotalMinutes);
        command.Parameters.AddWithValue("$reminders", user.ReceivesReminders ? 1 : 0);
        command.Parameters.AddWithValue("$workload", user.Workload);
        command.Parameters.AddWithValue("$notes", (object?)user.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$updated", WriteTime(user.UpdatedAt));
        command.ExecuteNonQuery();

        return GetUser(user.Id)!;
    }

    private static UserDto ReadUser(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        reader.GetString(1),
        reader.IsDBNull(2) ? null : reader.GetString(2),
        Enum.Parse<UserStatusDto>(reader.GetString(3)),
        reader.IsDBNull(4) ? null : DateOnly.ParseExact(reader.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        Enum.Parse<UserRoleDto>(reader.GetString(5)),
        reader.GetInt32(6),
        reader.GetInt64(7) / 100m,
        TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(reader.GetInt32(8))),
        reader.GetInt64(9) != 0,
        reader.GetInt32(10),
        reader.IsDBNull(11) ? null : reader.GetString(11),
        ReadTime(reader.GetString(12)));

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

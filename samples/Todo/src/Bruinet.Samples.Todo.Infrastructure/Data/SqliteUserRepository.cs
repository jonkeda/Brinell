using System.Globalization;
using Bruinet.Samples.Todo.Core.Models;
using Bruinet.Samples.Todo.Core.Services;
using System.Data.Common;

namespace Bruinet.Samples.Todo.Infrastructure.Data;

/// <summary>
/// <see cref="IUserRepository"/> over <see cref="TodoDatabase"/>: the device's copy of the server's users.
/// </summary>
/// <remarks>
/// Dates are <c>yyyy-MM-dd</c> and times round-trip UTC strings, as for todos. The rate is stored
/// as whole cents and the start of day as minutes after midnight.
/// </remarks>
public sealed class SqliteUserRepository(TodoDatabase database) : IUserRepository
{
    private const string Columns =
        "id, name, email, status, allowed_until, role, hours_per_week, hourly_rate_cents, start_minutes, receives_reminders, workload, notes, updated_at";

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM users;";

        var users = new List<User>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            users.Add(Read(reader));
        }

        return users;
    }

    /// <inheritdoc />
    public async Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM users WHERE id = $id;";
        command.AddParameter("$id", id.ToString("D"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Read(reader) : null;
    }

    /// <inheritdoc />
    public async Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = Upsert(connection, null, user);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ReplaceAllAsync(IReadOnlyList<User> users, CancellationToken cancellationToken = default)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var clear = connection.CreateCommand())
        {
            clear.Transaction = transaction;
            clear.CommandText = "DELETE FROM users;";
            await clear.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var user in users)
        {
            await using var insert = Upsert(connection, transaction, user);
            await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DbCommand Upsert(DbConnection connection, DbTransaction? transaction, User user)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"""
            INSERT INTO users ({Columns})
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

        command.AddParameter("$id", user.Id.ToString("D"));
        command.AddParameter("$name", user.Name);
        command.AddParameter("$email", (object?)user.Email ?? DBNull.Value);
        command.AddParameter("$status", user.Status.ToString());
        command.AddParameter("$until", user.AllowedUntil is { } until ? until.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : DBNull.Value);
        command.AddParameter("$role", user.Role.ToString());
        command.AddParameter("$hours", user.HoursPerWeek);
        command.AddParameter("$rate", decimal.ToInt64(decimal.Round(user.HourlyRate * 100m)));
        command.AddParameter("$start", (int)user.StartOfDay.ToTimeSpan().TotalMinutes);
        command.AddParameter("$reminders", user.ReceivesReminders ? 1 : 0);
        command.AddParameter("$workload", user.Workload);
        command.AddParameter("$notes", (object?)user.Notes ?? DBNull.Value);
        command.AddParameter("$updated", user.UpdatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        return command;
    }

    private static User Read(DbDataReader reader) => new()
    {
        Id = Guid.Parse(reader.GetString(0)),
        Name = reader.GetString(1),
        Email = reader.IsDBNull(2) ? null : reader.GetString(2),
        Status = Enum.Parse<UserStatus>(reader.GetString(3)),
        AllowedUntil = reader.IsDBNull(4) ? null : DateOnly.ParseExact(reader.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        Role = Enum.Parse<UserRole>(reader.GetString(5)),
        HoursPerWeek = reader.GetInt32(6),
        HourlyRate = reader.GetInt64(7) / 100m,
        StartOfDay = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(reader.GetInt32(8))),
        ReceivesReminders = reader.GetInt64(9) != 0,
        Workload = reader.GetInt32(10),
        Notes = reader.IsDBNull(11) ? null : reader.GetString(11),
        UpdatedAt = DateTimeOffset.Parse(reader.GetString(12), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
    };
}

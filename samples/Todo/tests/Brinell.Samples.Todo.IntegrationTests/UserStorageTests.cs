using Brinell.Samples.Todo.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace Brinell.Samples.Todo.IntegrationTests;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Integration")]
public sealed class UserStorageTests : IDisposable
{
    private readonly TempDatabase _file = new("users");

    public void Dispose() => _file.Dispose();

    [Fact]
    [Trait("Journey", "TOD.11.9")]
    public async Task UpgradingFromVersion2_KeepsEveryTodo_AndAddsAnEmptyUserCache()
    {
        var v2 = new TodoDatabase(_file.Path);
        await v2.MigrateAsync(targetVersion: 2);
        await using (var connection = new SqliteConnection($"Data Source={_file.Path}"))
        {
            await connection.OpenAsync();
            await using var insert = connection.CreateCommand();
            insert.CommandText =
                """
                INSERT INTO todos (id, title, notes, due_date, status, created_at, updated_at, sync_state, is_deleted)
                VALUES ('55555555-0000-0000-0000-000000000001', 'From v2', NULL, NULL, 'Open',
                        '2026-03-01T08:00:00.0000000+00:00', '2026-03-02T08:00:00.0000000+00:00', 'Pending', 0);
                """;
            await insert.ExecuteNonQueryAsync();
        }

        var database = new TodoDatabase(_file.Path);
        var todo = Assert.Single(await new SqliteTodoRepository(database).GetAllAsync());

        Assert.Equal("From v2", todo.Title);
        Assert.Equal(SyncState.Pending, todo.SyncState);
        Assert.Empty(await new SqliteUserRepository(database).GetAllAsync());
        Assert.Equal(3, await database.GetVersionAsync());
    }

    [Fact]
    [Trait("Journey", "TOD.11.4")]
    public async Task EveryUserField_RoundTrips()
    {
        var repository = new SqliteUserRepository(new TodoDatabase(_file.Path));
        var eva = new User
        {
            Id = Guid.NewGuid(),
            Name = "Eva",
            Email = "eva@example.com",
            Status = UserStatus.OnLeave,
            AllowedUntil = new DateOnly(2026, 12, 31),
            Role = UserRole.Lead,
            HoursPerWeek = 38,
            HourlyRate = 1250.75m,
            StartOfDay = new TimeOnly(8, 30),
            ReceivesReminders = false,
            Workload = 35,
            Notes = "Monday to Thursday",
            UpdatedAt = new DateTimeOffset(2026, 3, 2, 23, 59, 59, TimeSpan.FromHours(-5)).AddTicks(1234567),
        };

        await repository.SaveAsync(eva);
        var stored = await repository.GetAsync(eva.Id);

        Assert.Equal(eva with { UpdatedAt = eva.UpdatedAt.ToUniversalTime() }, stored! with { UpdatedAt = stored.UpdatedAt.ToUniversalTime() });
    }

    [Fact]
    [Trait("Journey", "TOD.11.9")]
    public async Task ReplaceAll_LeavesExactlyTheNewList()
    {
        var repository = new SqliteUserRepository(new TodoDatabase(_file.Path));
        await repository.SaveAsync(new User { Id = Guid.NewGuid(), Name = "Old" });

        await repository.ReplaceAllAsync([new User { Id = Guid.NewGuid(), Name = "Daan" }, new User { Id = Guid.NewGuid(), Name = "Eva" }]);

        Assert.Equal(["Daan", "Eva"], (await repository.GetAllAsync()).Select(user => user.Name).Order());
    }

    [Fact]
    [Trait("Journey", "TOD.11.1")]
    public async Task TheTeamTestSet_SeedsTheUserCache()
    {
        var testSet = TodoTestSet.Load("team");

        await testSet.SeedDatabaseAsync(_file.Path);

        var cached = await new SqliteUserRepository(new TodoDatabase(_file.Path)).GetAllAsync();
        Assert.Equal(["Daan", "Eva", "Finn", "Gus", "Hana"], cached.Select(user => user.Name).Order());
    }
}

using Brinell.Samples.Todo.Core.ViewModels;
using Brinell.Samples.Todo.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace Brinell.Samples.Todo.IntegrationTests;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Integration")]
public sealed class DatabaseTests : IDisposable
{
    private readonly TempDatabase _file = new("database");

    public void Dispose() => _file.Dispose();

    [Fact]
    [Trait("Journey", "TOD.09.1")]
    public async Task AFreshInstall_CreatesTheDatabaseAtTheLatestVersion()
    {
        var repository = new SqliteTodoRepository(new TodoDatabase(_file.Path));

        Assert.Empty(await repository.GetAllAsync());
        Assert.Equal(TodoDatabase.LatestVersion, await new TodoDatabase(_file.Path).GetVersionAsync());
    }

    [Fact]
    [Trait("Journey", "TOD.09.2")]
    public async Task UpgradingFromVersion1_KeepsEveryTodo()
    {
        var v1 = new TodoDatabase(_file.Path);
        await v1.MigrateAsync(targetVersion: 1);
        await using (var connection = new SqliteConnection($"Data Source={_file.Path}"))
        {
            await connection.OpenAsync();
            await using var insert = connection.CreateCommand();
            insert.CommandText =
                """
                INSERT INTO todos (id, title, notes, due_date, status, created_at, updated_at, sync_state)
                VALUES ('55555555-0000-0000-0000-000000000001', 'From v1', NULL, '2026-03-12', 'InProgress',
                        '2026-03-01T08:00:00.0000000+00:00', '2026-03-02T08:00:00.0000000+00:00', 'Synced');
                """;
            await insert.ExecuteNonQueryAsync();
        }

        var upgraded = await new SqliteTodoRepository(new TodoDatabase(_file.Path)).GetAllAsync();

        var todo = Assert.Single(upgraded);
        Assert.Equal("From v1", todo.Title);
        Assert.Equal(TodoStatus.InProgress, todo.Status);
        Assert.Equal(new DateOnly(2026, 3, 12), todo.DueDate);
        Assert.False(todo.IsDeleted);
        Assert.Equal(TodoDatabase.LatestVersion, await new TodoDatabase(_file.Path).GetVersionAsync());
    }

    [Fact]
    [Trait("Journey", "TOD.03.2")]
    public async Task EveryField_RoundTrips_WithTimesInUtc()
    {
        var repository = new SqliteTodoRepository(new TodoDatabase(_file.Path));
        var original = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = "Buy milk",
            Notes = "Oat — not dairy",
            DueDate = new DateOnly(2026, 12, 31),
            Status = TodoStatus.InProgress,
            CreatedAt = new DateTimeOffset(2026, 3, 1, 9, 30, 15, TimeSpan.FromHours(1)),
            UpdatedAt = new DateTimeOffset(2026, 3, 2, 23, 59, 59, TimeSpan.FromHours(-5)).AddTicks(1234567),
            SyncState = SyncState.Pending,
            IsDeleted = true,
        };

        await repository.SaveAsync(original);
        var read = await repository.GetAsync(original.Id);

        Assert.Equal(original, read);
        Assert.Equal(TimeSpan.Zero, read!.CreatedAt.Offset);
    }

    [Fact]
    [Trait("Journey", "TOD.06.4")]
    public async Task ATombstone_SurvivesARestart_AndStaysHidden()
    {
        using var host = await TodoHost.StartAsync("three-todos");
        var milk = host.Scenario.Local[0];

        await host.Todos.DeleteAsync(milk.Id);
        host.Restart();

        Assert.True((await host.Repository.GetAsync(milk.Id))!.IsDeleted);
        Assert.DoesNotContain(await host.Todos.GetVisibleAsync(), todo => todo.Id == milk.Id);
    }

    [Fact]
    [Trait("Journey", "TOD.09.3")]
    public async Task Todos_SurviveARestart_Unchanged()
    {
        using var host = await TodoHost.StartAsync("pending-local-change");
        var before = await host.LocalAsync();

        host.Restart();

        Assert.Equal(before.OrderBy(todo => todo.Id), (await host.LocalAsync()).OrderBy(todo => todo.Id));
    }

    [Fact]
    [Trait("Journey", "TOD.02.6")]
    public async Task ACreatedTodo_IsStoredLocally_WithItsOwnId_AndLocalOnly()
    {
        using var host = await TodoHost.StartAsync();

        var created = await host.Todos.CreateAsync("Buy milk", null, null, TodoStatus.Open);

        var stored = Assert.Single(await host.LocalAsync());
        Assert.Equal(created.Id, stored.Id);
        Assert.Equal(SyncState.LocalOnly, stored.SyncState);
        Assert.Equal(host.Scenario.Now, stored.CreatedAt);
    }

    [Fact]
    [Trait("Journey", "TOD.04.5")]
    public async Task AnUpdate_MovesUpdatedAt_AndKeepsCreatedAt_OnDisk()
    {
        using var host = await TodoHost.StartAsync("three-todos");
        var milk = host.Scenario.Local[0];

        await host.Todos.UpdateAsync(milk.Id, "Buy oat milk", null, null, TodoStatus.Open);
        host.Restart();

        var stored = (await host.Repository.GetAsync(milk.Id))!;
        Assert.Equal(milk.CreatedAt, stored.CreatedAt);
        Assert.Equal(host.Scenario.Now, stored.UpdatedAt);
    }

    [Fact]
    [Trait("Journey", "TOD.08.7")]
    public async Task ASave_IsAllOrNothing_ToAReaderOnAnotherConnection()
    {
        // The deterministic half of "killed during save": whatever moment a save is caught at, the
        // row holds one whole version, never a new title beside old notes. The real kill is manual
        // (manual/charters.md, CH-2).
        using var host = await TodoHost.StartAsync("three-todos");
        var milk = (await host.Repository.GetAsync(host.Scenario.Local[0].Id))!;
        var versionA = milk with { Title = "Version A", Notes = "Notes of A" };
        var versionB = milk with { Title = "Version B", Notes = "Notes of B" };
        await host.Repository.SaveAsync(versionA);

        var writer = Task.Run(async () =>
        {
            for (var i = 0; i < 100; i++)
            {
                await host.Repository.SaveAsync(i % 2 == 0 ? versionB : versionA);
            }
        });

        var seen = new HashSet<(string Title, string? Notes)>();
        while (!writer.IsCompleted)
        {
            var read = (await host.Repository.GetAsync(milk.Id))!;
            seen.Add((read.Title, read.Notes));
        }

        await writer;
        Assert.All(seen, pair => Assert.Equal($"Notes of {pair.Title[^1]}", pair.Notes));
    }

    [Fact]
    [Trait("Journey", "TOD.01.1")]
    public async Task TheComposition_ResolvesEveryViewModel()
    {
        using var host = await TodoHost.StartAsync();

        Assert.NotNull(host.Get<TodoListViewModel>());
        Assert.NotNull(host.Get<TodoDetailViewModel>());
        Assert.NotNull(host.Get<TodoEditViewModel>());
        Assert.Same(host.Get<TodoListViewModel>(), host.Get<TodoListViewModel>());
    }
}

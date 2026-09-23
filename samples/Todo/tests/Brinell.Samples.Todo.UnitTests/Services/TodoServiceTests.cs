namespace Brinell.Samples.Todo.UnitTests.Services;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Unit")]
public sealed class TodoServiceTests
{
    private readonly InMemoryTodoRepository _repository = new();
    private readonly Microsoft.Extensions.Time.Testing.FakeTimeProvider _clock = Build.Clock();
    private readonly TodoService _service;

    public TodoServiceTests()
    {
        _service = new TodoService(_repository, _clock);
    }

    [Fact]
    [Trait("Journey", "TOD.02.6")]
    public async Task Create_StoresATrimmedTodo_LocalOnly_WithAClientId()
    {
        var created = await _service.CreateAsync("  Buy milk  ", "  ", null, TodoStatus.Open);

        var stored = Assert.Single(_repository.Items);
        Assert.Equal(created, stored);
        Assert.NotEqual(Guid.Empty, stored.Id);
        Assert.Equal("Buy milk", stored.Title);
        Assert.Null(stored.Notes);
        Assert.Equal(SyncState.LocalOnly, stored.SyncState);
        Assert.Equal(Build.Now, stored.CreatedAt);
    }

    [Fact]
    [Trait("Journey", "TOD.02.3")]
    public async Task Create_WithAnInvalidTitle_Throws_AndStoresNothing()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(" ", null, null, TodoStatus.Open));

        Assert.Empty(_repository.Items);
    }

    [Fact]
    [Trait("Journey", "TOD.04.5")]
    public async Task Update_MovesUpdatedAt_NeverCreatedAt_AndMarksPending()
    {
        var todo = Build.Todo();
        _repository.With(todo);
        _clock.Advance(TimeSpan.FromHours(1));

        var updated = await _service.UpdateAsync(todo.Id, "Buy oat milk", null, null, TodoStatus.Open);

        Assert.Equal(todo.CreatedAt, updated.CreatedAt);
        Assert.Equal(Build.Now.AddHours(1), updated.UpdatedAt);
        Assert.Equal(SyncState.Pending, updated.SyncState);
    }

    [Fact]
    [Trait("Journey", "TOD.04.5")]
    public async Task Update_OfATodoNeverSent_StaysLocalOnly()
    {
        var todo = Build.Todo(sync: SyncState.LocalOnly);
        _repository.With(todo);

        var updated = await _service.UpdateAsync(todo.Id, "Renamed", null, null, TodoStatus.Open);

        Assert.Equal(SyncState.LocalOnly, updated.SyncState);
    }

    [Fact]
    [Trait("Journey", "TOD.05.5")]
    public async Task SetStatus_ToTheSameStatus_WritesNothing()
    {
        var todo = Build.Todo(status: TodoStatus.Done);
        _repository.With(todo);

        await _service.SetStatusAsync(todo.Id, TodoStatus.Done);

        Assert.Equal(0, _repository.Saves);
    }

    [Fact]
    [Trait("Journey", "TOD.06.4")]
    public async Task Delete_OfASyncedTodo_LeavesATombstoneForTheNextSync()
    {
        var todo = Build.Todo();
        _repository.With(todo);

        await _service.DeleteAsync(todo.Id);

        var tombstone = Assert.Single(_repository.Items);
        Assert.True(tombstone.IsDeleted);
        Assert.Equal(SyncState.Pending, tombstone.SyncState);
        Assert.Null(await _service.GetAsync(todo.Id));
        Assert.Empty(await _service.GetVisibleAsync());
    }

    [Fact]
    [Trait("Journey", "TOD.06.5")]
    public async Task Delete_OfATodoNeverSent_RemovesItAtOnce()
    {
        var todo = Build.Todo(sync: SyncState.LocalOnly);
        _repository.With(todo);

        await _service.DeleteAsync(todo.Id);

        Assert.Empty(_repository.Items);
    }

    [Fact]
    [Trait("Journey", "TOD.05.2")]
    public void Today_IsTheDateInTheDevicesZone()
    {
        _clock.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("UTC-10", TimeSpan.FromHours(-10), "UTC-10", "UTC-10"));

        Assert.Equal(new DateOnly(2026, 3, 9), _service.Today);
    }
}

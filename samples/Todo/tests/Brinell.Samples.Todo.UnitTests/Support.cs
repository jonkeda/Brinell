global using Brinell.Samples.Todo.Core.Models;
global using Brinell.Samples.Todo.Core.Rules;
global using Brinell.Samples.Todo.Core.Services;
global using Brinell.Samples.Todo.Core.Sync;
global using Brinell.Samples.Todo.Core.ViewModels;
global using NSubstitute;
global using Xunit;

using Microsoft.Extensions.Time.Testing;

namespace Brinell.Samples.Todo.UnitTests;

/// <summary>
/// A repository in a dictionary: the state a view model test arranges and then reads back.
/// </summary>
internal sealed class InMemoryTodoRepository : ITodoRepository
{
    private readonly Dictionary<Guid, TodoItem> _items = [];

    public int Saves { get; private set; }

    public IReadOnlyCollection<TodoItem> Items => _items.Values;

    public InMemoryTodoRepository With(params TodoItem[] items)
    {
        foreach (var item in items)
        {
            _items[item.Id] = item;
        }

        return this;
    }

    public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TodoItem>>(_items.Values.ToList());

    public Task<TodoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.GetValueOrDefault(id));

    public Task SaveAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        Saves++;
        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task PurgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _items.Remove(id);
        return Task.CompletedTask;
    }
}

/// <summary>Builders for the values the tests keep needing.</summary>
internal static class Build
{
    /// <summary>The instant every test is set at: 2026-03-10 09:00 UTC.</summary>
    public static readonly DateTimeOffset Now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The date of <see cref="Now"/>.</summary>
    public static readonly DateOnly Today = new(2026, 3, 10);

    public static FakeTimeProvider Clock()
    {
        var clock = new FakeTimeProvider(Now);
        clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        return clock;
    }

    public static TodoItem Todo(
        string title = "Buy milk",
        TodoStatus status = TodoStatus.Open,
        DateOnly? due = null,
        SyncState sync = SyncState.Synced,
        bool deleted = false,
        DateTimeOffset? updated = null,
        Guid? id = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Title = title,
            Status = status,
            DueDate = due,
            CreatedAt = Now.AddDays(-7),
            UpdatedAt = updated ?? Now.AddDays(-7),
            SyncState = sync,
            IsDeleted = deleted,
        };

    public static IConnectivityState Online(bool online = true)
    {
        var connectivity = Substitute.For<IConnectivityState>();
        connectivity.IsOnline.Returns(online);
        return connectivity;
    }
}

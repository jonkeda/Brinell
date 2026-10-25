using System.Diagnostics;
using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Services;

namespace Brinell.Samples.Todo.Web.UITests.Spike;

/// <summary>
/// The time one device's app spent in its repositories, for the SQL replay spike's measurement
/// (.my/sql-replay/plan.md, 0.4): what replay would take away.
/// </summary>
public sealed class DatabaseClock
{
    private long _ticks;
    private int _calls;

    public TimeSpan Elapsed => Stopwatch.GetElapsedTime(0, Interlocked.Read(ref _ticks));

    public int Calls => Volatile.Read(ref _calls);

    public void Reset()
    {
        Interlocked.Exchange(ref _ticks, 0);
        Interlocked.Exchange(ref _calls, 0);
    }

    public async Task<T> Time<T>(Func<Task<T>> call)
    {
        var start = Stopwatch.GetTimestamp();
        try
        {
            return await call().ConfigureAwait(false);
        }
        finally
        {
            Add(start);
        }
    }

    public async Task Time(Func<Task> call)
    {
        var start = Stopwatch.GetTimestamp();
        try
        {
            await call().ConfigureAwait(false);
        }
        finally
        {
            Add(start);
        }
    }

    private void Add(long start)
    {
        Interlocked.Add(ref _ticks, Stopwatch.GetTimestamp() - start);
        Interlocked.Increment(ref _calls);
    }
}

/// <summary><see cref="ITodoRepository"/> timed on a <see cref="DatabaseClock"/>.</summary>
public sealed class TimedTodoRepository(ITodoRepository inner, DatabaseClock clock) : ITodoRepository
{
    public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
        => clock.Time(() => inner.GetAllAsync(cancellationToken));

    public Task<TodoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => clock.Time(() => inner.GetAsync(id, cancellationToken));

    public Task SaveAsync(TodoItem item, CancellationToken cancellationToken = default)
        => clock.Time(() => inner.SaveAsync(item, cancellationToken));

    public Task PurgeAsync(Guid id, CancellationToken cancellationToken = default)
        => clock.Time(() => inner.PurgeAsync(id, cancellationToken));
}

/// <summary><see cref="IUserRepository"/> timed on a <see cref="DatabaseClock"/>.</summary>
public sealed class TimedUserRepository(IUserRepository inner, DatabaseClock clock) : IUserRepository
{
    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => clock.Time(() => inner.GetAllAsync(cancellationToken));

    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => clock.Time(() => inner.GetAsync(id, cancellationToken));

    public Task SaveAsync(User user, CancellationToken cancellationToken = default)
        => clock.Time(() => inner.SaveAsync(user, cancellationToken));

    public Task ReplaceAllAsync(IReadOnlyList<User> users, CancellationToken cancellationToken = default)
        => clock.Time(() => inner.ReplaceAllAsync(users, cancellationToken));
}

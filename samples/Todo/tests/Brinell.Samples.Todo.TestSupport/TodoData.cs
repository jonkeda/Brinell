using Brinell.Samples.Todo.Contracts;

namespace Brinell.Samples.Todo.TestSupport;

/// <summary>
/// Builds todos for a test to add on top of a test set: the real <see cref="TodoDto"/>, with
/// dates from the test set's pinned now and ids of their own.
/// </summary>
/// <remarks>
/// Ids count up for the whole run (<c>99999999-…-000000000001</c>, <c>…02</c>), so they are the
/// same on every run in the same order, and never collide with a test set's ids or with a todo an
/// earlier scenario left on the device.
/// </remarks>
/// <param name="now">The test set's now; created and updated times are set to it.</param>
public sealed class TodoData(DateTimeOffset now)
{
    private static int _next;

    /// <summary>A todo with <paramref name="title"/> and whatever else the test names.</summary>
    public TodoDto Todo(string title, TodoStatusDto status = TodoStatusDto.Open, DateOnly? dueDate = null, string? notes = null)
        => new(NextId(), title, notes, dueDate, status, now, now);

    private static Guid NextId() => new($"99999999-0000-0000-0000-{Interlocked.Increment(ref _next):D12}");
}

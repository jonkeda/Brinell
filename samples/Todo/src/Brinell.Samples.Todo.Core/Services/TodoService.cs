using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Rules;

namespace Brinell.Samples.Todo.Core.Services;

/// <summary>
/// What the user can do to todos, over the local repository.
/// </summary>
/// <remarks>
/// Every change is local first and marked for the next sync; nothing here talks to the server.
/// </remarks>
public sealed class TodoService(ITodoRepository repository, TimeProvider time)
{
    /// <summary>Today, in the device's time zone.</summary>
    public DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    /// <summary>The todos a list shows: no tombstones.</summary>
    public async Task<IReadOnlyList<TodoItem>> GetVisibleAsync(CancellationToken cancellationToken = default)
        => (await repository.GetAllAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => !item.IsDeleted)
            .ToList();

    /// <summary>One todo, or <c>null</c> when it does not exist or is deleted.</summary>
    public async Task<TodoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return item is { IsDeleted: false } ? item : null;
    }

    /// <summary>Creates a todo. It is local only until the next sync.</summary>
    /// <exception cref="ArgumentException">The title is not valid.</exception>
    public async Task<TodoItem> CreateAsync(
        string title,
        string? notes,
        DateOnly? dueDate,
        TodoStatus status,
        CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid(title);

        var now = time.GetUtcNow();
        var item = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Notes = Normalize(notes),
            DueDate = dueDate,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now,
            SyncState = SyncState.LocalOnly,
        };

        await repository.SaveAsync(item, cancellationToken).ConfigureAwait(false);
        return item;
    }

    /// <summary>Changes a todo's content. <see cref="TodoItem.CreatedAt"/> never changes.</summary>
    /// <exception cref="ArgumentException">The title is not valid.</exception>
    /// <exception cref="KeyNotFoundException">The todo does not exist, or is deleted.</exception>
    public async Task<TodoItem> UpdateAsync(
        Guid id,
        string title,
        string? notes,
        DateOnly? dueDate,
        TodoStatus status,
        CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid(title);

        var existing = await RequireAsync(id, cancellationToken).ConfigureAwait(false);
        var updated = Touch(existing) with
        {
            Title = title.Trim(),
            Notes = Normalize(notes),
            DueDate = dueDate,
            Status = status,
        };

        await repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return updated;
    }

    /// <summary>Changes only the status.</summary>
    /// <exception cref="KeyNotFoundException">The todo does not exist, or is deleted.</exception>
    public async Task<TodoItem> SetStatusAsync(Guid id, TodoStatus status, CancellationToken cancellationToken = default)
    {
        var existing = await RequireAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing.Status == status)
        {
            return existing;
        }

        var updated = Touch(existing) with { Status = status };
        await repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return updated;
    }

    /// <summary>
    /// Deletes a todo: a tombstone the next sync sends to the server, or - for a todo the server
    /// never had - gone at once.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null or { IsDeleted: true })
        {
            return;
        }

        if (existing.SyncState == SyncState.LocalOnly)
        {
            await repository.PurgeAsync(id, cancellationToken).ConfigureAwait(false);
            return;
        }

        await repository.SaveAsync(Touch(existing) with { IsDeleted = true }, cancellationToken).ConfigureAwait(false);
    }

    private static void ThrowIfInvalid(string title)
    {
        if (TodoValidation.ValidateTitle(title) is { } error)
        {
            throw new ArgumentException(error, nameof(title));
        }
    }

    private static string? Normalize(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    private TodoItem Touch(TodoItem item) => item with
    {
        UpdatedAt = time.GetUtcNow(),
        SyncState = item.SyncState == SyncState.LocalOnly ? SyncState.LocalOnly : SyncState.Pending,
    };

    private async Task<TodoItem> RequireAsync(Guid id, CancellationToken cancellationToken)
        => await GetAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Todo {id} does not exist.");
}

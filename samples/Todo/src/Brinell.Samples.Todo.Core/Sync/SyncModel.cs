using Brinell.Samples.Todo.Core.Models;

namespace Brinell.Samples.Todo.Core.Sync;

/// <summary>Why a sync did not complete.</summary>
public enum SyncError
{
    /// <summary>It completed.</summary>
    None,

    /// <summary>The device is offline; nothing was attempted.</summary>
    Offline,

    /// <summary>The server could not be reached.</summary>
    Unreachable,

    /// <summary>The server took too long to answer.</summary>
    Timeout,

    /// <summary>The server refused the API key (401).</summary>
    Unauthorized,

    /// <summary>The server failed (5xx) or answered with something unexpected.</summary>
    ServerError,
}

/// <summary>
/// What a sync did.
/// </summary>
/// <param name="Error">Why it stopped, or <see cref="SyncError.None"/>.</param>
/// <param name="At">When it finished.</param>
/// <param name="Pushed">Todos sent with <c>PUT</c>.</param>
/// <param name="Deleted">Todos deleted on the server.</param>
/// <param name="Pulled">Todos stored from the server's list.</param>
/// <param name="Removed">Local todos removed because the server no longer has them.</param>
public sealed record SyncResult(
    SyncError Error,
    DateTimeOffset At,
    int Pushed = 0,
    int Deleted = 0,
    int Pulled = 0,
    int Removed = 0)
{
    /// <summary>Whether the sync completed.</summary>
    public bool Succeeded => Error == SyncError.None;
}

/// <summary>
/// A call to the server failed; <see cref="Error"/> says how.
/// </summary>
public sealed class TodoApiException(SyncError error, string message, Exception? inner = null)
    : Exception(message, inner)
{
    /// <summary>How the call failed.</summary>
    public SyncError Error { get; } = error;
}

/// <summary>
/// The server, as the sync sees it. Implemented over HTTP in Infrastructure.
/// </summary>
/// <remarks>Every failure is a <see cref="TodoApiException"/>.</remarks>
public interface ITodoApi
{
    /// <summary>Every todo on the server.</summary>
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores <paramref name="item"/> and returns what the server kept - which is the server's
    /// own version when that one is newer.
    /// </summary>
    Task<TodoItem> PutAsync(TodoItem item, CancellationToken cancellationToken = default);

    /// <summary>Deletes a todo. One the server does not have counts as deleted.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Runs a sync: push local changes, then pull the server's list.</summary>
public interface ISyncService
{
    /// <summary>The last result, or <c>null</c> before the first sync.</summary>
    SyncResult? LastResult { get; }

    /// <summary>Syncs. Never throws for a server or network failure; the result says what happened.</summary>
    Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default);
}

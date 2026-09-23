namespace Brinell.Samples.Todo.Core.Models;

/// <summary>
/// A todo as the app stores it: the user's data plus what the sync needs to know.
/// </summary>
public sealed record TodoItem
{
    /// <summary>Chosen by the client when the todo is created, and kept on the server.</summary>
    public required Guid Id { get; init; }

    /// <summary>The title; required, at most <see cref="Rules.TodoValidation.MaxTitleLength"/> characters.</summary>
    public required string Title { get; init; }

    /// <summary>Free text, or <c>null</c>.</summary>
    public string? Notes { get; init; }

    /// <summary>The due date, or <c>null</c> for none.</summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>Where the todo is in its cycle.</summary>
    public TodoStatus Status { get; init; }

    /// <summary>When it was created, UTC. Never changes.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When it last changed, UTC. The newer one wins a sync conflict.</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Whether the server has this version.</summary>
    public SyncState SyncState { get; init; }

    /// <summary>
    /// Deleted locally and waiting for the server to be told: a tombstone. Hidden from every list.
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>Whether the next sync has something to send for this todo.</summary>
    public bool NeedsPush => SyncState != SyncState.Synced;
}

/// <summary>A todo's stored status. Overdue is not a status; see <see cref="DisplayStatus"/>.</summary>
public enum TodoStatus
{
    /// <summary>Not started.</summary>
    Open,

    /// <summary>Being worked on.</summary>
    InProgress,

    /// <summary>Finished.</summary>
    Done,
}

/// <summary>
/// What the status control shows: the stored status, or Overdue when a todo that is not done is
/// past its due date.
/// </summary>
public enum DisplayStatus
{
    /// <summary>Not started.</summary>
    Open,

    /// <summary>Being worked on.</summary>
    InProgress,

    /// <summary>Finished.</summary>
    Done,

    /// <summary>Not done, and past its due date.</summary>
    Overdue,
}

/// <summary>Whether the server has the current version of a todo.</summary>
public enum SyncState
{
    /// <summary>The server has exactly this version.</summary>
    Synced,

    /// <summary>Changed (or deleted) locally since the last sync.</summary>
    Pending,

    /// <summary>Created locally and never sent: the server has never heard of it.</summary>
    LocalOnly,
}

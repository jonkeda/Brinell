using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Sync;

namespace Brinell.Samples.Todo.Core.Rules;

/// <summary>The list's status filter.</summary>
public enum TodoFilter
{
    /// <summary>Every todo.</summary>
    All,

    /// <summary>Open todos only.</summary>
    Open,

    /// <summary>Todos in progress only.</summary>
    InProgress,

    /// <summary>Done todos only.</summary>
    Done,
}

/// <summary>What the list page shows instead of, or as, its rows.</summary>
public enum ListState
{
    /// <summary>Nothing to show yet: the first load or sync is running.</summary>
    Loading,

    /// <summary>No todos (in this filter).</summary>
    Empty,

    /// <summary>The last sync failed in a way the user has to act on.</summary>
    Error,

    /// <summary>The rows.</summary>
    Content,
}

/// <summary>
/// Filtering, ordering and the list page's state.
/// </summary>
public static class TodoListRules
{
    /// <summary>The filter names, in the order the picker shows them.</summary>
    public static IReadOnlyList<string> FilterNames { get; } = ["All", "Open", "In progress", "Done"];

    /// <summary>Whether <paramref name="item"/> passes <paramref name="filter"/>.</summary>
    public static bool Matches(TodoItem item, TodoFilter filter) => filter switch
    {
        TodoFilter.All => true,
        TodoFilter.Open => item.Status == TodoStatus.Open,
        TodoFilter.InProgress => item.Status == TodoStatus.InProgress,
        TodoFilter.Done => item.Status == TodoStatus.Done,
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null),
    };

    /// <summary>
    /// Not done first, then by due date with undated last, then by title. Deleted todos are left out.
    /// </summary>
    public static IReadOnlyList<TodoItem> Arrange(IEnumerable<TodoItem> items, TodoFilter filter)
        => items
            .Where(item => !item.IsDeleted && Matches(item, filter))
            .OrderBy(item => item.Status == TodoStatus.Done)
            .ThenBy(item => item.DueDate is null)
            .ThenBy(item => item.DueDate)
            .ThenBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>
    /// The list page's state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Only a server error or a refused key is an Error state.</b> The app works offline, so
    /// an unreachable server or a timeout leaves the rows on screen and says so in the sync line;
    /// hiding the user's own data because the network is slow would be the wrong trade.
    /// </para>
    /// <para>
    /// Loading shows only while there is nothing to show: a refresh keeps the rows visible.
    /// </para>
    /// </remarks>
    public static ListState StateFor(bool isLoading, int visibleCount, SyncError lastSyncError)
    {
        if (lastSyncError is SyncError.ServerError or SyncError.Unauthorized)
        {
            return ListState.Error;
        }

        if (visibleCount > 0)
        {
            return ListState.Content;
        }

        return isLoading ? ListState.Loading : ListState.Empty;
    }

    /// <summary>The text of the Error state.</summary>
    public static string ErrorMessage(SyncError error) => error switch
    {
        SyncError.Unauthorized => "The server refused the app's key. Check the API key and try again.",
        SyncError.ServerError => "The server had a problem. Try again.",
        _ => string.Empty,
    };
}

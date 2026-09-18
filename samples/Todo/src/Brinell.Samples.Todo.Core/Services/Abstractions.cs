using Brinell.Samples.Todo.Core.Models;

namespace Brinell.Samples.Todo.Core.Services;

/// <summary>
/// Local storage for todos. SQLite in the app; an in-memory fake in unit tests.
/// </summary>
public interface ITodoRepository
{
    /// <summary>Every todo, tombstones included.</summary>
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>One todo, tombstone or not, or <c>null</c>.</summary>
    Task<TodoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inserts or replaces a todo.</summary>
    Task SaveAsync(TodoItem item, CancellationToken cancellationToken = default);

    /// <summary>Removes a todo for good. Removing one that is not there does nothing.</summary>
    Task PurgeAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Whether the app is online.
/// </summary>
/// <remarks>
/// The app's implementation combines the platform's connectivity with the test network-state
/// file; <see cref="Changed"/> is raised on the UI thread there.
/// </remarks>
public interface IConnectivityState
{
    /// <summary>Whether the app may talk to the server.</summary>
    bool IsOnline { get; }

    /// <summary>Raised when <see cref="IsOnline"/> may have changed.</summary>
    event EventHandler? Changed;
}

/// <summary>
/// Page navigation, so view models do not know about Shell.
/// </summary>
public interface INavigator
{
    /// <summary>Opens <paramref name="route"/> with the given query parameters.</summary>
    Task GoToAsync(string route, IReadOnlyDictionary<string, object>? parameters = null);

    /// <summary>Returns to the previous page.</summary>
    Task GoBackAsync();
}

/// <summary>
/// Dialogs, so view models do not know about <c>DisplayAlert</c>.
/// </summary>
public interface IDialogs
{
    /// <summary>Asks a yes/no question; <c>true</c> for <paramref name="accept"/>.</summary>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);

    /// <summary>Tells the user something.</summary>
    Task AlertAsync(string title, string message, string ok = "OK");
}

/// <summary>The app's routes and their parameter names.</summary>
public static class TodoRoutes
{
    /// <summary>The list, the Shell root.</summary>
    public const string List = "todos";

    /// <summary>A todo's detail page; takes <see cref="IdParameter"/>.</summary>
    public const string Detail = "detail";

    /// <summary>The edit page; takes <see cref="IdParameter"/>, or nothing for a new todo.</summary>
    public const string Edit = "edit";

    /// <summary>The todo id query parameter.</summary>
    public const string IdParameter = "id";

    /// <summary>The parameters naming one todo.</summary>
    public static IReadOnlyDictionary<string, object> ForTodo(Guid id)
        => new Dictionary<string, object> { [IdParameter] = id.ToString("D") };

    /// <summary>Reads the todo id from query parameters, or <c>null</c> when there is none.</summary>
    public static Guid? ReadId(IDictionary<string, object> parameters)
        => parameters.TryGetValue(IdParameter, out var value) && Guid.TryParse(value?.ToString(), out var id)
            ? id
            : null;
}

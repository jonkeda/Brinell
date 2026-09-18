using Brinell.Samples.Todo.Core.Services;

namespace Brinell.Samples.Todo.App.Services;

/// <summary><see cref="INavigator"/> over Shell.</summary>
public sealed class ShellNavigator : INavigator
{
    /// <inheritdoc />
    public Task GoToAsync(string route, IReadOnlyDictionary<string, object>? parameters = null)
        => parameters is null
            ? Shell.Current.GoToAsync(route)
            : Shell.Current.GoToAsync(route, new Dictionary<string, object>(parameters));

    /// <inheritdoc />
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}

/// <summary><see cref="IDialogs"/> over the current page's alerts.</summary>
public sealed class ShellDialogs : IDialogs
{
    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        => CurrentPage.DisplayAlertAsync(title, message, accept, cancel);

    /// <inheritdoc />
    public Task AlertAsync(string title, string message, string ok = "OK")
        => CurrentPage.DisplayAlertAsync(title, message, ok);

    private static Page CurrentPage => Shell.Current.CurrentPage ?? Shell.Current;
}

/// <summary>
/// The device's connectivity. The network-state file used under test is layered on top of this
/// by <c>AddTodoServices</c>.
/// </summary>
public sealed class DeviceConnectivity : IConnectivityState
{
    /// <summary>Starts listening to the platform.</summary>
    public DeviceConnectivity()
    {
        Connectivity.Current.ConnectivityChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    /// <remarks>
    /// Anything but "no network at all" counts: the development API runs on this machine, which
    /// a <c>Local</c>-only connection still reaches.
    /// </remarks>
    public bool IsOnline => Connectivity.Current.NetworkAccess != NetworkAccess.None;
}

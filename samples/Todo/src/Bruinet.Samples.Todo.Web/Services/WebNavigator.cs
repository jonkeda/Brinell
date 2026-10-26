using Bruinet.Samples.Todo.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Bruinet.Samples.Todo.Web.Services;

/// <summary>
/// <see cref="INavigator"/> over Blazor's <see cref="NavigationManager"/>, with Shell's back stack.
/// </summary>
/// <remarks>
/// <para>
/// A singleton, because the list view model that uses it is one: this host is one device, as
/// the MAUI app is. The layout attaches each new circuit, and the last attached one wins, so two
/// browser tabs on one host share one app. Run two hosts for two devices.
/// </para>
/// <para>
/// The stack follows the address bar, so the browser's own back button and
/// <see cref="GoBackAsync"/> agree. Going back is the browser's <c>history.back()</c>, as Shell
/// pops: navigating forward to the previous URL would leave the page behind it in the history.
/// </para>
/// </remarks>
public sealed class WebNavigator : INavigator
{
    private readonly List<string> _stack = [];
    private readonly Lock _gate = new();
    private NavigationManager? _navigation;
    private IJSRuntime? _js;

    /// <summary>Makes <paramref name="navigation"/> (a new circuit's) the one this navigator drives.</summary>
    public void Attach(NavigationManager navigation, IJSRuntime js)
    {
        lock (_gate)
        {
            if (_navigation is not null)
            {
                _navigation.LocationChanged -= OnLocationChanged;
            }

            _navigation = navigation;
            _js = js;
            _navigation.LocationChanged += OnLocationChanged;
            _stack.Clear();
            _stack.Add(Relative(navigation, navigation.Uri));
        }
    }

    /// <summary>Stops following <paramref name="navigation"/> when its circuit goes away.</summary>
    public void Detach(NavigationManager navigation)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_navigation, navigation))
            {
                _navigation.LocationChanged -= OnLocationChanged;
                _navigation = null;
                _js = null;
            }
        }
    }

    /// <inheritdoc />
    public Task GoToAsync(string route, IReadOnlyDictionary<string, object>? parameters = null)
    {
        Navigation.NavigateTo(Url(route, parameters));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task GoBackAsync()
    {
        bool hasPrevious;
        IJSRuntime? js;
        lock (_gate)
        {
            hasPrevious = _stack.Count > 1;
            js = _js;
        }

        if (hasPrevious && js is not null)
        {
            await js.InvokeVoidAsync("history.back");
            return;
        }

        // Opened straight on this page: there is nothing behind it but the list.
        Navigation.NavigateTo("/");
    }

    /// <summary>A route and its parameters as a URL: <c>/detail?id=…</c>.</summary>
    public static string Url(string route, IReadOnlyDictionary<string, object>? parameters = null)
    {
        var path = "/" + route.TrimStart('/');
        if (parameters is null || parameters.Count == 0)
        {
            return path;
        }

        var query = string.Join("&", parameters.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value.ToString() ?? string.Empty)}"));
        return $"{path}?{query}";
    }

    private NavigationManager Navigation
        => _navigation ?? throw new InvalidOperationException("No page is open: the navigator has no circuit attached.");

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var location = Relative((NavigationManager)sender!, e.Location);
        lock (_gate)
        {
            if (_stack.Count > 0 && _stack[^1] == location)
            {
                return;
            }

            if (_stack.Count > 1 && _stack[^2] == location)
            {
                _stack.RemoveAt(_stack.Count - 1);
                return;
            }

            _stack.Add(location);
        }
    }

    private static string Relative(NavigationManager navigation, string uri) => "/" + navigation.ToBaseRelativePath(uri);
}

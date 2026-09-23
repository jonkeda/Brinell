namespace Brinell.Maui.Interfaces;

// R9: MAUI owns this contract and does not derive from Core's pages. See
// .docs/decisions/ad-010-maui-ahead-of-core.md.
/// <summary>
/// A page of the MAUI app under test: a root scope with load and busy state. Checks
/// (<c>Is*</c>, <c>Get*</c>) answer about now; waits (<c>Wait*</c>, <c>Assert*</c>) take a timeout.
/// </summary>
public interface IMauiPage : IMauiElementScope
{
    /// <summary>The page's name; its root is located by this AutomationId.</summary>
    string Name { get; }

    /// <summary>Whether the page is loaded now.</summary>
    bool IsLoaded();

    /// <summary>Waits until the loaded state matches <paramref name="expected"/>. Null skips.</summary>
    bool WaitLoaded(bool? expected, int? timeoutMs = null);

    /// <summary>Asserts the loaded state. Null skips.</summary>
    void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>Whether the page reports itself busy now.</summary>
    bool IsBusy();

    /// <summary>Waits until the busy state matches <paramref name="expected"/>. Null skips.</summary>
    bool WaitBusy(bool? expected, int? timeoutMs = null);

    /// <summary>Asserts that the page becomes idle.</summary>
    void AssertIdle(string? message = null, int? timeoutMs = null);

    /// <summary>The page's title.</summary>
    string? GetTitle();

    /// <summary>Waits until the title matches <paramref name="expected"/>. Null skips.</summary>
    bool WaitTitle(string? expected, int? timeoutMs = null);

    /// <summary>Asserts the title. Null skips.</summary>
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>Saves a screenshot of the app.</summary>
    void TakeScreenshot(string? filename = null);
}

/// <summary>
/// A page with a fluent self type. Pages are root scopes: they have no <c>Parent</c>.
/// </summary>
/// <typeparam name="TSelf">The page type itself (self-referencing).</typeparam>
public interface IMauiPage<TSelf> : IMauiPage, IMauiScope<TSelf>
    where TSelf : IMauiPage<TSelf>
{
}

namespace Bruinet.NativeAndroid.Interfaces;

// R9: NativeAndroid owns this contract and does not derive from Core's IPageObject. UAT and test
// composition still find NativeAndroid pages: PageObjectBase carries Core's [TestPage].

/// <summary>One screen of the app: loaded, busy and titled as the other stacks' pages are.</summary>
public interface INativeAndroidPage : INativeAndroidElementScope
{
    /// <summary>The page's name in logs and messages.</summary>
    string Name { get; }

    /// <summary>The activity the page is shown in (<c>.LoginActivity</c>); null when any will do.</summary>
    string? Activity { get; }

    /// <summary>Whether the page is shown now.</summary>
    bool IsLoaded();

    /// <summary>Waits until the page's loaded state matches <paramref name="expected"/>; null skips.</summary>
    bool WaitLoaded(bool? expected, int? timeoutMs = null);

    /// <summary>Fails unless the page's loaded state matches <paramref name="expected"/> within the budget; null skips.</summary>
    void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>Whether the page is busy now.</summary>
    bool IsBusy();

    /// <summary>Waits until the page's busy state matches <paramref name="expected"/>; null skips.</summary>
    bool WaitBusy(bool? expected, int? timeoutMs = null);

    /// <summary>Fails unless the page is ready (loaded and not busy) within the budget.</summary>
    void AssertIdle(string? message = null, int? timeoutMs = null);

    /// <summary>The page's title: its toolbar title, else its activity.</summary>
    string? GetTitle();

    /// <summary>Waits until the title is <paramref name="expected"/>; null skips.</summary>
    bool WaitTitle(string? expected, int? timeoutMs = null);

    /// <summary>Fails unless the title is <paramref name="expected"/> within the budget; null skips.</summary>
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>Saves the screen as a PNG file.</summary>
    void TakeScreenshot(string? filename = null);
}

/// <summary>A page that returns itself, so its controls' actions chain back to it.</summary>
/// <typeparam name="TSelf">The page's own type.</typeparam>
public interface INativeAndroidPage<TSelf> : INativeAndroidPage, INativeAndroidScope<TSelf>
    where TSelf : INativeAndroidPage<TSelf>
{
}

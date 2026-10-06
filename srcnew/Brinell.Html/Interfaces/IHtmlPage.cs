namespace Brinell.Html.Interfaces;

// R9: Html owns this contract and does not derive from Core's IPageObject. UAT and test
// composition still find Html pages: HtmlPageObjectBase carries Core's [TestPage].

/// <summary>
/// A page: a scope with a name, a load state and a title.
/// </summary>
public interface IHtmlPage : IHtmlElementScope
{
    /// <summary>The page's name, for messages.</summary>
    string Name { get; }

    /// <summary>The page's path, when it has one.</summary>
    string? Url { get; }

    /// <summary>Whether the page is loaded now.</summary>
    bool IsLoaded();

    /// <summary>Waits for the page to be loaded, or not.</summary>
    bool WaitLoaded(bool? expected, int? timeoutMs = null);

    /// <summary>Fails when the page's load state does not reach <paramref name="expected"/>.</summary>
    void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>Whether the page reports itself busy now (<c>data-busy</c>).</summary>
    bool IsBusy();

    /// <summary>Waits for the page to be busy, or not.</summary>
    bool WaitBusy(bool? expected, int? timeoutMs = null);

    /// <summary>Fails when the page does not become ready (loaded and idle).</summary>
    void AssertIdle(string? message = null, int? timeoutMs = null);

    /// <summary>The document title now.</summary>
    string? GetTitle();

    /// <summary>Waits for the document title.</summary>
    bool WaitTitle(string? expected, int? timeoutMs = null);

    /// <summary>Fails when the document title does not reach <paramref name="expected"/>.</summary>
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>Saves a screenshot of the page.</summary>
    void TakeScreenshot(string? filename = null);
}

/// <summary>A page that returns itself from its members, for fluent chains.</summary>
public interface IHtmlPage<TSelf> : IHtmlPage, IHtmlScope<TSelf>
    where TSelf : IHtmlPage<TSelf>
{
}

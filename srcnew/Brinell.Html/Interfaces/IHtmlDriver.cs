using Brinell.Core.Interfaces;
using Brinell.Core.Locators;

namespace Brinell.Html.Interfaces;

// ?A2 in .my/html/plan.md: the driver is its own object, as in MAUI, so the context and the
// driver can later carry different jobs (the frame model, the "browser is gone" check).

/// <summary>
/// The browser, as the Html stack talks to it: lookups, navigation, screenshots.
/// </summary>
public interface IHtmlDriver : IDiagnosticDriver, IDisposable
{
    /// <summary>The document's body, the root every page-level lookup starts from.</summary>
    IHtmlElement PageRoot { get; }

    /// <summary>The page's URL now.</summary>
    string CurrentUrl { get; }

    /// <summary>The document title now.</summary>
    string PageTitle { get; }

    /// <summary>Whether the page is idle now.</summary>
    bool IsIdle();

    /// <summary>Every matching element in the document. One attempt; never waits.</summary>
    IReadOnlyList<IHtmlElement> FindElements(Locator locator);

    /// <summary>Opens a URL.</summary>
    void GoTo(string url);

    /// <summary>Goes back in the browser history.</summary>
    void GoBack();

    /// <summary>Goes forward in the browser history.</summary>
    void GoForward();

    /// <summary>Reloads the page.</summary>
    void Reload();

    /// <summary>A screenshot of the page, as PNG bytes.</summary>
    byte[] TakeScreenshot();

    /// <summary>Saves a screenshot of the page to <paramref name="path"/>.</summary>
    void SaveScreenshot(string path);
}

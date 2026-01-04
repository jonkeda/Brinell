namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for WebView controls.
/// </summary>
public interface IWebViewControlObject : IControlObject
{
    /// <summary>
    /// Gets the current URL.
    /// </summary>
    string? GetCurrentUrl(int? timeoutMs = null);

    /// <summary>
    /// Asserts the current URL matches the expected value.
    /// </summary>
    void AssertCurrentUrl(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Asserts the current URL contains the specified substring.
    /// </summary>
    void AssertUrlContains(string? substring, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Navigates to the specified URL.
    /// </summary>
    void NavigateTo(string? url, int? timeoutMs = null);

    /// <summary>
    /// Navigates back.
    /// </summary>
    void GoBack(int? timeoutMs = null);

    /// <summary>
    /// Navigates forward.
    /// </summary>
    void GoForward(int? timeoutMs = null);

    /// <summary>
    /// Refreshes the page.
    /// </summary>
    void Refresh(int? timeoutMs = null);

    /// <summary>
    /// Checks if the back navigation is available.
    /// </summary>
    bool CanGoBack(int? timeoutMs = null);

    /// <summary>
    /// Checks if the forward navigation is available.
    /// </summary>
    bool CanGoForward(int? timeoutMs = null);

    /// <summary>
    /// Gets whether the page is currently loading.
    /// </summary>
    bool IsLoading(int? timeoutMs = null);

    /// <summary>
    /// Waits for the page to finish loading.
    /// </summary>
    bool WaitLoaded(int? timeoutMs = null);

    /// <summary>
    /// Asserts the page is loaded.
    /// </summary>
    void AssertLoaded(string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets the page title.
    /// </summary>
    string? GetTitle(int? timeoutMs = null);

    /// <summary>
    /// Asserts the page title matches the expected value.
    /// </summary>
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Executes JavaScript in the WebView context.
    /// </summary>
    string? ExecuteJavaScript(string? script, int? timeoutMs = null);
}

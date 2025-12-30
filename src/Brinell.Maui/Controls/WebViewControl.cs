using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI WebView control wrapper.
/// Provides web content navigation functionality.
/// </summary>
public class WebViewControl : ControlBase
{
    public WebViewControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public WebViewControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the current URL.
    /// </summary>
    public string? GetCurrentUrl()
    {
        var element = FindElement();
        return element?.GetAttribute("url") ?? element?.GetAttribute("source");
    }

    /// <summary>
    /// Get the page title.
    /// </summary>
    public string? GetTitle()
    {
        var element = FindElement();
        return element?.GetAttribute("title");
    }

    /// <summary>
    /// Check if the WebView is loading.
    /// </summary>
    public bool IsLoading()
    {
        var element = FindElement();
        var loading = element?.GetAttribute("isLoading") ?? element?.GetAttribute("loading");
        return loading?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Navigate to a URL.
    /// Note: Direct URL navigation may require app-side implementation.
    /// </summary>
    /// <param name="url">The URL to navigate to.</param>
    public void NavigateTo(string url)
    {
        LogAction("NavigateTo", url);
        Log($"NavigateTo: '{url}' - requires app-side WebView.Source = new Uri(url)");
    }

    /// <summary>
    /// Navigate back in WebView history.
    /// </summary>
    public void GoBack()
    {
        LogAction("GoBack");
        Log("GoBack: requires app-side WebView.GoBack()");
    }

    /// <summary>
    /// Navigate forward in WebView history.
    /// </summary>
    public void GoForward()
    {
        LogAction("GoForward");
        Log("GoForward: requires app-side WebView.GoForward()");
    }

    /// <summary>
    /// Reload the current page.
    /// </summary>
    public void Reload()
    {
        LogAction("Reload");
        Log("Reload: requires app-side WebView.Reload()");
    }

    /// <summary>
    /// Wait for page to finish loading.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    public bool WaitForPageLoad(int? timeoutMs = null)
    {
        Log("WaitForPageLoad()");
        return _context.WaitFor(() => !IsLoading(), timeoutMs, "page load complete");
    }

    #region Assert Methods

    /// <summary>
    /// Assert the current URL.
    /// </summary>
    public void AssertUrl(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetCurrentUrl();
        if (actual != expected)
        {
            ThrowAssertionFailed("Url", actual ?? "(null)", expected,
                message ?? $"Expected URL '{expected}' but got '{actual}'.");
        }
        LogAssertPass("Url", actual ?? "(null)", expected);
    }

    /// <summary>
    /// Assert the URL contains expected text.
    /// </summary>
    public void AssertUrlContains(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetCurrentUrl() ?? string.Empty;
        if (!actual.Contains(expected))
        {
            ThrowAssertionFailed("UrlContains", actual, $"contains '{expected}'",
                message ?? $"Expected URL to contain '{expected}' but got '{actual}'.");
        }
        LogAssertPass("UrlContains", actual, expected);
    }

    /// <summary>
    /// Assert the page title.
    /// </summary>
    public void AssertTitle(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetTitle();
        if (actual != expected)
        {
            ThrowAssertionFailed("Title", actual ?? "(null)", expected,
                message ?? $"Expected title '{expected}' but got '{actual}'.");
        }
        LogAssertPass("Title", actual ?? "(null)", expected);
    }

    /// <summary>
    /// Assert WebView is not loading.
    /// </summary>
    public void AssertLoaded(string? message = null)
    {
        CheckVisible(expected: true);
        if (IsLoading())
        {
            ThrowAssertionFailed("Loaded", "loading", "loaded",
                message ?? "Expected WebView to be loaded but it is still loading.");
        }
        LogAssertPass("Loaded", "loaded", "loaded");
    }

    #endregion
}

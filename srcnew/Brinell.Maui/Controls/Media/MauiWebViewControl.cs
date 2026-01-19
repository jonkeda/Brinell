namespace Brinell.Maui.Controls.Media;

/// <summary>
/// MAUI WebView control for displaying web content.
/// Provides methods for URL navigation and web content interaction.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiWebViewControl<TScope> : MauiControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a WebView control with locator.
    /// </summary>
    public MauiWebViewControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a WebView control with automation ID.
    /// </summary>
    public MauiWebViewControl(IMauiScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    #region WebView Methods

    /// <summary>
    /// Gets the current URL of the WebView.
    /// </summary>
    /// <returns>The current URL, or null if not available.</returns>
    public string? GetUrl()
    {
        var element = TryFindElement();
        if (element == null) return null;

        // Try common attributes for URL
        var url = element.GetAttribute("url");
        if (!string.IsNullOrEmpty(url)) return url;

        url = element.GetAttribute("Source");
        if (!string.IsNullOrEmpty(url)) return url;

        return null;
    }

    /// <summary>
    /// Gets the title of the currently loaded page.
    /// </summary>
    /// <returns>The page title, or null if not available.</returns>
    public string? GetPageTitle()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var title = element.GetAttribute("title");
        if (!string.IsNullOrEmpty(title)) return title;

        return element.Text;
    }

    /// <summary>
    /// Checks if the WebView can navigate back.
    /// </summary>
    /// <returns>True if can go back, false otherwise, null if unknown.</returns>
    public bool? CanGoBack()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var attr = element.GetAttribute("CanGoBack");
        if (!string.IsNullOrEmpty(attr))
        {
            return attr.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return null;
    }

    /// <summary>
    /// Checks if the WebView can navigate forward.
    /// </summary>
    /// <returns>True if can go forward, false otherwise, null if unknown.</returns>
    public bool? CanGoForward()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var attr = element.GetAttribute("CanGoForward");
        if (!string.IsNullOrEmpty(attr))
        {
            return attr.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return null;
    }

    /// <summary>
    /// Asserts that the current URL contains the specified text.
    /// </summary>
    /// <param name="expectedUrlPart">The expected URL part.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertUrlContains(string expectedUrlPart, string? message = null)
    {
        return RunAssert(nameof(AssertUrlContains), expectedUrlPart, () =>
        {
            var url = GetUrl();
            return url != null && url.Contains(expectedUrlPart, StringComparison.OrdinalIgnoreCase)
                ? expectedUrlPart
                : url;
        }, message);
    }

    #endregion
}

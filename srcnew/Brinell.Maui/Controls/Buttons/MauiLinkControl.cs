namespace Brinell.Maui.Controls.Buttons;

/// <summary>
/// MAUI hyperlink/link control for clickable links.
/// Typically a Label with GestureRecognizer or a custom hyperlink control.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiLinkControl<TScope> : MauiClickableControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a Link control with locator.
    /// </summary>
    public MauiLinkControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a Link control with automation ID.
    /// </summary>
    public MauiLinkControl(IMauiScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    #region Link Properties

    /// <summary>
    /// Gets the display text of the link.
    /// </summary>
    /// <returns>The link text, or null if not available.</returns>
    public string? GetLinkText()
    {
        return GetText();
    }

    /// <summary>
    /// Gets the URL/destination of the link (if available).
    /// </summary>
    /// <returns>The link URL, or null if not available.</returns>
    public string? GetUrl()
    {
        var element = TryFindElement();
        if (element == null) return null;

        // Try common attributes for URL
        var url = element.GetAttribute("Url");
        if (!string.IsNullOrEmpty(url)) return url;

        url = element.GetAttribute("href");
        if (!string.IsNullOrEmpty(url)) return url;

        url = element.GetAttribute("NavigateUri");
        if (!string.IsNullOrEmpty(url)) return url;

        return null;
    }

    /// <summary>
    /// Asserts that the link text equals the expected value.
    /// </summary>
    /// <param name="expected">The expected link text.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertLinkTextEquals(string expected, string? message = null)
    {
        return RunAssert(nameof(AssertLinkTextEquals), expected, () => GetLinkText(), message);
    }

    /// <summary>
    /// Asserts that the URL contains the expected text.
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

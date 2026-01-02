using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Async text control for Playwright (input, textarea elements).
/// Provides non-blocking text input and retrieval.
/// </summary>
public class TextControlAsync : ControlBaseAsync
{
    /// <summary>
    /// Create a new async text control.
    /// </summary>
    public TextControlAsync(PlaywrightTestContext context, IPageObject? page, string automationId, string selector)
        : base(context, page, automationId, selector)
    {
    }

    /// <summary>
    /// Get the current value of the text input.
    /// </summary>
    public new async ValueTask<string> GetTextAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var element = await _context.Page.QuerySelectorAsync(_selector);
            if (element == null) return string.Empty;

            var value = await element.GetAttributeAsync("value");
            return value ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Set the text value.
    /// </summary>
    public async ValueTask SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        var element = await _context.Page.QuerySelectorAsync(_selector);
        if (element == null)
        {
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");
        }

        await element.FillAsync(text);
        LogAction("SetText", text);
    }

    /// <summary>
    /// Clear the text value.
    /// </summary>
    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        var element = await _context.Page.QuerySelectorAsync(_selector);
        if (element == null)
        {
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");
        }

        await element.FillAsync(string.Empty);
        LogAction("Clear");
    }

    /// <summary>
    /// Append text to the existing value.
    /// </summary>
    public async ValueTask AppendAsync(string text, CancellationToken cancellationToken = default)
    {
        var current = await GetTextAsync(cancellationToken);
        await SetTextAsync(current + text, cancellationToken);
        LogAction("Append", text);
    }

    /// <summary>
    /// Clear and set new text.
    /// </summary>
    public async ValueTask ClearAndSetAsync(string text, CancellationToken cancellationToken = default)
    {
        await ClearAsync(cancellationToken);
        await SetTextAsync(text, cancellationToken);
    }

    /// <summary>
    /// Assert text equals expected value.
    /// </summary>
    public new async ValueTask AssertTextEqualsAsync(string expected, string? message = null, CancellationToken cancellationToken = default)
    {
        var actual = await GetTextAsync(cancellationToken);

        if (actual == expected)
        {
            LogAssertPass("TextEquals", actual, expected);
        }
        else
        {
            ThrowAssertionFailed(
                "TextEquals",
                actual,
                expected,
                message ?? $"Control '{AutomationId}' text mismatch. Expected: '{expected}', Actual: '{actual}'");
        }
    }
}

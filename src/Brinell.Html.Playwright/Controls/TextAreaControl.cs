using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright textarea control wrapper.
/// Works with &lt;textarea&gt; elements.
/// </summary>
public class TextAreaControl : TextControlBase
{
    public TextAreaControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TextAreaControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public TextAreaControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the rows attribute if set.
    /// </summary>
    public int? GetRows()
    {
        return GetRowsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the rows attribute asynchronously.
    /// </summary>
    public async Task<int?> GetRowsAsync()
    {
        var rowsAttr = await GetAttributeAsync("rows");
        if (int.TryParse(rowsAttr, out var result))
            return result;
        return null;
    }

    /// <summary>
    /// Get the cols attribute if set.
    /// </summary>
    public int? GetCols()
    {
        return GetColsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the cols attribute asynchronously.
    /// </summary>
    public async Task<int?> GetColsAsync()
    {
        var colsAttr = await GetAttributeAsync("cols");
        if (int.TryParse(colsAttr, out var result))
            return result;
        return null;
    }

    /// <summary>
    /// Get the current line count (based on content).
    /// </summary>
    public int GetLineCount()
    {
        return GetLineCountAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the current line count asynchronously.
    /// </summary>
    public async Task<int> GetLineCountAsync()
    {
        var text = await GetTextAsync();
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Split('\n').Length;
    }

    /// <summary>
    /// Append text to the existing content.
    /// </summary>
    public void AppendText(string text)
    {
        AppendTextAsync(text).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Append text to the existing content asynchronously.
    /// </summary>
    public async Task AppendTextAsync(string text)
    {
        LogAction("AppendText", text);
        var locator = GetLocator();
        var current = await locator.InputValueAsync();
        await locator.FillAsync(current + text);
    }

    /// <summary>
    /// Assert rows attribute equals expected.
    /// </summary>
    public void AssertRows(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetRows();
        if (actual != expected)
        {
            ThrowAssertionFailed("Rows", actual?.ToString() ?? "(null)", expected.ToString(),
                message ?? $"Expected rows '{expected}' but got '{actual}' for '{AutomationId}'.");
        }
        LogAssertPass("Rows", actual?.ToString() ?? "(null)", expected.ToString());
    }

    /// <summary>
    /// Assert line count equals expected.
    /// </summary>
    public void AssertLineCount(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetLineCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("LineCount", actual.ToString(), expected.ToString(),
                message ?? $"Expected {expected} lines but got {actual} for '{AutomationId}'.");
        }
        LogAssertPass("LineCount", actual.ToString(), expected.ToString());
    }
}

using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright select dropdown control wrapper.
/// Works with &lt;select&gt; elements.
/// </summary>
public class SelectControl : SelectorControlBase
{
    public SelectControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public SelectControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public SelectControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get all selected options (for multi-select).
    /// </summary>
    public IReadOnlyList<string> GetSelectedItems()
    {
        return GetSelectedItemsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get all selected options asynchronously (for multi-select).
    /// </summary>
    public async Task<IReadOnlyList<string>> GetSelectedItemsAsync()
    {
        var locator = GetLocator();
        var selectedOptions = locator.Locator("option:checked");
        var count = await selectedOptions.CountAsync();
        var items = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var text = await selectedOptions.Nth(i).TextContentAsync();
            items.Add(text ?? string.Empty);
        }
        return items;
    }

    /// <summary>
    /// Get all selected values (for multi-select).
    /// </summary>
    public async Task<IReadOnlyList<string>> GetSelectedValuesAsync()
    {
        var locator = GetLocator();
        var result = await locator.EvaluateAsync<string[]>(
            "el => Array.from(el.selectedOptions).map(o => o.value)");
        return result ?? Array.Empty<string>();
    }

    /// <summary>
    /// Deselect all options (for multi-select).
    /// </summary>
    public void DeselectAll()
    {
        DeselectAllAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Deselect all options asynchronously (for multi-select).
    /// </summary>
    public async Task DeselectAllAsync()
    {
        LogAction("DeselectAll");
        var locator = GetLocator();
        await locator.EvaluateAsync("el => { for (let o of el.options) o.selected = false; }");
    }

    /// <summary>
    /// Select multiple options by value (for multi-select).
    /// </summary>
    public void SelectMultiple(params string[] values)
    {
        SelectMultipleAsync(values).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Select multiple options by value asynchronously (for multi-select).
    /// </summary>
    public async Task SelectMultipleAsync(params string[] values)
    {
        LogAction("SelectMultiple", string.Join(", ", values));
        var locator = GetLocator();
        await locator.SelectOptionAsync(values);
    }

    /// <summary>
    /// Check if an option with the specified value exists.
    /// </summary>
    public bool HasOption(string value)
    {
        return HasOptionAsync(value).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Check if an option with the specified value exists asynchronously.
    /// </summary>
    public async Task<bool> HasOptionAsync(string value)
    {
        var locator = GetLocator();
        var option = locator.Locator($"option[value='{value}']");
        return await option.CountAsync() > 0;
    }

    /// <summary>
    /// Check if an option with the specified text exists.
    /// </summary>
    public bool HasOptionText(string text)
    {
        return HasOptionTextAsync(text).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Check if an option with the specified text exists asynchronously.
    /// </summary>
    public async Task<bool> HasOptionTextAsync(string text)
    {
        var items = await GetItemsAsync();
        return items.Contains(text);
    }

    /// <summary>
    /// Get selected option text (alias for GetText).
    /// </summary>
    public override string GetText()
    {
        return GetSelectedText() ?? string.Empty;
    }

    /// <summary>
    /// Get selected option text asynchronously.
    /// </summary>
    public override async Task<string> GetTextAsync()
    {
        return await GetSelectedTextAsync() ?? string.Empty;
    }

    /// <summary>
    /// Assert has option with value.
    /// </summary>
    public void AssertHasOption(string value, string? message = null)
    {
        CheckVisible(expected: true);
        if (!HasOption(value))
        {
            ThrowAssertionFailed("HasOption", "false", $"option '{value}'",
                message ?? $"Expected select '{AutomationId}' to have option with value '{value}'.");
        }
        LogAssertPass("HasOption", value, value);
    }

    /// <summary>
    /// Assert item count equals expected.
    /// </summary>
    public void AssertItemCount(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetItemCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("ItemCount", actual.ToString(), expected.ToString(),
                message ?? $"Expected {expected} options but got {actual} for '{AutomationId}'.");
        }
        LogAssertPass("ItemCount", actual.ToString(), expected.ToString());
    }
}

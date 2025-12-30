using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls.Base;

/// <summary>
/// Playwright base class for selector controls (select dropdowns, comboboxes).
/// </summary>
public abstract class SelectorControlBase : ControlBase, ISelectorControl
{
    protected SelectorControlBase(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected SelectorControlBase(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected SelectorControlBase(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the selected item text.
    /// </summary>
    public virtual string? GetSelectedText()
    {
        return GetSelectedTextAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the selected item text asynchronously.
    /// </summary>
    public virtual async Task<string?> GetSelectedTextAsync()
    {
        var locator = GetLocator();
        var selectedOption = locator.Locator("option:checked");
        var count = await selectedOption.CountAsync();
        if (count == 0) return null;
        return await selectedOption.First.TextContentAsync();
    }

    /// <summary>
    /// Get the selected item's value attribute.
    /// </summary>
    public virtual string? GetSelectedValue()
    {
        return GetSelectedValueAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the selected item's value attribute asynchronously.
    /// </summary>
    public virtual async Task<string?> GetSelectedValueAsync()
    {
        var locator = GetLocator();
        return await locator.InputValueAsync();
    }

    /// <summary>
    /// Get the selected item index.
    /// </summary>
    public virtual int GetSelectedIndex()
    {
        return GetSelectedIndexAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the selected item index asynchronously.
    /// </summary>
    public virtual async Task<int> GetSelectedIndexAsync()
    {
        var locator = GetLocator();
        var result = await locator.EvaluateAsync<int>("el => el.selectedIndex");
        return result;
    }

    /// <summary>
    /// Select an item by index.
    /// </summary>
    public virtual void SelectByIndex(int index)
    {
        SelectByIndexAsync(index).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Select an item by index asynchronously.
    /// </summary>
    public virtual async Task SelectByIndexAsync(int index)
    {
        LogAction("SelectByIndex", index.ToString());
        var locator = GetLocator();
        await locator.SelectOptionAsync(new SelectOptionValue { Index = index });
    }

    /// <summary>
    /// Select an item by text.
    /// </summary>
    public virtual void SelectByText(string text)
    {
        SelectByTextAsync(text).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Select an item by text asynchronously.
    /// </summary>
    public virtual async Task SelectByTextAsync(string text)
    {
        LogAction("SelectByText", text);
        var locator = GetLocator();
        await locator.SelectOptionAsync(new SelectOptionValue { Label = text });
    }

    /// <summary>
    /// Select an item by value attribute.
    /// </summary>
    public virtual void SelectByValue(string value)
    {
        SelectByValueAsync(value).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Select an item by value attribute asynchronously.
    /// </summary>
    public virtual async Task SelectByValueAsync(string value)
    {
        LogAction("SelectByValue", value);
        var locator = GetLocator();
        await locator.SelectOptionAsync(new SelectOptionValue { Value = value });
    }

    /// <summary>
    /// Get all item texts.
    /// </summary>
    public virtual IReadOnlyList<string> GetItems()
    {
        return GetItemsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get all item texts asynchronously.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetItemsAsync()
    {
        var locator = GetLocator();
        var options = locator.Locator("option");
        var count = await options.CountAsync();
        var items = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var text = await options.Nth(i).TextContentAsync();
            items.Add(text ?? string.Empty);
        }
        return items;
    }

    /// <summary>
    /// Get count of items.
    /// </summary>
    public virtual int GetItemCount()
    {
        return GetItemCountAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get count of items asynchronously.
    /// </summary>
    public virtual async Task<int> GetItemCountAsync()
    {
        var locator = GetLocator();
        var options = locator.Locator("option");
        return await options.CountAsync();
    }

    /// <summary>
    /// Check if this is a multi-select dropdown.
    /// </summary>
    public virtual bool IsMultiple()
    {
        return IsMultipleAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Check if this is a multi-select dropdown asynchronously.
    /// </summary>
    public virtual async Task<bool> IsMultipleAsync()
    {
        var locator = GetLocator();
        var multiple = await locator.GetAttributeAsync("multiple");
        return multiple != null;
    }

    /// <summary>
    /// Assert selected text equals expected.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertSelectedText(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSelectedText();
        if (actual != expected)
        {
            ThrowAssertionFailed("SelectedText", actual ?? "(null)", expected,
                message ?? $"Expected selected text '{expected}' but got '{actual}' for '{AutomationId}'.");
        }
        LogAssertPass("SelectedText", actual ?? "(null)", expected);
    }

    /// <summary>
    /// Assert selected text equals expected asynchronously.
    /// </summary>
    public virtual async Task AssertSelectedTextAsync(string expected, string? message = null)
    {
        await WaitVisibleAsync(expected: true);
        var actual = await GetSelectedTextAsync();
        if (actual != expected)
        {
            ThrowAssertionFailed("SelectedText", actual ?? "(null)", expected,
                message ?? $"Expected selected text '{expected}' but got '{actual}' for '{AutomationId}'.");
        }
        LogAssertPass("SelectedText", actual ?? "(null)", expected);
    }

    /// <summary>
    /// Assert selected value equals expected.
    /// </summary>
    public virtual void AssertSelectedValue(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSelectedValue();
        if (actual != expected)
        {
            ThrowAssertionFailed("SelectedValue", actual ?? "(null)", expected,
                message ?? $"Expected selected value '{expected}' but got '{actual}' for '{AutomationId}'.");
        }
        LogAssertPass("SelectedValue", actual ?? "(null)", expected);
    }

    /// <summary>
    /// Wait for visible asynchronously (helper for async assertions).
    /// </summary>
    protected async Task<bool> WaitVisibleAsync(bool expected = true, int? timeoutMs = null)
    {
        return await _context.WaitForAsync(
            async () => await IsVisibleAsync() == expected,
            timeoutMs,
            $"element '{AutomationId}' visible = {expected}");
    }
}

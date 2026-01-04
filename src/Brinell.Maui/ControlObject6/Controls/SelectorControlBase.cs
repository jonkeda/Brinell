using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for selector controls (Picker, ComboBox).
/// Provides item selection operations.
/// </summary>
public abstract class SelectorControlBase : ClickableControlBase, ISelectorControlObject
{
    /// <summary>
    /// Creates a new selector control.
    /// </summary>
    protected SelectorControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new selector control using AutomationId.
    /// </summary>
    protected SelectorControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    #region Selected Item

    /// <inheritdoc />
    public virtual int GetSelectedIndex(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var index = element.GetAttribute("SelectedIndex");
        return int.TryParse(index, out var i) ? i : -1;
    }

    /// <inheritdoc />
    public virtual string GetSelectedText(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        
        // Try SelectedItem first
        var selected = element.GetAttribute("SelectedItem");
        if (!string.IsNullOrEmpty(selected))
            return selected;

        // Fall back to element text (what's displayed)
        return element.Text ?? string.Empty;
    }

    /// <inheritdoc />
    public virtual void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedIndex(timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected selected index {expected}, but was {actual}",
                Locator.Value,
                "AssertSelectedIndex");
        }
    }

    /// <inheritdoc />
    public virtual void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedText(timeoutMs);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected selected text '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertSelectedText");
        }
    }

    #endregion

    #region Item Count

    /// <inheritdoc />
    public virtual int GetItemCount(int? timeoutMs = null)
    {
        // Default implementation - may need override for specific controls
        var items = GetItemTexts(timeoutMs);
        return items.Count;
    }

    /// <inheritdoc />
    public virtual void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetItemCount(timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected item count {expected}, but was {actual}",
                Locator.Value,
                "AssertItemCount");
        }
    }

    #endregion

    #region Select Actions

    /// <inheritdoc />
    public virtual void SelectByIndex(int? index, int? timeoutMs = null)
    {
        if (index is null) return;

        Log($"SelectByIndex({index})");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);

        PerformSelectByIndex(index.Value, timeoutMs);
    }

    /// <summary>
    /// Performs the select by index action. Override for control-specific behavior.
    /// </summary>
    protected virtual void PerformSelectByIndex(int index, int? timeoutMs = null)
    {
        // Default implementation: open picker, find item, click
        var items = GetItemTexts(timeoutMs);
        if (index < 0 || index >= items.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range (0-{items.Count - 1})");

        SelectByText(items[index], timeoutMs);
    }

    /// <inheritdoc />
    public virtual void SelectByText(string? text, int? timeoutMs = null)
    {
        if (text is null) return;

        Log($"SelectByText(\"{text}\")");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);

        PerformSelectByText(text, timeoutMs);
    }

    /// <summary>
    /// Performs the select by text action. Override for control-specific behavior.
    /// </summary>
    protected virtual void PerformSelectByText(string text, int? timeoutMs = null)
    {
        // Default implementation for MAUI Picker
        // Click to open the picker popup
        Click(timeoutMs);

        // Wait for popup and find the item
        Thread.Sleep(500); // Brief delay for popup animation

        // Find and click the item in the popup
        var itemLocator = MobileBy.XPath($"//*[contains(@text,'{text}') or contains(@Name,'{text}')]");
        var item = WaitFor(() =>
        {
            try { return (AppiumElement)Driver.FindElement(itemLocator); }
            catch { return null; }
        }, timeoutMs ?? DefaultTimeoutMs);

        if (item is null)
            throw new ElementNotFoundException($"Item '{text}' not found in selector");

        item.Click();
    }

    #endregion

    #region Items

    /// <inheritdoc />
    public virtual IReadOnlyList<string> GetItemTexts(int? timeoutMs = null)
    {
        // This typically requires opening the picker - implementation varies by control
        // Default: return empty list, override in derived classes
        return Array.Empty<string>();
    }

    /// <inheritdoc />
    public virtual bool HasItem(string text, int? timeoutMs = null)
    {
        var items = GetItemTexts(timeoutMs);
        return items.Contains(text);
    }

    #endregion
}

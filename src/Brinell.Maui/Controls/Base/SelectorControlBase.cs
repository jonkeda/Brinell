using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// MAUI base class for selector controls (Picker, etc.).
/// </summary>
public abstract class SelectorControlBase : ControlBase, ISelectorControl
{
    protected SelectorControlBase(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected SelectorControlBase(AppiumTestContext context, IPageObject? page, AppiumElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected SelectorControlBase(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the selected item text.
    /// </summary>
    public virtual string? GetSelectedText()
    {
        var element = FindElement();
        return element?.Text;
    }

    /// <summary>
    /// Get the selected item index. Returns -1 if unknown.
    /// </summary>
    public virtual int GetSelectedIndex()
    {
        // Most mobile pickers don't expose selected index directly
        return -1;
    }

    /// <summary>
    /// Select an item by index.
    /// </summary>
    public virtual void SelectByIndex(int index)
    {
        LogAction("SelectByIndex", index.ToString());
        
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for selection.");
        
        // Open the selector
        element.Click();
        Thread.Sleep(500); // Wait for picker to open
        
        PerformSelectByIndex(index);
    }

    /// <summary>
    /// Platform-specific selection by index.
    /// </summary>
    protected virtual void PerformSelectByIndex(int index)
    {
        // Try to find items and select by index
        var items = _context.Driver.Driver.FindElements(By.XPath("//*[@clickable='true']"));
        if (index < items.Count)
        {
            items[index].Click();
        }
    }

    /// <summary>
    /// Select an item by text.
    /// </summary>
    public virtual void SelectByText(string text)
    {
        LogAction("SelectByText", text);
        
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for selection.");
        
        // Open the selector
        element.Click();
        Thread.Sleep(500); // Wait for picker to open
        
        PerformSelectByText(text);
    }

    /// <summary>
    /// Platform-specific selection by text.
    /// </summary>
    protected virtual void PerformSelectByText(string text)
    {
        // Try to find item with matching text
        var item = _context.Driver.Driver.FindElements(
            By.XPath($"//*[@text='{text}' or @name='{text}' or contains(@content-desc, '{text}')]"))
            .FirstOrDefault();
        
        if (item != null)
        {
            item.Click();
        }
        else
        {
            throw new InvalidOperationException($"Item '{text}' not found in selector '{AutomationId}'.");
        }
    }

    /// <summary>
    /// Get all items. May not be fully available for virtualized lists.
    /// </summary>
    public virtual IReadOnlyList<string> GetItems()
    {
        // Most mobile pickers don't expose items until opened
        return Array.Empty<string>();
    }

    /// <summary>
    /// Get count of items. May not be available for all selectors.
    /// </summary>
    public virtual int GetItemCount()
    {
        return GetItems().Count;
    }

    #region Assert Methods

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
    /// Assert selected text contains expected.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertSelectedTextContains(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSelectedText() ?? string.Empty;
        if (!actual.Contains(expected))
        {
            ThrowAssertionFailed("SelectedTextContains", actual, $"contains '{expected}'",
                message ?? $"Expected selected text to contain '{expected}' but got '{actual}' for '{AutomationId}'.");
        }
        LogAssertPass("SelectedTextContains", actual, expected);
    }

    /// <summary>
    /// Assert selected index equals expected.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertSelectedIndex(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSelectedIndex();
        if (actual != expected)
        {
            ThrowAssertionFailed("SelectedIndex", actual.ToString(), expected.ToString(),
                message ?? $"Expected selected index {expected} but got {actual} for '{AutomationId}'.");
        }
        LogAssertPass("SelectedIndex", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert item count equals expected.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertItemCount(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetItemCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("ItemCount", actual.ToString(), expected.ToString(),
                message ?? $"Expected {expected} items but got {actual} for '{AutomationId}'.");
        }
        LogAssertPass("ItemCount", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert no item is selected.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertNoSelection(string? message = null)
    {
        CheckVisible(expected: true);
        var selected = GetSelectedText();
        if (!string.IsNullOrEmpty(selected))
        {
            ThrowAssertionFailed("NoSelection", selected, "(no selection)",
                message ?? $"Expected no selection but got '{selected}' for '{AutomationId}'.");
        }
        LogAssertPass("NoSelection", "(none)", "(no selection)");
    }

    #endregion
}

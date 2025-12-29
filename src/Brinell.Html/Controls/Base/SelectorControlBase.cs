using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls.Base;

/// <summary>
/// HTML/Selenium base class for selector controls (select dropdowns).
/// </summary>
public abstract class SelectorControlBase : ControlBase, ISelectorControl
{
    protected SelectorControlBase(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected SelectorControlBase(SeleniumTestContext context, IPageObject? page, IWebElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected SelectorControlBase(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the selected item text.
    /// </summary>
    public virtual string? GetSelectedText()
    {
        var element = FindElement();
        if (element == null) return null;
        
        var select = new SelectElement(element);
        return select.SelectedOption?.Text;
    }

    /// <summary>
    /// Get the selected item index.
    /// </summary>
    public virtual int GetSelectedIndex()
    {
        var element = FindElement();
        if (element == null) return -1;
        
        var select = new SelectElement(element);
        var options = select.Options;
        var selected = select.SelectedOption;
        
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] == selected) return i;
        }
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
        
        var select = new SelectElement(element);
        select.SelectByIndex(index);
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
        
        var select = new SelectElement(element);
        select.SelectByText(text);
    }

    /// <summary>
    /// Select an item by value attribute.
    /// </summary>
    public virtual void SelectByValue(string value)
    {
        LogAction("SelectByValue", value);
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for selection.");
        
        var select = new SelectElement(element);
        select.SelectByValue(value);
    }

    /// <summary>
    /// Get all item texts.
    /// </summary>
    public virtual IReadOnlyList<string> GetItems()
    {
        var element = FindElement();
        if (element == null) return Array.Empty<string>();
        
        var select = new SelectElement(element);
        return select.Options.Select(o => o.Text).ToList();
    }

    /// <summary>
    /// Get count of items.
    /// </summary>
    public virtual int GetItemCount()
    {
        var element = FindElement();
        if (element == null) return 0;
        
        var select = new SelectElement(element);
        return select.Options.Count;
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
    /// Check if this is a multi-select dropdown.
    /// </summary>
    public virtual bool IsMultiple()
    {
        var element = FindElement();
        if (element == null) return false;
        
        var select = new SelectElement(element);
        return select.IsMultiple;
    }
}

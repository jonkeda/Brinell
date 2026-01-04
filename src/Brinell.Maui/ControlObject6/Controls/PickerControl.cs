using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Control object for MAUI Picker elements.
/// </summary>
public class PickerControl : SelectorControlBase
{
    /// <summary>
    /// Creates a new PickerControl.
    /// </summary>
    public PickerControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new PickerControl using AutomationId.
    /// </summary>
    public PickerControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public override string GetSelectedText(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        
        // For Picker, the displayed text is the selected item
        var text = element.Text;
        if (!string.IsNullOrEmpty(text))
            return text;

        // Try SelectedItem attribute
        var selected = element.GetAttribute("SelectedItem");
        return selected ?? string.Empty;
    }

    /// <inheritdoc />
    protected override void PerformSelectByText(string text, int? timeoutMs = null)
    {
        Log($"PerformSelectByText(\"{text}\")");
        
        // Click to open the picker popup
        Click(timeoutMs);

        // Wait for popup animation
        Thread.Sleep(300);

        // Find the item in the popup - Windows typically uses ComboBox popup or ListView
        try
        {
            // Try finding in a list/popup
            var itemXPath = $"//*[@Name='{text}' or @text='{text}' or contains(@AutomationId,'{text}')]";
            var item = WaitFor(() =>
            {
                try { return (AppiumElement)Driver.FindElement(MobileBy.XPath(itemXPath)); }
                catch { return null; }
            }, timeoutMs ?? DefaultTimeoutMs);

            if (item is not null)
            {
                item.Click();
                return;
            }
        }
        catch
        {
            // Continue to fallback
        }

        // Fallback: try to find ListItem
        try
        {
            var listItemXPath = $"//ListItem[@Name='{text}'] | //*[@ClassName='ListBoxItem' and @Name='{text}']";
            var listItem = (AppiumElement)Driver.FindElement(MobileBy.XPath(listItemXPath));
            listItem.Click();
        }
        catch
        {
            throw new ElementNotFoundException($"Could not find item '{text}' in picker popup");
        }
    }

    /// <summary>
    /// Opens the picker dropdown.
    /// </summary>
    public void Open(int? timeoutMs = null)
    {
        Log("Open()");
        Click(timeoutMs);
    }

    /// <summary>
    /// Gets the title/placeholder of the picker.
    /// </summary>
    public string GetTitle(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.GetAttribute("Title") ?? string.Empty;
    }
}

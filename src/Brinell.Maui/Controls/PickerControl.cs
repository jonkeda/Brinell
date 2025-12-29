using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Picker control wrapper.
/// Equivalent to ComboBox/DropDown for MAUI applications.
/// </summary>
public class PickerControl : SelectorControlBase
{
    public PickerControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public PickerControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Platform-specific selection by text.
    /// Opens the picker and selects the matching item.
    /// </summary>
    protected override void PerformSelectByText(string text)
    {
        // Try to find and click the item with matching text
        var item = _context.Driver.Driver.FindElements(
            By.XPath($"//*[@text='{text}' or @name='{text}' or contains(@content-desc, '{text}')]"))
            .FirstOrDefault();
        
        if (item != null)
        {
            item.Click();
        }
        else
        {
            throw new InvalidOperationException($"Picker item '{text}' not found in '{AutomationId}'.");
        }
    }
}

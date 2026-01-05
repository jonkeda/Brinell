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
        // First click the picker to open the dropdown - it should already be open from base class
        // but search for the item in the dropdown
        var driver = _context.Driver.Driver;
        
        // Wait briefly for dropdown to appear
        Thread.Sleep(100);
        
        // On Windows MAUI, picker items appear as ListItem elements with Name attribute
        // Try multiple XPath patterns for compatibility
        var xpathPatterns = new[]
        {
            $"//ListItem[@Name='{text}']",
            $"//*[@Name='{text}' and (self::ListItem or self::List/*)]",
            $"//*[@text='{text}' or @name='{text}']"
        };
        
        foreach (var xpath in xpathPatterns)
        {
            try
            {
                var items = driver.FindElements(By.XPath(xpath));
                if (items.Count > 0)
                {
                    items[0].Click();
                    return;
                }
            }
            catch { }
        }
        
        // Fallback: Use keyboard to find item by typing first letter(s)
        // This is a last resort for pickers that don't expose items properly
        var actions = new OpenQA.Selenium.Interactions.Actions(driver);
        actions.SendKeys(text.Substring(0, Math.Min(3, text.Length))).Perform();
        Thread.Sleep(100);
        actions.SendKeys(OpenQA.Selenium.Keys.Enter).Perform();
        
        // Note: We don't throw here because keyboard fallback may work
        // The test should verify the selection was successful
    }
}

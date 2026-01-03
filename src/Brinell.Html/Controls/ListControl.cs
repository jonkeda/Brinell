using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML/Selenium control for HTML list elements (ul, ol, or custom lists).
/// Implements items control functionality for list-based UI elements.
/// </summary>
public class ListControl : ItemsControlBase
{
    /// <summary>
    /// CSS selector for list items.
    /// Defaults to 'li' for standard HTML lists, or items with role="listitem".
    /// </summary>
    protected override string ItemSelector => "li, [role='listitem'], [data-testid$='-item']";

    public ListControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ListControl(SeleniumTestContext context, IPageObject? page, IWebElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public ListControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get all items that are currently selected (have 'selected' or 'active' class).
    /// </summary>
    public virtual IReadOnlyList<string> GetSelectedItems()
    {
        return FindItems()
            .Where(item => 
                item.GetAttribute("class")?.Contains("selected") == true ||
                item.GetAttribute("class")?.Contains("active") == true ||
                item.GetAttribute("aria-selected") == "true")
            .Select(item => item.Text.Trim())
            .ToList();
    }

    /// <summary>
    /// Check if a specific item is selected.
    /// </summary>
    public virtual bool IsItemSelected(string text)
    {
        var items = FindItems();
        var item = items.FirstOrDefault(i => i.Text.Trim().Contains(text, StringComparison.OrdinalIgnoreCase));
        
        if (item == null) return false;
        
        return item.GetAttribute("class")?.Contains("selected") == true ||
               item.GetAttribute("class")?.Contains("active") == true ||
               item.GetAttribute("aria-selected") == "true";
    }

    /// <summary>
    /// Get the first selected item text, or null if none selected.
    /// </summary>
    public virtual string? GetSelectedItem()
    {
        return GetSelectedItems().FirstOrDefault();
    }
}

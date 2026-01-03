using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright control for HTML list elements (ul, ol, or custom lists).
/// Implements items control functionality for list-based UI elements.
/// </summary>
public class ListControl : ItemsControlBase
{
    /// <summary>
    /// CSS selector for list items.
    /// Defaults to 'li' for standard HTML lists, or items with role="listitem".
    /// </summary>
    protected override string ItemSelector => "li, [role='listitem'], [data-testid$='-item']";

    public ListControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ListControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public ListControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get all items that are currently selected (have 'selected' or 'active' class).
    /// </summary>
    public virtual IReadOnlyList<string> GetSelectedItems()
    {
        return GetSelectedItemsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get all items that are currently selected asynchronously.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetSelectedItemsAsync()
    {
        var itemsLocator = GetItemsLocator();
        var count = await itemsLocator.CountAsync();
        var selected = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var item = itemsLocator.Nth(i);
            var classAttr = await item.GetAttributeAsync("class") ?? "";
            var ariaSelected = await item.GetAttributeAsync("aria-selected");
            
            if (classAttr.Contains("selected") || classAttr.Contains("active") || ariaSelected == "true")
            {
                var text = await item.TextContentAsync();
                selected.Add(text?.Trim() ?? string.Empty);
            }
        }

        return selected;
    }

    /// <summary>
    /// Check if a specific item is selected.
    /// </summary>
    public virtual bool IsItemSelected(string text)
    {
        return IsItemSelectedAsync(text).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Check if a specific item is selected asynchronously.
    /// </summary>
    public virtual async Task<bool> IsItemSelectedAsync(string text)
    {
        var itemsLocator = GetItemsLocator();
        var item = itemsLocator.Filter(new LocatorFilterOptions { HasText = text }).First;
        
        var count = await item.CountAsync();
        if (count == 0) return false;
        
        var classAttr = await item.GetAttributeAsync("class") ?? "";
        var ariaSelected = await item.GetAttributeAsync("aria-selected");
        
        return classAttr.Contains("selected") || classAttr.Contains("active") || ariaSelected == "true";
    }

    /// <summary>
    /// Get the first selected item text, or null if none selected.
    /// </summary>
    public virtual string? GetSelectedItem()
    {
        return GetSelectedItemAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the first selected item text asynchronously, or null if none selected.
    /// </summary>
    public virtual async Task<string?> GetSelectedItemAsync()
    {
        var selected = await GetSelectedItemsAsync();
        return selected.FirstOrDefault();
    }
}

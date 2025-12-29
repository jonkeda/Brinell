using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML select control wrapper.
/// Works with &lt;select&gt; elements.
/// </summary>
public class SelectControl : SelectorControlBase
{
    public SelectControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public SelectControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the selected item's value attribute.
    /// </summary>
    public string? GetSelectedValue()
    {
        var element = FindElement();
        if (element == null) return null;
        
        var select = new SelectElement(element);
        return select.SelectedOption?.GetAttribute("value");
    }

    /// <summary>
    /// Get all selected options (for multi-select).
    /// </summary>
    public IReadOnlyList<string> GetSelectedItems()
    {
        var element = FindElement();
        if (element == null) return Array.Empty<string>();
        
        var select = new SelectElement(element);
        return select.AllSelectedOptions.Select(o => o.Text ?? string.Empty).ToList();
    }

    /// <summary>
    /// Deselect all options (for multi-select).
    /// </summary>
    public void DeselectAll()
    {
        LogAction("DeselectAll");
        var element = FindElement();
        if (element == null) return;
        
        var select = new SelectElement(element);
        select.DeselectAll();
    }

    /// <summary>
    /// Get selected option text.
    /// </summary>
    public override string GetText()
    {
        return GetSelectedText() ?? string.Empty;
    }
}

using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML checkbox control wrapper.
/// Works with &lt;input type="checkbox"&gt; elements.
/// </summary>
public class CheckBoxControl : ToggleControlBase
{
    public CheckBoxControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public CheckBoxControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get checkbox label text (if associated with a label element).
    /// </summary>
    public override string GetText()
    {
        var element = FindElement();
        if (element == null) return string.Empty;
        
        // Try to find associated label by id
        var id = element.GetAttribute("id");
        if (!string.IsNullOrEmpty(id))
        {
            try
            {
                var label = _context.Driver.Driver.FindElement(By.CssSelector($"label[for='{id}']"));
                if (label != null)
                {
                    return label.Text ?? string.Empty;
                }
            }
            catch { }
        }
        
        // Check if parent is a label
        try
        {
            var parent = element.FindElement(By.XPath(".."));
            if (parent?.TagName?.ToLowerInvariant() == "label")
            {
                return parent.Text ?? string.Empty;
            }
        }
        catch { }
        
        return string.Empty;
    }
}

using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML Button control wrapper.
/// Works with &lt;button&gt;, &lt;input type="button"&gt;, &lt;input type="submit"&gt; elements.
/// </summary>
public class ButtonControl : ContentControlBase
{
    public ButtonControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ButtonControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get button text/content.
    /// </summary>
    public override string GetText()
    {
        var element = FindElement();
        if (element == null) return string.Empty;
        
        // For input type=button/submit, get value attribute
        var tagName = element.TagName.ToLowerInvariant();
        if (tagName == "input")
        {
            return element.GetAttribute("value") ?? string.Empty;
        }
        
        // For button elements, get inner text
        return element.Text ?? string.Empty;
    }
}

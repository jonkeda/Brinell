using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML label control wrapper.
/// Works with &lt;label&gt;, &lt;span&gt;, &lt;p&gt;, &lt;div&gt; and other text-displaying elements.
/// </summary>
public class LabelControl : ContentControlBase
{
    public LabelControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public LabelControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if label element is present and visible.
    /// Labels don't have a disabled state, so we check visibility.
    /// </summary>
    public override bool IsEnabled()
    {
        return IsVisible();
    }

    /// <summary>
    /// Get inner HTML content.
    /// </summary>
    public string GetInnerHtml()
    {
        return GetAttribute("innerHTML") ?? string.Empty;
    }
}

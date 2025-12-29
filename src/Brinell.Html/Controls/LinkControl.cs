using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML link (anchor) control wrapper.
/// Works with &lt;a&gt; elements.
/// </summary>
public class LinkControl : ContentControlBase
{
    public LinkControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public LinkControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if link is enabled (not disabled via aria-disabled).
    /// Links don't have a standard disabled state.
    /// </summary>
    public override bool IsEnabled()
    {
        var ariaDisabled = GetAttribute("aria-disabled");
        return ariaDisabled != "true" && IsVisible();
    }

    /// <summary>
    /// Get the href attribute value.
    /// </summary>
    public string GetHref()
    {
        return GetAttribute("href") ?? string.Empty;
    }

    /// <summary>
    /// Get the target attribute value (_blank, _self, etc.).
    /// </summary>
    public string GetTarget()
    {
        return GetAttribute("target") ?? string.Empty;
    }

    /// <summary>
    /// Check if link opens in new tab/window.
    /// </summary>
    public bool OpensInNewTab()
    {
        return GetTarget() == "_blank";
    }
}

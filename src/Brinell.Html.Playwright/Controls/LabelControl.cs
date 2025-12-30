using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// HTML label control wrapper for Playwright.
/// Works with &lt;label&gt;, &lt;span&gt;, &lt;p&gt;, &lt;div&gt; and other text-displaying elements.
/// </summary>
public class LabelControl : ContentControlBase
{
    public LabelControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public LabelControl(PlaywrightTestContext context, string automationId)
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
        var element = FindElement();
        if (element == null) return string.Empty;
        return element.InnerHTMLAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get inner HTML content asynchronously.
    /// </summary>
    public async Task<string> GetInnerHtmlAsync()
    {
        var element = await FindElementAsync();
        if (element == null) return string.Empty;
        return await element.InnerHTMLAsync();
    }
}

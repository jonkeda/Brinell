using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright checkbox control wrapper.
/// Works with &lt;input type="checkbox"&gt; elements.
/// </summary>
public class CheckBoxControl : ToggleControlBase
{
    public CheckBoxControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public CheckBoxControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public CheckBoxControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get checkbox label text (if associated with a label element).
    /// </summary>
    public override string GetText()
    {
        return GetTextAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get checkbox label text asynchronously.
    /// </summary>
    public override async Task<string> GetTextAsync()
    {
        var locator = GetLocator();

        // Try to find associated label by id
        var id = await locator.GetAttributeAsync("id");
        if (!string.IsNullOrEmpty(id))
        {
            var label = _context.Page.Locator($"label[for='{id}']");
            if (await label.CountAsync() > 0)
            {
                return await label.TextContentAsync() ?? string.Empty;
            }
        }

        // Check if parent is a label
        var parent = locator.Locator("..");
        if (await parent.CountAsync() > 0)
        {
            var tagName = await parent.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
            if (tagName == "label")
            {
                return await parent.TextContentAsync() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}

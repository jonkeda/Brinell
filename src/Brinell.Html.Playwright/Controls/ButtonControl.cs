using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// HTML Button control wrapper for Playwright.
/// Works with &lt;button&gt;, &lt;input type="button"&gt;, &lt;input type="submit"&gt; elements.
/// </summary>
public class ButtonControl : ContentControlBase
{
    public ButtonControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ButtonControl(PlaywrightTestContext context, string automationId)
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

        // Get tag name to handle inputs differently
        var tagName = element.EvaluateAsync<string>("el => el.tagName.toLowerCase()")
            .GetAwaiter().GetResult();

        // For input type=button/submit, get value attribute
        if (tagName == "input")
        {
            return element.GetAttributeAsync("value").GetAwaiter().GetResult() ?? string.Empty;
        }

        // For button elements, get inner text
        return element.TextContentAsync().GetAwaiter().GetResult() ?? string.Empty;
    }

    /// <summary>
    /// Get button text asynchronously.
    /// </summary>
    public override async Task<string> GetTextAsync()
    {
        var element = await FindElementAsync();
        if (element == null) return string.Empty;

        var tagName = await element.EvaluateAsync<string>("el => el.tagName.toLowerCase()");

        if (tagName == "input")
        {
            return await element.GetAttributeAsync("value") ?? string.Empty;
        }

        return await element.TextContentAsync() ?? string.Empty;
    }
}

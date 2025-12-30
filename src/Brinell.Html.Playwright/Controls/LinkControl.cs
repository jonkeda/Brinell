using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright link (anchor) control wrapper.
/// Works with &lt;a&gt; elements.
/// </summary>
public class LinkControl : ContentControlBase
{
    public LinkControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public LinkControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public LinkControl(PlaywrightTestContext context, string automationId)
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
    /// Check if link is enabled asynchronously.
    /// </summary>
    public override async Task<bool> IsEnabledAsync()
    {
        var ariaDisabled = await GetAttributeAsync("aria-disabled");
        return ariaDisabled != "true" && await IsVisibleAsync();
    }

    /// <summary>
    /// Get the href attribute value.
    /// </summary>
    public string GetHref()
    {
        return GetAttribute("href") ?? string.Empty;
    }

    /// <summary>
    /// Get the href attribute value asynchronously.
    /// </summary>
    public async Task<string> GetHrefAsync()
    {
        return await GetAttributeAsync("href") ?? string.Empty;
    }

    /// <summary>
    /// Get the target attribute value (_blank, _self, etc.).
    /// </summary>
    public string GetTarget()
    {
        return GetAttribute("target") ?? string.Empty;
    }

    /// <summary>
    /// Get the target attribute value asynchronously.
    /// </summary>
    public async Task<string> GetTargetAsync()
    {
        return await GetAttributeAsync("target") ?? string.Empty;
    }

    /// <summary>
    /// Check if link opens in new tab/window.
    /// </summary>
    public bool OpensInNewTab()
    {
        return GetTarget() == "_blank";
    }

    /// <summary>
    /// Check if link opens in new tab/window asynchronously.
    /// </summary>
    public async Task<bool> OpensInNewTabAsync()
    {
        return await GetTargetAsync() == "_blank";
    }

    /// <summary>
    /// Assert href equals expected.
    /// </summary>
    public void AssertHref(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetHref();
        if (actual != expected)
        {
            ThrowAssertionFailed("Href", actual, expected,
                message ?? $"Expected href '{expected}' but got '{actual}' for '{AutomationId}'.");
        }
        LogAssertPass("Href", actual, expected);
    }

    /// <summary>
    /// Assert href equals expected asynchronously.
    /// </summary>
    public async Task AssertHrefAsync(string expected, string? message = null)
    {
        await WaitVisibleAsync(expected: true);
        var actual = await GetHrefAsync();
        if (actual != expected)
        {
            ThrowAssertionFailed("Href", actual, expected,
                message ?? $"Expected href '{expected}' but got '{actual}' for '{AutomationId}'.");
        }
        LogAssertPass("Href", actual, expected);
    }

    /// <summary>
    /// Assert link opens in new tab.
    /// </summary>
    public void AssertOpensInNewTab(string? message = null)
    {
        CheckVisible(expected: true);
        if (!OpensInNewTab())
        {
            var target = GetTarget();
            ThrowAssertionFailed("OpensInNewTab", target, "_blank",
                message ?? $"Expected link '{AutomationId}' to open in new tab but target was '{target}'.");
        }
        LogAssertPass("OpensInNewTab", "_blank", "_blank");
    }

    /// <summary>
    /// Wait for visible asynchronously (helper for async assertions).
    /// </summary>
    protected async Task<bool> WaitVisibleAsync(bool expected = true, int? timeoutMs = null)
    {
        return await _context.WaitForAsync(
            async () => await IsVisibleAsync() == expected,
            timeoutMs,
            $"element '{AutomationId}' visible = {expected}");
    }
}

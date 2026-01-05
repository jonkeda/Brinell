using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright control for radio button input elements.
/// Supports input[type="radio"] elements.
/// </summary>
public class RadioButtonControl : ToggleControlBase, IToggleControl
{
    public RadioButtonControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public RadioButtonControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public RadioButtonControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the radio button is selected.
    /// </summary>
    public override bool IsChecked()
    {
        var element = FindElement();
        return element?.IsCheckedAsync().GetAwaiter().GetResult() ?? false;
    }

    /// <summary>
    /// Check if the radio button is selected asynchronously.
    /// </summary>
    public override async Task<bool> IsCheckedAsync()
    {
        var element = await FindElementAsync();
        if (element == null) return false;
        return await element.IsCheckedAsync();
    }

    /// <summary>
    /// Select this radio button.
    /// </summary>
    public override void Check()
    {
        LogAction("Check");
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");

        element.CheckAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Select this radio button asynchronously.
    /// </summary>
    public override async Task CheckAsync()
    {
        LogAction("Check");
        var element = await FindElementAsync();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");

        await element.CheckAsync();
    }

    /// <summary>
    /// Radio buttons cannot be unchecked directly (must select another radio in the group).
    /// This method does nothing.
    /// </summary>
    public override void Uncheck()
    {
        Log("Uncheck() ignored - radio buttons cannot be unchecked directly");
    }

    /// <summary>
    /// Radio buttons cannot be unchecked directly (must select another radio in the group).
    /// This method does nothing.
    /// </summary>
    public override Task UncheckAsync()
    {
        Log("UncheckAsync() ignored - radio buttons cannot be unchecked directly");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Toggle is equivalent to Check for radio buttons.
    /// </summary>
    public override void Toggle()
    {
        Check();
    }

    /// <summary>
    /// Toggle is equivalent to Check for radio buttons asynchronously.
    /// </summary>
    public override Task ToggleAsync()
    {
        return CheckAsync();
    }

    /// <summary>
    /// Get the label text associated with this radio button.
    /// </summary>
    public string GetLabel()
    {
        // Try to find label via for attribute
        var id = GetAttribute("id");
        if (!string.IsNullOrEmpty(id))
        {
            var label = _context.Page.Locator($"label[for='{id}']");
            if (label.CountAsync().GetAwaiter().GetResult() > 0)
            {
                return label.TextContentAsync().GetAwaiter().GetResult()?.Trim() ?? "";
            }
        }

        // Try to find parent label
        var parentLabel = GetLocator().Locator("xpath=./ancestor::label");
        if (parentLabel.CountAsync().GetAwaiter().GetResult() > 0)
        {
            return parentLabel.TextContentAsync().GetAwaiter().GetResult()?.Trim() ?? "";
        }

        // Try next sibling label
        var siblingLabel = GetLocator().Locator("xpath=./following-sibling::label");
        if (siblingLabel.CountAsync().GetAwaiter().GetResult() > 0)
        {
            return siblingLabel.First.TextContentAsync().GetAwaiter().GetResult()?.Trim() ?? "";
        }

        return "";
    }

    /// <summary>
    /// Assert this radio button is selected.
    /// </summary>
    public void AssertSelected(string? message = null)
    {
        AssertChecked(message);
    }

    /// <summary>
    /// Assert this radio button is not selected.
    /// </summary>
    public void AssertNotSelected(string? message = null)
    {
        AssertUnchecked(message);
    }
}

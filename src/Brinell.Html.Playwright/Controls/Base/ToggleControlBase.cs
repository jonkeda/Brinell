using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls.Base;

/// <summary>
/// Playwright base class for toggle controls (checkbox, radio, switch).
/// </summary>
public abstract class ToggleControlBase : ControlBase, IToggleControl
{
    protected ToggleControlBase(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ToggleControlBase(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ToggleControlBase(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the control is checked.
    /// </summary>
    public virtual bool IsChecked()
    {
        var locator = GetLocator();
        return locator.IsCheckedAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Check if the control is checked asynchronously.
    /// </summary>
    public virtual async Task<bool> IsCheckedAsync()
    {
        var locator = GetLocator();
        return await locator.IsCheckedAsync();
    }

    /// <summary>
    /// Toggle the checked state.
    /// </summary>
    public virtual void Toggle()
    {
        LogAction("Toggle");
        Click();
    }

    /// <summary>
    /// Toggle the checked state asynchronously.
    /// </summary>
    public virtual async Task ToggleAsync()
    {
        LogAction("Toggle");
        await ClickAsync();
    }

    /// <summary>
    /// Check (set to true) the control.
    /// </summary>
    public virtual void Check()
    {
        LogAction("Check");
        var locator = GetLocator();
        locator.CheckAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Check (set to true) the control asynchronously.
    /// </summary>
    public virtual async Task CheckAsync()
    {
        LogAction("Check");
        var locator = GetLocator();
        await locator.CheckAsync();
    }

    /// <summary>
    /// Uncheck (set to false) the control.
    /// </summary>
    public virtual void Uncheck()
    {
        LogAction("Uncheck");
        var locator = GetLocator();
        locator.UncheckAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Uncheck (set to false) the control asynchronously.
    /// </summary>
    public virtual async Task UncheckAsync()
    {
        LogAction("Uncheck");
        var locator = GetLocator();
        await locator.UncheckAsync();
    }

    /// <summary>
    /// Set checked state.
    /// </summary>
    public virtual void SetChecked(bool value)
    {
        var locator = GetLocator();
        locator.SetCheckedAsync(value).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Set checked state asynchronously.
    /// </summary>
    public virtual async Task SetCheckedAsync(bool value)
    {
        var locator = GetLocator();
        await locator.SetCheckedAsync(value);
    }

    /// <summary>
    /// Wait for checked state.
    /// </summary>
    public virtual bool WaitChecked(bool expected = true, int? timeoutMs = null)
    {
        Log($"WaitChecked(expected={expected})");
        return _context.WaitFor(() => IsChecked() == expected, timeoutMs,
            $"element '{AutomationId}' checked = {expected}");
    }

    /// <summary>
    /// Wait for checked state asynchronously.
    /// </summary>
    public virtual async Task<bool> WaitCheckedAsync(bool expected = true, int? timeoutMs = null)
    {
        Log($"WaitCheckedAsync(expected={expected})");
        return await _context.WaitForAsync(
            async () => await IsCheckedAsync() == expected, 
            timeoutMs,
            $"element '{AutomationId}' checked = {expected}");
    }

    /// <summary>
    /// Assert checked state.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertChecked(string? message = null)
    {
        CheckVisible(expected: true);
        var actual = IsChecked();
        if (!actual)
        {
            ThrowAssertionFailed("Checked", "false", "true",
                message ?? $"Expected element '{AutomationId}' to be checked but it was not.");
        }
        LogAssertPass("Checked", "true", "true");
    }

    /// <summary>
    /// Assert checked state asynchronously.
    /// </summary>
    public virtual async Task AssertCheckedAsync(string? message = null)
    {
        await WaitVisibleAsync(expected: true);
        var actual = await IsCheckedAsync();
        if (!actual)
        {
            ThrowAssertionFailed("Checked", "false", "true",
                message ?? $"Expected element '{AutomationId}' to be checked but it was not.");
        }
        LogAssertPass("Checked", "true", "true");
    }

    /// <summary>
    /// Assert unchecked state.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertUnchecked(string? message = null)
    {
        CheckVisible(expected: true);
        var actual = IsChecked();
        if (actual)
        {
            ThrowAssertionFailed("Unchecked", "true", "false",
                message ?? $"Expected element '{AutomationId}' to be unchecked but it was checked.");
        }
        LogAssertPass("Unchecked", "false", "false");
    }

    /// <summary>
    /// Assert unchecked state asynchronously.
    /// </summary>
    public virtual async Task AssertUncheckedAsync(string? message = null)
    {
        await WaitVisibleAsync(expected: true);
        var actual = await IsCheckedAsync();
        if (actual)
        {
            ThrowAssertionFailed("Unchecked", "true", "false",
                message ?? $"Expected element '{AutomationId}' to be unchecked but it was checked.");
        }
        LogAssertPass("Unchecked", "false", "false");
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

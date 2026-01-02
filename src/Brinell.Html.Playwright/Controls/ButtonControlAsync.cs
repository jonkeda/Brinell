using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;
using Microsoft.Playwright;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Async button control for Playwright.
/// Provides non-blocking click and interaction.
/// </summary>
public class ButtonControlAsync : ControlBaseAsync
{
    /// <summary>
    /// Create a new async button control.
    /// </summary>
    public ButtonControlAsync(PlaywrightTestContext context, IPageObject? page, string automationId, string selector)
        : base(context, page, automationId, selector)
    {
    }

    /// <summary>
    /// Click the button.
    /// </summary>
    public async ValueTask ClickAsync(CancellationToken cancellationToken = default)
    {
        var element = await _context.Page.QuerySelectorAsync(_selector);
        if (element == null)
        {
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");
        }

        await element.ClickAsync();
        LogAction("Click");
    }

    /// <summary>
    /// Double-click the button.
    /// </summary>
    public async ValueTask DoubleClickAsync(CancellationToken cancellationToken = default)
    {
        var element = await _context.Page.QuerySelectorAsync(_selector);
        if (element == null)
        {
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");
        }

        await element.DblClickAsync();
        LogAction("DoubleClick");
    }

    /// <summary>
    /// Right-click (context menu) the button.
    /// </summary>
    public async ValueTask RightClickAsync(CancellationToken cancellationToken = default)
    {
        var element = await _context.Page.QuerySelectorAsync(_selector);
        if (element == null)
        {
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");
        }

        await element.ClickAsync(new ElementHandleClickOptions { Button = MouseButton.Right });
        LogAction("RightClick");
    }

    /// <summary>
    /// Hover over the button.
    /// </summary>
    public async ValueTask HoverAsync(CancellationToken cancellationToken = default)
    {
        var element = await _context.Page.QuerySelectorAsync(_selector);
        if (element == null)
        {
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");
        }

        await element.HoverAsync();
        LogAction("Hover");
    }

    /// <summary>
    /// Get the button text.
    /// </summary>
    public new async ValueTask<string> GetTextAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var element = await _context.Page.QuerySelectorAsync(_selector);
            if (element == null) return string.Empty;

            return await element.TextContentAsync() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Wait for button to be clickable, then click it.
    /// </summary>
    public async ValueTask WaitAndClickAsync(int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        await WaitClickableAsync(timeoutMs, cancellationToken);
        await ClickAsync(cancellationToken);
    }

    /// <summary>
    /// Check if button is clickable.
    /// </summary>
    public async ValueTask<bool> IsClickableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var element = await _context.Page.QuerySelectorAsync(_selector);
            if (element == null) return false;

            return await element.IsVisibleAsync() && 
                   await element.IsEnabledAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Wait for button to be clickable.
    /// </summary>
    public async ValueTask<bool> WaitClickableAsync(int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var startTime = sw.Elapsed;

        while (sw.Elapsed - startTime < TimeSpan.FromMilliseconds(timeout))
        {
            if (await IsClickableAsync(cancellationToken))
            {
                sw.Stop();
                LogWait("Clickable", true, (int)sw.ElapsedMilliseconds);
                return true;
            }
            await Task.Delay(100, cancellationToken);
        }

        sw.Stop();
        LogWait("Clickable", false, (int)sw.ElapsedMilliseconds);
        return false;
    }
}

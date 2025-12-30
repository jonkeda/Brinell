using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Logging;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls.Base;

/// <summary>
/// Playwright-specific base class for page objects.
/// Uses PlaywrightTestContext directly for Playwright automation.
/// </summary>
public abstract class PageBase : IPageObject
{
    protected readonly PlaywrightTestContext _context;

    /// <summary>
    /// The CSS selector or data-testid that identifies this page.
    /// </summary>
    public abstract string AutomationId { get; }

    /// <summary>
    /// Name of the page (defaults to class name).
    /// </summary>
    public virtual string Name => GetType().Name;

    /// <summary>
    /// The test name for logging.
    /// </summary>
    protected string TestName => _context.TestName;

    /// <summary>
    /// Logger instance.
    /// </summary>
    protected ITestLogger? Logger => _context.Logger;

    /// <summary>
    /// Access to the underlying Playwright context (typed).
    /// </summary>
    public PlaywrightTestContext PlaywrightContext => _context;

    /// <summary>
    /// Access to the test context (interface).
    /// </summary>
    ITestContext IPageObject.Context => _context;

    /// <summary>
    /// Access to the underlying Playwright page.
    /// </summary>
    protected IPage Page => _context.Page;

    protected PageBase(PlaywrightTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Check if page is currently displayed (immediate check).
    /// </summary>
    public virtual bool IsDisplayed()
    {
        return _context.ElementIsVisible(AutomationId);
    }

    /// <summary>
    /// Check if page is displayed asynchronously.
    /// </summary>
    public virtual async Task<bool> IsDisplayedAsync()
    {
        var locator = BuildLocator(AutomationId);
        var count = await locator.CountAsync();
        if (count == 0) return false;
        return await locator.IsVisibleAsync();
    }

    /// <summary>
    /// Check if page is ready for interaction.
    /// Override in derived classes to add custom ready conditions.
    /// </summary>
    public virtual bool IsReady()
    {
        return IsDisplayed();
    }

    /// <summary>
    /// Wait for page to be displayed.
    /// </summary>
    public virtual bool WaitForDisplayed(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        Log($"WaitForDisplayed (timeout: {timeout}ms)");
        return _context.WaitFor(IsDisplayed, timeout, $"page '{GetType().Name}' displayed");
    }

    /// <summary>
    /// Wait for page to be displayed asynchronously.
    /// </summary>
    public virtual async Task<bool> WaitForDisplayedAsync(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        Log($"WaitForDisplayedAsync (timeout: {timeout}ms)");
        return await _context.WaitForAsync(IsDisplayedAsync, timeout, $"page '{GetType().Name}' displayed");
    }

    /// <summary>
    /// Wait for page to be ready (displayed and ready for interaction).
    /// </summary>
    public virtual bool WaitForReady(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        Log($"WaitForReady (timeout: {timeout}ms)");
        return _context.WaitFor(IsReady, timeout, $"page '{GetType().Name}' ready");
    }

    /// <summary>
    /// Check that page is displayed, throw if not.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void CheckDisplayed(int? timeoutMs = null)
    {
        if (!WaitForDisplayed(timeoutMs))
        {
            ThrowPageNotDisplayed("CheckDisplayed", $"Page '{Name}' was not displayed within timeout.");
        }
    }

    /// <summary>
    /// Check that page is ready, throw if not.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void CheckReady(int? timeoutMs = null)
    {
        if (!WaitForReady(timeoutMs))
        {
            ThrowPageNotReady("CheckReady", $"Page '{Name}' was not ready within timeout.");
        }
    }

    /// <summary>
    /// Assert page is displayed.
    /// </summary>
    public virtual void AssertDisplayed(string? message = null)
    {
        CheckDisplayed();
        LogAssertPass("Displayed", "true", "true");
    }

    /// <summary>
    /// Assert page is displayed asynchronously.
    /// </summary>
    public virtual async Task AssertDisplayedAsync(string? message = null)
    {
        if (!await WaitForDisplayedAsync())
        {
            ThrowPageNotDisplayed("AssertDisplayedAsync", 
                message ?? $"Page '{Name}' was not displayed within timeout.");
        }
        LogAssertPass("Displayed", "true", "true");
    }

    /// <summary>
    /// Assert page is not displayed.
    /// </summary>
    public virtual void AssertNotDisplayed(string? message = null)
    {
        if (IsDisplayed())
        {
            ThrowAssertionFailed("NotDisplayed", "true", "false",
                message ?? $"Page '{Name}' is displayed but expected not displayed.");
        }

        LogAssertPass("NotDisplayed", "false", "false");
    }

    /// <summary>
    /// Take a screenshot of the current page.
    /// </summary>
    public virtual string? TakeScreenshot(string suffix = "")
    {
        return _context.TakeScreenshot($"{GetType().Name}_{suffix}");
    }

    /// <summary>
    /// Log a message with page context.
    /// </summary>
    protected void Log(string message)
    {
        _context.Log($"[{GetType().Name}] {message}");
    }

    /// <summary>
    /// Build a locator for the given selector.
    /// </summary>
    protected ILocator BuildLocator(string selector)
    {
        // If it's already a CSS selector, use it directly
        if (selector.StartsWith('#') || selector.StartsWith('.') || selector.StartsWith('['))
        {
            return Page.Locator(selector).First;
        }

        // Otherwise, search by automation ID attribute or id
        return Page.Locator($"[data-automation-id='{selector}'], [id='{selector}']").First;
    }

    /// <summary>
    /// Log and throw PageNotDisplayedException with screenshot capture.
    /// </summary>
    protected void ThrowPageNotDisplayed(string action, string message)
    {
        Logger.ThrowPageNotDisplayed(TestName, Name, AutomationId, action, message, _context);
    }

    /// <summary>
    /// Log and throw PageNotReadyException with screenshot capture.
    /// </summary>
    protected void ThrowPageNotReady(string action, string message)
    {
        Logger.ThrowPageNotReady(TestName, Name, AutomationId, action, message, _context);
    }

    /// <summary>
    /// Log assertion success.
    /// </summary>
    protected void LogAssertPass(string assertType, string? actual, string? expected)
    {
        Logger?.LogAssertPass(TestName, Name, AutomationId, assertType, actual, expected);
    }

    /// <summary>
    /// Log assertion failure and throw.
    /// </summary>
    protected void ThrowAssertionFailed(string assertType, string? actual, string? expected, string message)
    {
        Logger.ThrowAssertionFailed(TestName, Name, AutomationId, assertType, actual, expected, message, _context);
    }

    /// <summary>
    /// Navigate to this page's URL (if applicable).
    /// Override to provide page-specific navigation.
    /// </summary>
    public virtual void NavigateTo()
    {
        throw new NotImplementedException($"NavigateTo not implemented for {GetType().Name}");
    }

    /// <summary>
    /// Navigate to this page's URL asynchronously (if applicable).
    /// Override to provide page-specific navigation.
    /// </summary>
    public virtual Task NavigateToAsync()
    {
        throw new NotImplementedException($"NavigateToAsync not implemented for {GetType().Name}");
    }

    /// <summary>
    /// Get the current page URL.
    /// </summary>
    public string GetCurrentUrl()
    {
        return _context.GetCurrentUrl();
    }

    /// <summary>
    /// Get the page title.
    /// </summary>
    public string GetTitle()
    {
        return _context.GetTitle();
    }

    /// <summary>
    /// Get the page title asynchronously.
    /// </summary>
    public async Task<string> GetTitleAsync()
    {
        return await Page.TitleAsync();
    }
}

/// <summary>
/// Page base class that includes loading indicator support.
/// </summary>
public abstract class LoadingPageBase : PageBase
{
    /// <summary>
    /// CSS selector for the loading indicator.
    /// </summary>
    protected virtual string? LoadingIndicatorSelector => null;

    protected LoadingPageBase(PlaywrightTestContext context) : base(context)
    {
    }

    /// <summary>
    /// Check if the page is currently loading.
    /// </summary>
    public virtual bool IsLoading()
    {
        if (string.IsNullOrEmpty(LoadingIndicatorSelector))
            return false;

        return _context.ElementIsVisible(LoadingIndicatorSelector);
    }

    /// <summary>
    /// Check if the page is loading asynchronously.
    /// </summary>
    public virtual async Task<bool> IsLoadingAsync()
    {
        if (string.IsNullOrEmpty(LoadingIndicatorSelector))
            return false;

        var locator = Page.Locator(LoadingIndicatorSelector);
        var count = await locator.CountAsync();
        if (count == 0) return false;
        return await locator.IsVisibleAsync();
    }

    /// <summary>
    /// Wait for the page to finish loading.
    /// </summary>
    public virtual bool WaitForLoaded(int? timeoutMs = null)
    {
        if (string.IsNullOrEmpty(LoadingIndicatorSelector))
            return true;

        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        Log($"WaitForLoaded (timeout: {timeout}ms)");
        return _context.WaitFor(() => !IsLoading(), timeout, "page loaded");
    }

    /// <summary>
    /// Wait for the page to finish loading asynchronously.
    /// </summary>
    public virtual async Task<bool> WaitForLoadedAsync(int? timeoutMs = null)
    {
        if (string.IsNullOrEmpty(LoadingIndicatorSelector))
            return true;

        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        Log($"WaitForLoadedAsync (timeout: {timeout}ms)");
        return await _context.WaitForAsync(async () => !await IsLoadingAsync(), timeout, "page loaded");
    }

    /// <summary>
    /// Page is ready when displayed and not loading.
    /// </summary>
    public override bool IsReady()
    {
        return base.IsReady() && !IsLoading();
    }
}

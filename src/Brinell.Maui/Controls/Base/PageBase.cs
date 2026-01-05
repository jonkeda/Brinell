using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Core.Exceptions;
using Brinell.Core.Logging;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// MAUI-specific base class for page objects.
/// Uses AppiumTestContext directly for Appium automation.
/// </summary>
public abstract class PageBase : IPageObject
{
    protected readonly AppiumTestContext _context;
    
    /// <summary>
    /// The AutomationId of the root element that identifies this page.
    /// </summary>
    public abstract string AutomationId { get; }
    
    /// <summary>
    /// Name of the page (defaults to class name).
    /// </summary>
    public virtual string Name => GetType().Name;
    
    /// <summary>
    /// Access to the underlying Appium context (typed).
    /// </summary>
    public AppiumTestContext AppiumContext => _context;
    
    /// <summary>
    /// Access to the test context (interface).
    /// </summary>
    ITestContext IPageObject.Context => _context;
    
    /// <summary>
    /// The test name for logging.
    /// </summary>
    protected string TestName => _context.TestName;
    
    /// <summary>
    /// Logger instance.
    /// </summary>
    protected ITestLogger? Logger => _context.Logger;

    protected PageBase(AppiumTestContext context)
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
            ThrowPageNotDisplayed("CheckDisplayed", 
                $"Page '{Name}' was not displayed within timeout.");
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
            ThrowPageNotReady("CheckReady",
                $"Page '{Name}' was not ready within timeout.");
        }
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
    /// Throw PageNotDisplayedException with screenshot capture.
    /// </summary>
    protected void ThrowPageNotDisplayed(string action, string message)
    {
        Logger.ThrowPageNotDisplayed(TestName, Name, AutomationId, action, message, _context);
    }
    
    /// <summary>
    /// Throw PageNotReadyException with screenshot capture.
    /// </summary>
    protected void ThrowPageNotReady(string action, string message)
    {
        Logger.ThrowPageNotReady(TestName, Name, AutomationId, action, message, _context);
    }
}

/// <summary>
/// Page base class that includes a busy indicator.
/// Implements IBusyPageObject for cross-platform busy state tracking.
/// </summary>
public abstract class BusyPageBase : PageBase, IBusyPageObject
{
    /// <summary>
    /// AutomationId of the busy indicator control.
    /// </summary>
    protected virtual string? BusyIndicatorId => null;

    protected BusyPageBase(AppiumTestContext context) : base(context)
    {
    }

    /// <summary>
    /// Check if the page is currently busy (showing loading indicator).
    /// </summary>
    public virtual bool IsBusy()
    {
        if (string.IsNullOrEmpty(BusyIndicatorId))
            return false;
        
        return _context.ElementIsVisible(BusyIndicatorId);
    }

    /// <summary>
    /// Wait for the page to not be busy.
    /// </summary>
    public virtual bool WaitForNotBusy(int? timeoutMs = null)
    {
        if (string.IsNullOrEmpty(BusyIndicatorId))
            return true;
        
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        Log($"WaitForNotBusy (timeout: {timeout}ms)");
        return _context.WaitFor(() => !IsBusy(), timeout, "page not busy");
    }

    /// <summary>
    /// Assert the page is not busy.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertNotBusy(string? message = null)
    {
        if (IsBusy())
        {
            ThrowPageNotReady("AssertNotBusy", 
                message ?? $"Expected page '{Name}' to not be busy but it is currently busy.");
        }
    }

    /// <summary>
    /// Page is ready when displayed and not busy.
    /// </summary>
    public override bool IsReady()
    {
        return base.IsReady() && !IsBusy();
    }
}

using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Exceptions;
using Brinell.Core.Logging;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls.Base;

/// <summary>
/// WinForms-specific base class for page objects using FlaUI.
/// </summary>
public abstract class PageBase : IPageObject
{
    protected readonly FlaUITestContext _context;
    protected readonly string _pageAutomationId;
    protected readonly string _pageName;

    /// <summary>
    /// Name of the page for logging.
    /// </summary>
    public virtual string Name => _pageName;
    
    /// <summary>
    /// The AutomationId of the page root element.
    /// </summary>
    public virtual string AutomationId => _pageAutomationId;
    
    /// <summary>
    /// The test context.
    /// </summary>
    public ITestContext Context => _context;
    
    /// <summary>
    /// Direct access to the FlaUI context for platform-specific operations.
    /// </summary>
    protected FlaUITestContext FlaContext => _context;

    protected PageBase(FlaUITestContext context, string pageAutomationId)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _pageAutomationId = pageAutomationId ?? throw new ArgumentNullException(nameof(pageAutomationId));
        _pageName = GetType().Name.Replace("Page", "").Replace("Dialog", "");
    }

    #region Logging Methods

    /// <summary>
    /// Log a debug message to console only (not CSV).
    /// </summary>
    protected void LogDebug(string message) => _context.Log($"[{GetType().Name}] {message}");

    /// <summary>
    /// Log a message to console (alias for LogDebug for compatibility with page objects).
    /// </summary>
    protected void Log(string message) => LogDebug(message);

    /// <summary>
    /// Log a navigation action to CSV.
    /// </summary>
    protected void LogNavigation(string action, string? value = null)
    {
        _context.Logger?.LogNavigation(_context.TestName, Name, _pageAutomationId, action, value);
    }

    /// <summary>
    /// Log a wait result to CSV.
    /// </summary>
    protected void LogWait(string waitType, bool success, int elapsedMs)
    {
        _context.Logger?.LogWait(_context.TestName, Name, _pageAutomationId, waitType, success, elapsedMs);
    }

    /// <summary>
    /// Log assertion pass to CSV.
    /// </summary>
    protected void LogAssertPass(string assertType, string actual, string expected)
    {
        _context.Logger?.LogAssertPass(_context.TestName, Name, _pageAutomationId, assertType, actual, expected);
    }

    /// <summary>
    /// Log assertion failure to CSV and throw AssertionException.
    /// </summary>
    protected void ThrowAssertionFailed(string assertType, string actual, string expected, string message)
    {
        _context.Logger?.ThrowAssertionFailed(_context.TestName, Name, _pageAutomationId, assertType, actual, expected, message, _context);
    }

    /// <summary>
    /// Log page not ready error to CSV and throw PageNotReadyException.
    /// </summary>
    protected void ThrowPageNotReady(string action, string message)
    {
        _context.Logger?.ThrowPageNotReady(_context.TestName, Name, _pageAutomationId, action, message, _context);
    }

    /// <summary>
    /// Log page not displayed error to CSV and throw PageNotDisplayedException.
    /// </summary>
    protected void ThrowPageNotDisplayed(string action, string message)
    {
        _context.Logger?.ThrowPageNotDisplayed(_context.TestName, Name, _pageAutomationId, action, message, _context);
    }

    #endregion

    /// <summary>
    /// Find an element by AutomationId within this page context.
    /// </summary>
    protected AutomationElement? FindElement(string automationId) =>
        _context.FindElementInternal(automationId);

    /// <summary>
    /// Check if this page is currently displayed.
    /// Override to provide page-specific display check.
    /// </summary>
    public abstract bool IsDisplayed();

    /// <summary>
    /// Check if the page is ready for interaction.
    /// Override in derived classes for page-specific ready checks.
    /// Default returns IsDisplayed().
    /// </summary>
    public virtual bool IsReady() => IsDisplayed();

    /// <summary>
    /// Wait for page to be displayed.
    /// </summary>
    public virtual bool WaitForDisplayed(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(IsDisplayed, timeout, $"page '{Name}' displayed");
        LogWait("Displayed", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Wait for page to be ready (displayed and not busy).
    /// </summary>
    public virtual bool WaitForReady(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(IsReady, timeout, $"page '{Name}' ready");
        LogWait("Ready", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Assert that the page is currently displayed.
    /// </summary>
    public virtual void AssertDisplayed(string? message = null)
    {
        if (!IsDisplayed())
        {
            ThrowPageNotDisplayed("AssertDisplayed", message ?? $"Page '{Name}' is not displayed.");
        }
        LogAssertPass("Displayed", "true", "true");
    }

    /// <summary>
    /// Assert that the page is ready.
    /// </summary>
    public virtual void AssertReady(string? message = null)
    {
        if (!IsReady())
        {
            ThrowPageNotReady("AssertReady", message ?? $"Page '{Name}' is not ready.");
        }
        LogAssertPass("Ready", "true", "true");
    }

    /// <summary>
    /// Check page is displayed - waits and throws if not.
    /// </summary>
    public virtual void CheckDisplayed(int? timeoutMs = null)
    {
        if (!WaitForDisplayed(timeoutMs))
        {
            ThrowPageNotDisplayed("CheckDisplayed",
                $"Page '{Name}' is not displayed after waiting {timeoutMs ?? _context.DefaultTimeoutMs}ms.");
        }
    }

    /// <summary>
    /// Check page is ready - waits and throws if not.
    /// </summary>
    public virtual void CheckReady(int? timeoutMs = null)
    {
        if (!WaitForReady(timeoutMs))
        {
            ThrowPageNotReady("CheckReady",
                $"Page '{Name}' is not ready after waiting {timeoutMs ?? _context.DefaultTimeoutMs}ms.");
        }
    }

    /// <summary>
    /// Take a screenshot of the current page.
    /// </summary>
    public virtual string? TakeScreenshot(string suffix = "")
    {
        var filename = string.IsNullOrEmpty(suffix) ? Name : $"{Name}_{suffix}";
        return _context.TakeScreenshot(filename);
    }
}

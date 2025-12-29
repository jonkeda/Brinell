using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Exceptions;
using Brinell.Core.Logging;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls.Base;

/// <summary>
/// WPF-specific base class for all controls using FlaUI directly.
/// Implements the Is/Wait/Check/Assert pattern for consistent control interaction.
/// </summary>
public abstract class ControlBase : IControlObject
{
    protected readonly FlaUITestContext _context;
    protected readonly IPageObject? _page;
    protected readonly AutomationElement? _container;

    /// <summary>
    /// The AutomationId used to locate this control.
    /// </summary>
    public string AutomationId { get; }
    
    /// <summary>
    /// The parent page object.
    /// </summary>
    public IPageObject? Page => _page;
    
    /// <summary>
    /// The page name for logging (from Page or "Global").
    /// </summary>
    protected string PageName => _page?.Name ?? "Global";
    
    /// <summary>
    /// The test name for logging.
    /// </summary>
    protected string TestName => _context.TestName;
    
    /// <summary>
    /// Logger instance.
    /// </summary>
    protected ITestLogger? Logger => _context.Logger;

    /// <summary>
    /// Create a control with page context and AutomationId.
    /// Searches from the main window root.
    /// </summary>
    protected ControlBase(FlaUITestContext context, IPageObject? page, string automationId)
        : this(context, page, container: null, automationId)
    {
    }

    /// <summary>
    /// Create a control that searches within a container element.
    /// Use this for controls inside list items or repeated templates.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <param name="page">The parent page object.</param>
    /// <param name="container">The container element to search within, or null for window root.</param>
    /// <param name="automationId">The AutomationId of the control.</param>
    protected ControlBase(FlaUITestContext context, IPageObject? page, AutomationElement? container, string automationId)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _page = page;
        _container = container;
        AutomationId = automationId ?? throw new ArgumentNullException(nameof(automationId));
    }

    /// <summary>
    /// Create a control without page context (for global controls).
    /// </summary>
    protected ControlBase(FlaUITestContext context, string automationId)
        : this(context, null, null, automationId)
    {
    }

    #region Element Access - Direct FlaUI
    
    /// <summary>
    /// Find the element by AutomationId.
    /// Searches within container if specified, otherwise from window root.
    /// </summary>
    protected virtual AutomationElement? FindElement()
    {
        if (_container != null)
        {
            return _container.FindFirstDescendant(cf => cf.ByAutomationId(AutomationId));
        }
        return _context.FindElementInternal(AutomationId);
    }
    
    /// <summary>
    /// Wait for element to exist.
    /// </summary>
    protected AutomationElement? WaitForElement(int? timeoutMs = null)
    {
        AutomationElement? element = null;
        _context.WaitFor(() => (element = FindElement()) != null, timeoutMs, $"'{AutomationId}' exists");
        return element;
    }
    
    /// <summary>
    /// Wait for element to be visible.
    /// </summary>
    protected AutomationElement? WaitForElementVisible(int? timeoutMs = null)
    {
        AutomationElement? element = null;
        _context.WaitFor(() =>
        {
            element = FindElement();
            return element != null && !element.IsOffscreen;
        }, timeoutMs, $"'{AutomationId}' visible");
        return element;
    }
    
    #endregion

    #region Logging - Unified Approach
    
    /// <summary>
    /// Log to console only (for verbose/debug output).
    /// Use sparingly - prefer LogAction for CSV tracking.
    /// </summary>
    protected void LogDebug(string message)
    {
        _context.Log($"[{GetType().Name}:{AutomationId}] {message}");
    }
    
    /// <summary>
    /// Log a control action to CSV.
    /// </summary>
    protected void LogAction(string action, string? value = null)
    {
        Logger?.LogAction(TestName, PageName, AutomationId, action, value);
    }
    
    /// <summary>
    /// Log assertion success to CSV.
    /// </summary>
    protected void LogAssertPass(string assertType, string? actual, string? expected)
    {
        Logger?.LogAssertPass(TestName, PageName, AutomationId, assertType, actual, expected);
    }
    
    /// <summary>
    /// Log assertion failure to CSV and throw AssertionException.
    /// </summary>
    protected void ThrowAssertionFailed(
        string assertType, 
        string? actual, 
        string? expected, 
        string message)
    {
        Logger.ThrowAssertionFailed(TestName, PageName, AutomationId, assertType, actual, expected, message);
    }
    
    /// <summary>
    /// Log check failure to CSV and throw CheckFailedException.
    /// </summary>
    protected void ThrowCheckFailed(string checkType, string message)
    {
        Logger.ThrowCheckFailed(TestName, PageName, AutomationId, checkType, message);
    }
    
    /// <summary>
    /// Log wait result to CSV.
    /// </summary>
    protected void LogWait(string waitType, bool success, int elapsedMs)
    {
        Logger?.LogWait(TestName, PageName, AutomationId, waitType, success, elapsedMs);
    }
    
    #endregion

    #region Exists - Is/Wait/Check/Assert
    
    public virtual bool IsExists()
    {
        return FindElement() != null;
    }
    
    public virtual bool WaitExists(bool expected = true, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(() => IsExists() == expected, timeout, 
            expected ? "element exists" : "element gone");
        LogWait($"Exists={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }
    
    public virtual void CheckExists(bool expected = true, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var success = _context.WaitFor(() => IsExists() == expected, timeout, 
            expected ? "element exists" : "element gone");
        LogWait($"Exists={expected}", success, (int)sw.ElapsedMilliseconds);
        
        if (!success)
        {
            var state = IsExists() ? "exists" : "does not exist";
            ThrowCheckFailed("Exists", 
                $"Check failed: Element '{AutomationId}' {state}, expected {(expected ? "exists" : "not exists")}.");
        }
    }
    
    public virtual void AssertExists(string? message = null)
    {
        CheckExists(expected: true);
        LogAssertPass("Exists", "true", "true");
    }
    
    public virtual void AssertNotExists(string? message = null)
    {
        CheckExists(expected: false);
        LogAssertPass("NotExists", "false", "false");
    }
    
    #endregion

    #region Visible - Is/Wait/Check/Assert
    
    public virtual bool IsVisible()
    {
        var element = FindElement();
        return element != null && !element.IsOffscreen;
    }
    
    public virtual bool WaitVisible(bool expected = true, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(() => IsVisible() == expected, timeout,
            expected ? "element visible" : "element not visible");
        LogWait($"Visible={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }
    
    public virtual void CheckVisible(bool expected = true, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var success = _context.WaitFor(() => IsVisible() == expected, timeout,
            expected ? "element visible" : "element not visible");
        LogWait($"Visible={expected}", success, (int)sw.ElapsedMilliseconds);
        
        if (!success)
        {
            var state = IsVisible() ? "visible" : "not visible";
            ThrowCheckFailed("Visible", 
                $"Check failed: Element '{AutomationId}' is {state}, expected {(expected ? "visible" : "not visible")}.");
        }
    }
    
    public virtual void AssertVisible(string? message = null)
    {
        CheckVisible(expected: true);
        LogAssertPass("Visible", "true", "true");
    }
    
    public virtual void AssertNotVisible(string? message = null)
    {
        CheckVisible(expected: false);
        LogAssertPass("NotVisible", "false", "false");
    }
    
    #endregion

    #region Enabled - Is/Wait/Check/Assert
    
    public virtual bool IsEnabled()
    {
        var element = FindElement();
        return element?.IsEnabled ?? false;
    }
    
    public virtual bool WaitEnabled(bool expected = true, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(() => IsEnabled() == expected, timeout,
            expected ? "element enabled" : "element disabled");
        LogWait($"Enabled={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }
    
    public virtual void CheckEnabled(bool expected = true, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var sw = Stopwatch.StartNew();
        var success = _context.WaitFor(() => IsEnabled() == expected, timeout,
            expected ? "element enabled" : "element disabled");
        LogWait($"Enabled={expected}", success, (int)sw.ElapsedMilliseconds);
        
        if (!success)
        {
            var state = IsEnabled() ? "enabled" : "disabled";
            ThrowCheckFailed("Enabled", 
                $"Check failed: Element '{AutomationId}' is {state}, expected {(expected ? "enabled" : "disabled")}.");
        }
    }
    
    public virtual void AssertEnabled(string? message = null)
    {
        CheckEnabled(expected: true);
        LogAssertPass("Enabled", "true", "true");
    }
    
    public virtual void AssertDisabled(string? message = null)
    {
        CheckEnabled(expected: false);
        LogAssertPass("Disabled", "false", "false");
    }
    
    #endregion

    #region Text - Get/Assert
    
    public virtual string GetText()
    {
        var element = FindElement();
        if (element == null) return string.Empty;
        
        // Try different patterns
        var textBox = element.AsTextBox();
        if (textBox != null) return textBox.Text ?? string.Empty;
        
        var label = element.AsLabel();
        if (label != null) return label.Text ?? string.Empty;
        
        return element.Name ?? string.Empty;
    }
    
    public virtual void AssertTextEquals(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetText();
        if (actual != expected)
        {
            ThrowAssertionFailed("TextEquals", actual, expected,
                message ?? $"Expected text '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("TextEquals", actual, expected);
    }
    
    public virtual void AssertTextContains(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetText();
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            ThrowAssertionFailed("TextContains", actual, expected,
                message ?? $"Expected text to contain '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("TextContains", actual, expected);
    }
    
    #endregion
}

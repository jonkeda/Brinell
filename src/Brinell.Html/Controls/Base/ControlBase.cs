using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Exceptions;
using Brinell.Core.Logging;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls.Base;

/// <summary>
/// HTML/Selenium-specific base class for all UI controls.
/// Uses SeleniumTestContext directly for Selenium automation.
/// Implements the Is/Wait/Check/Assert pattern.
/// </summary>
public abstract class ControlBase : IControlObject
{
    protected readonly SeleniumTestContext _context;
    protected readonly IPageObject? _page;
    protected readonly IWebElement? _container;

    /// <summary>
    /// The CSS selector, data-testid, or ID used to locate this control.
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
    /// Searches from the document root.
    /// </summary>
    protected ControlBase(SeleniumTestContext context, IPageObject? page, string automationId)
        : this(context, page, container: null, automationId)
    {
    }

    /// <summary>
    /// Create a control that searches within a container element.
    /// Use this for controls inside list items or repeated templates.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <param name="page">The parent page object.</param>
    /// <param name="container">The container element to search within, or null for document root.</param>
    /// <param name="automationId">The AutomationId/selector of the control.</param>
    protected ControlBase(SeleniumTestContext context, IPageObject? page, IWebElement? container, string automationId)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _page = page;
        _container = container;
        AutomationId = automationId ?? throw new ArgumentNullException(nameof(automationId));
    }

    /// <summary>
    /// Create a control without page context (for global controls).
    /// </summary>
    protected ControlBase(SeleniumTestContext context, string automationId)
        : this(context, null, null, automationId)
    {
    }

    #region Element Access
    
    /// <summary>
    /// Find the element using Selenium.
    /// Searches within container if specified, otherwise from document root.
    /// </summary>
    protected IWebElement? FindElement()
    {
        if (_container != null)
        {
            return _context.Driver.FindElementInContainer(_container, AutomationId);
        }
        return _context.Driver.FindElementDirect(AutomationId);
    }

    /// <summary>
    /// Find element and wait for it to be visible.
    /// </summary>
    protected IWebElement? WaitForElementVisible(int? timeoutMs = null)
    {
        IWebElement? element = null;
        var found = _context.WaitFor(() =>
        {
            element = FindElement();
            return element != null && element.Displayed;
        }, timeoutMs, $"element '{AutomationId}' visible");
        
        return found ? element : null;
    }
    
    #endregion

    #region Logging Helpers

    /// <summary>
    /// Log a message with control context.
    /// </summary>
    protected void Log(string message)
    {
        _context.Log($"[{GetType().Name}:{AutomationId}] {message}");
    }

    /// <summary>
    /// Log an action being performed.
    /// </summary>
    protected void LogAction(string action, string? parameter = null, bool success = true)
    {
        var paramStr = parameter != null ? $"(\"{parameter}\")" : "()";
        var statusStr = success ? "" : " [FAILED]";
        Log($"{action}{paramStr}{statusStr}");
        Logger?.LogAction(TestName, PageName, AutomationId, action, parameter);
    }

    /// <summary>
    /// Log assertion success to CSV.
    /// </summary>
    protected void LogAssertPass(string assertType, string? actual, string? expected)
    {
        Logger?.LogAssertPass(TestName, PageName, AutomationId, assertType, actual, expected);
    }

    /// <summary>
    /// Log assertion failure, capture screenshot, and throw.
    /// </summary>
    protected void ThrowAssertionFailed(string assertType, string? actual, string? expected, string message)
    {
        Logger.ThrowAssertionFailed(TestName, PageName, AutomationId, assertType, actual, expected, message, _context);
    }

    /// <summary>
    /// Log check failure, capture screenshot, and throw.
    /// </summary>
    protected void ThrowCheckFailed(string checkType, string message)
    {
        Logger.ThrowCheckFailed(TestName, PageName, AutomationId, checkType, message, _context);
    }

    /// <summary>
    /// Log wait result to CSV.
    /// </summary>
    protected void LogWait(string waitType, bool success, int elapsedMs)
    {
        Logger?.LogWait(TestName, PageName, AutomationId, waitType, success, elapsedMs);
    }

    /// <summary>
    /// Log an assertion result (legacy method for compatibility).
    /// </summary>
    protected void LogAssert(string assertion, string expected, string actual, bool passed, string? message = null)
    {
        var status = passed ? "PASS" : "FAIL";
        var msgPart = message != null ? $" - {message}" : "";
        Log($"Assert{assertion}: expected='{expected}', actual='{actual}' [{status}]{msgPart}");
        
        if (passed)
            Logger?.LogAssertPass(TestName, PageName, AutomationId, assertion, actual, expected);
        else
            Logger?.LogAssertFail(TestName, PageName, AutomationId, assertion, actual, expected, message);
    }

    #endregion

    #region Is Methods (Immediate state check, no waiting)

    /// <summary>
    /// Check if element exists (immediate, no wait).
    /// </summary>
    public virtual bool IsExists()
    {
        return FindElement() != null;
    }

    /// <summary>
    /// Check if element is visible (immediate, no wait).
    /// </summary>
    public virtual bool IsVisible()
    {
        var element = FindElement();
        return element != null && element.Displayed;
    }

    /// <summary>
    /// Check if element is enabled (immediate, no wait).
    /// </summary>
    public virtual bool IsEnabled()
    {
        var element = FindElement();
        return element?.Enabled ?? false;
    }

    /// <summary>
    /// Get element text (immediate).
    /// </summary>
    public virtual string GetText()
    {
        var element = FindElement();
        if (element == null) return string.Empty;
        
        // For input elements, return value attribute
        var tagName = element.TagName?.ToLowerInvariant();
        if (tagName == "input" || tagName == "textarea")
        {
            return element.GetAttribute("value") ?? string.Empty;
        }
        
        return element.Text ?? string.Empty;
    }

    /// <summary>
    /// Get the length of the text in the element.
    /// </summary>
    public virtual int GetTextLength()
    {
        return GetText().Length;
    }

    #endregion

    #region Wait Methods (Poll until condition or timeout)

    /// <summary>
    /// Wait for element to exist or not exist.
    /// </summary>
    public virtual bool WaitExists(bool expected = true, int? timeoutMs = null)
    {
        Log($"WaitExists(expected={expected})");
        if (expected)
            return _context.WaitFor(IsExists, timeoutMs, $"element '{AutomationId}' exists");
        else
            return _context.WaitFor(() => !IsExists(), timeoutMs, $"element '{AutomationId}' not exists");
    }

    /// <summary>
    /// Wait for element to be visible or not visible.
    /// </summary>
    public virtual bool WaitVisible(bool expected = true, int? timeoutMs = null)
    {
        Log($"WaitVisible(expected={expected})");
        if (expected)
            return _context.WaitFor(IsVisible, timeoutMs, $"element '{AutomationId}' visible");
        else
            return _context.WaitFor(() => !IsVisible(), timeoutMs, $"element '{AutomationId}' not visible");
    }

    /// <summary>
    /// Wait for element to not be visible.
    /// </summary>
    public virtual bool WaitNotVisible(int? timeoutMs = null)
    {
        Log($"WaitNotVisible()");
        return _context.WaitFor(() => !IsVisible(), timeoutMs, $"element '{AutomationId}' not visible");
    }

    /// <summary>
    /// Wait for element to be enabled or disabled.
    /// </summary>
    public virtual bool WaitEnabled(bool expected = true, int? timeoutMs = null)
    {
        Log($"WaitEnabled(expected={expected})");
        if (expected)
            return _context.WaitFor(IsEnabled, timeoutMs, $"element '{AutomationId}' enabled");
        else
            return _context.WaitFor(() => !IsEnabled(), timeoutMs, $"element '{AutomationId}' disabled");
    }

    /// <summary>
    /// Wait for element text to equal expected value.
    /// </summary>
    public virtual bool WaitTextEquals(string expected, int? timeoutMs = null)
    {
        Log($"WaitTextEquals(\"{expected}\")");
        return _context.WaitFor(() => GetText() == expected, timeoutMs,
            $"element '{AutomationId}' text = '{expected}'");
    }

    /// <summary>
    /// Wait for element text to contain expected value.
    /// </summary>
    public virtual bool WaitTextContains(string expected, int? timeoutMs = null)
    {
        Log($"WaitTextContains(\"{expected}\")");
        return _context.WaitFor(() => GetText().Contains(expected), timeoutMs,
            $"element '{AutomationId}' text contains '{expected}'");
    }

    #endregion

    #region Check Methods (Throw if condition not met, with screenshot capture)

    /// <summary>
    /// Check element exists - waits and throws if not met.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void CheckExists(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitExists(expected, timeoutMs))
        {
            var state = expected ? "exist" : "not exist";
            ThrowCheckFailed("Exists",
                $"Expected element '{AutomationId}' to {state} but it did not within timeout.");
        }
    }

    /// <summary>
    /// Check element visibility - waits and throws if not met.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void CheckVisible(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitVisible(expected, timeoutMs))
        {
            var state = expected ? "visible" : "not visible";
            ThrowCheckFailed("Visible",
                $"Expected element '{AutomationId}' to be {state} but it was not within timeout.");
        }
    }

    /// <summary>
    /// Check element enabled state - waits and throws if not met.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void CheckEnabled(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitEnabled(expected, timeoutMs))
        {
            var state = expected ? "enabled" : "disabled";
            ThrowCheckFailed("Enabled",
                $"Expected element '{AutomationId}' to be {state} but it was not within timeout.");
        }
    }

    #endregion

    #region Assert Methods (For test assertions with logging and screenshot capture)

    /// <summary>
    /// Assert element exists.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertExists(string? message = null)
    {
        if (!IsExists())
        {
            ThrowAssertionFailed("Exists", "false", "true",
                message ?? $"Expected element '{AutomationId}' to exist but it did not.");
        }
        LogAssertPass("Exists", "true", "true");
    }

    /// <summary>
    /// Assert element does not exist.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertNotExists(string? message = null)
    {
        if (IsExists())
        {
            ThrowAssertionFailed("NotExists", "true", "false",
                message ?? $"Expected element '{AutomationId}' to not exist but it did.");
        }
        LogAssertPass("NotExists", "false", "false");
    }

    /// <summary>
    /// Assert element is visible.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertVisible(string? message = null)
    {
        if (!IsVisible())
        {
            ThrowAssertionFailed("Visible", "false", "true",
                message ?? $"Expected element '{AutomationId}' to be visible but it was not.");
        }
        LogAssertPass("Visible", "true", "true");
    }

    /// <summary>
    /// Assert element is not visible.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertNotVisible(string? message = null)
    {
        if (IsVisible())
        {
            ThrowAssertionFailed("NotVisible", "true", "false",
                message ?? $"Expected element '{AutomationId}' to not be visible but it was visible.");
        }
        LogAssertPass("NotVisible", "false", "false");
    }

    /// <summary>
    /// Assert element is enabled.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertEnabled(string? message = null)
    {
        CheckVisible(expected: true);
        if (!IsEnabled())
        {
            ThrowAssertionFailed("Enabled", "false", "true",
                message ?? $"Expected element '{AutomationId}' to be enabled but it was disabled.");
        }
        LogAssertPass("Enabled", "true", "true");
    }

    /// <summary>
    /// Assert element is disabled.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertDisabled(string? message = null)
    {
        CheckVisible(expected: true);
        if (IsEnabled())
        {
            ThrowAssertionFailed("Disabled", "true", "false",
                message ?? $"Expected element '{AutomationId}' to be disabled but it was enabled.");
        }
        LogAssertPass("Disabled", "false", "false");
    }

    /// <summary>
    /// Assert element text equals expected.
    /// Captures screenshot on failure.
    /// </summary>
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

    /// <summary>
    /// Assert element text contains expected.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertTextContains(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetText();
        if (!actual.Contains(expected))
        {
            ThrowAssertionFailed("TextContains", actual, expected,
                message ?? $"Expected text to contain '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("TextContains", actual, expected);
    }
    
    /// <summary>
    /// Assert element text is empty.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertTextEmpty(string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetText();
        if (!string.IsNullOrEmpty(actual))
        {
            ThrowAssertionFailed("TextEmpty", actual, "(empty)",
                message ?? $"Expected empty text but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("TextEmpty", "(empty)", "(empty)");
    }
    
    /// <summary>
    /// Assert element text is not empty.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertTextNotEmpty(string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetText();
        if (string.IsNullOrEmpty(actual))
        {
            ThrowAssertionFailed("TextNotEmpty", "(empty)", "(non-empty)",
                message ?? $"Expected non-empty text but got empty for element '{AutomationId}'.");
        }
        LogAssertPass("TextNotEmpty", actual, "(non-empty)");
    }
    
    /// <summary>
    /// Assert element text starts with expected prefix.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertTextStartsWith(string prefix, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetText();
        if (!actual.StartsWith(prefix, StringComparison.Ordinal))
        {
            ThrowAssertionFailed("TextStartsWith", actual, $"starts with '{prefix}'",
                message ?? $"Expected text to start with '{prefix}' but got '{actual}'.");
        }
        LogAssertPass("TextStartsWith", actual, prefix);
    }
    
    /// <summary>
    /// Assert element text ends with expected suffix.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertTextEndsWith(string suffix, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetText();
        if (!actual.EndsWith(suffix, StringComparison.Ordinal))
        {
            ThrowAssertionFailed("TextEndsWith", actual, $"ends with '{suffix}'",
                message ?? $"Expected text to end with '{suffix}' but got '{actual}'.");
        }
        LogAssertPass("TextEndsWith", actual, suffix);
    }
    
    /// <summary>
    /// Assert element text matches the specified regex pattern.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertTextMatches(string pattern, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetText();
        if (!System.Text.RegularExpressions.Regex.IsMatch(actual, pattern))
        {
            ThrowAssertionFailed("TextMatches", actual, $"matches pattern '{pattern}'",
                message ?? $"Expected text to match pattern '{pattern}' but got '{actual}'.");
        }
        LogAssertPass("TextMatches", actual, pattern);
    }
    
    /// <summary>
    /// Assert element has a CSS class.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertHasClass(string className, string? message = null)
    {
        CheckVisible(expected: true);
        if (!HasClass(className))
        {
            var classes = GetAttribute("class") ?? "(none)";
            ThrowAssertionFailed("HasClass", classes, className,
                message ?? $"Expected element to have class '{className}' but has '{classes}'.");
        }
        LogAssertPass("HasClass", className, className);
    }
    
    /// <summary>
    /// Assert element does not have a CSS class.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertNotHasClass(string className, string? message = null)
    {
        CheckVisible(expected: true);
        if (HasClass(className))
        {
            var classes = GetAttribute("class") ?? "(none)";
            ThrowAssertionFailed("NotHasClass", classes, $"not '{className}'",
                message ?? $"Expected element to not have class '{className}' but it does.");
        }
        LogAssertPass("NotHasClass", "(no class)", className);
    }
    
    /// <summary>
    /// Assert element attribute equals expected value.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertAttribute(string attributeName, string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetAttribute(attributeName) ?? "(null)";
        if (actual != expected)
        {
            ThrowAssertionFailed($"Attribute[{attributeName}]", actual, expected,
                message ?? $"Expected attribute '{attributeName}' to be '{expected}' but got '{actual}'.");
        }
        LogAssertPass($"Attribute[{attributeName}]", actual, expected);
    }
    
    /// <summary>
    /// Assert element has a non-empty placeholder attribute.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertHasPlaceholder(string? message = null)
    {
        CheckVisible(expected: true);
        var placeholder = GetAttribute("placeholder");
        if (string.IsNullOrEmpty(placeholder))
        {
            ThrowAssertionFailed("HasPlaceholder", "(none)", "(placeholder)",
                message ?? $"Expected element to have a placeholder but it doesn't.");
        }
        LogAssertPass("HasPlaceholder", placeholder, "(placeholder)");
    }

    #endregion
    
    #region HTML-specific helpers
    
    /// <summary>
    /// Get an attribute value from the element.
    /// </summary>
    public virtual string? GetAttribute(string attributeName)
    {
        var element = FindElement();
        return element?.GetAttribute(attributeName);
    }
    
    /// <summary>
    /// Get a CSS property value from the element.
    /// </summary>
    public virtual string? GetCssValue(string propertyName)
    {
        var element = FindElement();
        return element?.GetCssValue(propertyName);
    }
    
    /// <summary>
    /// Check if element has a specific CSS class.
    /// </summary>
    public virtual bool HasClass(string className)
    {
        var classAttr = GetAttribute("class");
        if (string.IsNullOrEmpty(classAttr)) return false;
        
        var classes = classAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return classes.Contains(className);
    }
    
    #endregion
}

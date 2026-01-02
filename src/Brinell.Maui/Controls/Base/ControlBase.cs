using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Exceptions;
using Brinell.Core.Logging;
using Brinell.Maui.Gestures;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// MAUI-specific base class for all UI controls.
/// Uses AppiumTestContext directly for Appium automation.
/// Implements the Is/Wait/Check/Assert pattern.
/// </summary>
public abstract class ControlBase : IControlObject
{
    protected readonly AppiumTestContext _context;
    protected readonly IPageObject? _page;
    protected readonly AppiumElement? _container;

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
    /// Searches from the app root.
    /// </summary>
    protected ControlBase(AppiumTestContext context, IPageObject? page, string automationId)
        : this(context, page, container: null, automationId)
    {
    }

    /// <summary>
    /// Create a control that searches within a container element.
    /// Use this for controls inside list items or repeated templates.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <param name="page">The parent page object.</param>
    /// <param name="container">The container element to search within, or null for app root.</param>
    /// <param name="automationId">The AutomationId of the control.</param>
    protected ControlBase(AppiumTestContext context, IPageObject? page, AppiumElement? container, string automationId)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _page = page;
        _container = container;
        AutomationId = automationId ?? throw new ArgumentNullException(nameof(automationId));
    }

    /// <summary>
    /// Create a control without page context (for global controls).
    /// </summary>
    protected ControlBase(AppiumTestContext context, string automationId)
        : this(context, null, null, automationId)
    {
    }

    #region Element Access
    
    /// <summary>
    /// Find the element by AutomationId using Appium.
    /// Searches within container if specified, otherwise from app root.
    /// </summary>
    protected AppiumElement? FindElement()
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
    protected AppiumElement? WaitForElementVisible(int? timeoutMs = null)
    {
        AppiumElement? element = null;
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
        return element?.Text ?? string.Empty;
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

    #endregion

    #region Gesture Methods (Touch Actions)

    /// <summary>
    /// Tap the control. Waits for visibility before tapping.
    /// </summary>
    public virtual void Tap()
    {
        var element = WaitForElementVisible();
        if (element == null)
            ThrowCheckFailed("Tap", $"Element '{AutomationId}' not visible for tap.");
        element!.Click();
        LogAction("Tap");
    }

    /// <summary>
    /// Click the control. Alias for Tap on mobile.
    /// </summary>
    public virtual void Click() => Tap();

    /// <summary>
    /// Double-tap the control. Waits for visibility before double-tapping.
    /// </summary>
    public virtual void DoubleTap()
    {
        var element = WaitForElementVisible();
        if (element == null)
            ThrowCheckFailed("DoubleTap", $"Element '{AutomationId}' not visible for double-tap.");
        _context.Driver.PerformDoubleTap(element!);
        LogAction("DoubleTap");
    }

    /// <summary>
    /// Long-press the control. Waits for visibility before long-pressing.
    /// </summary>
    /// <param name="durationMs">Duration of press in milliseconds. Default 1000ms.</param>
    public virtual void LongPress(int durationMs = 1000)
    {
        var element = WaitForElementVisible();
        if (element == null)
            ThrowCheckFailed("LongPress", $"Element '{AutomationId}' not visible for long press.");
        _context.Driver.PerformLongPress(element!, durationMs);
        LogAction("LongPress", durationMs.ToString());
    }

    /// <summary>
    /// Swipe in a direction starting from this control. Waits for visibility before swiping.
    /// </summary>
    /// <param name="direction">Direction to swipe.</param>
    /// <param name="distance">Distance in pixels. Default 200.</param>
    public virtual void Swipe(SwipeDirection direction, int distance = 200)
    {
        var element = WaitForElementVisible();
        if (element == null)
            ThrowCheckFailed("Swipe", $"Element '{AutomationId}' not visible for swipe.");
        _context.Driver.PerformSwipe(element!, direction, distance);
        LogAction("Swipe", $"{direction}, {distance}px");
    }

    /// <summary>
    /// Swipe left on this control. Waits for visibility before swiping.
    /// </summary>
    /// <param name="distance">Distance in pixels. Default 200.</param>
    public virtual void SwipeLeft(int distance = 200) => Swipe(SwipeDirection.Left, distance);

    /// <summary>
    /// Swipe right on this control. Waits for visibility before swiping.
    /// </summary>
    /// <param name="distance">Distance in pixels. Default 200.</param>
    public virtual void SwipeRight(int distance = 200) => Swipe(SwipeDirection.Right, distance);

    /// <summary>
    /// Swipe up on this control. Waits for visibility before swiping.
    /// </summary>
    /// <param name="distance">Distance in pixels. Default 200.</param>
    public virtual void SwipeUp(int distance = 200) => Swipe(SwipeDirection.Up, distance);

    /// <summary>
    /// Swipe down on this control. Waits for visibility before swiping.
    /// </summary>
    /// <param name="distance">Distance in pixels. Default 200.</param>
    public virtual void SwipeDown(int distance = 200) => Swipe(SwipeDirection.Down, distance);

    #endregion
}

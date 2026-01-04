using System.Text.RegularExpressions;
using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using By = Brinell.Core.ControlObject6.Locators.By;
using SeleniumBy = OpenQA.Selenium.By;

namespace Brinell.Maui.Tests.ControlObject6.Mocks;

/// <summary>
/// Testable version of ControlObjectBase that uses TestableMauiTestContext.
/// This allows unit testing control behavior without a real Appium driver.
/// </summary>
public abstract class TestableControlBase : IInteractiveControlObject
{
    private readonly TestableMauiTestContext _context;

    /// <inheritdoc />
    public ControlLocator Locator { get; }

    /// <inheritdoc />
    public IPageObject? Page { get; }

    /// <summary>
    /// Gets the test context.
    /// </summary>
    protected TestableMauiTestContext Context => _context;

    /// <summary>
    /// Gets the default timeout in milliseconds.
    /// </summary>
    protected int DefaultTimeoutMs => _context.DefaultTimeoutMs;

    /// <summary>
    /// Gets the default polling interval in milliseconds.
    /// </summary>
    protected int DefaultPollingIntervalMs => _context.DefaultPollingIntervalMs;

    /// <summary>
    /// Creates a new control object.
    /// </summary>
    protected TestableControlBase(TestableMauiTestContext context, ControlLocator locator, IPageObject? page)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
        Page = page;
    }

    /// <summary>
    /// Creates a new control object using AutomationId.
    /// </summary>
    protected TestableControlBase(TestableMauiTestContext context, string automationId, IPageObject? page)
        : this(context, By.AutomationId(automationId), page)
    {
    }

    #region Logging

    /// <summary>
    /// Logs a message using the test context.
    /// </summary>
    protected void Log(string message)
    {
        Context.Log($"[{GetType().Name}] {Locator}: {message}");
    }

    #endregion

    #region Element Finding

    /// <summary>
    /// Finds the element using the locator. Returns null if not found.
    /// </summary>
    protected IWebElement? FindElement()
    {
        try
        {
            var by = ConvertLocator(Locator);
            return Context.FindElement(by);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>
    /// Finds the element, throwing if not found.
    /// </summary>
    protected IWebElement FindElementRequired(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var element = WaitFor(() => FindElement(), timeout);
        
        if (element is null)
        {
            throw new ElementNotFoundException(
                $"Element not found: {Locator}");
        }

        return element;
    }

    /// <summary>
    /// Converts a ControlLocator to a Selenium By locator.
    /// </summary>
    protected SeleniumBy ConvertLocator(ControlLocator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => MobileBy.AccessibilityId(locator.Value),
            LocatorStrategy.AccessibilityId => MobileBy.AccessibilityId(locator.Value),
            LocatorStrategy.Id => SeleniumBy.Id(locator.Value),
            LocatorStrategy.Name => SeleniumBy.Name(locator.Value),
            LocatorStrategy.ClassName => SeleniumBy.ClassName(locator.Value),
            LocatorStrategy.XPath => SeleniumBy.XPath(locator.Value),
            LocatorStrategy.Css => SeleniumBy.CssSelector(locator.Value),
            LocatorStrategy.TagName => SeleniumBy.TagName(locator.Value),
            LocatorStrategy.Text => SeleniumBy.XPath($"//*[text()='{locator.Value}']"),
            LocatorStrategy.PartialText => SeleniumBy.XPath($"//*[contains(text(),'{locator.Value}')]"),
            LocatorStrategy.Label => MobileBy.AccessibilityId(locator.Value),
            LocatorStrategy.TestId => MobileBy.AccessibilityId(locator.Value),
            _ => throw new NotSupportedException($"Locator strategy '{locator.Strategy}' is not supported.")
        };
    }

    #endregion

    #region Waiting

    /// <summary>
    /// Waits for a condition to be true.
    /// </summary>
    protected T? WaitFor<T>(Func<T?> condition, int timeoutMs, int? pollingMs = null) where T : class
    {
        var polling = pollingMs ?? DefaultPollingIntervalMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            var result = condition();
            if (result is not null)
                return result;

            Thread.Sleep(polling);
        }

        return default;
    }

    /// <summary>
    /// Waits for a boolean condition to be true.
    /// </summary>
    protected bool WaitForBool(Func<bool> condition, bool expected, int timeoutMs, int? pollingMs = null)
    {
        var polling = pollingMs ?? DefaultPollingIntervalMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            if (condition() == expected)
                return true;

            Thread.Sleep(polling);
        }

        return false;
    }

    #endregion

    #region Existence

    /// <inheritdoc />
    public bool IsExists() => FindElement() is not null;

    /// <inheritdoc />
    public bool WaitExists(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return WaitForBool(IsExists, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public void CheckExists(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;
        
        if (!WaitExists(expected, timeoutMs))
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            throw new UITestTimeoutException(
                $"Element {(expected.Value ? "does not exist" : "still exists")}",
                Locator.Value,
                timeout,
                "CheckExists",
                $"Exists={IsExists()}");
        }
    }

    /// <inheritdoc />
    public void AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        CheckExists(expected, timeoutMs);

        var actual = IsExists();
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected element to {(expected.Value ? "exist" : "not exist")}, but it {(actual ? "exists" : "does not exist")}",
                Locator.Value,
                "AssertExists");
        }
    }

    #endregion

    #region Visibility

    /// <inheritdoc />
    public bool IsVisible()
    {
        var element = FindElement();
        return element is not null && element.Displayed;
    }

    /// <inheritdoc />
    public bool WaitVisible(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return WaitForBool(IsVisible, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public void CheckVisible(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (!WaitVisible(expected, timeoutMs))
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            throw new UITestTimeoutException(
                $"Element is {(expected.Value ? "not visible" : "still visible")}",
                Locator.Value,
                timeout,
                "CheckVisible",
                $"Visible={IsVisible()}");
        }
    }

    /// <inheritdoc />
    public void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        CheckVisible(expected, timeoutMs);

        var actual = IsVisible();
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected element to be {(expected.Value ? "visible" : "not visible")}",
                Locator.Value,
                "AssertVisible");
        }
    }

    #endregion

    #region Enabled

    /// <inheritdoc />
    public bool IsEnabled()
    {
        var element = FindElement();
        return element is not null && element.Enabled;
    }

    /// <inheritdoc />
    public bool WaitEnabled(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return WaitForBool(IsEnabled, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public void CheckEnabled(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (!WaitEnabled(expected, timeoutMs))
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            throw new UITestTimeoutException(
                $"Element is {(expected.Value ? "not enabled" : "still enabled")}",
                Locator.Value,
                timeout,
                "CheckEnabled",
                $"Enabled={IsEnabled()}");
        }
    }

    /// <inheritdoc />
    public void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        CheckEnabled(expected, timeoutMs);

        var actual = IsEnabled();
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected element to be {(expected.Value ? "enabled" : "disabled")}",
                Locator.Value,
                "AssertEnabled");
        }
    }

    #endregion

    #region Text

    /// <inheritdoc />
    public virtual string GetText(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.Text ?? string.Empty;
    }

    /// <inheritdoc />
    public void AssertText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetText(timeoutMs);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected text '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertText");
        }
    }

    /// <inheritdoc />
    public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetText(timeoutMs);
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new AssertionException(
                message ?? $"Expected text to contain '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertTextContains");
        }
    }

    /// <inheritdoc />
    public void AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetText(timeoutMs);
        if (!actual.StartsWith(expected, StringComparison.Ordinal))
        {
            throw new AssertionException(
                message ?? $"Expected text to start with '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertTextStartsWith");
        }
    }

    /// <inheritdoc />
    public void AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetText(timeoutMs);
        if (!actual.EndsWith(expected, StringComparison.Ordinal))
        {
            throw new AssertionException(
                message ?? $"Expected text to end with '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertTextEndsWith");
        }
    }

    /// <inheritdoc />
    public void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null)
    {
        if (pattern is null) return;

        var actual = GetText(timeoutMs);
        if (!Regex.IsMatch(actual, pattern))
        {
            throw new AssertionException(
                message ?? $"Expected text to match pattern '{pattern}', but was '{actual}'",
                Locator.Value,
                "AssertTextMatches");
        }
    }

    /// <inheritdoc />
    public void AssertTextEmpty(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetText(timeoutMs);
        var isEmpty = string.IsNullOrEmpty(actual);

        if (isEmpty != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected text to be {(expected.Value ? "empty" : "not empty")}, but was '{actual}'",
                Locator.Value,
                "AssertTextEmpty");
        }
    }

    #endregion
}

/// <summary>
/// Testable clickable control base.
/// </summary>
public abstract class TestableClickableControlBase : TestableControlBase, IClickableControlObject
{
    protected TestableClickableControlBase(TestableMauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    protected TestableClickableControlBase(TestableMauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public virtual void Click(int? timeoutMs = null)
    {
        Log("Click");
        var element = FindElementRequired(timeoutMs);
        element.Click();
    }

    /// <inheritdoc />
    public virtual void DoubleClick(int? timeoutMs = null)
    {
        Log("DoubleClick");
        var element = FindElementRequired(timeoutMs);
        // In a real implementation, this would do a double click
        element.Click();
        element.Click();
    }

    /// <inheritdoc />
    public virtual void RightClick(int? timeoutMs = null)
    {
        Log("RightClick");
        // In a real implementation, this would do a right click
        var element = FindElementRequired(timeoutMs);
    }

    /// <inheritdoc />
    public virtual void Hover(int? timeoutMs = null)
    {
        Log("Hover");
        // In a real implementation, this would hover the element
        var element = FindElementRequired(timeoutMs);
    }

    /// <inheritdoc />
    public virtual void LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        Log("LongPress");
        // In a real implementation, this would do a long press
        var element = FindElementRequired(timeoutMs);
    }
}

/// <summary>
/// Testable text control base.
/// </summary>
public abstract class TestableTextControlBase : TestableClickableControlBase, ITextControlObject
{
    protected TestableTextControlBase(TestableMauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    protected TestableTextControlBase(TestableMauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    #region IFocusableControlObject

    /// <inheritdoc />
    public virtual bool IsFocused() => false; // Simplified for testing

    /// <inheritdoc />
    public virtual bool WaitFocused(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return WaitForBool(IsFocused, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public virtual void CheckFocused(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;
        // Simplified for testing
    }

    /// <inheritdoc />
    public virtual void AssertFocused(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        // Simplified for testing
    }

    /// <inheritdoc />
    public virtual void Focus(int? timeoutMs = null)
    {
        Log("Focus");
        var element = FindElementRequired(timeoutMs);
        element.Click();
    }

    /// <inheritdoc />
    public virtual void Blur(int? timeoutMs = null)
    {
        Log("Blur");
        // Simplified for testing
    }

    #endregion

    #region ITextControlObject

    /// <inheritdoc />
    public virtual void Enter(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        
        Log($"Enter: {text}");
        var element = FindElementRequired(timeoutMs);
        element.Clear();
        element.SendKeys(text);
    }

    /// <inheritdoc />
    public virtual void Clear(int? timeoutMs = null)
    {
        Log("Clear");
        var element = FindElementRequired(timeoutMs);
        element.Clear();
    }

    /// <inheritdoc />
    public virtual void ClearAndEnter(string? text, int? timeoutMs = null)
    {
        Log($"ClearAndEnter: {text}");
        var element = FindElementRequired(timeoutMs);
        element.Clear();
        
        if (text is not null)
        {
            element.SendKeys(text);
        }
    }

    /// <inheritdoc />
    public virtual void Append(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        
        Log($"Append: {text}");
        var element = FindElementRequired(timeoutMs);
        element.SendKeys(text);
    }

    /// <inheritdoc />
    public virtual bool IsReadOnly() => false; // Simplified for testing

    /// <inheritdoc />
    public virtual bool WaitReadOnly(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return WaitForBool(IsReadOnly, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public virtual void AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        // Simplified for testing
    }

    /// <inheritdoc />
    public virtual int GetTextLength(int? timeoutMs = null)
    {
        return GetText(timeoutMs).Length;
    }

    /// <inheritdoc />
    public virtual void AssertTextLength(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        // Simplified for testing
    }

    #endregion
}

/// <summary>
/// Testable button control for unit testing.
/// </summary>
public class TestableButtonControl : TestableClickableControlBase
{
    public TestableButtonControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableButtonControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}

/// <summary>
/// Testable entry control for unit testing.
/// </summary>
public class TestableEntryControl : TestableTextControlBase
{
    public TestableEntryControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableEntryControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}

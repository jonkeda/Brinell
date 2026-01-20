using System.Text.RegularExpressions;
using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using By = Brinell.Core.ControlObject6.Locators.By;
using SeleniumBy = OpenQA.Selenium.By;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for all MAUI control objects.
/// Provides common functionality for element location, waiting, and assertions.
/// </summary>
public abstract class ControlObjectBase : IInteractiveControlObject
{
    private readonly MauiTestContext _context;

    /// <inheritdoc />
    public ControlLocator Locator { get; }

    /// <inheritdoc />
    public IPageObject? Page { get; }

    /// <summary>
    /// Gets the test context.
    /// </summary>
    protected MauiTestContext Context => _context;

    /// <summary>
    /// Gets the Appium driver.
    /// </summary>
    protected AppiumDriver Driver => _context.Driver;

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
    protected ControlObjectBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
        Page = page;
    }

    /// <summary>
    /// Creates a new control object using AutomationId.
    /// </summary>
    protected ControlObjectBase(MauiTestContext context, string automationId, IPageObject? page)
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
    protected AppiumElement? FindElement()
    {
        try
        {
            var by = ConvertLocator(Locator);
            return (AppiumElement)Driver.FindElement(by);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>
    /// Finds the element, throwing if not found.
    /// </summary>
    protected AppiumElement FindElementRequired(int? timeoutMs = null)
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

    #region Scroll Into View

    /// <summary>
    /// Scrolls the element into view if it exists but is not visible.
    /// This is useful for elements in ScrollView containers that report Displayed=false when off-screen.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual void ScrollIntoView(int? timeoutMs = null)
    {
        var element = FindElement();
        if (element is null) return;
        if (element.Displayed) return;

        Log("ScrollIntoView()");
        PerformScrollIntoView(element, timeoutMs);
    }

    /// <summary>
    /// Performs the actual scroll into view operation.
    /// Override this method to provide control-specific scrolling behavior.
    /// </summary>
    /// <param name="element">The element to scroll into view.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void PerformScrollIntoView(AppiumElement element, int? timeoutMs = null)
    {
        // Use mobile:scroll command if available, otherwise use coordinate-based scrolling
        try
        {
            // Get element location
            var location = element.Location;
            var size = element.Size;
            var centerX = location.X + size.Width / 2;
            var centerY = location.Y + size.Height / 2;

            // Get window size
            var windowSize = Driver.Manage().Window.Size;

            // If element is above visible area, scroll up
            // If element is below visible area, scroll down
            if (centerY < 0 || centerY > windowSize.Height)
            {
                // Calculate scroll target - aim to put element in center of screen
                var targetY = windowSize.Height / 2;
                var scrollAmount = centerY - targetY;

                // Perform scroll using touch action
                var finger = new OpenQA.Selenium.Interactions.PointerInputDevice(OpenQA.Selenium.Interactions.PointerKind.Touch, "finger");
                var sequence = new OpenQA.Selenium.Interactions.ActionSequence(finger, 0);

                var startX = windowSize.Width / 2;
                var startY = windowSize.Height / 2;
                var endY = startY - (int)(scrollAmount * 0.8); // Scroll proportionally

                // Clamp endY to valid range
                endY = Math.Max(100, Math.Min(windowSize.Height - 100, endY));

                sequence.AddAction(finger.CreatePointerMove(OpenQA.Selenium.Interactions.CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
                sequence.AddAction(finger.CreatePointerDown(OpenQA.Selenium.Interactions.MouseButton.Left));
                sequence.AddAction(finger.CreatePointerMove(OpenQA.Selenium.Interactions.CoordinateOrigin.Viewport, startX, endY, TimeSpan.FromMilliseconds(300)));
                sequence.AddAction(finger.CreatePointerUp(OpenQA.Selenium.Interactions.MouseButton.Left));

                Driver.PerformActions(new List<OpenQA.Selenium.Interactions.ActionSequence> { sequence });

                // Wait briefly for scroll animation
                Thread.Sleep(200);
            }
        }
        catch (Exception ex)
        {
            Log($"ScrollIntoView failed: {ex.Message}");
            // Swallow exception - element may still become visible
        }
    }

    /// <summary>
    /// Ensures the element is ready for interaction by checking existence, scrolling into view if needed,
    /// verifying visibility, and checking enabled state.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual void EnsureInteractable(int? timeoutMs = null)
    {
        CheckExists(true, timeoutMs);
        ScrollIntoView(timeoutMs);
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
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

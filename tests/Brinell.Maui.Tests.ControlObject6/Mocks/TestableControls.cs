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

/// <summary>
/// Testable label control for unit testing.
/// </summary>
public class TestableLabelControl : TestableControlBase
{
    public TestableLabelControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableLabelControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}

/// <summary>
/// Testable toggle control base for unit testing.
/// </summary>
public abstract class TestableToggleControlBase : TestableClickableControlBase, IToggleControlObject
{
    protected TestableToggleControlBase(TestableMauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    protected TestableToggleControlBase(TestableMauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    private bool _isChecked = false;

    /// <inheritdoc />
    public virtual bool IsChecked()
    {
        var element = FindElement();
        if (element is null) return false;

        // Check for checked attribute (mocked behavior - tests use "checked")
        var isChecked = element.GetAttribute("checked");
        if (isChecked is not null)
        {
            return isChecked.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        
        // Fallback to IsChecked attribute
        isChecked = element.GetAttribute("IsChecked");
        if (isChecked is not null)
        {
            return isChecked.Equals("True", StringComparison.OrdinalIgnoreCase);
        }
        
        return _isChecked;
    }

    /// <inheritdoc />
    public virtual bool WaitChecked(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return WaitForBool(IsChecked, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public virtual void CheckChecked(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitChecked(expected, timeoutMs);
    }

    /// <inheritdoc />
    public virtual void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = IsChecked();
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected control to be {(expected.Value ? "checked" : "unchecked")}, but was {(actual ? "checked" : "unchecked")}",
                Locator.Value,
                "AssertChecked");
        }
    }

    /// <inheritdoc />
    public virtual void Check(int? timeoutMs = null)
    {
        Log("Check");
        if (!IsChecked())
        {
            Toggle(timeoutMs);
        }
    }

    /// <inheritdoc />
    public virtual void Uncheck(int? timeoutMs = null)
    {
        Log("Uncheck");
        if (IsChecked())
        {
            Toggle(timeoutMs);
        }
    }

    /// <inheritdoc />
    public virtual void Toggle(int? timeoutMs = null)
    {
        Log("Toggle");
        var element = FindElementRequired(timeoutMs);
        element.Click();
        _isChecked = !_isChecked;
    }

    /// <inheritdoc />
    public virtual void SetChecked(bool? value, int? timeoutMs = null)
    {
        if (value is null) return;

        Log($"SetChecked: {value}");
        if (value.Value)
        {
            Check(timeoutMs);
        }
        else
        {
            Uncheck(timeoutMs);
        }
    }
}

/// <summary>
/// Testable checkbox control for unit testing.
/// </summary>
public class TestableCheckBoxControl : TestableToggleControlBase
{
    public TestableCheckBoxControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableCheckBoxControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}

/// <summary>
/// Testable switch control for unit testing.
/// </summary>
public class TestableSwitchControl : TestableToggleControlBase
{
    public TestableSwitchControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableSwitchControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}

/// <summary>
/// Testable selector control base for unit testing.
/// </summary>
public abstract class TestableSelectorControlBase : TestableClickableControlBase, ISelectorControlObject
{
    protected TestableSelectorControlBase(TestableMauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    protected TestableSelectorControlBase(TestableMauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    private int _selectedIndex = -1;
    private readonly List<string> _items = new() { "Item 1", "Item 2", "Item 3" };

    /// <inheritdoc />
    public virtual int GetSelectedIndex(int? timeoutMs = null)
    {
        return _selectedIndex;
    }

    /// <inheritdoc />
    public virtual string GetSelectedText(int? timeoutMs = null)
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
            return string.Empty;
        return _items[_selectedIndex];
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<string> GetItemTexts(int? timeoutMs = null)
    {
        return _items.AsReadOnly();
    }

    /// <inheritdoc />
    public virtual int GetItemCount(int? timeoutMs = null)
    {
        return _items.Count;
    }

    /// <inheritdoc />
    public virtual bool HasItem(string text, int? timeoutMs = null)
    {
        return _items.Contains(text);
    }

    /// <inheritdoc />
    public virtual void SelectByIndex(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        
        Log($"SelectByIndex: {index}");
        FindElementRequired(timeoutMs);
        _selectedIndex = index.Value;
    }

    /// <inheritdoc />
    public virtual void SelectByText(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        
        Log($"SelectByText: {text}");
        FindElementRequired(timeoutMs);
        var index = _items.IndexOf(text);
        if (index >= 0)
        {
            _selectedIndex = index;
        }
    }

    /// <inheritdoc />
    public virtual void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedIndex(timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected selected index {expected}, but was {actual}",
                Locator.Value,
                "AssertSelectedIndex");
        }
    }

    /// <inheritdoc />
    public virtual void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedText(timeoutMs);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected selected text '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertSelectedText");
        }
    }

    /// <inheritdoc />
    public virtual void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetItemCount(timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected {expected} items, but was {actual}",
                Locator.Value,
                "AssertItemCount");
        }
    }
}

/// <summary>
/// Testable picker control for unit testing.
/// </summary>
public class TestablePickerControl : TestableSelectorControlBase
{
    public TestablePickerControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestablePickerControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets whether an item is selected.
    /// </summary>
    public bool HasSelection()
    {
        return GetSelectedIndex() >= 0;
    }

    /// <summary>
    /// Asserts that the picker has items.
    /// </summary>
    public void AssertHasItems()
    {
        if (GetItemCount() == 0)
        {
            throw new AssertionException(
                "Expected picker to have items, but it was empty",
                Locator.Value,
                "AssertHasItems");
        }
    }

    /// <inheritdoc />
    public override int GetSelectedIndex(int? timeoutMs = null)
    {
        var element = FindElement();
        if (element is null) return -1;

        // Check for selectedIndex attribute (for mocked tests)
        var selectedIndex = element.GetAttribute("selectedIndex");
        if (selectedIndex is not null && int.TryParse(selectedIndex, out var index))
        {
            return index;
        }

        return base.GetSelectedIndex(timeoutMs);
    }

    /// <inheritdoc />
    public override void SelectByIndex(int? index, int? timeoutMs = null)
    {
        // Call base to set internal state, then perform element interaction
        base.SelectByIndex(index, timeoutMs);
        
        if (index is null) return;
        var element = FindElement();
        element?.SendKeys(index.Value.ToString());
    }

    /// <inheritdoc />
    public override void SelectByText(string? text, int? timeoutMs = null)
    {
        // Call base to set internal state, then perform element interaction
        base.SelectByText(text, timeoutMs);
        
        if (text is null) return;
        var element = FindElement();
        element?.SendKeys(text);
    }

    /// <inheritdoc />
    public override int GetItemCount(int? timeoutMs = null)
    {
        var element = FindElement();
        if (element is null) return 0;

        // Check for itemCount attribute (for mocked tests)
        var itemCount = element.GetAttribute("itemCount");
        if (itemCount is not null && int.TryParse(itemCount, out var count))
        {
            return count;
        }

        return base.GetItemCount(timeoutMs);
    }
}

/// <summary>
/// Testable items control base for unit testing.
/// </summary>
public abstract class TestableItemsControlBase : TestableControlBase, IItemsControlObject
{
    protected TestableItemsControlBase(TestableMauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    protected TestableItemsControlBase(TestableMauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    protected readonly List<string> Items = new() { "Item 1", "Item 2", "Item 3", "Item 4", "Item 5" };

    /// <inheritdoc />
    public virtual int GetItemCount(int? timeoutMs = null)
    {
        return Items.Count;
    }

    /// <inheritdoc />
    public virtual bool WaitItemCount(int? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return WaitForBool(() => GetItemCount() == expected.Value, true, timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public virtual void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetItemCount(timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected {expected} items, but was {actual}",
                Locator.Value,
                "AssertItemCount");
        }
    }

    /// <inheritdoc />
    public virtual string GetItemText(int index, int? timeoutMs = null)
    {
        if (index < 0 || index >= Items.Count)
            return string.Empty;
        return Items[index];
    }

    /// <inheritdoc />
    public virtual void AssertItemText(int index, string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetItemText(index, timeoutMs);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected item at index {index} to have text '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertItemText");
        }
    }

    /// <inheritdoc />
    public virtual bool HasItem(string text, int? timeoutMs = null)
    {
        return Items.Contains(text);
    }

    /// <inheritdoc />
    public virtual int GetItemIndex(string text, int? timeoutMs = null)
    {
        return Items.IndexOf(text);
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<string> GetAllItemTexts(int? timeoutMs = null)
    {
        return Items.AsReadOnly();
    }

    /// <inheritdoc />
    public virtual void ClickItem(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"ClickItem at index: {index}");
    }

    /// <inheritdoc />
    public virtual void ClickItem(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        Log($"ClickItem with text: {text}");
    }
}

/// <summary>
/// Testable selectable items control base for unit testing.
/// </summary>
public abstract class TestableSelectableItemsControlBase : TestableItemsControlBase, ISelectableItemsControlObject
{
    protected TestableSelectableItemsControlBase(TestableMauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    protected TestableSelectableItemsControlBase(TestableMauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    private int _selectedIndex = -1;

    /// <inheritdoc />
    public virtual void SelectItem(int? index, int? timeoutMs = null)
    {
        if (index is null) return;

        Log($"SelectItem: {index}");
        FindElementRequired(timeoutMs);
        _selectedIndex = index.Value;
    }

    /// <inheritdoc />
    public virtual void SelectItem(string? text, int? timeoutMs = null)
    {
        if (text is null) return;

        Log($"SelectItem: {text}");
        FindElementRequired(timeoutMs);
        var index = Items.IndexOf(text);
        if (index >= 0)
        {
            _selectedIndex = index;
        }
    }

    /// <inheritdoc />
    public virtual int GetSelectedItemIndex(int? timeoutMs = null)
    {
        return _selectedIndex;
    }

    /// <inheritdoc />
    public virtual void AssertSelectedItemIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedItemIndex(timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected selected index {expected}, but was {actual}",
                Locator.Value,
                "AssertSelectedItemIndex");
        }
    }

    /// <inheritdoc />
    public virtual string? GetSelectedItemText(int? timeoutMs = null)
    {
        if (_selectedIndex < 0 || _selectedIndex >= Items.Count)
            return null;
        return Items[_selectedIndex];
    }

    /// <inheritdoc />
    public virtual void AssertSelectedItemText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedItemText(timeoutMs);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected selected item '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertSelectedItemText");
        }
    }

    /// <inheritdoc />
    public virtual bool IsItemSelected(int index, int? timeoutMs = null)
    {
        return _selectedIndex == index;
    }

    /// <inheritdoc />
    public virtual void AssertItemSelected(int index, bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = IsItemSelected(index, timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected item at index {index} to be {(expected.Value ? "selected" : "not selected")}",
                Locator.Value,
                "AssertItemSelected");
        }
    }
}

/// <summary>
/// Testable list view control for unit testing.
/// </summary>
public class TestableListViewControl : TestableSelectableItemsControlBase
{
    public TestableListViewControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableListViewControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the item count from child elements.
    /// </summary>
    public override int GetItemCount(int? timeoutMs = null)
    {
        var element = FindElement();
        if (element is null) return 0;

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        return items.Count;
    }

    /// <summary>
    /// Gets whether the list is empty.
    /// </summary>
    public bool IsEmpty()
    {
        return GetItemCount() == 0;
    }

    /// <summary>
    /// Selects an item by index.
    /// </summary>
    public void SelectItemByIndex(int index)
    {
        var element = FindElement();
        if (element is null) return;

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        if (index >= 0 && index < items.Count)
        {
            items[index].Click();
        }
    }

    /// <summary>
    /// Selects an item by text.
    /// </summary>
    public void SelectItemByText(string text)
    {
        var element = FindElement();
        if (element is null) return;

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        foreach (var item in items)
        {
            if (item.Text == text)
            {
                item.Click();
                return;
            }
        }
    }

    /// <summary>
    /// Gets the selected item text (with non-nullable signature for backward compat).
    /// </summary>
    public string? GetSelectedItemTextLocal()
    {
        var element = FindElement();
        if (element is null) return null;

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        foreach (var item in items)
        {
            if (item.GetAttribute("selected") == "true")
            {
                return item.Text;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets whether any item is selected.
    /// </summary>
    public bool HasSelectedItem()
    {
        var element = FindElement();
        if (element is null) return false;

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        return items.Any(item => item.GetAttribute("selected") == "true");
    }

    /// <summary>
    /// Scrolls to item by index.
    /// </summary>
    public void ScrollToItemByIndex(int index)
    {
        Log($"ScrollToItemByIndex: {index}");
        // In a real implementation, this would scroll to the item
    }

    /// <summary>
    /// Scrolls to the top of the list.
    /// </summary>
    public void ScrollToTop()
    {
        Log("ScrollToTop");
        // In a real implementation, this would scroll to the top
    }

    /// <summary>
    /// Scrolls to the bottom of the list.
    /// </summary>
    public void ScrollToBottom()
    {
        Log("ScrollToBottom");
        // In a real implementation, this would scroll to the bottom
    }

    /// <summary>
    /// Gets the item text at index.
    /// </summary>
    public string? GetItemTextAtIndex(int index)
    {
        var element = FindElement();
        if (element is null) return null;

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        if (index >= 0 && index < items.Count)
        {
            return items[index].Text;
        }
        return null;
    }
}

/// <summary>
/// Testable collection view control for unit testing.
/// </summary>
public class TestableCollectionViewControl : TestableSelectableItemsControlBase
{
    public TestableCollectionViewControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableCollectionViewControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the item count from child elements.
    /// </summary>
    public override int GetItemCount(int? timeoutMs = null)
    {
        var element = FindElement();
        if (element is null) return 0;

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        return items.Count;
    }

    /// <summary>
    /// Gets whether the collection is empty.
    /// </summary>
    public bool IsEmpty()
    {
        return GetItemCount() == 0;
    }

    /// <summary>
    /// Selects an item by index.
    /// </summary>
    public void SelectItemByIndex(int index)
    {
        var element = FindElement();
        if (element is null) return;

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        if (index >= 0 && index < items.Count)
        {
            items[index].Click();
        }
    }

    /// <summary>
    /// Selects an item by text.
    /// </summary>
    public void SelectItemByText(string text)
    {
        var element = FindElement();
        if (element is null) return;

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        foreach (var item in items)
        {
            if (item.Text == text)
            {
                item.Click();
                return;
            }
        }
    }

    /// <summary>
    /// Gets the selected items text.
    /// </summary>
    public IReadOnlyList<string> GetSelectedItemsText()
    {
        var element = FindElement();
        if (element is null) return Array.Empty<string>();

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        var selectedItems = new List<string>();
        foreach (var item in items)
        {
            if (item.GetAttribute("selected") == "true")
            {
                selectedItems.Add(item.Text);
            }
        }
        return selectedItems;
    }

    /// <summary>
    /// Gets whether any items are selected.
    /// </summary>
    public bool HasSelectedItems()
    {
        var element = FindElement();
        if (element is null) return false;

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        return items.Any(item => item.GetAttribute("selected") == "true");
    }

    /// <summary>
    /// Scrolls to item by index.
    /// </summary>
    public void ScrollToItemByIndex(int index)
    {
        Log($"ScrollToItemByIndex: {index}");
        // In a real implementation, this would scroll to the item
    }

    /// <summary>
    /// Scrolls to the top of the collection.
    /// </summary>
    public void ScrollToTop()
    {
        Log("ScrollToTop");
        // In a real implementation, this would scroll to the top
    }

    /// <summary>
    /// Scrolls to the bottom of the collection.
    /// </summary>
    public void ScrollToBottom()
    {
        Log("ScrollToBottom");
        // In a real implementation, this would scroll to the bottom
    }

    /// <summary>
    /// Gets the item text at index.
    /// </summary>
    public string? GetItemTextAtIndex(int index)
    {
        var element = FindElement();
        if (element is null) return null;

        var items = element.FindElements(OpenQA.Selenium.By.XPath(".//*"));
        if (index >= 0 && index < items.Count)
        {
            return items[index].Text;
        }
        return null;
    }
}

/// <summary>
/// Testable editor control for unit testing.
/// </summary>
public class TestableEditorControl : TestableTextControlBase
{
    public TestableEditorControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableEditorControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Enters text into the editor.
    /// </summary>
    public void EnterText(string text)
    {
        Log($"EnterText: {text}");
        var element = FindElementRequired();
        element.SendKeys(text);
    }

    /// <summary>
    /// Sets the text of the editor (clear + enter).
    /// </summary>
    public void SetText(string text)
    {
        Log($"SetText: {text}");
        var element = FindElementRequired();
        element.Clear();
        element.SendKeys(text);
    }

    /// <summary>
    /// Appends text to the editor.
    /// </summary>
    public void AppendText(string text)
    {
        Log($"AppendText: {text}");
        var element = FindElementRequired();
        element.SendKeys(text);
    }

    /// <summary>
    /// Gets the line count of the text.
    /// </summary>
    public int GetLineCount()
    {
        var text = GetText();
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Split('\n').Length;
    }

    /// <summary>
    /// Asserts that the editor is empty.
    /// </summary>
    public void AssertIsEmpty()
    {
        var text = GetText();
        if (!string.IsNullOrEmpty(text))
        {
            throw new AssertionException(
                $"Expected editor to be empty, but was '{text}'",
                Locator.Value,
                "AssertIsEmpty");
        }
    }

    /// <summary>
    /// Asserts that the editor is not empty.
    /// </summary>
    public void AssertIsNotEmpty()
    {
        var text = GetText();
        if (string.IsNullOrEmpty(text))
        {
            throw new AssertionException(
                "Expected editor to have text, but it was empty",
                Locator.Value,
                "AssertIsNotEmpty");
        }
    }
}

/// <summary>
/// Testable RadioButton control for unit testing.
/// RadioButton can only be checked, not unchecked directly.
/// </summary>
public class TestableRadioButtonControl : TestableToggleControlBase
{
    public TestableRadioButtonControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableRadioButtonControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Selects this radio button. Alias for Check.
    /// </summary>
    public void Select(int? timeoutMs = null)
    {
        Log("Select()");
        Check(timeoutMs);
    }

    /// <inheritdoc />
    /// <remarks>
    /// RadioButtons cannot be unchecked directly - select another RadioButton in the group instead.
    /// This method is a no-op for RadioButton.
    /// </remarks>
    public override void Uncheck(int? timeoutMs = null)
    {
        Log("Uncheck() - RadioButton cannot be unchecked directly");
        // RadioButtons cannot be unchecked directly
    }
}

/// <summary>
/// Abstract base class for testable range controls.
/// </summary>
public abstract class TestableRangeControlBase : TestableControlBase, IRangeControlObject
{
    protected TestableRangeControlBase(TestableMauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    protected TestableRangeControlBase(TestableMauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    protected double _value = 50;
    protected double _minimum = 0;
    protected double _maximum = 100;

    /// <inheritdoc />
    public virtual double GetValue(int? timeoutMs = null)
    {
        FindElement();
        return _value;
    }

    /// <inheritdoc />
    public virtual void SetValue(double? value, int? timeoutMs = null)
    {
        if (value is null) return;
        
        Log($"SetValue({value})");
        FindElementRequired(timeoutMs);
        
        // Clamp to range
        _value = Math.Clamp(value.Value, _minimum, _maximum);
    }

    /// <inheritdoc />
    public virtual bool WaitValue(double? expected, double tolerance = 0.01, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return Math.Abs(_value - expected.Value) <= tolerance;
    }

    /// <inheritdoc />
    public virtual void AssertValue(double? expected, double tolerance = 0.01, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (Math.Abs(_value - expected.Value) > tolerance)
        {
            throw new AssertionException(
                message ?? $"Expected value {expected} (±{tolerance}), but was {_value}",
                Locator.Value,
                "AssertValue");
        }
    }

    /// <inheritdoc />
    public virtual double GetMinimum(int? timeoutMs = null) => _minimum;

    /// <inheritdoc />
    public virtual double GetMaximum(int? timeoutMs = null) => _maximum;

    /// <inheritdoc />
    public virtual (double minimum, double maximum) GetRange(int? timeoutMs = null) => (_minimum, _maximum);

    /// <inheritdoc />
    public virtual double GetValuePercent(int? timeoutMs = null)
    {
        if (Math.Abs(_maximum - _minimum) < 0.0001) return 0;
        return (_value - _minimum) / (_maximum - _minimum);
    }

    /// <inheritdoc />
    public virtual void SetValuePercent(double? percent, int? timeoutMs = null)
    {
        if (percent is null) return;
        
        Log($"SetValuePercent({percent})");
        var p = Math.Clamp(percent.Value, 0, 1);
        _value = _minimum + (_maximum - _minimum) * p;
    }

    /// <inheritdoc />
    public virtual void Increase(int? timeoutMs = null)
    {
        Log("Increase()");
        var step = (_maximum - _minimum) / 10;
        _value = Math.Min(_value + step, _maximum);
    }

    /// <inheritdoc />
    public virtual void Decrease(int? timeoutMs = null)
    {
        Log("Decrease()");
        var step = (_maximum - _minimum) / 10;
        _value = Math.Max(_value - step, _minimum);
    }

    /// <inheritdoc />
    public virtual void SetToMinimum(int? timeoutMs = null)
    {
        Log("SetToMinimum()");
        _value = _minimum;
    }

    /// <inheritdoc />
    public virtual void SetToMaximum(int? timeoutMs = null)
    {
        Log("SetToMaximum()");
        _value = _maximum;
    }

    /// <summary>
    /// Sets the range for testing purposes.
    /// </summary>
    public void SetRange(double min, double max)
    {
        _minimum = min;
        _maximum = max;
    }
}

/// <summary>
/// Testable slider control for unit testing.
/// </summary>
public class TestableSliderControl : TestableRangeControlBase
{
    public TestableSliderControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableSliderControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Slides to a specific percentage (0-100).
    /// </summary>
    public void SlideToPercent(double percent, int? timeoutMs = null)
    {
        Log($"SlideToPercent({percent})");
        SetValuePercent(percent / 100.0, timeoutMs);
    }

    /// <summary>
    /// Slides left (decreases value).
    /// </summary>
    public void SlideLeft(int? timeoutMs = null)
    {
        Log("SlideLeft()");
        Decrease(timeoutMs);
    }

    /// <summary>
    /// Slides right (increases value).
    /// </summary>
    public void SlideRight(int? timeoutMs = null)
    {
        Log("SlideRight()");
        Increase(timeoutMs);
    }
}

/// <summary>
/// Testable stepper control for unit testing.
/// </summary>
public class TestableStepperControl : TestableRangeControlBase
{
    private double _increment = 1;

    public TestableStepperControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        _value = 0;
    }

    public TestableStepperControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
        _value = 0;
    }

    /// <summary>
    /// Gets the increment/step size.
    /// </summary>
    public double GetIncrement(int? timeoutMs = null)
    {
        return _increment;
    }

    /// <summary>
    /// Sets the increment for testing purposes.
    /// </summary>
    public void SetIncrement(double increment)
    {
        _increment = increment;
    }

    /// <inheritdoc />
    public override void Increase(int? timeoutMs = null)
    {
        Log("Increase()");
        _value = Math.Min(_value + _increment, _maximum);
    }

    /// <inheritdoc />
    public override void Decrease(int? timeoutMs = null)
    {
        Log("Decrease()");
        _value = Math.Max(_value - _increment, _minimum);
    }

    /// <summary>
    /// Clicks the increment button once.
    /// </summary>
    public void Increment(int? timeoutMs = null)
    {
        Log("Increment()");
        Increase(timeoutMs);
    }

    /// <summary>
    /// Clicks the decrement button once.
    /// </summary>
    public void Decrement(int? timeoutMs = null)
    {
        Log("Decrement()");
        Decrease(timeoutMs);
    }

    /// <summary>
    /// Clicks the increment button multiple times.
    /// </summary>
    public void IncrementBy(int steps, int? timeoutMs = null)
    {
        Log($"IncrementBy({steps})");
        for (int i = 0; i < steps; i++)
            Increase(timeoutMs);
    }

    /// <summary>
    /// Clicks the decrement button multiple times.
    /// </summary>
    public void DecrementBy(int steps, int? timeoutMs = null)
    {
        Log($"DecrementBy({steps})");
        for (int i = 0; i < steps; i++)
            Decrease(timeoutMs);
    }
}

/// <summary>
/// Testable date picker control for unit testing.
/// </summary>
public class TestableDatePickerControl : TestableControlBase, IDateControlObject
{
    private DateTime _date = DateTime.Today;
    private DateTime _minDate = DateTime.MinValue;
    private DateTime _maxDate = DateTime.MaxValue;
    private bool _isPickerOpen = false;

    public TestableDatePickerControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableDatePickerControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public DateTime GetDate(int? timeoutMs = null)
    {
        FindElement();
        return _date;
    }

    /// <inheritdoc />
    public void SetDate(DateTime? date, int? timeoutMs = null)
    {
        if (date is null) return;
        Log($"SetDate({date:yyyy-MM-dd})");
        FindElementRequired(timeoutMs);
        _date = date.Value.Date;
    }

    /// <inheritdoc />
    public bool WaitDate(DateTime? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return _date.Date == expected.Value.Date;
    }

    /// <inheritdoc />
    public void AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (_date.Date != expected.Value.Date)
        {
            throw new AssertionException(
                message ?? $"Expected date {expected:yyyy-MM-dd} but was {_date:yyyy-MM-dd}",
                Locator.Value,
                "AssertDate");
        }
    }

    /// <inheritdoc />
    public void AssertDateInRange(DateTime? min, DateTime? max, string? message = null, int? timeoutMs = null)
    {
        if (min.HasValue && _date.Date < min.Value.Date)
        {
            throw new AssertionException(
                message ?? $"Date {_date:yyyy-MM-dd} is less than minimum {min:yyyy-MM-dd}",
                Locator.Value,
                "AssertDateInRange");
        }

        if (max.HasValue && _date.Date > max.Value.Date)
        {
            throw new AssertionException(
                message ?? $"Date {_date:yyyy-MM-dd} is greater than maximum {max:yyyy-MM-dd}",
                Locator.Value,
                "AssertDateInRange");
        }
    }

    /// <inheritdoc />
    public DateTime GetMinDate(int? timeoutMs = null) => _minDate;

    /// <inheritdoc />
    public DateTime GetMaxDate(int? timeoutMs = null) => _maxDate;

    /// <inheritdoc />
    public bool IsPickerOpen(int? timeoutMs = null) => _isPickerOpen;

    /// <inheritdoc />
    public void OpenPicker(int? timeoutMs = null)
    {
        Log("OpenPicker");
        _isPickerOpen = true;
    }

    /// <inheritdoc />
    public void ClosePicker(int? timeoutMs = null)
    {
        Log("ClosePicker");
        _isPickerOpen = false;
    }

    /// <summary>
    /// Sets the date range for testing purposes.
    /// </summary>
    public void SetDateRange(DateTime min, DateTime max)
    {
        _minDate = min;
        _maxDate = max;
    }

    /// <summary>
    /// Gets the date format.
    /// </summary>
    public string GetFormat(int? timeoutMs = null)
    {
        return "d";
    }
}

/// <summary>
/// Testable time picker control for unit testing.
/// </summary>
public class TestableTimePickerControl : TestableControlBase, ITimeControlObject
{
    private TimeSpan _time = TimeSpan.FromHours(12);
    private TimeSpan _minTime = TimeSpan.Zero;
    private TimeSpan _maxTime = new TimeSpan(23, 59, 59);
    private bool _isPickerOpen = false;

    public TestableTimePickerControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableTimePickerControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public TimeSpan GetTime(int? timeoutMs = null)
    {
        FindElement();
        return _time;
    }

    /// <inheritdoc />
    public void SetTime(TimeSpan? time, int? timeoutMs = null)
    {
        if (time is null) return;
        Log($"SetTime({time})");
        FindElementRequired(timeoutMs);
        _time = time.Value;
    }

    /// <inheritdoc />
    public bool WaitTime(TimeSpan? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return _time.Hours == expected.Value.Hours && _time.Minutes == expected.Value.Minutes;
    }

    /// <inheritdoc />
    public void AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (_time.Hours != expected.Value.Hours || _time.Minutes != expected.Value.Minutes)
        {
            throw new AssertionException(
                message ?? $"Expected time {expected:hh\\:mm} but was {_time:hh\\:mm}",
                Locator.Value,
                "AssertTime");
        }
    }

    /// <inheritdoc />
    public void AssertTimeInRange(TimeSpan? min, TimeSpan? max, string? message = null, int? timeoutMs = null)
    {
        if (min.HasValue && _time < min.Value)
        {
            throw new AssertionException(
                message ?? $"Time {_time:hh\\:mm} is less than minimum {min:hh\\:mm}",
                Locator.Value,
                "AssertTimeInRange");
        }

        if (max.HasValue && _time > max.Value)
        {
            throw new AssertionException(
                message ?? $"Time {_time:hh\\:mm} is greater than maximum {max:hh\\:mm}",
                Locator.Value,
                "AssertTimeInRange");
        }
    }

    /// <inheritdoc />
    public TimeSpan GetMinTime(int? timeoutMs = null) => _minTime;

    /// <inheritdoc />
    public TimeSpan GetMaxTime(int? timeoutMs = null) => _maxTime;

    /// <inheritdoc />
    public bool IsPickerOpen(int? timeoutMs = null) => _isPickerOpen;

    /// <inheritdoc />
    public void OpenPicker(int? timeoutMs = null)
    {
        Log("OpenPicker");
        _isPickerOpen = true;
    }

    /// <inheritdoc />
    public void ClosePicker(int? timeoutMs = null)
    {
        Log("ClosePicker");
        _isPickerOpen = false;
    }

    /// <summary>
    /// Sets the time range for testing purposes.
    /// </summary>
    public void SetTimeRange(TimeSpan min, TimeSpan max)
    {
        _minTime = min;
        _maxTime = max;
    }

    /// <summary>
    /// Gets the time format.
    /// </summary>
    public string GetFormat(int? timeoutMs = null)
    {
        return "t";
    }
}

/// <summary>
/// Testable progress bar control for unit testing.
/// </summary>
public class TestableProgressBarControl : TestableControlBase, IProgressControlObject
{
    private double _progress = 0;
    private double _min = 0;
    private double _max = 1;

    public TestableProgressBarControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableProgressBarControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public double GetProgress(int? timeoutMs = null)
    {
        FindElement();
        return _progress;
    }

    /// <inheritdoc />
    public bool WaitProgress(double? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return Math.Abs(_progress - expected.Value) < 0.001;
    }

    /// <inheritdoc />
    public void AssertProgress(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var tol = tolerance ?? 0.001;
        if (Math.Abs(_progress - expected.Value) > tol)
        {
            throw new AssertionException(
                message ?? $"Expected progress {expected} but was {_progress}",
                Locator.Value,
                "AssertProgress");
        }
    }

    /// <inheritdoc />
    public (double min, double max) GetMinMax(int? timeoutMs = null) => (_min, _max);

    /// <inheritdoc />
    public double GetProgressPercent(int? timeoutMs = null)
    {
        if (Math.Abs(_max - _min) < 0.001) return 0;
        return (_progress - _min) / (_max - _min) * 100;
    }

    /// <inheritdoc />
    public bool IsComplete(int? timeoutMs = null)
    {
        return Math.Abs(_progress - _max) < 0.001;
    }

    /// <inheritdoc />
    public bool WaitComplete(int? timeoutMs = null)
    {
        return IsComplete();
    }

    /// <inheritdoc />
    public void AssertComplete(string? message = null, int? timeoutMs = null)
    {
        if (!IsComplete())
        {
            throw new AssertionException(
                message ?? $"Expected progress to be complete ({_max}) but was {_progress}",
                Locator.Value,
                "AssertComplete");
        }
    }

    /// <summary>
    /// Sets the progress value for testing purposes.
    /// </summary>
    public void SetProgress(double progress)
    {
        _progress = progress;
    }

    /// <summary>
    /// Sets the min/max range for testing purposes.
    /// </summary>
    public void SetMinMax(double min, double max)
    {
        _min = min;
        _max = max;
    }

    /// <summary>
    /// Gets the progress color.
    /// </summary>
    public string? GetProgressColor(int? timeoutMs = null)
    {
        return "Blue";
    }
}

/// <summary>
/// Testable image control for unit testing.
/// </summary>
public class TestableImageControl : TestableControlBase, IImageControlObject
{
    private string? _source = "image.png";
    private (int width, int height) _dimensions = (100, 100);
    private bool _isLoading = false;

    public TestableImageControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableImageControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public string? GetSource(int? timeoutMs = null)
    {
        FindElement();
        return _source;
    }

    /// <inheritdoc />
    public bool HasSource(int? timeoutMs = null)
    {
        return !string.IsNullOrEmpty(_source);
    }

    /// <inheritdoc />
    public void AssertSource(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (_source != expected)
        {
            throw new AssertionException(
                message ?? $"Expected image source '{expected}' but was '{_source}'",
                Locator.Value,
                "AssertSource");
        }
    }

    /// <inheritdoc />
    public (int width, int height) GetDimensions(int? timeoutMs = null)
    {
        FindElement();
        return _dimensions;
    }

    /// <inheritdoc />
    public void AssertDimensions(int? expectedWidth, int? expectedHeight, string? message = null, int? timeoutMs = null)
    {
        if (expectedWidth.HasValue && _dimensions.width != expectedWidth.Value)
        {
            throw new AssertionException(
                message ?? $"Expected width {expectedWidth} but was {_dimensions.width}",
                Locator.Value,
                "AssertDimensions");
        }

        if (expectedHeight.HasValue && _dimensions.height != expectedHeight.Value)
        {
            throw new AssertionException(
                message ?? $"Expected height {expectedHeight} but was {_dimensions.height}",
                Locator.Value,
                "AssertDimensions");
        }
    }

    /// <inheritdoc />
    public bool IsLoading(int? timeoutMs = null)
    {
        FindElement();
        return _isLoading;
    }

    /// <inheritdoc />
    public bool WaitLoaded(int? timeoutMs = null)
    {
        return !_isLoading;
    }

    /// <inheritdoc />
    public void AssertLoaded(string? message = null, int? timeoutMs = null)
    {
        if (_isLoading)
        {
            throw new AssertionException(
                message ?? "Image is still loading",
                Locator.Value,
                "AssertLoaded");
        }
    }

    /// <summary>
    /// Sets the source for testing purposes.
    /// </summary>
    public void SetSource(string? source)
    {
        _source = source;
    }

    /// <summary>
    /// Sets the dimensions for testing purposes.
    /// </summary>
    public void SetDimensions(int width, int height)
    {
        _dimensions = (width, height);
    }

    /// <summary>
    /// Sets the loading state for testing purposes.
    /// </summary>
    public void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;
    }

    /// <summary>
    /// Gets the aspect ratio setting.
    /// </summary>
    public string? GetAspect(int? timeoutMs = null)
    {
        return "AspectFit";
    }

    /// <summary>
    /// Checks if the image is opaque.
    /// </summary>
    public bool IsOpaque(int? timeoutMs = null)
    {
        return true;
    }
}

/// <summary>
/// Testable scroll view control for unit testing.
/// </summary>
public class TestableScrollViewControl : TestableControlBase, IScrollableControlObject
{
    private (double horizontal, double vertical) _scrollPosition = (0, 0);
    private bool _canScrollH = false;
    private bool _canScrollV = true;

    public TestableScrollViewControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableScrollViewControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public (double horizontal, double vertical) GetScrollPosition(int? timeoutMs = null)
    {
        FindElement();
        return _scrollPosition;
    }

    /// <inheritdoc />
    public bool CanScrollHorizontally(int? timeoutMs = null)
    {
        return _canScrollH;
    }

    /// <inheritdoc />
    public bool CanScrollVertically(int? timeoutMs = null)
    {
        return _canScrollV;
    }

    /// <inheritdoc />
    public void ScrollTo(double? horizontalPercent, double? verticalPercent, int? timeoutMs = null)
    {
        Log($"ScrollTo({horizontalPercent}, {verticalPercent})");
        if (horizontalPercent.HasValue)
            _scrollPosition = (horizontalPercent.Value, _scrollPosition.vertical);
        if (verticalPercent.HasValue)
            _scrollPosition = (_scrollPosition.horizontal, verticalPercent.Value);
    }

    /// <inheritdoc />
    public void ScrollToTop(int? timeoutMs = null)
    {
        Log("ScrollToTop");
        _scrollPosition = (_scrollPosition.horizontal, 0);
    }

    /// <inheritdoc />
    public void ScrollToBottom(int? timeoutMs = null)
    {
        Log("ScrollToBottom");
        _scrollPosition = (_scrollPosition.horizontal, 100);
    }

    /// <inheritdoc />
    public void ScrollToLeft(int? timeoutMs = null)
    {
        Log("ScrollToLeft");
        _scrollPosition = (0, _scrollPosition.vertical);
    }

    /// <inheritdoc />
    public void ScrollToRight(int? timeoutMs = null)
    {
        Log("ScrollToRight");
        _scrollPosition = (100, _scrollPosition.vertical);
    }

    /// <inheritdoc />
    public void ScrollUp(double? amount = null, int? timeoutMs = null)
    {
        Log($"ScrollUp({amount})");
        var newV = Math.Max(0, _scrollPosition.vertical - (amount ?? 10));
        _scrollPosition = (_scrollPosition.horizontal, newV);
    }

    /// <inheritdoc />
    public void ScrollDown(double? amount = null, int? timeoutMs = null)
    {
        Log($"ScrollDown({amount})");
        var newV = Math.Min(100, _scrollPosition.vertical + (amount ?? 10));
        _scrollPosition = (_scrollPosition.horizontal, newV);
    }

    /// <inheritdoc />
    public void ScrollLeft(double? amount = null, int? timeoutMs = null)
    {
        Log($"ScrollLeft({amount})");
        var newH = Math.Max(0, _scrollPosition.horizontal - (amount ?? 10));
        _scrollPosition = (newH, _scrollPosition.vertical);
    }

    /// <inheritdoc />
    public void ScrollRight(double? amount = null, int? timeoutMs = null)
    {
        Log($"ScrollRight({amount})");
        var newH = Math.Min(100, _scrollPosition.horizontal + (amount ?? 10));
        _scrollPosition = (newH, _scrollPosition.vertical);
    }

    /// <inheritdoc />
    public void ScrollToElement(IControlObject? control, int? timeoutMs = null)
    {
        if (control is null) return;
        Log($"ScrollToElement({control})");
        // For testing, just pretend we scrolled to make the element visible
    }

    /// <inheritdoc />
    public bool WaitScrollComplete(int? timeoutMs = null)
    {
        return true;
    }

    /// <summary>
    /// Sets scroll capabilities for testing purposes.
    /// </summary>
    public void SetScrollCapabilities(bool canScrollH, bool canScrollV)
    {
        _canScrollH = canScrollH;
        _canScrollV = canScrollV;
    }
}

/// <summary>
/// Testable container control base for unit testing.
/// </summary>
public abstract class TestableContainerControlBase : TestableControlBase, IContainerControlObject
{
    protected readonly List<IControlObject> _children = new();

    protected TestableContainerControlBase(TestableMauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    protected TestableContainerControlBase(TestableMauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public virtual int GetChildCount(int? timeoutMs = null)
    {
        FindElement();
        return _children.Count;
    }

    /// <inheritdoc />
    public virtual void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetChildCount(timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected {expected} children but found {actual}",
                Locator.Value,
                "AssertChildCount");
        }
    }

    /// <inheritdoc />
    public virtual T FindChild<T>(ControlLocator locator) where T : IControlObject
    {
        return (T)Activator.CreateInstance(typeof(T), Context, locator, Page)!;
    }

    /// <inheritdoc />
    public virtual T FindChild<T>(string automationId) where T : IControlObject
    {
        var locator = By.AutomationId(automationId);
        return FindChild<T>(locator);
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject
    {
        var results = new List<T>();
        foreach (var child in _children.OfType<T>())
        {
            results.Add(child);
        }
        return results.AsReadOnly();
    }

    /// <summary>
    /// Sets the child count for testing purposes.
    /// </summary>
    public void SetChildCount(int count)
    {
        _children.Clear();
        for (int i = 0; i < count; i++)
        {
            _children.Add(new TestableLabelControl(Context, $"child{i}", Page));
        }
    }
}

/// <summary>
/// Testable frame control for unit testing.
/// </summary>
public class TestableFrameControl : TestableContainerControlBase
{
    public TestableFrameControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableFrameControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the border color.
    /// </summary>
    public string? GetBorderColor(int? timeoutMs = null)
    {
        FindElement();
        return "Gray";
    }

    /// <summary>
    /// Gets the corner radius.
    /// </summary>
    public double GetCornerRadius(int? timeoutMs = null)
    {
        FindElement();
        return 5.0;
    }

    /// <summary>
    /// Gets whether the frame has a shadow.
    /// </summary>
    public bool HasShadow(int? timeoutMs = null)
    {
        FindElement();
        return true;
    }
}

/// <summary>
/// Testable border control for unit testing.
/// </summary>
public class TestableBorderControl : TestableContainerControlBase
{
    public TestableBorderControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableBorderControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the stroke color.
    /// </summary>
    public string? GetStrokeColor(int? timeoutMs = null)
    {
        FindElement();
        return "Black";
    }

    /// <summary>
    /// Gets the stroke thickness.
    /// </summary>
    public double GetStrokeThickness(int? timeoutMs = null)
    {
        FindElement();
        return 1.0;
    }
}

/// <summary>
/// Testable expander control for unit testing.
/// </summary>
public class TestableExpanderControl : TestableContainerControlBase, IExpandableControlObject
{
    private bool _isExpanded = false;
    private string _headerText = "Header";

    public TestableExpanderControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableExpanderControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public bool IsExpanded(int? timeoutMs = null)
    {
        FindElement();
        return _isExpanded;
    }

    /// <inheritdoc />
    public bool WaitExpanded(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return _isExpanded == expected.Value;
    }

    /// <inheritdoc />
    public void AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (_isExpanded != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected expander to be {(expected.Value ? "expanded" : "collapsed")} but was {(_isExpanded ? "expanded" : "collapsed")}",
                Locator.Value,
                "AssertExpanded");
        }
    }

    /// <inheritdoc />
    public void Expand(int? timeoutMs = null)
    {
        Log("Expand()");
        FindElementRequired(timeoutMs);
        _isExpanded = true;
    }

    /// <inheritdoc />
    public void Collapse(int? timeoutMs = null)
    {
        Log("Collapse()");
        FindElementRequired(timeoutMs);
        _isExpanded = false;
    }

    /// <inheritdoc />
    public void Toggle(int? timeoutMs = null)
    {
        Log("Toggle()");
        FindElementRequired(timeoutMs);
        _isExpanded = !_isExpanded;
    }

    /// <inheritdoc />
    public string GetHeaderText(int? timeoutMs = null)
    {
        FindElement();
        return _headerText;
    }

    /// <summary>
    /// Sets the header text for testing purposes.
    /// </summary>
    public void SetHeaderText(string text)
    {
        _headerText = text;
    }

    /// <summary>
    /// Sets the expanded state for testing purposes.
    /// </summary>
    public void SetExpanded(bool expanded)
    {
        _isExpanded = expanded;
    }
}

/// <summary>
/// Testable refresh view control for unit testing.
/// </summary>
public class TestableRefreshViewControl : TestableContainerControlBase, IRefreshableControlObject
{
    private bool _isRefreshing = false;

    public TestableRefreshViewControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableRefreshViewControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public bool IsRefreshing(int? timeoutMs = null)
    {
        FindElement();
        return _isRefreshing;
    }

    /// <inheritdoc />
    public bool WaitRefreshing(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return _isRefreshing == expected.Value;
    }

    /// <inheritdoc />
    public void AssertRefreshing(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (_isRefreshing != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected IsRefreshing to be {expected} but was {_isRefreshing}",
                Locator.Value,
                "AssertRefreshing");
        }
    }

    /// <inheritdoc />
    public void Refresh(int? timeoutMs = null)
    {
        Log("Refresh()");
        FindElementRequired(timeoutMs);
        _isRefreshing = true;
    }

    /// <inheritdoc />
    public void WaitRefreshComplete(int? timeoutMs = null)
    {
        Log("WaitRefreshComplete()");
        _isRefreshing = false;
    }

    /// <summary>
    /// Sets the refreshing state for testing purposes.
    /// </summary>
    public void SetRefreshing(bool isRefreshing)
    {
        _isRefreshing = isRefreshing;
    }
}

/// <summary>
/// Testable activity indicator control for unit testing.
/// </summary>
public class TestableActivityIndicatorControl : TestableControlBase, IActivityIndicatorControlObject
{
    private bool _isRunning = false;
    private string _color = "Blue";

    public TestableActivityIndicatorControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableActivityIndicatorControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public bool IsRunning(int? timeoutMs = null)
    {
        FindElement();
        return _isRunning;
    }

    /// <inheritdoc />
    public bool WaitRunning(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        return _isRunning == expected.Value;
    }

    /// <inheritdoc />
    public void AssertRunning(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        if (_isRunning != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected activity indicator to be {(expected.Value ? "running" : "stopped")} but was {(_isRunning ? "running" : "stopped")}",
                Locator.Value,
                "AssertRunning");
        }
    }

    /// <inheritdoc />
    public void WaitUntilStopped(int? timeoutMs = null)
    {
        Log("WaitUntilStopped()");
        WaitRunning(false, timeoutMs);
    }

    /// <inheritdoc />
    public void WaitUntilStarted(int? timeoutMs = null)
    {
        Log("WaitUntilStarted()");
        WaitRunning(true, timeoutMs);
    }

    /// <summary>
    /// Gets the indicator color.
    /// </summary>
    public string? GetColor(int? timeoutMs = null)
    {
        FindElement();
        return _color;
    }

    /// <summary>
    /// Sets the running state for testing purposes.
    /// </summary>
    public void SetRunning(bool isRunning)
    {
        _isRunning = isRunning;
    }

    /// <summary>
    /// Sets the color for testing purposes.
    /// </summary>
    public void SetColor(string color)
    {
        _color = color;
    }
}

/// <summary>
/// Testable tab control base for unit testing.
/// </summary>
public class TestableTabControlBase : TestableControlBase, ITabControlObject
{
    private List<string> _tabNames = new() { "Tab1", "Tab2", "Tab3" };
    private int _selectedTabIndex = 0;

    public TestableTabControlBase(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableTabControlBase(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public int GetTabCount(int? timeoutMs = null)
    {
        FindElement();
        return _tabNames.Count;
    }

    /// <inheritdoc />
    public void AssertTabCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetTabCount(timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected {expected} tabs but found {actual}",
                Locator.Value,
                "AssertTabCount");
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetTabNames(int? timeoutMs = null)
    {
        FindElement();
        return _tabNames.AsReadOnly();
    }

    /// <inheritdoc />
    public int GetSelectedTabIndex(int? timeoutMs = null)
    {
        FindElement();
        return _selectedTabIndex;
    }

    /// <inheritdoc />
    public void AssertSelectedTabIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedTabIndex(timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected selected tab index {expected} but was {actual}",
                Locator.Value,
                "AssertSelectedTabIndex");
        }
    }

    /// <inheritdoc />
    public string? GetSelectedTabName(int? timeoutMs = null)
    {
        var index = GetSelectedTabIndex(timeoutMs);
        if (index < 0 || index >= _tabNames.Count) return null;
        return _tabNames[index];
    }

    /// <inheritdoc />
    public void AssertSelectedTabName(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedTabName(timeoutMs);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected selected tab name '{expected}' but was '{actual}'",
                Locator.Value,
                "AssertSelectedTabName");
        }
    }

    /// <inheritdoc />
    public void SelectTab(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"SelectTab({index})");
        FindElementRequired(timeoutMs);

        if (index.Value < 0 || index.Value >= _tabNames.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range");

        _selectedTabIndex = index.Value;
    }

    /// <inheritdoc />
    public void SelectTab(string? name, int? timeoutMs = null)
    {
        if (name is null) return;
        Log($"SelectTab(\"{name}\")");
        FindElementRequired(timeoutMs);

        var index = _tabNames.IndexOf(name);
        if (index < 0)
            throw new ElementNotFoundException($"Tab with name '{name}' not found");

        _selectedTabIndex = index;
    }

    /// <inheritdoc />
    public bool WaitTabSelected(int index, int? timeoutMs = null)
    {
        return _selectedTabIndex == index;
    }

    /// <summary>
    /// Sets the tab names for testing purposes.
    /// </summary>
    public void SetTabNames(params string[] names)
    {
        _tabNames = names.ToList();
    }

    /// <summary>
    /// Sets the selected tab index for testing purposes.
    /// </summary>
    public void SetSelectedTabIndex(int index)
    {
        _selectedTabIndex = index;
    }
}

/// <summary>
/// Testable TabbedPage control for unit testing.
/// </summary>
public class TestableTabbedPageControl : TestableTabControlBase
{
    public TestableTabbedPageControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableTabbedPageControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}

/// <summary>
/// Testable TabBar control for unit testing.
/// </summary>
public class TestableTabBarControl : TestableTabControlBase
{
    public TestableTabBarControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableTabBarControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}

/// <summary>
/// Testable SearchBar control for unit testing.
/// </summary>
public class TestableSearchBarControl : TestableTextControlBase
{
    private string _searchText = "";
    private string _placeholder = "Search...";
    private bool _isSubmitted = false;

    public TestableSearchBarControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableSearchBarControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the search text.
    /// </summary>
    public string GetSearchText(int? timeoutMs = null)
    {
        FindElement();
        return _searchText;
    }

    /// <summary>
    /// Enters search text and submits.
    /// </summary>
    public void Search(string searchText, int? timeoutMs = null)
    {
        Log($"Search(\"{searchText}\")");
        FindElementRequired(timeoutMs);
        _searchText = searchText;
        Submit(timeoutMs);
    }

    /// <summary>
    /// Submits the current search query.
    /// </summary>
    public void Submit(int? timeoutMs = null)
    {
        Log("Submit()");
        FindElementRequired(timeoutMs);
        _isSubmitted = true;
    }

    /// <summary>
    /// Clears the search text.
    /// </summary>
    public void ClearSearch(int? timeoutMs = null)
    {
        Log("ClearSearch()");
        FindElementRequired(timeoutMs);
        _searchText = "";
    }

    /// <summary>
    /// Gets the placeholder text.
    /// </summary>
    public string GetPlaceholder(int? timeoutMs = null)
    {
        FindElement();
        return _placeholder;
    }

    /// <summary>
    /// Sets the search text for testing purposes.
    /// </summary>
    public void SetSearchText(string text)
    {
        _searchText = text;
    }

    /// <summary>
    /// Sets the placeholder for testing purposes.
    /// </summary>
    public void SetPlaceholder(string placeholder)
    {
        _placeholder = placeholder;
    }

    /// <summary>
    /// Gets whether the search was submitted.
    /// </summary>
    public bool WasSubmitted()
    {
        return _isSubmitted;
    }
}

/// <summary>
/// Testable ContentView control for unit testing.
/// </summary>
public class TestableContentViewControl : TestableContainerControlBase
{
    private bool _hasContent = true;

    public TestableContentViewControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableContentViewControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets whether the content view has content.
    /// </summary>
    public bool HasContent(int? timeoutMs = null)
    {
        FindElement();
        return _hasContent;
    }

    /// <summary>
    /// Sets whether the content view has content.
    /// </summary>
    public void SetHasContent(bool hasContent)
    {
        _hasContent = hasContent;
    }

    /// <summary>
    /// Asserts that the content view has content.
    /// </summary>
    public void AssertHasContent(string? message = null, int? timeoutMs = null)
    {
        if (!HasContent(timeoutMs))
        {
            throw new AssertionException(
                message ?? "Expected ContentView to have content",
                Locator.Value,
                "AssertHasContent");
        }
    }
}

/// <summary>
/// Testable CarouselView control for unit testing.
/// </summary>
public class TestableCarouselViewControl : TestableItemsControlBase
{
    private int _currentPosition = 0;

    public TestableCarouselViewControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableCarouselViewControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the current position.
    /// </summary>
    public int GetCurrentPosition(int? timeoutMs = null)
    {
        FindElement();
        return _currentPosition;
    }

    /// <summary>
    /// Swipes to the next item.
    /// </summary>
    public void SwipeNext(int? timeoutMs = null)
    {
        Log("SwipeNext()");
        FindElementRequired(timeoutMs);
        if (_currentPosition < Items.Count - 1)
            _currentPosition++;
    }

    /// <summary>
    /// Swipes to the previous item.
    /// </summary>
    public void SwipePrevious(int? timeoutMs = null)
    {
        Log("SwipePrevious()");
        FindElementRequired(timeoutMs);
        if (_currentPosition > 0)
            _currentPosition--;
    }

    /// <summary>
    /// Navigates to a specific position.
    /// </summary>
    public void GoToPosition(int position, int? timeoutMs = null)
    {
        Log($"GoToPosition({position})");
        FindElementRequired(timeoutMs);
        if (position < 0 || position >= Items.Count)
            throw new ArgumentOutOfRangeException(nameof(position));
        _currentPosition = position;
    }

    /// <summary>
    /// Checks if at the start.
    /// </summary>
    public bool IsAtStart()
    {
        return _currentPosition == 0;
    }

    /// <summary>
    /// Checks if at the end.
    /// </summary>
    public bool IsAtEnd()
    {
        return _currentPosition == Items.Count - 1;
    }

    /// <summary>
    /// Asserts the current position.
    /// </summary>
    public void AssertPosition(int expected, string? message = null, int? timeoutMs = null)
    {
        var actual = GetCurrentPosition(timeoutMs);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected position {expected} but was {actual}",
                Locator.Value,
                "AssertPosition");
        }
    }

    /// <summary>
    /// Sets the current position for testing.
    /// </summary>
    public void SetCurrentPosition(int position)
    {
        _currentPosition = position;
    }
}

/// <summary>
/// Testable SwipeView control for unit testing.
/// </summary>
public class TestableSwipeViewControl : TestableContainerControlBase
{
    private bool _isLeftSwipeOpen = false;
    private bool _isRightSwipeOpen = false;

    public TestableSwipeViewControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableSwipeViewControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Swipes left to reveal right actions.
    /// </summary>
    public void SwipeLeft(int distance = 200, int? timeoutMs = null)
    {
        Log($"SwipeLeft({distance})");
        FindElementRequired(timeoutMs);
        _isRightSwipeOpen = true;
        _isLeftSwipeOpen = false;
    }

    /// <summary>
    /// Swipes right to reveal left actions.
    /// </summary>
    public void SwipeRight(int distance = 200, int? timeoutMs = null)
    {
        Log($"SwipeRight({distance})");
        FindElementRequired(timeoutMs);
        _isLeftSwipeOpen = true;
        _isRightSwipeOpen = false;
    }

    /// <summary>
    /// Closes any open swipe actions.
    /// </summary>
    public void CloseSwipe(int? timeoutMs = null)
    {
        Log("CloseSwipe()");
        FindElementRequired(timeoutMs);
        _isLeftSwipeOpen = false;
        _isRightSwipeOpen = false;
    }

    /// <summary>
    /// Checks if left swipe is open.
    /// </summary>
    public bool IsLeftSwipeOpen()
    {
        return _isLeftSwipeOpen;
    }

    /// <summary>
    /// Checks if right swipe is open.
    /// </summary>
    public bool IsRightSwipeOpen()
    {
        return _isRightSwipeOpen;
    }

    /// <summary>
    /// Asserts left swipe is open.
    /// </summary>
    public void AssertLeftSwipeOpen(string? message = null, int? timeoutMs = null)
    {
        if (!_isLeftSwipeOpen)
        {
            throw new AssertionException(
                message ?? "Expected left swipe to be open",
                Locator.Value,
                "AssertLeftSwipeOpen");
        }
    }

    /// <summary>
    /// Asserts right swipe is open.
    /// </summary>
    public void AssertRightSwipeOpen(string? message = null, int? timeoutMs = null)
    {
        if (!_isRightSwipeOpen)
        {
            throw new AssertionException(
                message ?? "Expected right swipe to be open",
                Locator.Value,
                "AssertRightSwipeOpen");
        }
    }
}

/// <summary>
/// Testable WebView control for unit testing.
/// </summary>
public class TestableWebViewControl : TestableControlBase
{
    private string? _url = "https://example.com";
    private string? _title = "Example Page";
    private bool _isLoading = false;

    public TestableWebViewControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableWebViewControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the current URL.
    /// </summary>
    public string? GetCurrentUrl(int? timeoutMs = null)
    {
        FindElement();
        return _url;
    }

    /// <summary>
    /// Gets the page title.
    /// </summary>
    public string? GetTitle(int? timeoutMs = null)
    {
        FindElement();
        return _title;
    }

    /// <summary>
    /// Checks if the WebView is loading.
    /// </summary>
    public bool IsLoading(int? timeoutMs = null)
    {
        FindElement();
        return _isLoading;
    }

    /// <summary>
    /// Navigates to a URL.
    /// </summary>
    public void NavigateTo(string url, int? timeoutMs = null)
    {
        Log($"NavigateTo(\"{url}\")");
        FindElementRequired(timeoutMs);
        _url = url;
    }

    /// <summary>
    /// Navigates back.
    /// </summary>
    public void GoBack(int? timeoutMs = null)
    {
        Log("GoBack()");
        FindElementRequired(timeoutMs);
    }

    /// <summary>
    /// Navigates forward.
    /// </summary>
    public void GoForward(int? timeoutMs = null)
    {
        Log("GoForward()");
        FindElementRequired(timeoutMs);
    }

    /// <summary>
    /// Reloads the page.
    /// </summary>
    public void Reload(int? timeoutMs = null)
    {
        Log("Reload()");
        FindElementRequired(timeoutMs);
    }

    /// <summary>
    /// Waits for page to finish loading.
    /// </summary>
    public bool WaitForPageLoad(int? timeoutMs = null)
    {
        return !_isLoading;
    }

    /// <summary>
    /// Asserts the current URL.
    /// </summary>
    public void AssertUrl(string expected, string? message = null, int? timeoutMs = null)
    {
        var actual = GetCurrentUrl(timeoutMs);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected URL '{expected}' but was '{actual}'",
                Locator.Value,
                "AssertUrl");
        }
    }

    /// <summary>
    /// Asserts URL contains expected text.
    /// </summary>
    public void AssertUrlContains(string expected, string? message = null, int? timeoutMs = null)
    {
        var actual = GetCurrentUrl(timeoutMs) ?? "";
        if (!actual.Contains(expected))
        {
            throw new AssertionException(
                message ?? $"Expected URL to contain '{expected}' but was '{actual}'",
                Locator.Value,
                "AssertUrlContains");
        }
    }

    /// <summary>
    /// Asserts the page title.
    /// </summary>
    public void AssertTitle(string expected, string? message = null, int? timeoutMs = null)
    {
        var actual = GetTitle(timeoutMs);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected title '{expected}' but was '{actual}'",
                Locator.Value,
                "AssertTitle");
        }
    }

    /// <summary>
    /// Asserts WebView is loaded.
    /// </summary>
    public void AssertLoaded(string? message = null, int? timeoutMs = null)
    {
        if (_isLoading)
        {
            throw new AssertionException(
                message ?? "Expected WebView to be loaded but it is still loading",
                Locator.Value,
                "AssertLoaded");
        }
    }

    /// <summary>
    /// Sets URL for testing.
    /// </summary>
    public void SetUrl(string? url)
    {
        _url = url;
    }

    /// <summary>
    /// Sets title for testing.
    /// </summary>
    public void SetTitle(string? title)
    {
        _title = title;
    }

    /// <summary>
    /// Sets loading state for testing.
    /// </summary>
    public void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;
    }
}

/// <summary>
/// Testable FlyoutItem control for unit testing.
/// </summary>
public class TestableFlyoutItemControl : TestableContainerControlBase
{
    private bool _isSelected = false;
    private string? _icon = "icon.png";

    public TestableFlyoutItemControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableFlyoutItemControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Checks if the flyout item is selected.
    /// </summary>
    public bool IsSelected(int? timeoutMs = null)
    {
        FindElement();
        return _isSelected;
    }

    /// <summary>
    /// Gets the icon.
    /// </summary>
    public string? GetIcon(int? timeoutMs = null)
    {
        FindElement();
        return _icon;
    }

    /// <summary>
    /// Selects this flyout item.
    /// </summary>
    public void Select(int? timeoutMs = null)
    {
        Log("Select()");
        FindElementRequired(timeoutMs);
        _isSelected = true;
    }

    /// <summary>
    /// Asserts the flyout item is selected.
    /// </summary>
    public void AssertSelected(string? message = null, int? timeoutMs = null)
    {
        if (!IsSelected(timeoutMs))
        {
            throw new AssertionException(
                message ?? "Expected flyout item to be selected",
                Locator.Value,
                "AssertSelected");
        }
    }

    /// <summary>
    /// Asserts the flyout item is not selected.
    /// </summary>
    public void AssertNotSelected(string? message = null, int? timeoutMs = null)
    {
        if (IsSelected(timeoutMs))
        {
            throw new AssertionException(
                message ?? "Expected flyout item to not be selected",
                Locator.Value,
                "AssertNotSelected");
        }
    }

    /// <summary>
    /// Sets the selected state for testing.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
    }

    /// <summary>
    /// Sets the icon for testing.
    /// </summary>
    public void SetIcon(string? icon)
    {
        _icon = icon;
    }
}

/// <summary>
/// Testable Shell control for unit testing.
/// </summary>
public class TestableShellControl : TestableControlBase
{
    private bool _isFlyoutOpen = false;

    public TestableShellControl(TestableMauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    public TestableShellControl(TestableMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Checks if the flyout menu is open.
    /// </summary>
    public bool IsFlyoutOpen(int? timeoutMs = null)
    {
        FindElement();
        return _isFlyoutOpen;
    }

    /// <summary>
    /// Opens the flyout menu.
    /// </summary>
    public void OpenFlyout(int? timeoutMs = null)
    {
        Log("OpenFlyout()");
        FindElementRequired(timeoutMs);
        _isFlyoutOpen = true;
    }

    /// <summary>
    /// Closes the flyout menu.
    /// </summary>
    public void CloseFlyout(int? timeoutMs = null)
    {
        Log("CloseFlyout()");
        FindElementRequired(timeoutMs);
        _isFlyoutOpen = false;
    }

    /// <summary>
    /// Navigates to a Shell route.
    /// </summary>
    public void NavigateToRoute(string route, int? timeoutMs = null)
    {
        Log($"NavigateToRoute(\"{route}\")");
        FindElementRequired(timeoutMs);
    }

    /// <summary>
    /// Gets a flyout item by title.
    /// </summary>
    public TestableFlyoutItemControl GetFlyoutItem(string title)
    {
        OpenFlyout();
        return new TestableFlyoutItemControl(Context, title, Page);
    }

    /// <summary>
    /// Gets the tab bar control.
    /// </summary>
    public TestableTabBarControl GetTabBar()
    {
        return new TestableTabBarControl(Context, "ShellTabBar", Page);
    }

    /// <summary>
    /// Asserts the flyout is open.
    /// </summary>
    public void AssertFlyoutOpen(string? message = null, int? timeoutMs = null)
    {
        if (!IsFlyoutOpen(timeoutMs))
        {
            throw new AssertionException(
                message ?? "Expected flyout to be open",
                Locator.Value,
                "AssertFlyoutOpen");
        }
    }

    /// <summary>
    /// Asserts the flyout is closed.
    /// </summary>
    public void AssertFlyoutClosed(string? message = null, int? timeoutMs = null)
    {
        if (IsFlyoutOpen(timeoutMs))
        {
            throw new AssertionException(
                message ?? "Expected flyout to be closed",
                Locator.Value,
                "AssertFlyoutClosed");
        }
    }

    /// <summary>
    /// Sets the flyout state for testing.
    /// </summary>
    public void SetFlyoutOpen(bool isOpen)
    {
        _isFlyoutOpen = isOpen;
    }
}

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

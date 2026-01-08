# 250.007 Blazor Base Classes Specification

**Block Type:** SPC (Specification)  
**ID:** 250.007  
**Title:** Blazor Base Class Hierarchy Specification  
**Status:** Draft  
**Version:** 1.0  
**Level:** 0 - Foundation

---

## 1. Overview

This specification defines the complete base class hierarchy for Blazor platform controls. All Blazor controls inherit from these base classes, which implement the core interfaces using Selenium WebDriver and IWebElement.

### Package Identity

- **Package:** `Brinell.Blazor`
- **Namespace:** `Brinell.Blazor.Base`
- **Dependencies:** `Brinell.Core`, `Selenium.WebDriver`
- **Element Type:** `IWebElement`
- **Driver Type:** `IWebDriver`

---

## 2. Base Class Hierarchy

```
BlazorControlBase                        # Implements IControlObject
│
├── BlazorClickableControlBase           # Implements IClickableControlObject
│
├── BlazorTextControlBase                # Implements ITextControlObject
│   └── BlazorEditableTextControlBase    # Implements IEditableTextControlObject
│
├── BlazorToggleControlBase              # Implements IToggleControlObject
│
├── BlazorSelectorControlBase            # Implements ISelectorControlObject
│   └── BlazorMultiSelectorControlBase   # Implements IMultiSelectorControlObject
│
├── BlazorRangeControlBase               # Implements IRangeControlObject
│
├── BlazorContainerControlBase           # Implements IContainerControlObject
│
├── BlazorItemsControlBase               # Implements IItemsControlObject
│   └── BlazorDataGridControlBase        # Implements IDataGridControlObject
│
├── BlazorScrollableControlBase          # Implements IScrollableControlObject
│
└── BlazorDateTimeControlBase            # Implements IDateTimeControlObject
```

---

## 3. BlazorControlBase

Foundation for all Blazor controls.

```csharp
namespace Brinell.Blazor.Base
{
    public abstract class BlazorControlBase : IControlObject
    {
        protected readonly IBlazorTestContext _context;
        protected readonly Locator _locator;
        protected readonly IPageObject? _page;
        
        protected BlazorControlBase(IBlazorTestContext context, Locator locator, IPageObject? page = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _page = page;
        }
        
        // Convenience constructor with CSS selector
        protected BlazorControlBase(IBlazorTestContext context, string cssSelector, IPageObject? page = null)
            : this(context, Locator.ByCss(cssSelector), page) { }
        
        // Identity
        public Locator Locator => _locator;
        public IPageObject? Page => _page;
        
        // State methods
        public bool IsExists()
        {
            try { return TryFindElement() != null; }
            catch { return false; }
        }
        
        public bool? IsVisible()
        {
            var element = TryFindElement();
            if (element == null) return null;
            return element.Displayed;
        }
        
        public bool? IsEnabled()
        {
            var element = TryFindElement();
            if (element == null) return null;
            return element.Enabled;
        }
        
        // Wait methods
        public bool WaitExists(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
            
            var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
            wait.PollingInterval = TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval);
            
            try
            {
                return wait.Until(_ => IsExists() == expected.Value);
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }
        
        public bool WaitVisible(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
            
            var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
            wait.PollingInterval = TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval);
            
            try
            {
                return wait.Until(_ => IsVisible() == expected.Value);
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }
        
        public bool WaitEnabled(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
            
            var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
            wait.PollingInterval = TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval);
            
            try
            {
                return wait.Until(_ => IsEnabled() == expected.Value);
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }
        
        // Assert methods
        public void AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            WaitExists(expected, timeoutMs);
            if (IsExists() != expected.Value)
                throw new AssertionException(message ?? $"Control '{_locator}' exists={IsExists()}, expected={expected.Value}");
        }
        
        public void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            WaitVisible(expected, timeoutMs);
            if (IsVisible() != expected.Value)
                throw new AssertionException(message ?? $"Control '{_locator}' visible={IsVisible()}, expected={expected.Value}");
        }
        
        public void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            WaitEnabled(expected, timeoutMs);
            if (IsEnabled() != expected.Value)
                throw new AssertionException(message ?? $"Control '{_locator}' enabled={IsEnabled()}, expected={expected.Value}");
        }
        
        // Text methods
        public string? GetText(int? timeoutMs = null)
        {
            var element = TryFindElement();
            if (element is null) return null;
            // For input elements, use value attribute; otherwise use text
            if (element.TagName.ToLower() == "input")
                return element.GetAttribute("value");
            return element.Text;
        }
        
        public bool WaitText(string? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            var timeout = timeoutMs ?? _context.Timeouts.ElementState;
            var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
            wait.PollingInterval = TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval);
            try
            {
                return wait.Until(_ => GetText() == expected);
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }
        
        public void AssertText(string? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            WaitText(expected, timeoutMs);
            var actual = GetText();
            if (actual != expected)
                throw new AssertionException(message ?? $"Expected text '{expected}' but was '{actual}'");
        }
        
        public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            var actual = GetText(timeoutMs);
            if (actual is null || !actual.Contains(expected))
                throw new AssertionException(message ?? $"Text '{actual}' does not contain '{expected}'");
        }
        
        // Attribute methods
        public string? GetAttribute(string name)
        {
            var element = TryFindElement();
            return element?.GetAttribute(name);
        }
        
        // CSS methods (Blazor-specific)
        public string? GetCssClass()
        {
            return GetAttribute("class");
        }
        
        public bool HasCssClass(string className)
        {
            var classes = GetCssClass() ?? string.Empty;
            return classes.Split(' ').Contains(className);
        }
        
        // Protected element finding
        protected IWebElement? TryFindElement()
        {
            return _context.TryFindElement(_locator);
        }
        
        protected IWebElement FindElement(int? timeoutMs = null)
        {
            var timeout = timeoutMs ?? _context.Timeouts.ElementFind;
            
            var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
            wait.PollingInterval = TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval);
            
            try
            {
                return wait.Until(_ => _context.TryFindElement(_locator)) 
                    ?? throw new ElementNotFoundException($"Element not found: {_locator}");
            }
            catch (WebDriverTimeoutException ex)
            {
                throw new ElementNotFoundException($"Element not found: {_locator}", ex);
            }
        }
        
        // JavaScript execution (Blazor-specific)
        protected T ExecuteScript<T>(string script, params object[] args)
        {
            var jsExecutor = (IJavaScriptExecutor)_context.Driver;
            return (T)jsExecutor.ExecuteScript(script, args);
        }
        
        protected void ExecuteScript(string script, params object[] args)
        {
            var jsExecutor = (IJavaScriptExecutor)_context.Driver;
            jsExecutor.ExecuteScript(script, args);
        }
    }
}
```

---

## 4. BlazorClickableControlBase

Click capability for buttons, links.

```csharp
public abstract class BlazorClickableControlBase : BlazorControlBase, IClickableControlObject
{
    protected BlazorClickableControlBase(IBlazorTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual void Click(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        element.Click();
        _context.Logger.LogAction("Click", _locator);
    }
    
    public virtual void DoubleClick(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var actions = new Actions(_context.Driver);
        actions.DoubleClick(element).Perform();
        _context.Logger.LogAction("DoubleClick", _locator);
    }
    
    public virtual void RightClick(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var actions = new Actions(_context.Driver);
        actions.ContextClick(element).Perform();
        _context.Logger.LogAction("RightClick", _locator);
    }
    
    public bool WaitClickable(bool? clickable, int? timeoutMs = null)
    {
        if (clickable is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => (IsVisible() && IsEnabled()) == clickable.Value);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    // Blazor-specific: hover before click for UI components
    public virtual void Hover(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var actions = new Actions(_context.Driver);
        actions.MoveToElement(element).Perform();
        _context.Logger.LogAction("Hover", _locator);
    }
}
```

---

## 5. BlazorTextControlBase

Text display and verification.

```csharp
public abstract class BlazorTextControlBase : BlazorControlBase, ITextControlObject
{
    protected BlazorTextControlBase(IBlazorTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public bool WaitTextEquals(string? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => GetText() == expected);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    public bool WaitTextContains(string? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => {
                var text = GetText();
                return text is not null && text.Contains(expected);
            });
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    public void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null)
    {
        if (pattern is null) return;
        var actual = GetText(timeoutMs);
        if (actual is null || !System.Text.RegularExpressions.Regex.IsMatch(actual, pattern))
            throw new AssertionException(message ?? $"Text '{actual}' does not match pattern '{pattern}'");
    }
    
    // Blazor-specific: Inner HTML
    public string? GetInnerHtml(int? timeoutMs = null)
    {
        var element = TryFindElement();
        return element?.GetAttribute("innerHTML");
    }
    
    public string? GetOuterHtml(int? timeoutMs = null)
    {
        var element = TryFindElement();
        return element?.GetAttribute("outerHTML");
    }
}
```

---

## 6. BlazorEditableTextControlBase

Text input for input, textarea elements.

```csharp
public abstract class BlazorEditableTextControlBase : BlazorTextControlBase, IEditableTextControlObject
{
    protected BlazorEditableTextControlBase(IBlazorTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual void Enter(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        var element = FindElement(timeoutMs);
        element.SendKeys(text);
        _context.Logger.LogAction("Enter", _locator, text);
    }
    
    public virtual void Clear(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        element.Clear();
        _context.Logger.LogAction("Clear", _locator);
    }
    
    public virtual void SetText(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        Clear(timeoutMs);
        Enter(text, timeoutMs);
    }
    
    public virtual string? GetPlaceholder()
    {
        return GetAttribute("placeholder");
    }
    
    public virtual bool? IsReadOnly()
    {
        var element = TryFindElement();
        if (element == null) return null;
        var readOnly = GetAttribute("readonly");
        return readOnly != null;
    }
    
    // Blazor-specific: Form validation
    public bool HasValidationError()
    {
        return HasCssClass("invalid") || HasCssClass("validation-error");
    }
    
    public string? GetValidationMessage()
    {
        // Look for associated validation message element
        var validationLocator = _locator.ToString() + " ~ .validation-message";
        try
        {
            var element = _context.TryFindElement(Locator.ByCss(validationLocator));
            return element?.Text;
        }
        catch
        {
            return null;
        }
    }
    
    // Keyboard actions
    public void PressKey(string key, int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        element.SendKeys(key);
    }
    
    public void PressEnter(int? timeoutMs = null) => PressKey(Keys.Enter, timeoutMs);
    public void PressTab(int? timeoutMs = null) => PressKey(Keys.Tab, timeoutMs);
    public void PressEscape(int? timeoutMs = null) => PressKey(Keys.Escape, timeoutMs);
}
```

---

## 7. BlazorToggleControlBase

Toggle state for checkboxes, toggle buttons.

```csharp
public abstract class BlazorToggleControlBase : BlazorControlBase, IToggleControlObject
{
    protected BlazorToggleControlBase(IBlazorTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual bool? IsChecked()
    {
        var element = TryFindElement();
        if (element == null) return null;
        
        // HTML checkbox uses "checked" property
        var checkedAttr = element.GetAttribute("checked");
        if (checkedAttr != null) return true;
        
        // For custom Blazor toggle components, check aria-checked
        var ariaChecked = element.GetAttribute("aria-checked");
        if (ariaChecked != null) return ariaChecked == "true";
        
        return element.Selected;
    }
    
    public virtual void Toggle(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        element.Click();
        _context.Logger.LogAction("Toggle", _locator);
    }
    
    public virtual void SetChecked(bool? @checked, int? timeoutMs = null)
    {
        if (@checked is null) return;
        if (IsChecked() != @checked.Value)
            Toggle(timeoutMs);
    }
    
    public void Check(int? timeoutMs = null) => SetChecked(true, timeoutMs);
    public void Uncheck(int? timeoutMs = null) => SetChecked(false, timeoutMs);
    
    public void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitChecked(expected, timeoutMs);
        if (IsChecked() != expected.Value)
            throw new AssertionException(message ?? $"Control '{_locator}' checked={IsChecked()}, expected={expected.Value}");
    }
    
    public bool WaitChecked(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => IsChecked() == expected.Value);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    // Blazor-specific: Indeterminate state
    public bool? IsIndeterminate()
    {
        var element = TryFindElement();
        if (element == null) return null;
        return GetAttribute("indeterminate") == "true";
    }
}
```

---

## 8. BlazorSelectorControlBase

Selection from list for select, combobox.

```csharp
public abstract class BlazorSelectorControlBase : BlazorControlBase, ISelectorControlObject
{
    protected BlazorSelectorControlBase(IBlazorTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual void SelectByText(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        
        var element = FindElement(timeoutMs);
        var selectElement = new SelectElement(element);
        selectElement.SelectByText(text);
        
        _context.Logger.LogAction("SelectByText", _locator, text);
    }
    
    public virtual void SelectByIndex(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        
        var element = FindElement(timeoutMs);
        var selectElement = new SelectElement(element);
        selectElement.SelectByIndex(index.Value);
        
        _context.Logger.LogAction("SelectByIndex", _locator, index.ToString());
    }
    
    public virtual void SelectByValue(string? value, int? timeoutMs = null)
    {
        if (value is null) return;
        
        var element = FindElement(timeoutMs);
        var selectElement = new SelectElement(element);
        selectElement.SelectByValue(value);
        
        _context.Logger.LogAction("SelectByValue", _locator, value);
    }
    
    public virtual string? GetSelectedText(int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element is null) return null;
        var selectElement = new SelectElement(element);
        return selectElement.SelectedOption?.Text;
    }
    
    public bool WaitSelectedText(string? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.ElementState;
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => GetSelectedText() == expected);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    public virtual int? GetSelectedIndex(int? timeoutMs = null)
    {
        var selected = GetSelectedText(timeoutMs);
        if (selected is null) return null;
        var items = GetItemTexts(timeoutMs);
        if (items is null) return null;
        var index = items.ToList().IndexOf(selected);
        return index >= 0 ? index : null;
    }
    
    public bool WaitSelectedIndex(int? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.ElementState;
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => GetSelectedIndex() == expected);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    public virtual IReadOnlyList<string>? GetItemTexts(int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element is null) return null;
        var selectElement = new SelectElement(element);
        return selectElement.Options.Select(o => o.Text).ToList().AsReadOnly();
    }
    
    public virtual int? GetItemCount(int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element is null) return null;
        var selectElement = new SelectElement(element);
        return selectElement.Options.Count;
    }
    
    public bool WaitItemCount(int? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.ElementState;
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => GetItemCount() == expected);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    public void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitItemCount(expected, timeoutMs);
        var actual = GetItemCount();
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected item count {expected} but was {actual}");
    }
    
    public void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitSelectedText(expected, timeoutMs);
        var actual = GetSelectedText();
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected selected text '{expected}' but was '{actual}'");
    }
    
    public void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitSelectedIndex(expected, timeoutMs);
        var actual = GetSelectedIndex();
        if (actual != expected.Value)
            throw new AssertionException(message ?? $"Expected selected index {expected} but was {actual}");
    }
}
```

---

## 9. BlazorMultiSelectorControlBase

Multi-selection for listboxes.

```csharp
public abstract class BlazorMultiSelectorControlBase : BlazorSelectorControlBase, IMultiSelectorControlObject
{
    protected BlazorMultiSelectorControlBase(IBlazorTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual IReadOnlyList<string>? GetSelectedTexts(int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element is null) return null;
        var selectElement = new SelectElement(element);
        return selectElement.AllSelectedOptions.Select(o => o.Text).ToList().AsReadOnly();
    }
    
    public bool WaitSelectedTexts(IEnumerable<string>? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var expectedList = expected.ToList();
        var timeout = timeoutMs ?? _context.Timeouts.ElementState;
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => {
                var actual = GetSelectedTexts();
                return actual is not null && actual.SequenceEqual(expectedList);
            });
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    public void AssertSelectedTexts(IEnumerable<string>? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitSelectedTexts(expected, timeoutMs);
        var actual = GetSelectedTexts();
        var expectedList = expected.ToList();
        if (actual is null || !actual.SequenceEqual(expectedList))
            throw new AssertionException(message ?? $"Expected selected texts do not match actual");
    }
    
    public virtual IReadOnlyList<int>? GetSelectedIndices(int? timeoutMs = null)
    {
        var allItems = GetItemTexts(timeoutMs);
        var selected = GetSelectedTexts(timeoutMs);
        if (allItems is null || selected is null) return null;
        return selected.Select(s => allItems.ToList().IndexOf(s)).ToList().AsReadOnly();
    }
    
    public bool WaitSelectedIndices(IEnumerable<int>? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var expectedList = expected.ToList();
        var timeout = timeoutMs ?? _context.Timeouts.ElementState;
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => {
                var actual = GetSelectedIndices();
                return actual is not null && actual.SequenceEqual(expectedList);
            });
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    public void AssertSelectedIndices(IEnumerable<int>? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitSelectedIndices(expected, timeoutMs);
        var actual = GetSelectedIndices();
        var expectedList = expected.ToList();
        if (actual is null || !actual.SequenceEqual(expectedList))
            throw new AssertionException(message ?? $"Expected selected indices do not match actual");
    }
    
    public virtual void SelectMultiple(IEnumerable<string>? texts, int? timeoutMs = null)
    {
        if (texts is null) return;
        ClearSelection(timeoutMs);
        foreach (var text in texts)
            SelectByText(text, timeoutMs);
    }
    
    public virtual void DeselectByText(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        var element = FindElement(timeoutMs);
        var selectElement = new SelectElement(element);
        selectElement.DeselectByText(text);
        
        _context.Logger.LogAction("DeselectByText", _locator, text);
    }
    
    public virtual void DeselectByIndex(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        var element = FindElement(timeoutMs);
        var selectElement = new SelectElement(element);
        selectElement.DeselectByIndex(index.Value);
        
        _context.Logger.LogAction("DeselectByIndex", _locator, index.ToString());
    }
    
    public virtual void ClearSelection(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var selectElement = new SelectElement(element);
        selectElement.DeselectAll();
        
        _context.Logger.LogAction("ClearSelection", _locator);
    }
    
    public int? GetSelectedCount(int? timeoutMs = null)
    {
        return GetSelectedTexts(timeoutMs)?.Count;
    }
    
    public bool WaitSelectedCount(int? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.ElementState;
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => GetSelectedCount() == expected);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    public void AssertSelectedCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitSelectedCount(expected, timeoutMs);
        var actual = GetSelectedCount();
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected selected count {expected} but was {actual}");
    }
}
```

---

## 10. BlazorDataGridControlBase

Data grid for table elements.

```csharp
public abstract class BlazorDataGridControlBase : BlazorItemsControlBase, IDataGridControlObject
{
    protected BlazorDataGridControlBase(IBlazorTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual int? GetRowCount(int? timeoutMs = null)
    {
        var element = TryFindElement();
        return element?.FindElements(By.CssSelector("tbody tr")).Count;
    }
    
    public virtual int? GetColumnCount(int? timeoutMs = null)
    {
        var element = TryFindElement();
        var headerRow = element?.FindElement(By.CssSelector("thead tr"));
        return headerRow?.FindElements(By.CssSelector("th")).Count;
    }
    
    public bool WaitColumnCount(int? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.ElementState;
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => GetColumnCount() == expected);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    public void AssertColumnCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitColumnCount(expected, timeoutMs);
        var actual = GetColumnCount();
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected column count {expected} but was {actual}");
    }
    
    public virtual IReadOnlyList<string>? GetColumnHeaders(int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element is null) return null;
        return element.FindElements(By.CssSelector("thead th"))
            .Select(e => e.Text)
            .ToList()
            .AsReadOnly();
    }
    
    public virtual string? GetCellText(int row, int column, int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element is null) return null;
        var rows = element.FindElements(By.CssSelector("tbody tr"));
        if (row >= rows.Count)
            return null;
        
        var cells = rows[row].FindElements(By.CssSelector("td"));
        if (column >= cells.Count)
            return null;
        
        return cells[column].Text;
    }
    
    public bool WaitCellText(int row, int column, string? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.ElementState;
        var wait = new WebDriverWait(_context.Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => GetCellText(row, column) == expected);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    public void AssertCellText(int row, int column, string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitCellText(row, column, expected, timeoutMs);
        var actual = GetCellText(row, column);
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected cell text '{expected}' but was '{actual}'");
    }
    
    public virtual IWebElement? GetCell(int row, int column, int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element is null) return null;
        try
        {
            return element.FindElement(By.CssSelector($"tbody tr:nth-child({row + 1}) td:nth-child({column + 1})"));
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }
    
    public virtual IReadOnlyList<string>? GetRowTexts(int row, int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element is null) return null;
        var rows = element.FindElements(By.CssSelector("tbody tr"));
        if (row >= rows.Count)
            return null;
        
        return rows[row].FindElements(By.CssSelector("td"))
            .Select(e => e.Text)
            .ToList()
            .AsReadOnly();
    }
    
    public virtual IReadOnlyList<string>? GetColumnTexts(int column, int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element is null) return null;
        return element.FindElements(By.CssSelector($"tbody tr td:nth-child({column + 1})"))
            .Select(e => e.Text)
            .ToList()
            .AsReadOnly();
    }
    
    public virtual void SelectRow(int row, int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var rows = element.FindElements(By.CssSelector("tbody tr"));
        if (row >= rows.Count)
            throw new ArgumentOutOfRangeException(nameof(row));
        
        rows[row].Click();
        _context.Logger.LogAction("SelectRow", _locator, row.ToString());
    }
    
    public virtual void ClickCell(int row, int column, int? timeoutMs = null)
    {
        GetCell(row, column, timeoutMs).Click();
        _context.Logger.LogAction("ClickCell", _locator, $"({row}, {column})");
    }
}
```

---

## 11. Remaining Base Classes

### BlazorRangeControlBase
- Implements `IRangeControlObject`
- Uses JavaScript for slider manipulation
- Handles HTML range input

### BlazorContainerControlBase
- Implements `IContainerControlObject`
- Provides scoped CSS selector composition
- See [250_003_IContainerScope.spx.md](250_003_IContainerScope.spx.md)

### BlazorItemsControlBase
- Implements `IItemsControlObject`
- Handles lists, repeated components

### BlazorScrollableControlBase
- Implements `IScrollableControlObject`
- Uses JavaScript scroll methods

### BlazorDateTimeControlBase
- Implements `IDateTimeControlObject`
- Handles date/time input types

---

## 12. Validation Checklist

- [ ] All base classes implement their corresponding interfaces
- [ ] Nullable skip pattern implemented consistently
- [ ] Logging integrated in all action methods
- [ ] WebDriverWait used for all wait operations
- [ ] SelectElement used for select/option elements
- [ ] Actions class used for mouse interactions
- [ ] JavaScript execution available for complex operations
- [ ] CSS class helper methods available
- [ ] Form validation helpers included

---

## Related Documents

- [Interface Hierarchy](250_005_InterfaceHierarchy.spx.md)
- [MAUI Base Classes](250_006_MauiBaseClasses.spx.md)
- [WPF Base Classes](250_008_WpfBaseClasses.spx.md)
- [Blazor Test Context](250_009_PlatformContexts.spx.md)
- [Selenium External](../../200_architecture/220_External/220_002_Selenium.spx.md)

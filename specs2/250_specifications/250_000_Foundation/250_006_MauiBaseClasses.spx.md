# 250.006 MAUI Base Classes Specification

**Block Type:** SPC (Specification)  
**ID:** 250.006  
**Title:** MAUI Base Class Hierarchy Specification  
**Status:** Draft  
**Version:** 1.0  
**Level:** 0 - Foundation

---

## 1. Overview

This specification defines the complete base class hierarchy for MAUI platform controls. All MAUI controls inherit from these base classes, which implement the core interfaces using Appium and AppiumElement.

### Package Identity

- **Package:** `Brinell.Maui`
- **Namespace:** `Brinell.Maui.Base`
- **Dependencies:** `Brinell.Core`, `Appium.WebDriver`
- **Element Type:** `AppiumElement`
- **Driver Type:** `AppiumDriver`

---

## 2. Base Class Hierarchy

```
MauiControlBase                        # Implements IControlObject
│
├── MauiClickableControlBase           # Implements IClickableControlObject
│   └── MauiLongPressControlBase       # Implements ILongPressControlObject
│
├── MauiTextControlBase                # Implements ITextControlObject
│   └── MauiEditableTextControlBase    # Implements IEditableTextControlObject
│
├── MauiToggleControlBase              # Implements IToggleControlObject
│
├── MauiSelectorControlBase            # Implements ISelectorControlObject
│
├── MauiRangeControlBase               # Implements IRangeControlObject
│
├── MauiContainerControlBase           # Implements IContainerControlObject
│
├── MauiItemsControlBase               # Implements IItemsControlObject
│
├── MauiScrollableControlBase          # Implements IScrollableControlObject
│
└── MauiDateTimeControlBase            # Implements IDateTimeControlObject
```

---

## 3. MauiControlBase

Foundation for all MAUI controls.

```csharp
namespace Brinell.Maui.Base
{
    public abstract class MauiControlBase : IControlObject
    {
        protected readonly IMauiTestContext _context;
        protected readonly Locator _locator;
        protected readonly IPageObject? _page;
        
        protected MauiControlBase(IMauiTestContext context, Locator locator, IPageObject? page = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _page = page;
        }
        
        // Convenience constructor with string AutomationId
        protected MauiControlBase(IMauiTestContext context, string automationId, IPageObject? page = null)
            : this(context, Locator.ByAutomationId(automationId), page) { }
        
        // Identity
        public Locator Locator => _locator;
        public IPageObject? Page => _page;
        
        // State methods
        public bool IsExists()
        {
            try { return TryFindElement() != null; }
            catch { return false; }
        }
        
        public bool IsVisible()
        {
            var element = TryFindElement();
            return element?.Displayed ?? false;
        }
        
        public bool IsEnabled()
        {
            var element = TryFindElement();
            return element?.Enabled ?? false;
        }
        
        // Wait methods
        public bool WaitExists(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
            return WaitHelper.WaitFor(() => IsExists() == expected.Value, timeout, _context.Timeouts.PollingInterval);
        }
        
        public bool WaitVisible(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
            return WaitHelper.WaitFor(() => IsVisible() == expected.Value, timeout, _context.Timeouts.PollingInterval);
        }
        
        public bool WaitEnabled(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
            return WaitHelper.WaitFor(() => IsEnabled() == expected.Value, timeout, _context.Timeouts.PollingInterval);
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
        public string GetText(int? timeoutMs = null)
        {
            var element = FindElement(timeoutMs);
            return element.Text ?? string.Empty;
        }
        
        public void AssertText(string? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            var actual = GetText(timeoutMs);
            if (actual != expected)
                throw new AssertionException(message ?? $"Expected text '{expected}' but was '{actual}'");
        }
        
        public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            var actual = GetText(timeoutMs);
            if (!actual.Contains(expected))
                throw new AssertionException(message ?? $"Text '{actual}' does not contain '{expected}'");
        }
        
        // Attribute methods
        public string? GetAttribute(string name)
        {
            var element = TryFindElement();
            return element?.GetDomAttribute(name);
        }
        
        // Protected element finding
        protected AppiumElement? TryFindElement()
        {
            return _context.TryFindElement(_locator);
        }
        
        protected AppiumElement FindElement(int? timeoutMs = null)
        {
            var timeout = timeoutMs ?? _context.Timeouts.ElementFind;
            var element = WaitHelper.WaitFor(
                () => _context.TryFindElement(_locator),
                e => e != null,
                timeout,
                _context.Timeouts.PollingInterval);
            
            return element ?? throw new ElementNotFoundException($"Element not found: {_locator}");
        }
    }
}
```

---

## 4. MauiClickableControlBase

Click capability for buttons, images.

```csharp
public abstract class MauiClickableControlBase : MauiControlBase, IClickableControlObject
{
    protected MauiClickableControlBase(IMauiTestContext context, Locator locator, IPageObject? page = null)
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
        return WaitHelper.WaitFor(
            () => (IsVisible() && IsEnabled()) == clickable.Value,
            timeout,
            _context.Timeouts.PollingInterval);
    }
}
```

---

## 5. MauiLongPressControlBase

Long press for mobile gestures.

```csharp
public abstract class MauiLongPressControlBase : MauiClickableControlBase, ILongPressControlObject
{
    protected MauiLongPressControlBase(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual void LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var duration = durationMs ?? 1000; // Default 1 second
        
        var actions = new Actions(_context.Driver);
        actions.ClickAndHold(element)
               .Pause(TimeSpan.FromMilliseconds(duration))
               .Release()
               .Perform();
        
        _context.Logger.LogAction("LongPress", _locator, $"{duration}ms");
    }
}
```

---

## 6. MauiTextControlBase

Text display and verification.

```csharp
public abstract class MauiTextControlBase : MauiControlBase, ITextControlObject
{
    protected MauiTextControlBase(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public bool WaitTextEquals(string? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return WaitHelper.WaitFor(
            () => GetText() == expected,
            timeout,
            _context.Timeouts.PollingInterval);
    }
    
    public bool WaitTextContains(string? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return WaitHelper.WaitFor(
            () => GetText().Contains(expected),
            timeout,
            _context.Timeouts.PollingInterval);
    }
    
    public void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null)
    {
        if (pattern is null) return;
        var actual = GetText(timeoutMs);
        if (!System.Text.RegularExpressions.Regex.IsMatch(actual, pattern))
            throw new AssertionException(message ?? $"Text '{actual}' does not match pattern '{pattern}'");
    }
}
```

---

## 7. MauiEditableTextControlBase

Text input for Entry, Editor.

```csharp
public abstract class MauiEditableTextControlBase : MauiTextControlBase, IEditableTextControlObject
{
    protected MauiEditableTextControlBase(IMauiTestContext context, Locator locator, IPageObject? page = null)
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
        return GetAttribute("hint") ?? GetAttribute("placeholder");
    }
    
    public virtual bool IsReadOnly()
    {
        var readOnly = GetAttribute("readonly") ?? GetAttribute("editable");
        return readOnly == "true" || readOnly == "false"; // editable="false" means read-only
    }
}
```

---

## 8. MauiToggleControlBase

Toggle state for CheckBox, Switch.

```csharp
public abstract class MauiToggleControlBase : MauiControlBase, IToggleControlObject
{
    protected MauiToggleControlBase(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual bool IsChecked()
    {
        var element = TryFindElement();
        if (element == null) return false;
        
        // MAUI exposes checked state via "checked" attribute or "selected" property
        var checkedAttr = element.GetDomAttribute("checked");
        if (checkedAttr != null) return checkedAttr == "true";
        
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
        return WaitHelper.WaitFor(
            () => IsChecked() == expected.Value,
            timeout,
            _context.Timeouts.PollingInterval);
    }
}
```

---

## 9. MauiSelectorControlBase

Selection from list for Picker.

```csharp
public abstract class MauiSelectorControlBase : MauiControlBase, ISelectorControlObject
{
    protected MauiSelectorControlBase(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual void SelectByText(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        
        var element = FindElement(timeoutMs);
        element.Click(); // Open picker
        
        // Find and click item in picker
        var itemLocator = Locator.ByText(text);
        var item = _context.FindElement(itemLocator);
        item.Click();
        
        _context.Logger.LogAction("SelectByText", _locator, text);
    }
    
    public virtual void SelectByIndex(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        var items = GetItemTexts(timeoutMs);
        if (index.Value >= 0 && index.Value < items.Count)
            SelectByText(items[index.Value], timeoutMs);
    }
    
    public virtual void SelectByValue(string? value, int? timeoutMs = null)
    {
        // MAUI Picker doesn't have value attribute, use text
        SelectByText(value, timeoutMs);
    }
    
    public virtual string GetSelectedText(int? timeoutMs = null)
    {
        return GetText(timeoutMs);
    }
    
    public virtual int GetSelectedIndex(int? timeoutMs = null)
    {
        var selected = GetSelectedText(timeoutMs);
        var items = GetItemTexts(timeoutMs);
        return items.ToList().IndexOf(selected);
    }
    
    public abstract IReadOnlyList<string> GetItemTexts(int? timeoutMs = null);
    
    public virtual int GetItemCount(int? timeoutMs = null)
    {
        return GetItemTexts(timeoutMs).Count;
    }
    
    public void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = GetSelectedText(timeoutMs);
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected selected text '{expected}' but was '{actual}'");
    }
    
    public void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = GetSelectedIndex(timeoutMs);
        if (actual != expected.Value)
            throw new AssertionException(message ?? $"Expected selected index {expected} but was {actual}");
    }
}
```

---

## 10. MauiRangeControlBase

Range for Slider, Stepper.

```csharp
public abstract class MauiRangeControlBase : MauiControlBase, IRangeControlObject
{
    protected MauiRangeControlBase(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual double GetValue(int? timeoutMs = null)
    {
        var text = GetText(timeoutMs);
        return double.TryParse(text, out var value) ? value : 0;
    }
    
    public abstract void SetValue(double? value, int? timeoutMs = null);
    
    public virtual double GetMinimum(int? timeoutMs = null)
    {
        var attr = GetAttribute("min") ?? GetAttribute("minimum");
        return double.TryParse(attr, out var min) ? min : 0;
    }
    
    public virtual double GetMaximum(int? timeoutMs = null)
    {
        var attr = GetAttribute("max") ?? GetAttribute("maximum");
        return double.TryParse(attr, out var max) ? max : 100;
    }
    
    public virtual double GetStep(int? timeoutMs = null)
    {
        var attr = GetAttribute("step") ?? GetAttribute("increment");
        return double.TryParse(attr, out var step) ? step : 1;
    }
    
    public void AssertValue(double? expected, double tolerance = 0.001, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = GetValue(timeoutMs);
        if (Math.Abs(actual - expected.Value) > tolerance)
            throw new AssertionException(message ?? $"Expected value {expected} but was {actual}");
    }
    
    public bool WaitValue(double? expected, double tolerance = 0.001, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return WaitHelper.WaitFor(
            () => Math.Abs(GetValue() - expected.Value) <= tolerance,
            timeout,
            _context.Timeouts.PollingInterval);
    }
    
    public virtual void Increment(int? timeoutMs = null)
    {
        SetValue(GetValue(timeoutMs) + GetStep(timeoutMs), timeoutMs);
    }
    
    public virtual void Decrement(int? timeoutMs = null)
    {
        SetValue(GetValue(timeoutMs) - GetStep(timeoutMs), timeoutMs);
    }
}
```

---

## 11. Remaining Base Classes

The following base classes follow similar patterns:

### MauiContainerControlBase
- Implements `IContainerControlObject`
- Provides `FindChild<T>`, `FindChildren<T>`, `GetChildCount`, `ChildExists`
- See [250_003_IContainerScope.spx.md](250_003_IContainerScope.spx.md) for interface details

### MauiItemsControlBase
- Implements `IItemsControlObject`
- Provides `GetItemCount`, `GetItemLocator`, `FindItemLocator`, `GetItemTexts`
- Handles ListView, CollectionView

### MauiScrollableControlBase
- Implements `IScrollableControlObject`
- Uses Appium touch actions for scrolling
- Provides `ScrollToTop`, `ScrollToEnd`, `ScrollTo`

### MauiDateTimeControlBase
- Implements `IDateTimeControlObject`
- Handles DatePicker, TimePicker interaction
- Platform-specific picker interaction

---

## 12. Validation Checklist

- [ ] All base classes implement their corresponding interfaces
- [ ] Nullable skip pattern implemented consistently
- [ ] Logging integrated in all action methods
- [ ] Timeout inheritance from context settings
- [ ] AppiumElement used throughout
- [ ] Actions class used for complex gestures
- [ ] Error handling returns appropriate exceptions

---

## Related Documents

- [Interface Hierarchy](250_005_InterfaceHierarchy.spx.md)
- [Blazor Base Classes](250_007_BlazorBaseClasses.spx.md)
- [WPF Base Classes](250_008_WpfBaseClasses.spx.md)
- [MAUI Test Context](250_009_PlatformContexts.spx.md)
- [Appium External](../../200_architecture/220_External/220_001_Appium.spx.md)

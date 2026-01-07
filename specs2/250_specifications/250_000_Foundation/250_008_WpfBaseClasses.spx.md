# 250.008 WPF Base Classes Specification

**Block Type:** SPC (Specification)  
**ID:** 250.008  
**Title:** WPF Base Class Hierarchy Specification  
**Status:** Draft  
**Version:** 1.0  
**Level:** 0 - Foundation

---

## 1. Overview

This specification defines the complete base class hierarchy for WPF platform controls. All WPF controls inherit from these base classes, which implement the core interfaces using FlaUI and AutomationElement.

### Package Identity

- **Package:** `Brinell.Wpf`
- **Namespace:** `Brinell.Wpf.Base`
- **Dependencies:** `Brinell.Core`, `FlaUI.Core`, `FlaUI.UIA3`
- **Element Type:** `AutomationElement`
- **Framework:** FlaUI.UIA3 (UI Automation)

---

## 2. Base Class Hierarchy

```
WpfControlBase                          # Implements IControlObject
│
├── WpfClickableControlBase             # Implements IClickableControlObject
│
├── WpfTextControlBase                  # Implements ITextControlObject
│   └── WpfEditableTextControlBase      # Implements IEditableTextControlObject
│
├── WpfToggleControlBase                # Implements IToggleControlObject
│
├── WpfSelectorControlBase              # Implements ISelectorControlObject
│   └── WpfMultiSelectorControlBase     # Implements IMultiSelectorControlObject
│
├── WpfRangeControlBase                 # Implements IRangeControlObject
│
├── WpfContainerControlBase             # Implements IContainerControlObject
│
├── WpfItemsControlBase                 # Implements IItemsControlObject
│   └── WpfDataGridControlBase          # Implements IDataGridControlObject
│
├── WpfScrollableControlBase            # Implements IScrollableControlObject
│
├── WpfWindowControlBase                # Implements IWindowControlObject
│
└── WpfDateTimeControlBase              # Implements IDateTimeControlObject
```

---

## 3. WpfControlBase

Foundation for all WPF controls.

```csharp
namespace Brinell.Wpf.Base
{
    public abstract class WpfControlBase : IControlObject
    {
        protected readonly IWpfTestContext _context;
        protected readonly Locator _locator;
        protected readonly IPageObject? _page;
        
        protected WpfControlBase(IWpfTestContext context, Locator locator, IPageObject? page = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _page = page;
        }
        
        // Convenience constructor with AutomationId
        protected WpfControlBase(IWpfTestContext context, string automationId, IPageObject? page = null)
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
            return element?.IsOffscreen == false;
        }
        
        public bool IsEnabled()
        {
            var element = TryFindElement();
            return element?.IsEnabled ?? false;
        }
        
        // Wait methods
        public bool WaitExists(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
            return Retry.WhileFalse(
                () => IsExists() == expected.Value,
                TimeSpan.FromMilliseconds(timeout),
                TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval)).Result;
        }
        
        public bool WaitVisible(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
            return Retry.WhileFalse(
                () => IsVisible() == expected.Value,
                TimeSpan.FromMilliseconds(timeout),
                TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval)).Result;
        }
        
        public bool WaitEnabled(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
            return Retry.WhileFalse(
                () => IsEnabled() == expected.Value,
                TimeSpan.FromMilliseconds(timeout),
                TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval)).Result;
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
            return element.Name ?? string.Empty;
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
        
        // Attribute methods (UI Automation properties)
        public string? GetAttribute(string name)
        {
            var element = TryFindElement();
            if (element == null) return null;
            
            return name.ToLower() switch
            {
                "automationid" => element.AutomationId,
                "name" => element.Name,
                "classname" => element.ClassName,
                "controltype" => element.ControlType.ToString(),
                "helptext" => element.HelpText,
                "itemstatus" => element.ItemStatus,
                "itemtype" => element.ItemType,
                "localizedcontroltype" => element.LocalizedControlType,
                _ => null
            };
        }
        
        // WPF-specific: Control type
        public ControlType GetControlType()
        {
            var element = TryFindElement();
            return element?.ControlType ?? ControlType.Unknown;
        }
        
        // Protected element finding
        protected AutomationElement? TryFindElement()
        {
            return _context.TryFindElement(_locator);
        }
        
        protected AutomationElement FindElement(int? timeoutMs = null)
        {
            var timeout = timeoutMs ?? _context.Timeouts.ElementFind;
            
            var result = Retry.WhileNull(
                () => _context.TryFindElement(_locator),
                TimeSpan.FromMilliseconds(timeout),
                TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval));
            
            return result.Result ?? throw new ElementNotFoundException($"Element not found: {_locator}");
        }
        
        // WPF-specific: Get as specific control type
        protected T AsPattern<T>() where T : class, IPattern
        {
            var element = FindElement();
            return element.Patterns.GetPattern<T>();
        }
        
        protected T? TryAsPattern<T>() where T : class, IPattern
        {
            var element = TryFindElement();
            return element?.Patterns.TryGetPattern<T>();
        }
    }
}
```

---

## 4. WpfClickableControlBase

Click capability using InvokePattern.

```csharp
public abstract class WpfClickableControlBase : WpfControlBase, IClickableControlObject
{
    protected WpfClickableControlBase(IWpfTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual void Click(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        
        // Try InvokePattern first (for buttons)
        var invokePattern = element.Patterns.Invoke.TryGetPattern();
        if (invokePattern != null)
        {
            invokePattern.Invoke();
        }
        else
        {
            // Fall back to mouse click
            element.Click();
        }
        
        _context.Logger.LogAction("Click", _locator);
    }
    
    public virtual void DoubleClick(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        element.DoubleClick();
        _context.Logger.LogAction("DoubleClick", _locator);
    }
    
    public virtual void RightClick(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        element.RightClick();
        _context.Logger.LogAction("RightClick", _locator);
    }
    
    public bool WaitClickable(bool? clickable, int? timeoutMs = null)
    {
        if (clickable is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return Retry.WhileFalse(
            () => (IsVisible() && IsEnabled()) == clickable.Value,
            TimeSpan.FromMilliseconds(timeout),
            TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval)).Result;
    }
    
    // WPF-specific: Focus
    public virtual void Focus(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        element.Focus();
        _context.Logger.LogAction("Focus", _locator);
    }
    
    public bool IsFocused()
    {
        var element = TryFindElement();
        return element?.HasKeyboardFocus ?? false;
    }
}
```

---

## 5. WpfTextControlBase

Text display using TextPattern or Name.

```csharp
public abstract class WpfTextControlBase : WpfControlBase, ITextControlObject
{
    protected WpfTextControlBase(IWpfTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public override string GetText(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        
        // Try TextPattern first
        var textPattern = element.Patterns.Text.TryGetPattern();
        if (textPattern != null)
            return textPattern.DocumentRange.GetText(-1);
        
        // Fall back to Name property
        return element.Name ?? string.Empty;
    }
    
    public bool WaitTextEquals(string? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return Retry.WhileFalse(
            () => GetText() == expected,
            TimeSpan.FromMilliseconds(timeout),
            TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval)).Result;
    }
    
    public bool WaitTextContains(string? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return Retry.WhileFalse(
            () => GetText().Contains(expected),
            TimeSpan.FromMilliseconds(timeout),
            TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval)).Result;
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

## 6. WpfEditableTextControlBase

Text input using ValuePattern or keyboard.

```csharp
public abstract class WpfEditableTextControlBase : WpfTextControlBase, IEditableTextControlObject
{
    protected WpfEditableTextControlBase(IWpfTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public override string GetText(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        
        // Try ValuePattern first (for TextBox, etc.)
        var valuePattern = element.Patterns.Value.TryGetPattern();
        if (valuePattern != null)
            return valuePattern.Value.Value ?? string.Empty;
        
        // Fall back to TextPattern
        return base.GetText(timeoutMs);
    }
    
    public virtual void Enter(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        var element = FindElement(timeoutMs);
        
        // Try ValuePattern first
        var valuePattern = element.Patterns.Value.TryGetPattern();
        if (valuePattern != null && !valuePattern.IsReadOnly.Value)
        {
            valuePattern.SetValue(valuePattern.Value.Value + text);
        }
        else
        {
            // Fall back to keyboard input
            element.Focus();
            Keyboard.Type(text);
        }
        
        _context.Logger.LogAction("Enter", _locator, text);
    }
    
    public virtual void Clear(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        
        // Try ValuePattern first
        var valuePattern = element.Patterns.Value.TryGetPattern();
        if (valuePattern != null && !valuePattern.IsReadOnly.Value)
        {
            valuePattern.SetValue(string.Empty);
        }
        else
        {
            // Fall back to select all + delete
            element.Focus();
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Keyboard.Type(VirtualKeyShort.DELETE);
        }
        
        _context.Logger.LogAction("Clear", _locator);
    }
    
    public virtual void SetText(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        
        var element = FindElement(timeoutMs);
        var valuePattern = element.Patterns.Value.TryGetPattern();
        
        if (valuePattern != null && !valuePattern.IsReadOnly.Value)
        {
            valuePattern.SetValue(text);
        }
        else
        {
            Clear(timeoutMs);
            Enter(text, timeoutMs);
        }
        
        _context.Logger.LogAction("SetText", _locator, text);
    }
    
    public virtual string? GetPlaceholder()
    {
        // WPF TextBox doesn't have native placeholder
        // Check for common watermark patterns
        return GetAttribute("helptext");
    }
    
    public virtual bool IsReadOnly()
    {
        var element = TryFindElement();
        var valuePattern = element?.Patterns.Value.TryGetPattern();
        return valuePattern?.IsReadOnly.Value ?? true;
    }
}
```

---

## 7. WpfToggleControlBase

Toggle state using TogglePattern.

```csharp
public abstract class WpfToggleControlBase : WpfControlBase, IToggleControlObject
{
    protected WpfToggleControlBase(IWpfTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual bool IsChecked()
    {
        var element = TryFindElement();
        var togglePattern = element?.Patterns.Toggle.TryGetPattern();
        return togglePattern?.ToggleState.Value == ToggleState.On;
    }
    
    public virtual void Toggle(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var togglePattern = element.Patterns.Toggle.Pattern;
        togglePattern.Toggle();
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
        return Retry.WhileFalse(
            () => IsChecked() == expected.Value,
            TimeSpan.FromMilliseconds(timeout),
            TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval)).Result;
    }
    
    // WPF-specific: Tri-state support
    public ToggleState GetToggleState()
    {
        var element = TryFindElement();
        var togglePattern = element?.Patterns.Toggle.TryGetPattern();
        return togglePattern?.ToggleState.Value ?? ToggleState.Indeterminate;
    }
    
    public bool IsIndeterminate()
    {
        return GetToggleState() == ToggleState.Indeterminate;
    }
}
```

---

## 8. WpfSelectorControlBase

Selection using SelectionPattern.

```csharp
public abstract class WpfSelectorControlBase : WpfControlBase, ISelectorControlObject
{
    protected WpfSelectorControlBase(IWpfTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual void SelectByText(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        
        var element = FindElement(timeoutMs);
        var items = element.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem));
        
        var item = items.FirstOrDefault(i => i.Name == text);
        if (item == null)
            throw new ElementNotFoundException($"Item '{text}' not found");
        
        var selectionItemPattern = item.Patterns.SelectionItem.Pattern;
        selectionItemPattern.Select();
        
        _context.Logger.LogAction("SelectByText", _locator, text);
    }
    
    public virtual void SelectByIndex(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        
        var element = FindElement(timeoutMs);
        var items = element.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem));
        
        if (index.Value >= items.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        var selectionItemPattern = items[index.Value].Patterns.SelectionItem.Pattern;
        selectionItemPattern.Select();
        
        _context.Logger.LogAction("SelectByIndex", _locator, index.ToString());
    }
    
    public virtual void SelectByValue(string? value, int? timeoutMs = null)
    {
        // WPF ListItem doesn't have value, use text
        SelectByText(value, timeoutMs);
    }
    
    public virtual string GetSelectedText(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var selectionPattern = element.Patterns.Selection.TryGetPattern();
        
        var selection = selectionPattern?.Selection.Value;
        return selection?.FirstOrDefault()?.Name ?? string.Empty;
    }
    
    public virtual int GetSelectedIndex(int? timeoutMs = null)
    {
        var selected = GetSelectedText(timeoutMs);
        var items = GetItemTexts(timeoutMs);
        return items.ToList().IndexOf(selected);
    }
    
    public virtual IReadOnlyList<string> GetItemTexts(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var items = element.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem));
        return items.Select(i => i.Name).ToList().AsReadOnly();
    }
    
    public virtual int GetItemCount(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var items = element.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem));
        return items.Length;
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

## 9. WpfWindowControlBase

Window control using WindowPattern.

```csharp
public abstract class WpfWindowControlBase : WpfControlBase, IWindowControlObject
{
    protected WpfWindowControlBase(IWpfTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual string GetTitle(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        return element.Name ?? string.Empty;
    }
    
    public virtual void Close(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var windowPattern = element.Patterns.Window.Pattern;
        windowPattern.Close();
        _context.Logger.LogAction("Close", _locator);
    }
    
    public virtual void Maximize(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var windowPattern = element.Patterns.Window.Pattern;
        windowPattern.SetWindowVisualState(WindowVisualState.Maximized);
        _context.Logger.LogAction("Maximize", _locator);
    }
    
    public virtual void Minimize(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var windowPattern = element.Patterns.Window.Pattern;
        windowPattern.SetWindowVisualState(WindowVisualState.Minimized);
        _context.Logger.LogAction("Minimize", _locator);
    }
    
    public virtual void Restore(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var windowPattern = element.Patterns.Window.Pattern;
        windowPattern.SetWindowVisualState(WindowVisualState.Normal);
        _context.Logger.LogAction("Restore", _locator);
    }
    
    public virtual bool IsMaximized()
    {
        var element = TryFindElement();
        var windowPattern = element?.Patterns.Window.TryGetPattern();
        return windowPattern?.WindowVisualState.Value == WindowVisualState.Maximized;
    }
    
    public virtual bool IsMinimized()
    {
        var element = TryFindElement();
        var windowPattern = element?.Patterns.Window.TryGetPattern();
        return windowPattern?.WindowVisualState.Value == WindowVisualState.Minimized;
    }
    
    public virtual bool IsModal()
    {
        var element = TryFindElement();
        var windowPattern = element?.Patterns.Window.TryGetPattern();
        return windowPattern?.IsModal.Value ?? false;
    }
    
    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = GetTitle(timeoutMs);
        if (actual != expected)
            throw new AssertionException(message ?? $"Expected title '{expected}' but was '{actual}'");
    }
}
```

---

## 10. WpfRangeControlBase

Range using RangeValuePattern.

```csharp
public abstract class WpfRangeControlBase : WpfControlBase, IRangeControlObject
{
    protected WpfRangeControlBase(IWpfTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual double GetValue(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var rangePattern = element.Patterns.RangeValue.Pattern;
        return rangePattern.Value.Value;
    }
    
    public virtual void SetValue(double? value, int? timeoutMs = null)
    {
        if (value is null) return;
        
        var element = FindElement(timeoutMs);
        var rangePattern = element.Patterns.RangeValue.Pattern;
        
        if (rangePattern.IsReadOnly.Value)
            throw new InvalidOperationException("Control is read-only");
        
        rangePattern.SetValue(value.Value);
        _context.Logger.LogAction("SetValue", _locator, value.ToString());
    }
    
    public virtual double GetMinimum(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var rangePattern = element.Patterns.RangeValue.Pattern;
        return rangePattern.Minimum.Value;
    }
    
    public virtual double GetMaximum(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var rangePattern = element.Patterns.RangeValue.Pattern;
        return rangePattern.Maximum.Value;
    }
    
    public virtual double GetStep(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var rangePattern = element.Patterns.RangeValue.Pattern;
        return rangePattern.SmallChange.Value;
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
        return Retry.WhileFalse(
            () => Math.Abs(GetValue() - expected.Value) <= tolerance,
            TimeSpan.FromMilliseconds(timeout),
            TimeSpan.FromMilliseconds(_context.Timeouts.PollingInterval)).Result;
    }
    
    public virtual void Increment(int? timeoutMs = null)
    {
        var current = GetValue(timeoutMs);
        var step = GetStep(timeoutMs);
        SetValue(current + step, timeoutMs);
    }
    
    public virtual void Decrement(int? timeoutMs = null)
    {
        var current = GetValue(timeoutMs);
        var step = GetStep(timeoutMs);
        SetValue(current - step, timeoutMs);
    }
}
```

---

## 11. Remaining Base Classes

### WpfMultiSelectorControlBase
- Implements `IMultiSelectorControlObject`
- Uses SelectionPattern with multi-select support
- AddToSelection/RemoveFromSelection methods

### WpfContainerControlBase
- Implements `IContainerControlObject`
- Uses FlaUI tree traversal
- See [250_003_IContainerScope.spx.md](250_003_IContainerScope.spx.md)

### WpfItemsControlBase
- Implements `IItemsControlObject`
- Handles ListBox, TreeView, ListView

### WpfDataGridControlBase
- Implements `IDataGridControlObject`
- Uses Grid and Table patterns
- Row/Column navigation

### WpfScrollableControlBase
- Implements `IScrollableControlObject`
- Uses ScrollPattern
- Horizontal and vertical scrolling

### WpfDateTimeControlBase
- Implements `IDateTimeControlObject`
- Handles DatePicker control

---

## 12. FlaUI Patterns Summary

| Pattern | Used By | Purpose |
|---------|---------|---------|
| InvokePattern | Button | Click actions |
| TogglePattern | CheckBox, RadioButton | Check/uncheck |
| SelectionPattern | ListBox, ComboBox | Selection state |
| SelectionItemPattern | ListItem | Select item |
| ValuePattern | TextBox | Get/set text |
| TextPattern | Label, TextBlock | Get text |
| RangeValuePattern | Slider, ProgressBar | Range values |
| ScrollPattern | ScrollViewer | Scrolling |
| WindowPattern | Window | Window control |
| GridPattern | DataGrid | Grid navigation |
| TablePattern | DataGrid | Table structure |
| TransformPattern | Window | Move/resize |
| ExpandCollapsePattern | TreeViewItem | Expand/collapse |

---

## 13. Validation Checklist

- [ ] All base classes implement their corresponding interfaces
- [ ] Nullable skip pattern implemented consistently
- [ ] Logging integrated in all action methods
- [ ] FlaUI Retry used for wait operations
- [ ] UI Automation patterns used where appropriate
- [ ] Keyboard class used for text input fallback
- [ ] ControlType checking for element identification
- [ ] Pattern availability checked before use

---

## Related Documents

- [Interface Hierarchy](250_005_InterfaceHierarchy.spx.md)
- [MAUI Base Classes](250_006_MauiBaseClasses.spx.md)
- [Blazor Base Classes](250_007_BlazorBaseClasses.spx.md)
- [WPF Test Context](250_009_PlatformContexts.spx.md)
- [FlaUI External](../../200_architecture/220_External/220_003_FlaUI.spx.md)

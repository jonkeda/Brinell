# 4. Control Objects for Stride UI

**Parent:** [Documentation Index](30d0_StrideUITestFramework_Index.md)  
**Previous:** [Core Framework](30d3_CoreFramework.md)  
**Next:** [Page Objects](30d5_PageObjects.md)  
**Version:** 1.0 (Proposal - January 2025)

---

## 4.1 Control Object Hierarchy

```
IControlObject (Core Interface)
?
??? StrideControlBase (Stride Implementation)
    ?
    ??? StrideContentControlBase (IContentControl)
    ?   ??? StrideButtonControl
    ?   ??? StrideLabelControl
    ?
    ??? StrideTextControlBase (ITextControl)
    ?   ??? StrideTextBlockControl (display only)
    ?   ??? StrideEditTextControl (editable)
    ?
    ??? StrideToggleControlBase (IToggleControl)
    ?   ??? StrideCheckBoxControl
    ?   ??? StrideToggleButtonControl
    ?
    ??? StrideSelectorControlBase (ISelectorControl)
    ?   ??? StrideListBoxControl
    ?   ??? StrideComboBoxControl (if available)
    ?
    ??? StrideRangeControlBase (IRangeControl)
    ?   ??? StrideSliderControl
    ?
    ??? StrideContainerControlBase
        ??? StrideStackPanelControl
        ??? StrideGridControl
        ??? StrideCanvasControl
```

---

## 4.2 StrideControlBase

Base class for all Stride UI control wrappers:

```csharp
/// <summary>
/// Base class for all Stride UI control objects.
/// Implements the Wait/Check/Is/Assert pattern.
/// </summary>
public abstract class StrideControlBase : IControlObject
{
    protected readonly StrideTestContext Context;
    protected readonly string _automationId;
    
    public string AutomationId => _automationId;
    public IPageObject? Page { get; }
    
    protected StrideControlBase(
        StrideTestContext context, 
        IPageObject? page, 
        string automationId)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Page = page;
        _automationId = automationId ?? throw new ArgumentNullException(nameof(automationId));
    }
    
    #region Element State Access
    
    /// <summary>
    /// Get current element state from game.
    /// </summary>
    protected ElementState GetState() => Context.GetElementState(_automationId);
    
    /// <summary>
    /// Get element screen bounds for input simulation.
    /// </summary>
    protected Rectangle GetBounds() => GetState().Bounds;
    
    #endregion
    
    #region Is* Methods (Immediate State Check)
    
    public bool IsExists() => GetState().Exists;
    
    public bool IsVisible()
    {
        var state = GetState();
        return state.Exists && state.IsVisible;
    }
    
    public bool IsEnabled()
    {
        var state = GetState();
        return state.Exists && state.IsEnabled;
    }
    
    public bool IsClickable()
    {
        var state = GetState();
        return state.Exists && state.IsVisible && state.IsEnabled && state.IsHitTestVisible;
    }
    
    public bool IsFocused() => GetState().IsFocused;
    
    public string GetText() => GetState().Text ?? string.Empty;
    
    #endregion
    
    #region Wait* Methods (Poll Until Condition)
    
    public bool WaitExists(bool expected = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsExists() == expected,
            timeoutMs,
            $"element '{_automationId}' exists={expected}");
    }
    
    public bool WaitVisible(bool expected = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsVisible() == expected,
            timeoutMs,
            $"element '{_automationId}' visible={expected}");
    }
    
    public bool WaitEnabled(bool expected = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsEnabled() == expected,
            timeoutMs,
            $"element '{_automationId}' enabled={expected}");
    }
    
    public bool WaitClickable(int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsClickable(),
            timeoutMs,
            $"element '{_automationId}' clickable");
    }
    
    public bool WaitText(string expected, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => GetText() == expected,
            timeoutMs,
            $"element '{_automationId}' text='{expected}'");
    }
    
    public bool WaitTextContains(string substring, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => GetText().Contains(substring),
            timeoutMs,
            $"element '{_automationId}' text contains '{substring}'");
    }
    
    #endregion
    
    #region Check* Methods (Wait + Throw on Failure)
    
    public void CheckExists(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitExists(expected, timeoutMs))
        {
            throw new CheckFailedException(
                $"Control '{_automationId}' exists check failed. Expected: {expected}, Actual: {IsExists()}");
        }
    }
    
    public void CheckVisible(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitVisible(expected, timeoutMs))
        {
            throw new CheckFailedException(
                $"Control '{_automationId}' visibility check failed. Expected: {expected}, Actual: {IsVisible()}");
        }
    }
    
    public void CheckEnabled(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitEnabled(expected, timeoutMs))
        {
            throw new CheckFailedException(
                $"Control '{_automationId}' enabled check failed. Expected: {expected}, Actual: {IsEnabled()}");
        }
    }
    
    public void CheckClickable(int? timeoutMs = null)
    {
        if (!WaitClickable(timeoutMs))
        {
            var state = GetState();
            throw new CheckFailedException(
                $"Control '{_automationId}' is not clickable. " +
                $"Visible: {state.IsVisible}, Enabled: {state.IsEnabled}, HitTestVisible: {state.IsHitTestVisible}");
        }
    }
    
    public void CheckText(string expected, int? timeoutMs = null)
    {
        if (!WaitText(expected, timeoutMs))
        {
            throw new CheckFailedException(
                $"Control '{_automationId}' text check failed. Expected: '{expected}', Actual: '{GetText()}'");
        }
    }
    
    #endregion
    
    #region Assert* Methods (Semantic Assertion with Logging)
    
    public void AssertExists(string? message = null)
    {
        var exists = IsExists();
        LogAssertion("AssertExists", expected: true, actual: exists);
        
        if (!exists)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should exist but does not.");
        }
    }
    
    public void AssertNotExists(string? message = null)
    {
        var exists = IsExists();
        LogAssertion("AssertNotExists", expected: false, actual: exists);
        
        if (exists)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should not exist but does.");
        }
    }
    
    public void AssertVisible(string? message = null)
    {
        CheckExists();
        var visible = IsVisible();
        LogAssertion("AssertVisible", expected: true, actual: visible);
        
        if (!visible)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should be visible but is not.");
        }
    }
    
    public void AssertNotVisible(string? message = null)
    {
        var visible = IsExists() && IsVisible();
        LogAssertion("AssertNotVisible", expected: false, actual: visible);
        
        if (visible)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should not be visible but is.");
        }
    }
    
    public void AssertEnabled(string? message = null)
    {
        CheckVisible();
        var enabled = IsEnabled();
        LogAssertion("AssertEnabled", expected: true, actual: enabled);
        
        if (!enabled)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should be enabled but is not.");
        }
    }
    
    public void AssertDisabled(string? message = null)
    {
        CheckVisible();
        var enabled = IsEnabled();
        LogAssertion("AssertDisabled", expected: false, actual: enabled);
        
        if (enabled)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' should be disabled but is not.");
        }
    }
    
    public void AssertTextEquals(string expected, string? message = null)
    {
        var actual = GetText();
        LogAssertion("AssertTextEquals", expected: expected, actual: actual);
        
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' text mismatch. Expected: '{expected}', Actual: '{actual}'");
        }
    }
    
    public void AssertTextContains(string substring, string? message = null)
    {
        var actual = GetText();
        var contains = actual.Contains(substring);
        LogAssertion("AssertTextContains", expected: substring, actual: actual);
        
        if (!contains)
        {
            throw new AssertionException(
                message ?? $"Control '{_automationId}' text should contain '{substring}' but was '{actual}'");
        }
    }
    
    #endregion
    
    #region Logging Helpers
    
    protected void LogAction(string action, string? value = null)
    {
        Context.Logger?.LogAction(
            Context.TestName,
            Page?.PageName ?? "",
            _automationId,
            action,
            value ?? "");
    }
    
    protected void LogAssertion(string assertion, object expected, object actual)
    {
        var success = expected?.Equals(actual) ?? actual == null;
        Context.Logger?.LogAssertion(
            Context.TestName,
            Page?.PageName ?? "",
            _automationId,
            assertion,
            expected?.ToString() ?? "",
            actual?.ToString() ?? "",
            success ? LogResult.Pass : LogResult.Fail);
    }
    
    #endregion
}
```

---

## 4.3 StrideContentControlBase

Base for clickable controls:

```csharp
/// <summary>
/// Base class for Stride content controls (clickable elements).
/// </summary>
public abstract class StrideContentControlBase : StrideControlBase, IContentControl
{
    protected StrideContentControlBase(
        StrideTestContext context, 
        IPageObject? page, 
        string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>
    /// Click the control using input simulation.
    /// </summary>
    public virtual void Click()
    {
        CheckClickable();
        
        var bounds = GetBounds();
        Context.ClickElement(_automationId);
        
        LogAction("Click");
    }
    
    /// <summary>
    /// Double-click the control.
    /// </summary>
    public virtual void DoubleClick()
    {
        CheckClickable();
        
        var bounds = GetBounds();
        var center = bounds.Center();
        Context.MoveMouse(center);
        Thread.Sleep(50);
        
        // Use input simulator for double-click
        var simulator = new StrideInputSimulator(new StrideTestOptions());
        simulator.DoubleClick(center);
        
        LogAction("DoubleClick");
    }
    
    /// <summary>
    /// Right-click the control.
    /// </summary>
    public virtual void RightClick()
    {
        CheckClickable();
        
        var bounds = GetBounds();
        var center = bounds.Center();
        
        var simulator = new StrideInputSimulator(new StrideTestOptions());
        simulator.RightClick(center);
        
        LogAction("RightClick");
    }
    
    /// <summary>
    /// Hover over the control without clicking.
    /// </summary>
    public virtual void Hover()
    {
        CheckVisible();
        
        var bounds = GetBounds();
        Context.MoveMouse(bounds.Center());
        
        LogAction("Hover");
    }
}
```

---

## 4.4 StrideButtonControl

```csharp
/// <summary>
/// Control wrapper for Stride Button.
/// </summary>
public class StrideButtonControl : StrideContentControlBase
{
    public StrideButtonControl(
        StrideTestContext context, 
        IPageObject? page, 
        string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>
    /// Get button text content.
    /// </summary>
    public string GetButtonText() => GetText();
    
    /// <summary>
    /// Click and wait for a condition.
    /// </summary>
    public void ClickAndWait(Func<bool> condition, int? timeoutMs = null)
    {
        Click();
        
        if (!Context.WaitFor(condition, timeoutMs, "post-click condition"))
        {
            throw new TimeoutException($"Condition not met after clicking '{AutomationId}'");
        }
    }
    
    /// <summary>
    /// Click if enabled, otherwise do nothing.
    /// </summary>
    public bool TryClick()
    {
        if (IsClickable())
        {
            Click();
            return true;
        }
        return false;
    }
}
```

---

## 4.5 StrideTextControlBase

```csharp
/// <summary>
/// Base class for text-related controls.
/// </summary>
public abstract class StrideTextControlBase : StrideControlBase, ITextControl
{
    protected StrideTextControlBase(
        StrideTestContext context, 
        IPageObject? page, 
        string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>
    /// Whether this control supports text input.
    /// </summary>
    public abstract bool IsEditable { get; }
}

/// <summary>
/// Control wrapper for Stride TextBlock (display only).
/// </summary>
public class StrideTextBlockControl : StrideTextControlBase
{
    public override bool IsEditable => false;
    
    public StrideTextBlockControl(
        StrideTestContext context, 
        IPageObject? page, 
        string automationId)
        : base(context, page, automationId)
    {
    }
}

/// <summary>
/// Control wrapper for Stride EditText (editable text input).
/// </summary>
public class StrideEditTextControl : StrideTextControlBase
{
    public override bool IsEditable => true;
    
    public StrideEditTextControl(
        StrideTestContext context, 
        IPageObject? page, 
        string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>
    /// Enter text into the control.
    /// </summary>
    public void Enter(string text)
    {
        CheckEnabled();
        
        // Focus the control by clicking
        Focus();
        
        // Type the text
        Context.TypeText(text);
        
        LogAction("Enter", text);
    }
    
    /// <summary>
    /// Clear the control content.
    /// </summary>
    public void Clear()
    {
        CheckEnabled();
        
        Focus();
        
        // Select all and delete
        Context.PressKey(VirtualKeyCode.CONTROL);
        Context.TypeText("a");
        Context.PressKey(VirtualKeyCode.DELETE);
        
        LogAction("Clear");
    }
    
    /// <summary>
    /// Clear and enter new text.
    /// </summary>
    public void ClearAndEnter(string text)
    {
        Clear();
        Enter(text);
    }
    
    /// <summary>
    /// Alias for ClearAndEnter.
    /// </summary>
    public void SetText(string text) => ClearAndEnter(text);
    
    /// <summary>
    /// Append text to existing content.
    /// </summary>
    public void Append(string text)
    {
        CheckEnabled();
        
        Focus();
        
        // Move to end
        Context.PressKey(VirtualKeyCode.END);
        
        // Type additional text
        Context.TypeText(text);
        
        LogAction("Append", text);
    }
    
    /// <summary>
    /// Focus the text control.
    /// </summary>
    public void Focus()
    {
        var bounds = GetBounds();
        Context.ClickElement(_automationId);
        
        // Wait for focus
        Context.WaitFor(() => IsFocused(), Context.ShortTimeoutMs, "focus");
    }
    
    /// <summary>
    /// Submit by pressing Enter.
    /// </summary>
    public void Submit()
    {
        Focus();
        Context.PressKey(VirtualKeyCode.RETURN);
        LogAction("Submit");
    }
}
```

---

## 4.6 StrideToggleControlBase

```csharp
/// <summary>
/// Base class for toggle controls (CheckBox, ToggleButton).
/// </summary>
public abstract class StrideToggleControlBase : StrideContentControlBase, IToggleControl
{
    protected StrideToggleControlBase(
        StrideTestContext context, 
        IPageObject? page, 
        string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>
    /// Get current checked state.
    /// </summary>
    public bool IsChecked() => GetState().IsChecked ?? false;
    
    /// <summary>
    /// Toggle the current state.
    /// </summary>
    public void Toggle()
    {
        var before = IsChecked();
        Click();
        LogAction("Toggle", $"{before} -> {!before}");
    }
    
    /// <summary>
    /// Set to checked state.
    /// </summary>
    public void Check()
    {
        if (!IsChecked())
        {
            Click();
        }
        LogAction("Check");
    }
    
    /// <summary>
    /// Set to unchecked state.
    /// </summary>
    public void Uncheck()
    {
        if (IsChecked())
        {
            Click();
        }
        LogAction("Uncheck");
    }
    
    /// <summary>
    /// Set specific checked state.
    /// </summary>
    public void SetChecked(bool value)
    {
        if (value)
            Check();
        else
            Uncheck();
    }
    
    /// <summary>
    /// Wait for checked state.
    /// </summary>
    public bool WaitChecked(bool expected = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsChecked() == expected,
            timeoutMs,
            $"element '{AutomationId}' checked={expected}");
    }
    
    /// <summary>
    /// Assert is checked.
    /// </summary>
    public void AssertChecked(string? message = null)
    {
        var isChecked = IsChecked();
        LogAssertion("AssertChecked", expected: true, actual: isChecked);
        
        if (!isChecked)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' should be checked but is not.");
        }
    }
    
    /// <summary>
    /// Assert is unchecked.
    /// </summary>
    public void AssertUnchecked(string? message = null)
    {
        var isChecked = IsChecked();
        LogAssertion("AssertUnchecked", expected: false, actual: isChecked);
        
        if (isChecked)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' should be unchecked but is checked.");
        }
    }
}

/// <summary>
/// Control wrapper for Stride CheckBox.
/// </summary>
public class StrideCheckBoxControl : StrideToggleControlBase
{
    public StrideCheckBoxControl(
        StrideTestContext context, 
        IPageObject? page, 
        string automationId)
        : base(context, page, automationId)
    {
    }
}
```

---

## 4.7 StrideSliderControl

```csharp
/// <summary>
/// Control wrapper for Stride Slider.
/// </summary>
public class StrideSliderControl : StrideControlBase, IRangeControl
{
    public StrideSliderControl(
        StrideTestContext context, 
        IPageObject? page, 
        string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>
    /// Get current slider value.
    /// </summary>
    public double GetValue() => GetState().Value ?? 0;
    
    /// <summary>
    /// Get minimum value.
    /// </summary>
    public double GetMinimum() => GetState().Minimum ?? 0;
    
    /// <summary>
    /// Get maximum value.
    /// </summary>
    public double GetMaximum() => GetState().Maximum ?? 100;
    
    /// <summary>
    /// Set slider value by clicking at appropriate position.
    /// </summary>
    public void SetValue(double value)
    {
        CheckEnabled();
        
        var min = GetMinimum();
        var max = GetMaximum();
        var range = max - min;
        
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), 
                $"Value {value} is outside range [{min}, {max}]");
        }
        
        // Calculate relative position
        var percentage = (value - min) / range;
        
        // Get slider bounds
        var bounds = GetBounds();
        
        // Calculate click position (assuming horizontal slider)
        var clickX = bounds.X + (int)(bounds.Width * percentage);
        var clickY = bounds.Y + bounds.Height / 2;
        
        // Click to set value
        var simulator = new StrideInputSimulator(new StrideTestOptions());
        simulator.Click(new Point(clickX, clickY));
        
        LogAction("SetValue", value.ToString());
    }
    
    /// <summary>
    /// Wait for specific value.
    /// </summary>
    public bool WaitValue(double expected, double tolerance = 0.01, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => Math.Abs(GetValue() - expected) <= tolerance,
            timeoutMs,
            $"element '{AutomationId}' value={expected}±{tolerance}");
    }
    
    /// <summary>
    /// Assert value within tolerance.
    /// </summary>
    public void AssertValue(double expected, double tolerance = 0.01, string? message = null)
    {
        var actual = GetValue();
        var inRange = Math.Abs(actual - expected) <= tolerance;
        LogAssertion("AssertValue", expected: expected, actual: actual);
        
        if (!inRange)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' value mismatch. Expected: {expected}±{tolerance}, Actual: {actual}");
        }
    }
    
    /// <summary>
    /// Increment value by step.
    /// </summary>
    public void Increment(double step = 1)
    {
        SetValue(GetValue() + step);
    }
    
    /// <summary>
    /// Decrement value by step.
    /// </summary>
    public void Decrement(double step = 1)
    {
        SetValue(GetValue() - step);
    }
}
```

---

## 4.8 StrideListBoxControl

```csharp
/// <summary>
/// Control wrapper for Stride ListBox.
/// </summary>
public class StrideListBoxControl : StrideControlBase, IItemsControl, ISelectorControl
{
    public StrideListBoxControl(
        StrideTestContext context, 
        IPageObject? page, 
        string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>
    /// Get all item texts.
    /// </summary>
    public List<string> GetItems() => GetState().Items ?? new List<string>();
    
    /// <summary>
    /// Get number of items.
    /// </summary>
    public int GetItemCount() => GetItems().Count;
    
    /// <summary>
    /// Get selected item text.
    /// </summary>
    public string GetSelectedText() => GetState().SelectedText ?? string.Empty;
    
    /// <summary>
    /// Get selected item index.
    /// </summary>
    public int GetSelectedIndex() => GetState().SelectedIndex;
    
    /// <summary>
    /// Select item by index.
    /// </summary>
    public void SelectByIndex(int index)
    {
        CheckEnabled();
        
        var items = GetItems();
        if (index < 0 || index >= items.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index), 
                $"Index {index} is outside range [0, {items.Count - 1}]");
        }
        
        // Send command to select by index
        Context.SendCommandAsync(new AutomationCommand
        {
            Type = "Action",
            Target = _automationId,
            Method = "SelectIndex",
            Args = new object[] { index }
        }).Wait();
        
        LogAction("SelectByIndex", index.ToString());
    }
    
    /// <summary>
    /// Select item by text.
    /// </summary>
    public void SelectByText(string text)
    {
        var items = GetItems();
        var index = items.IndexOf(text);
        
        if (index < 0)
        {
            throw new InvalidOperationException(
                $"Item '{text}' not found in list. Available: {string.Join(", ", items)}");
        }
        
        SelectByIndex(index);
        LogAction("SelectByText", text);
    }
    
    /// <summary>
    /// Wait for selected text.
    /// </summary>
    public bool WaitSelectedText(string expected, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => GetSelectedText() == expected,
            timeoutMs,
            $"element '{AutomationId}' selected='{expected}'");
    }
    
    /// <summary>
    /// Assert selected text.
    /// </summary>
    public void AssertSelectedText(string expected, string? message = null)
    {
        var actual = GetSelectedText();
        LogAssertion("AssertSelectedText", expected: expected, actual: actual);
        
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' selection mismatch. Expected: '{expected}', Actual: '{actual}'");
        }
    }
    
    /// <summary>
    /// Assert item count.
    /// </summary>
    public void AssertItemCount(int expected, string? message = null)
    {
        var actual = GetItemCount();
        LogAssertion("AssertItemCount", expected: expected, actual: actual);
        
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' item count mismatch. Expected: {expected}, Actual: {actual}");
        }
    }
    
    /// <summary>
    /// Assert item exists.
    /// </summary>
    public void AssertItemExists(string text, string? message = null)
    {
        var items = GetItems();
        var exists = items.Contains(text);
        LogAssertion("AssertItemExists", expected: text, actual: exists);
        
        if (!exists)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' should contain item '{text}' but does not. " +
                          $"Available: {string.Join(", ", items)}");
        }
    }
}
```

---

## 4.9 Stride Control Type Mapping

| Stride UI Class | Control Wrapper | Interface |
|-----------------|-----------------|-----------|
| `Button` | `StrideButtonControl` | `IContentControl` |
| `TextBlock` | `StrideTextBlockControl` | `ITextControl` |
| `EditText` | `StrideEditTextControl` | `ITextControl` |
| `CheckBox` | `StrideCheckBoxControl` | `IToggleControl` |
| `ToggleButton` | `StrideToggleButtonControl` | `IToggleControl` |
| `Slider` | `StrideSliderControl` | `IRangeControl` |
| `ListBox` | `StrideListBoxControl` | `IItemsControl`, `ISelectorControl` |
| `ScrollViewer` | `StrideScrollViewerControl` | `IControlObject` |
| `Border` | `StrideBorderControl` | `IControlObject` |
| `StackPanel` | `StrideStackPanelControl` | `IControlObject` |
| `Grid` | `StrideGridControl` | `IControlObject` |
| `Canvas` | `StrideCanvasControl` | `IControlObject` |

---

*Document Version: 1.0*  
*Last Updated: January 2025*

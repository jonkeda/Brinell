# SPEC-006-003b: Toggle Controls

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Parent:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

---

## 1. MAUI Toggle Classes

### 1.1 ToggleControlBase

```csharp
public abstract class ToggleControlBase : ControlObjectBase, IToggleControlObject
{
    #region Constructors

    protected ToggleControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ToggleControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #endregion

    #region Is/Wait/Check/Assert Checked (Example: IsChecked)

    public virtual bool IsChecked(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var toggleState = element?.GetAttribute("Toggle.ToggleState");
        return toggleState == "1" || toggleState == "On" || 
               element?.GetAttribute("IsChecked") == "True";
    }

    public virtual bool WaitChecked(bool? expected, int? timeoutMs = null);
    public virtual void CheckChecked(bool? expected, int? timeoutMs = null);
    public virtual void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Toggle Actions (Example: Toggle)

    public virtual void Toggle(int? timeoutMs = null)
    {
        Log("Toggle()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        FindElementRequired(timeoutMs).Click();
    }

    public virtual void SetChecked(bool? value, int? timeoutMs = null)
    {
        if (value is null) return;
        Log($"SetChecked({value})");
        if (IsChecked(timeoutMs) != value.Value)
        {
            Toggle(timeoutMs);
        }
    }

    #endregion
}
```

### 1.2 CheckBoxControl

```csharp
public class CheckBoxControl : ToggleControlBase, ICheckBoxControlObject
{
    public CheckBoxControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public CheckBoxControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }

    #region Tri-State (Example: GetState)

    /// <summary>Gets tri-state: true=checked, false=unchecked, null=indeterminate.</summary>
    public virtual bool? GetState(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        var state = element?.GetAttribute("Toggle.ToggleState");
        return state switch
        {
            "0" or "Off" => false,
            "1" or "On" => true,
            "2" or "Indeterminate" => null,
            _ => element?.GetAttribute("IsChecked") == "True"
        };
    }

    public virtual void AssertState(bool? expected, string? message = null, int? timeoutMs = null);
    public virtual void SetState(bool? value, int? timeoutMs = null);
    public virtual void SetIndeterminate(int? timeoutMs = null);

    #endregion
}
```

### 1.3 SwitchControl

```csharp
public class SwitchControl : ToggleControlBase, ISwitchControlObject
{
    public SwitchControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public SwitchControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }

    #region On/Off Actions

    public virtual void TurnOn(int? timeoutMs = null)
    {
        Log("TurnOn()");
        SetChecked(true, timeoutMs);
    }

    public virtual void TurnOff(int? timeoutMs = null);

    #endregion
}
```

### 1.4 RadioButtonControl

```csharp
public class RadioButtonControl : ControlObjectBase, IRadioButtonControlObject
{
    public RadioButtonControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public RadioButtonControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }

    #region Selection (Example: IsSelected)

    public virtual bool IsSelected(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        return element?.GetAttribute("SelectionItem.IsSelected") == "True" ||
               element?.GetAttribute("IsChecked") == "True";
    }

    public virtual bool WaitSelected(bool? expected, int? timeoutMs = null);
    public virtual void AssertSelected(bool? expected, string? message = null, int? timeoutMs = null);
    
    public virtual void Select(int? timeoutMs = null)
    {
        Log("Select()");
        if (!IsSelected(timeoutMs))
        {
            CheckVisible(true, timeoutMs);
            CheckEnabled(true, timeoutMs);
            FindElementRequired(timeoutMs).Click();
        }
    }

    public virtual string GetGroupName(int? timeoutMs = null);

    #endregion
}
```

---

## 2. Blazor Toggle Classes

### 2.1 AsyncToggleControlBase

```csharp
public abstract class AsyncToggleControlBase : AsyncControlObjectBase, IAsyncToggleControlObject
{
    protected AsyncToggleControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncToggleControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Is/Wait/Assert Checked (Example: IsCheckedAsync)

    public virtual async Task<bool> IsCheckedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().IsCheckedAsync();
    }

    public virtual Task<bool> WaitCheckedAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task AssertCheckedAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Toggle Actions (Example: ToggleAsync)

    public virtual async Task ToggleAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("ToggleAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        await GetLocator().ClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    public virtual Task SetCheckedAsync(bool? value, int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.2 Concrete Blazor Controls

```csharp
public class CheckBoxControl : AsyncToggleControlBase
{
    public CheckBoxControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public CheckBoxControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

public class RadioButtonControl : AsyncToggleControlBase
{
    public RadioButtonControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public RadioButtonControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    public virtual Task<string> GetGroupNameAsync(int? timeoutMs = null, CancellationToken ct = default);
}
```

---

## 3. Inheritance Summary

```
MAUI:
ToggleControlBase : ControlObjectBase, IToggleControlObject
├── CheckBoxControl : ICheckBoxControlObject
└── SwitchControl : ISwitchControlObject

RadioButtonControl : ControlObjectBase, IRadioButtonControlObject

Blazor:
AsyncToggleControlBase : AsyncControlObjectBase, IAsyncToggleControlObject
├── CheckBoxControl
└── RadioButtonControl
```

---

**Next:** [SPEC-006-003b-SELECTION](SPEC-006-003b-SELECTION.md)

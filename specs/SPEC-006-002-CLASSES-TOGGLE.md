# SPEC-006-002d: Toggle Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. ToggleControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for controls with boolean toggle state (on/off).
/// </summary>
public abstract class ToggleControlBase : InteractiveControlBase, IToggleControlObject
{
    protected ToggleControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    #region Toggle State Methods

    // Full implementation for IsChecked
    public virtual bool IsChecked(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return false;
        
        var isChecked = GetCheckedState(element);
        Log($"IsChecked: {isChecked}");
        return isChecked;
    }

    // Full implementation for Toggle with logging
    public virtual void Toggle(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        Click(timeoutMs);
        LogAction("Toggle");
    }

    // Full implementation for SetChecked with logging
    public virtual void SetChecked(bool? value, int? timeoutMs = null)
    {
        if (value == null) return;
        
        EnsureEnabled(timeoutMs);
        var current = IsChecked(timeoutMs);
        if (current != value.Value)
        {
            Toggle(timeoutMs);
        }
        LogAction("SetChecked", value.Value.ToString());
    }

    // Full implementation for Check with logging
    public virtual void Check(int? timeoutMs = null)
    {
        SetChecked(true, timeoutMs);
        LogAction("Check");
    }

    // Full implementation for Uncheck with logging
    public virtual void Uncheck(int? timeoutMs = null)
    {
        SetChecked(false, timeoutMs);
        LogAction("Uncheck");
    }

    // Abstract helper
    protected abstract bool GetCheckedState(object element);

    #endregion

    #region Wait Methods

    // Full implementation for WaitChecked
    public virtual bool WaitChecked(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return IsChecked();
        
        Log($"WaitChecked(expected={expected})");
        var timeout = GetTimeout(timeoutMs);
        return WaitUntil(() => IsChecked() == expected.Value, timeout);
    }

    #endregion

    #region Assert Methods

    // Full implementation for AssertChecked
    public virtual void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        
        var success = WaitChecked(expected, timeoutMs);
        if (!success)
        {
            var actual = IsChecked();
            ThrowAssertionFailed("Checked", actual.ToString(), expected.Value.ToString(),
                message ?? $"Expected element '{_locator}' checked={expected.Value} but was {actual}.");
        }
        LogAssertPass("Checked", expected.Value.ToString(), expected.Value.ToString());
    }

    #endregion
}
```

---

## 2. CheckBoxControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for checkbox controls.
/// </summary>
public abstract class CheckBoxControlBase : ToggleControlBase, ICheckBoxControlObject
{
    protected CheckBoxControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsIndeterminate
    public virtual bool IsIndeterminate(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return false;
        
        var indeterminate = GetIndeterminateState(element);
        Log($"IsIndeterminate: {indeterminate}");
        return indeterminate;
    }

    // Abstract helper
    protected abstract bool GetIndeterminateState(object element);

    // Method signatures only
    public abstract bool WaitIndeterminate(bool? expected, int? timeoutMs = null);
    public abstract void AssertIndeterminate(bool? expected, string? message = null, int? timeoutMs = null);
    public abstract string? GetLabel(int? timeoutMs = null);
    public abstract bool WaitLabel(string? expected, int? timeoutMs = null);
    public abstract void AssertLabel(string? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 3. SwitchControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for switch/toggle controls.
/// </summary>
public abstract class SwitchControlBase : ToggleControlBase, ISwitchControlObject
{
    protected SwitchControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for TurnOn with logging
    public virtual void TurnOn(int? timeoutMs = null)
    {
        SetChecked(true, timeoutMs);
        LogAction("TurnOn");
    }

    // Full implementation for TurnOff with logging
    public virtual void TurnOff(int? timeoutMs = null)
    {
        SetChecked(false, timeoutMs);
        LogAction("TurnOff");
    }

    // Aliases for switch terminology
    public virtual bool IsOn(int? timeoutMs = null) => IsChecked(timeoutMs);
    public virtual bool IsOff(int? timeoutMs = null) => !IsChecked(timeoutMs);

    // Method signatures only
    public abstract bool WaitOn(bool? expected, int? timeoutMs = null);
    public abstract void AssertOn(bool? expected, string? message = null, int? timeoutMs = null);
    public abstract string? GetOnText(int? timeoutMs = null);
    public abstract string? GetOffText(int? timeoutMs = null);
}
```

---

## 4. RadioButtonControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for radio button controls.
/// </summary>
public abstract class RadioButtonControlBase : ToggleControlBase, IRadioButtonControlObject
{
    protected RadioButtonControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for Select with logging
    public virtual void Select(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        if (!IsChecked(timeoutMs))
        {
            Click(timeoutMs);
        }
        LogAction("Select");
    }

    // Note: Radio buttons typically cannot be unchecked directly
    public override void Uncheck(int? timeoutMs = null)
    {
        Log("Uncheck: Radio buttons cannot be unchecked directly. Select another option instead.");
        // No-op for radio buttons - they can only be deselected by selecting another
    }

    // Method signatures only
    public abstract string? GetLabel(int? timeoutMs = null);
    public abstract string? GetGroupName(int? timeoutMs = null);
    public abstract bool WaitSelected(bool? expected, int? timeoutMs = null);
    public abstract void AssertSelected(bool? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertLabel(string? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 5. MAUI Implementation

```csharp
namespace Brinell.Maui;

/// <summary>
/// MAUI CheckBox control implementation.
/// </summary>
public class MauiCheckBox : MauiInteractiveControlBase, ICheckBoxControlObject
{
    public MauiCheckBox(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsChecked
    public bool IsChecked(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        if (element == null) return false;
        
        var isChecked = element.GetAttribute("checked") == "true";
        Log($"IsChecked: {isChecked}");
        return isChecked;
    }

    // Full implementation for Toggle
    public void Toggle(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        Click(timeoutMs);
        LogAction("Toggle");
    }

    // Full implementation for SetChecked
    public void SetChecked(bool? value, int? timeoutMs = null)
    {
        if (value == null) return;
        
        EnsureEnabled(timeoutMs);
        var current = IsChecked(timeoutMs);
        if (current != value.Value)
        {
            Toggle(timeoutMs);
        }
        LogAction("SetChecked", value.Value.ToString());
    }

    // Method signatures only
    public void Check(int? timeoutMs = null);
    public void Uncheck(int? timeoutMs = null);
    public bool WaitChecked(bool? expected, int? timeoutMs = null);
    public void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);
    public bool IsIndeterminate(int? timeoutMs = null);
    public bool WaitIndeterminate(bool? expected, int? timeoutMs = null);
    public void AssertIndeterminate(bool? expected, string? message = null, int? timeoutMs = null);
    public string? GetLabel(int? timeoutMs = null);
    public bool WaitLabel(string? expected, int? timeoutMs = null);
    public void AssertLabel(string? expected, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// MAUI Switch control implementation.
/// </summary>
public class MauiSwitch : MauiInteractiveControlBase, ISwitchControlObject
{
    public MauiSwitch(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsChecked (IsOn)
    public bool IsChecked(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        if (element == null) return false;
        
        var isChecked = element.GetAttribute("checked") == "true";
        Log($"IsChecked: {isChecked}");
        return isChecked;
    }

    // Full implementation for Toggle
    public void Toggle(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        Click(timeoutMs);
        LogAction("Toggle");
    }

    // Method signatures only
    public void SetChecked(bool? value, int? timeoutMs = null);
    public void Check(int? timeoutMs = null);
    public void Uncheck(int? timeoutMs = null);
    public bool WaitChecked(bool? expected, int? timeoutMs = null);
    public void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);
    public void TurnOn(int? timeoutMs = null);
    public void TurnOff(int? timeoutMs = null);
    public bool IsOn(int? timeoutMs = null);
    public bool IsOff(int? timeoutMs = null);
    public bool WaitOn(bool? expected, int? timeoutMs = null);
    public void AssertOn(bool? expected, string? message = null, int? timeoutMs = null);
    public string? GetOnText(int? timeoutMs = null);
    public string? GetOffText(int? timeoutMs = null);
}

/// <summary>
/// MAUI RadioButton control implementation.
/// </summary>
public class MauiRadioButton : MauiInteractiveControlBase, IRadioButtonControlObject
{
    public MauiRadioButton(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsChecked
    public bool IsChecked(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        if (element == null) return false;
        
        var isChecked = element.GetAttribute("checked") == "true";
        Log($"IsChecked: {isChecked}");
        return isChecked;
    }

    // Full implementation for Select
    public void Select(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        if (!IsChecked(timeoutMs))
        {
            Click(timeoutMs);
        }
        LogAction("Select");
    }

    // Method signatures only
    public void Toggle(int? timeoutMs = null);
    public void SetChecked(bool? value, int? timeoutMs = null);
    public void Check(int? timeoutMs = null);
    public void Uncheck(int? timeoutMs = null);
    public bool WaitChecked(bool? expected, int? timeoutMs = null);
    public void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);
    public string? GetLabel(int? timeoutMs = null);
    public string? GetGroupName(int? timeoutMs = null);
    public bool WaitSelected(bool? expected, int? timeoutMs = null);
    public void AssertSelected(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertLabel(string? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 6. Blazor Implementation

```csharp
namespace Brinell.Blazor;

/// <summary>
/// Blazor checkbox control implementation.
/// </summary>
public class BlazorCheckbox : BlazorInteractiveControlBase, ICheckBoxControlObject
{
    public BlazorCheckbox(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsChecked
    public bool IsChecked(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var isChecked = locator.IsCheckedAsync().GetAwaiter().GetResult();
        Log($"IsChecked: {isChecked}");
        return isChecked;
    }

    // Full implementation for Toggle
    public void Toggle(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        Click(timeoutMs);
        LogAction("Toggle");
    }

    // Full implementation for SetChecked
    public void SetChecked(bool? value, int? timeoutMs = null)
    {
        if (value == null) return;
        
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.SetCheckedAsync(value.Value).GetAwaiter().GetResult();
        LogAction("SetChecked", value.Value.ToString());
    }

    // Full implementation for Check
    public void Check(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.CheckAsync().GetAwaiter().GetResult();
        LogAction("Check");
    }

    // Full implementation for Uncheck
    public void Uncheck(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.UncheckAsync().GetAwaiter().GetResult();
        LogAction("Uncheck");
    }

    // Method signatures only
    public bool WaitChecked(bool? expected, int? timeoutMs = null);
    public void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);
    public bool IsIndeterminate(int? timeoutMs = null);
    public bool WaitIndeterminate(bool? expected, int? timeoutMs = null);
    public void AssertIndeterminate(bool? expected, string? message = null, int? timeoutMs = null);
    public string? GetLabel(int? timeoutMs = null);
    public bool WaitLabel(string? expected, int? timeoutMs = null);
    public void AssertLabel(string? expected, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// Blazor switch/toggle control implementation.
/// </summary>
public class BlazorSwitch : BlazorCheckbox, ISwitchControlObject
{
    public BlazorSwitch(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public void TurnOn(int? timeoutMs = null);
    public void TurnOff(int? timeoutMs = null);
    public bool IsOn(int? timeoutMs = null);
    public bool IsOff(int? timeoutMs = null);
    public bool WaitOn(bool? expected, int? timeoutMs = null);
    public void AssertOn(bool? expected, string? message = null, int? timeoutMs = null);
    public string? GetOnText(int? timeoutMs = null);
    public string? GetOffText(int? timeoutMs = null);
}

/// <summary>
/// Blazor radio button control implementation.
/// </summary>
public class BlazorRadioButton : BlazorInteractiveControlBase, IRadioButtonControlObject
{
    public BlazorRadioButton(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsChecked
    public bool IsChecked(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var isChecked = locator.IsCheckedAsync().GetAwaiter().GetResult();
        Log($"IsChecked: {isChecked}");
        return isChecked;
    }

    // Full implementation for Select
    public void Select(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.CheckAsync().GetAwaiter().GetResult();
        LogAction("Select");
    }

    // Method signatures only
    public void Toggle(int? timeoutMs = null);
    public void SetChecked(bool? value, int? timeoutMs = null);
    public void Check(int? timeoutMs = null);
    public void Uncheck(int? timeoutMs = null);
    public bool WaitChecked(bool? expected, int? timeoutMs = null);
    public void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);
    public string? GetLabel(int? timeoutMs = null);
    public string? GetGroupName(int? timeoutMs = null);
    public bool WaitSelected(bool? expected, int? timeoutMs = null);
    public void AssertSelected(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertLabel(string? expected, string? message = null, int? timeoutMs = null);
}
```

---

**Next:** [SPEC-006-002e: Selection Classes](SPEC-006-002-CLASSES-SELECTION.md)

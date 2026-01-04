# SPEC-006-003b: Range Controls

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Parent:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

---

## 1. MAUI Range Classes

### 1.1 RangeControlBase

```csharp
public abstract class RangeControlBase : ControlObjectBase, IRangeControlObject
{
    protected RangeControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected RangeControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Get Value (Example: GetValue)

    public virtual double GetValue(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var value = element.GetAttribute("RangeValue.Value") ?? 
                    element.GetAttribute("Value");
        return double.TryParse(value, out var result) ? result : 0;
    }

    public virtual bool WaitValue(double? expected, double? tolerance = null, int? timeoutMs = null);
    public virtual void AssertValue(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);
    public virtual void AssertValueInRange(double? min, double? max, string? message = null, int? timeoutMs = null);

    #endregion

    #region Set Value (Example: SetValue)

    public virtual void SetValue(double? value, int? timeoutMs = null)
    {
        if (value is null) return;
        Log($"SetValue({value})");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        
        var element = FindElementRequired(timeoutMs);
        // Use RangeValue pattern if available
        var min = GetMinimum(timeoutMs);
        var max = GetMaximum(timeoutMs);
        var clampedValue = Math.Clamp(value.Value, min, max);
        
        // Platform-specific value setting via accessibility patterns
        element.SetAttribute("RangeValue.Value", clampedValue.ToString());
    }

    public virtual void Increment(int? timeoutMs = null);
    public virtual void Decrement(int? timeoutMs = null);
    public virtual void IncrementBy(double? amount, int? timeoutMs = null);
    public virtual void DecrementBy(double? amount, int? timeoutMs = null);

    #endregion

    #region Range Properties (Example: GetMinimum)

    public virtual double GetMinimum(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var value = element.GetAttribute("RangeValue.Minimum") ?? 
                    element.GetAttribute("Minimum");
        return double.TryParse(value, out var result) ? result : 0;
    }

    public virtual double GetMaximum(int? timeoutMs = null);
    public virtual double GetStep(int? timeoutMs = null);

    #endregion
}
```

### 1.2 SliderControl

```csharp
public class SliderControl : RangeControlBase, ISliderControlObject
{
    public SliderControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public SliderControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }

    #region Drag Actions (Example: DragToValue)

    public virtual void DragToValue(double? value, int? timeoutMs = null)
    {
        if (value is null) return;
        Log($"DragToValue({value})");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        
        var element = FindElementRequired(timeoutMs);
        var min = GetMinimum(timeoutMs);
        var max = GetMaximum(timeoutMs);
        var percent = (value.Value - min) / (max - min);
        
        var size = element.Size;
        var targetX = (int)(size.Width * percent);
        
        new Actions(Driver)
            .MoveToElement(element, 0, size.Height / 2)
            .ClickAndHold()
            .MoveByOffset(targetX, 0)
            .Release()
            .Perform();
    }

    public virtual void DragByOffset(int? pixelOffset, int? timeoutMs = null);
    public virtual double GetThumbPosition(int? timeoutMs = null);

    #endregion
}
```

### 1.3 StepperControl

```csharp
public class StepperControl : RangeControlBase, IStepperControlObject
{
    public StepperControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public StepperControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }

    #region Step Actions (Example: StepUp)

    public virtual void StepUp(int? timeoutMs = null)
    {
        Log("StepUp()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        // Find and click the increment button
        var incrementBtn = FindElementRequired(timeoutMs)
            .FindElement(MobileBy.AccessibilityId("IncrementButton"));
        incrementBtn.Click();
    }

    public virtual void StepDown(int? timeoutMs = null);
    public virtual void StepUpMultiple(int? count, int? timeoutMs = null);
    public virtual void StepDownMultiple(int? count, int? timeoutMs = null);

    #endregion
}
```

---

## 2. Blazor Range Classes

### 2.1 AsyncRangeControlBase

```csharp
public abstract class AsyncRangeControlBase : AsyncControlObjectBase, IAsyncRangeControlObject
{
    protected AsyncRangeControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncRangeControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Get Value (Example: GetValueAsync)

    public virtual async Task<double> GetValueAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var value = await GetLocator().InputValueAsync();
        return double.TryParse(value, out var result) ? result : 0;
    }

    public virtual Task<bool> WaitValueAsync(double? expected, double? tolerance = null, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task AssertValueAsync(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Set Value (Example: SetValueAsync)

    public virtual async Task SetValueAsync(double? value, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (value is null) return;
        Log($"SetValueAsync({value})");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        await GetLocator().FillAsync(value.Value.ToString());
    }

    public virtual Task IncrementAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task DecrementAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Range Properties

    public virtual Task<double> GetMinimumAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<double> GetMaximumAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<double> GetStepAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.2 Concrete Blazor Control

```csharp
/// <summary>HTML input type="range" element.</summary>
public class RangeInputControl : AsyncRangeControlBase
{
    public RangeInputControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public RangeInputControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}
```

---

## 3. Inheritance Summary

```
MAUI:
RangeControlBase : ControlObjectBase, IRangeControlObject
├── SliderControl : ISliderControlObject
└── StepperControl : IStepperControlObject

Blazor:
AsyncRangeControlBase : AsyncControlObjectBase, IAsyncRangeControlObject
└── RangeInputControl
```

---

**Next:** [SPEC-006-003b-DATETIME](SPEC-006-003b-DATETIME.md)

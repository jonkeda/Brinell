# SPEC-006-002f: Range Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. RangeControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for controls with numeric range values (sliders, progress bars, steppers).
/// </summary>
public abstract class RangeControlBase : InteractiveControlBase, IRangeControlObject
{
    protected RangeControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    #region Value Methods

    // Full implementation for GetValue
    public virtual double GetValue(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return 0;
        
        var value = GetValueCore(element);
        Log($"GetValue: {value}");
        return value;
    }

    // Full implementation for SetValue with logging
    public virtual void SetValue(double? value, int? timeoutMs = null)
    {
        if (value == null) return;
        
        EnsureEnabled(timeoutMs);
        SetValueCore(FindElement(timeoutMs), value.Value);
        LogAction("SetValue", value.Value.ToString());
    }

    // Full implementation for GetMinimum
    public virtual double GetMinimum(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return 0;
        
        var min = GetMinimumCore(element);
        Log($"GetMinimum: {min}");
        return min;
    }

    // Full implementation for GetMaximum
    public virtual double GetMaximum(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return 100;
        
        var max = GetMaximumCore(element);
        Log($"GetMaximum: {max}");
        return max;
    }

    // Abstract helpers
    protected abstract double GetValueCore(object element);
    protected abstract void SetValueCore(object element, double value);
    protected abstract double GetMinimumCore(object element);
    protected abstract double GetMaximumCore(object element);

    #endregion

    #region Wait Methods

    // Full implementation for WaitValue
    public virtual bool WaitValue(double? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        Log($"WaitValue(expected={expected})");
        var timeout = GetTimeout(timeoutMs);
        return WaitUntil(() => Math.Abs(GetValue() - expected.Value) < 0.001, timeout);
    }

    // Full implementation for WaitValueGreaterThan
    public virtual bool WaitValueGreaterThan(double? threshold, int? timeoutMs = null)
    {
        if (threshold == null) return true;
        
        Log($"WaitValueGreaterThan(threshold={threshold})");
        var timeout = GetTimeout(timeoutMs);
        return WaitUntil(() => GetValue() > threshold.Value, timeout);
    }

    // Method signatures only
    public abstract bool WaitValueLessThan(double? threshold, int? timeoutMs = null);
    public abstract bool WaitValueInRange(double? min, double? max, int? timeoutMs = null);

    #endregion

    #region Assert Methods

    // Full implementation for AssertValue
    public virtual void AssertValue(double? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        
        var success = WaitValue(expected, timeoutMs);
        if (!success)
        {
            var actual = GetValue();
            ThrowAssertionFailed("Value", actual.ToString(), expected.Value.ToString(),
                message ?? $"Expected element '{_locator}' value={expected.Value} but was {actual}.");
        }
        LogAssertPass("Value", expected.Value.ToString(), expected.Value.ToString());
    }

    // Full implementation for AssertValueGreaterThan
    public virtual void AssertValueGreaterThan(double? threshold, string? message = null, int? timeoutMs = null)
    {
        if (threshold == null) return;
        
        var success = WaitValueGreaterThan(threshold, timeoutMs);
        if (!success)
        {
            var actual = GetValue();
            ThrowAssertionFailed("Value", actual.ToString(), $">{threshold.Value}",
                message ?? $"Expected element '{_locator}' value>{threshold.Value} but was {actual}.");
        }
        LogAssertPass("Value", $">{threshold.Value}", $">{threshold.Value}");
    }

    // Method signatures only
    public abstract void AssertValueLessThan(double? threshold, string? message = null, int? timeoutMs = null);
    public abstract void AssertValueInRange(double? min, double? max, string? message = null, int? timeoutMs = null);

    #endregion
}
```

---

## 2. SliderControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for slider controls with drag interaction.
/// </summary>
public abstract class SliderControlBase : RangeControlBase, ISliderControlObject
{
    protected SliderControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for SlideToValue with logging
    public virtual void SlideToValue(double? value, int? timeoutMs = null)
    {
        if (value == null) return;
        
        EnsureEnabled(timeoutMs);
        SlideToValueCore(FindElement(timeoutMs), value.Value);
        LogAction("SlideToValue", value.Value.ToString());
    }

    // Full implementation for SlideByPercent with logging
    public virtual void SlideByPercent(double? percent, int? timeoutMs = null)
    {
        if (percent == null) return;
        
        EnsureEnabled(timeoutMs);
        var min = GetMinimum(timeoutMs);
        var max = GetMaximum(timeoutMs);
        var range = max - min;
        var targetValue = min + (range * percent.Value / 100.0);
        SlideToValue(targetValue, timeoutMs);
        LogAction("SlideByPercent", percent.Value.ToString());
    }

    // Full implementation for Increment with logging
    public virtual void Increment(int? steps = 1, int? timeoutMs = null)
    {
        if (steps == null) return;
        
        EnsureEnabled(timeoutMs);
        for (int i = 0; i < steps.Value; i++)
        {
            IncrementCore(FindElement(timeoutMs));
        }
        LogAction("Increment", steps.Value.ToString());
    }

    // Full implementation for Decrement with logging
    public virtual void Decrement(int? steps = 1, int? timeoutMs = null)
    {
        if (steps == null) return;
        
        EnsureEnabled(timeoutMs);
        for (int i = 0; i < steps.Value; i++)
        {
            DecrementCore(FindElement(timeoutMs));
        }
        LogAction("Decrement", steps.Value.ToString());
    }

    // Abstract helpers
    protected abstract void SlideToValueCore(object element, double value);
    protected abstract void IncrementCore(object element);
    protected abstract void DecrementCore(object element);

    // Method signatures only
    public abstract void SlideToMin(int? timeoutMs = null);
    public abstract void SlideToMax(int? timeoutMs = null);
    public abstract double GetStep(int? timeoutMs = null);
}
```

---

## 3. StepperControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for stepper/spinner controls with increment/decrement buttons.
/// </summary>
public abstract class StepperControlBase : RangeControlBase, IStepperControlObject
{
    protected StepperControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for Increment with logging
    public virtual void Increment(int? steps = 1, int? timeoutMs = null)
    {
        if (steps == null) return;
        
        EnsureEnabled(timeoutMs);
        for (int i = 0; i < steps.Value; i++)
        {
            ClickIncrementButton(timeoutMs);
        }
        LogAction("Increment", steps.Value.ToString());
    }

    // Full implementation for Decrement with logging
    public virtual void Decrement(int? steps = 1, int? timeoutMs = null)
    {
        if (steps == null) return;
        
        EnsureEnabled(timeoutMs);
        for (int i = 0; i < steps.Value; i++)
        {
            ClickDecrementButton(timeoutMs);
        }
        LogAction("Decrement", steps.Value.ToString());
    }

    // Full implementation for GetStep
    public virtual double GetStep(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return 1;
        
        var step = GetStepCore(element);
        Log($"GetStep: {step}");
        return step;
    }

    // Abstract helpers
    protected abstract void ClickIncrementButton(int? timeoutMs);
    protected abstract void ClickDecrementButton(int? timeoutMs);
    protected abstract double GetStepCore(object element);
}
```

---

## 4. ProgressControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for progress bar/indicator controls (read-only range display).
/// </summary>
public abstract class ProgressControlBase : ControlBase, IProgressControlObject
{
    protected ProgressControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetProgress
    public virtual double GetProgress(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return 0;
        
        var progress = GetProgressCore(element);
        Log($"GetProgress: {progress}");
        return progress;
    }

    // Full implementation for GetProgressPercent
    public virtual double GetProgressPercent(int? timeoutMs = null)
    {
        var progress = GetProgress(timeoutMs);
        var min = GetMinimum(timeoutMs);
        var max = GetMaximum(timeoutMs);
        var range = max - min;
        
        if (range <= 0) return 0;
        var percent = ((progress - min) / range) * 100.0;
        Log($"GetProgressPercent: {percent}%");
        return percent;
    }

    // Full implementation for IsIndeterminate
    public virtual bool IsIndeterminate(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return false;
        
        var indeterminate = GetIndeterminateState(element);
        Log($"IsIndeterminate: {indeterminate}");
        return indeterminate;
    }

    // Full implementation for GetMinimum
    public virtual double GetMinimum(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return 0;
        
        var min = GetMinimumCore(element);
        Log($"GetMinimum: {min}");
        return min;
    }

    // Full implementation for GetMaximum
    public virtual double GetMaximum(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return 100;
        
        var max = GetMaximumCore(element);
        Log($"GetMaximum: {max}");
        return max;
    }

    // Abstract helpers
    protected abstract double GetProgressCore(object element);
    protected abstract bool GetIndeterminateState(object element);
    protected abstract double GetMinimumCore(object element);
    protected abstract double GetMaximumCore(object element);

    // Method signatures only
    public abstract bool WaitProgress(double? expected, int? timeoutMs = null);
    public abstract bool WaitProgressGreaterThan(double? threshold, int? timeoutMs = null);
    public abstract bool WaitProgressComplete(int? timeoutMs = null);
    public abstract void AssertProgress(double? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertProgressGreaterThan(double? threshold, string? message = null, int? timeoutMs = null);
    public abstract void AssertProgressComplete(string? message = null, int? timeoutMs = null);
    public abstract void AssertIndeterminate(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 5. MAUI Implementation

```csharp
namespace Brinell.Maui;

/// <summary>
/// MAUI Slider control implementation.
/// </summary>
public class MauiSlider : MauiInteractiveControlBase, ISliderControlObject
{
    public MauiSlider(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetValue
    public double GetValue(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        if (element == null) return 0;
        
        // Android uses "text" for current value, iOS uses "value"
        var valueStr = element.GetAttribute("value") ?? element.Text;
        var value = double.TryParse(valueStr, out var v) ? v : 0;
        Log($"GetValue: {value}");
        return value;
    }

    // Full implementation for SetValue
    public void SetValue(double? value, int? timeoutMs = null)
    {
        if (value == null) return;
        
        EnsureEnabled(timeoutMs);
        SlideToValue(value.Value, timeoutMs);
        LogAction("SetValue", value.Value.ToString());
    }

    // Full implementation for SlideToValue using touch actions
    public void SlideToValue(double? value, int? timeoutMs = null)
    {
        if (value == null) return;
        
        EnsureEnabled(timeoutMs);
        var element = FindElement(timeoutMs) as AppiumElement;
        if (element == null) return;
        
        var min = GetMinimum(timeoutMs);
        var max = GetMaximum(timeoutMs);
        var range = max - min;
        var percent = (value.Value - min) / range;
        
        // Calculate target position on slider
        var size = element.Size;
        var location = element.Location;
        var targetX = location.X + (int)(size.Width * percent);
        var targetY = location.Y + (size.Height / 2);
        
        // Use touch action to slide
        _context.TouchAction()
            .Tap(targetX, targetY)
            .Perform();
        
        LogAction("SlideToValue", value.Value.ToString());
    }

    // Method signatures only
    public double GetMinimum(int? timeoutMs = null);
    public double GetMaximum(int? timeoutMs = null);
    public void SlideByPercent(double? percent, int? timeoutMs = null);
    public void Increment(int? steps = 1, int? timeoutMs = null);
    public void Decrement(int? steps = 1, int? timeoutMs = null);
    public void SlideToMin(int? timeoutMs = null);
    public void SlideToMax(int? timeoutMs = null);
    public double GetStep(int? timeoutMs = null);
    public bool WaitValue(double? expected, int? timeoutMs = null);
    public bool WaitValueGreaterThan(double? threshold, int? timeoutMs = null);
    public bool WaitValueLessThan(double? threshold, int? timeoutMs = null);
    public bool WaitValueInRange(double? min, double? max, int? timeoutMs = null);
    public void AssertValue(double? expected, string? message = null, int? timeoutMs = null);
    public void AssertValueGreaterThan(double? threshold, string? message = null, int? timeoutMs = null);
    public void AssertValueLessThan(double? threshold, string? message = null, int? timeoutMs = null);
    public void AssertValueInRange(double? min, double? max, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// MAUI Stepper control implementation.
/// </summary>
public class MauiStepper : MauiInteractiveControlBase, IStepperControlObject
{
    public MauiStepper(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetValue
    public double GetValue(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        if (element == null) return 0;
        
        var valueStr = element.GetAttribute("value") ?? element.Text;
        var value = double.TryParse(valueStr, out var v) ? v : 0;
        Log($"GetValue: {value}");
        return value;
    }

    // Full implementation for Increment
    public void Increment(int? steps = 1, int? timeoutMs = null)
    {
        if (steps == null) return;
        
        EnsureEnabled(timeoutMs);
        var element = FindElement(timeoutMs) as AppiumElement;
        
        // Find increment button (typically "+" or right side)
        var incrementBtn = element?.FindElement(OpenQA.Selenium.By.XPath(".//*[contains(@content-desc, 'increment') or contains(@text, '+')]"));
        
        for (int i = 0; i < steps.Value; i++)
        {
            incrementBtn?.Click();
        }
        LogAction("Increment", steps.Value.ToString());
    }

    // Full implementation for Decrement
    public void Decrement(int? steps = 1, int? timeoutMs = null)
    {
        if (steps == null) return;
        
        EnsureEnabled(timeoutMs);
        var element = FindElement(timeoutMs) as AppiumElement;
        
        // Find decrement button (typically "-" or left side)
        var decrementBtn = element?.FindElement(OpenQA.Selenium.By.XPath(".//*[contains(@content-desc, 'decrement') or contains(@text, '-')]"));
        
        for (int i = 0; i < steps.Value; i++)
        {
            decrementBtn?.Click();
        }
        LogAction("Decrement", steps.Value.ToString());
    }

    // Method signatures only
    public void SetValue(double? value, int? timeoutMs = null);
    public double GetMinimum(int? timeoutMs = null);
    public double GetMaximum(int? timeoutMs = null);
    public double GetStep(int? timeoutMs = null);
    public bool WaitValue(double? expected, int? timeoutMs = null);
    public bool WaitValueGreaterThan(double? threshold, int? timeoutMs = null);
    public bool WaitValueLessThan(double? threshold, int? timeoutMs = null);
    public bool WaitValueInRange(double? min, double? max, int? timeoutMs = null);
    public void AssertValue(double? expected, string? message = null, int? timeoutMs = null);
    public void AssertValueGreaterThan(double? threshold, string? message = null, int? timeoutMs = null);
    public void AssertValueLessThan(double? threshold, string? message = null, int? timeoutMs = null);
    public void AssertValueInRange(double? min, double? max, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// MAUI ProgressBar control implementation.
/// </summary>
public class MauiProgressBar : MauiControlBase, IProgressControlObject
{
    public MauiProgressBar(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetProgress
    public double GetProgress(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        if (element == null) return 0;
        
        var valueStr = element.GetAttribute("value") ?? element.Text;
        var progress = double.TryParse(valueStr, out var v) ? v : 0;
        Log($"GetProgress: {progress}");
        return progress;
    }

    // Full implementation for GetProgressPercent
    public double GetProgressPercent(int? timeoutMs = null)
    {
        var progress = GetProgress(timeoutMs);
        // MAUI ProgressBar uses 0-1 range by default
        var percent = progress * 100.0;
        Log($"GetProgressPercent: {percent}%");
        return percent;
    }

    // Method signatures only
    public bool IsIndeterminate(int? timeoutMs = null);
    public double GetMinimum(int? timeoutMs = null);
    public double GetMaximum(int? timeoutMs = null);
    public bool WaitProgress(double? expected, int? timeoutMs = null);
    public bool WaitProgressGreaterThan(double? threshold, int? timeoutMs = null);
    public bool WaitProgressComplete(int? timeoutMs = null);
    public void AssertProgress(double? expected, string? message = null, int? timeoutMs = null);
    public void AssertProgressGreaterThan(double? threshold, string? message = null, int? timeoutMs = null);
    public void AssertProgressComplete(string? message = null, int? timeoutMs = null);
    public void AssertIndeterminate(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 6. Blazor Implementation

```csharp
namespace Brinell.Blazor;

/// <summary>
/// Blazor range input (slider) implementation.
/// </summary>
public class BlazorRange : BlazorInteractiveControlBase, ISliderControlObject
{
    public BlazorRange(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetValue
    public double GetValue(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var valueStr = locator.InputValueAsync().GetAwaiter().GetResult();
        var value = double.TryParse(valueStr, out var v) ? v : 0;
        Log($"GetValue: {value}");
        return value;
    }

    // Full implementation for SetValue using Playwright fill
    public void SetValue(double? value, int? timeoutMs = null)
    {
        if (value == null) return;
        
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.FillAsync(value.Value.ToString()).GetAwaiter().GetResult();
        LogAction("SetValue", value.Value.ToString());
    }

    // Full implementation for GetMinimum
    public double GetMinimum(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var minStr = locator.GetAttributeAsync("min").GetAwaiter().GetResult();
        var min = double.TryParse(minStr, out var v) ? v : 0;
        Log($"GetMinimum: {min}");
        return min;
    }

    // Full implementation for GetMaximum
    public double GetMaximum(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var maxStr = locator.GetAttributeAsync("max").GetAwaiter().GetResult();
        var max = double.TryParse(maxStr, out var v) ? v : 100;
        Log($"GetMaximum: {max}");
        return max;
    }

    // Full implementation for GetStep
    public double GetStep(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var stepStr = locator.GetAttributeAsync("step").GetAwaiter().GetResult();
        var step = double.TryParse(stepStr, out var v) ? v : 1;
        Log($"GetStep: {step}");
        return step;
    }

    // Method signatures only
    public void SlideToValue(double? value, int? timeoutMs = null);
    public void SlideByPercent(double? percent, int? timeoutMs = null);
    public void Increment(int? steps = 1, int? timeoutMs = null);
    public void Decrement(int? steps = 1, int? timeoutMs = null);
    public void SlideToMin(int? timeoutMs = null);
    public void SlideToMax(int? timeoutMs = null);
    public bool WaitValue(double? expected, int? timeoutMs = null);
    public bool WaitValueGreaterThan(double? threshold, int? timeoutMs = null);
    public bool WaitValueLessThan(double? threshold, int? timeoutMs = null);
    public bool WaitValueInRange(double? min, double? max, int? timeoutMs = null);
    public void AssertValue(double? expected, string? message = null, int? timeoutMs = null);
    public void AssertValueGreaterThan(double? threshold, string? message = null, int? timeoutMs = null);
    public void AssertValueLessThan(double? threshold, string? message = null, int? timeoutMs = null);
    public void AssertValueInRange(double? min, double? max, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// Blazor number input with stepper behavior.
/// </summary>
public class BlazorNumberInput : BlazorInteractiveControlBase, IStepperControlObject
{
    public BlazorNumberInput(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetValue
    public double GetValue(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var valueStr = locator.InputValueAsync().GetAwaiter().GetResult();
        var value = double.TryParse(valueStr, out var v) ? v : 0;
        Log($"GetValue: {value}");
        return value;
    }

    // Full implementation for SetValue
    public void SetValue(double? value, int? timeoutMs = null)
    {
        if (value == null) return;
        
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.FillAsync(value.Value.ToString()).GetAwaiter().GetResult();
        LogAction("SetValue", value.Value.ToString());
    }

    // Full implementation for Increment using keyboard
    public void Increment(int? steps = 1, int? timeoutMs = null)
    {
        if (steps == null) return;
        
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.FocusAsync().GetAwaiter().GetResult();
        
        for (int i = 0; i < steps.Value; i++)
        {
            locator.PressAsync("ArrowUp").GetAwaiter().GetResult();
        }
        LogAction("Increment", steps.Value.ToString());
    }

    // Full implementation for Decrement using keyboard
    public void Decrement(int? steps = 1, int? timeoutMs = null)
    {
        if (steps == null) return;
        
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.FocusAsync().GetAwaiter().GetResult();
        
        for (int i = 0; i < steps.Value; i++)
        {
            locator.PressAsync("ArrowDown").GetAwaiter().GetResult();
        }
        LogAction("Decrement", steps.Value.ToString());
    }

    // Method signatures only
    public double GetMinimum(int? timeoutMs = null);
    public double GetMaximum(int? timeoutMs = null);
    public double GetStep(int? timeoutMs = null);
    public bool WaitValue(double? expected, int? timeoutMs = null);
    public bool WaitValueGreaterThan(double? threshold, int? timeoutMs = null);
    public bool WaitValueLessThan(double? threshold, int? timeoutMs = null);
    public bool WaitValueInRange(double? min, double? max, int? timeoutMs = null);
    public void AssertValue(double? expected, string? message = null, int? timeoutMs = null);
    public void AssertValueGreaterThan(double? threshold, string? message = null, int? timeoutMs = null);
    public void AssertValueLessThan(double? threshold, string? message = null, int? timeoutMs = null);
    public void AssertValueInRange(double? min, double? max, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// Blazor progress element implementation.
/// </summary>
public class BlazorProgress : BlazorControlBase, IProgressControlObject
{
    public BlazorProgress(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetProgress
    public double GetProgress(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var valueStr = locator.GetAttributeAsync("value").GetAwaiter().GetResult();
        var progress = double.TryParse(valueStr, out var v) ? v : 0;
        Log($"GetProgress: {progress}");
        return progress;
    }

    // Full implementation for GetProgressPercent
    public double GetProgressPercent(int? timeoutMs = null)
    {
        var progress = GetProgress(timeoutMs);
        var max = GetMaximum(timeoutMs);
        
        if (max <= 0) return 0;
        var percent = (progress / max) * 100.0;
        Log($"GetProgressPercent: {percent}%");
        return percent;
    }

    // Full implementation for GetMaximum
    public double GetMaximum(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var maxStr = locator.GetAttributeAsync("max").GetAwaiter().GetResult();
        var max = double.TryParse(maxStr, out var v) ? v : 100;
        Log($"GetMaximum: {max}");
        return max;
    }

    // Full implementation for IsIndeterminate
    public bool IsIndeterminate(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var value = locator.GetAttributeAsync("value").GetAwaiter().GetResult();
        var indeterminate = string.IsNullOrEmpty(value);
        Log($"IsIndeterminate: {indeterminate}");
        return indeterminate;
    }

    // Method signatures only
    public double GetMinimum(int? timeoutMs = null);
    public bool WaitProgress(double? expected, int? timeoutMs = null);
    public bool WaitProgressGreaterThan(double? threshold, int? timeoutMs = null);
    public bool WaitProgressComplete(int? timeoutMs = null);
    public void AssertProgress(double? expected, string? message = null, int? timeoutMs = null);
    public void AssertProgressGreaterThan(double? threshold, string? message = null, int? timeoutMs = null);
    public void AssertProgressComplete(string? message = null, int? timeoutMs = null);
    public void AssertIndeterminate(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

**Next:** [SPEC-006-002g: DateTime Classes](SPEC-006-002-CLASSES-DATETIME.md)

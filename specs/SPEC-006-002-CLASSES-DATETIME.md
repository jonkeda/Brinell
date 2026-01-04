# SPEC-006-002g: DateTime Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. DateControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class DateControlBase : InteractiveControlBase, IDateControlObject
{
    protected DateControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation example for first method
    public abstract DateTime GetDate(int? timeoutMs = null);

    public virtual bool WaitDate(DateTime? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        Log($"WaitDate(expected={expected:yyyy-MM-dd})");
        var timeout = GetTimeout(timeoutMs);
        return WaitUntil(() => GetDate() == expected.Value, timeout);
    }

    public virtual void AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        CheckVisible(true, timeoutMs);
        var actual = GetDate(timeoutMs);
        if (actual.Date != expected.Value.Date)
        {
            ThrowAssertionFailed("Date", actual.ToString("yyyy-MM-dd"), expected.Value.ToString("yyyy-MM-dd"),
                message ?? $"Expected date '{expected:yyyy-MM-dd}' but was '{actual:yyyy-MM-dd}'.");
        }
        LogAssertPass("Date", actual.ToString("yyyy-MM-dd"), expected.Value.ToString("yyyy-MM-dd"));
    }

    // Method signatures only
    public abstract void AssertDateInRange(DateTime? min, DateTime? max, string? message = null, int? timeoutMs = null);
    public abstract void SetDate(DateTime? date, int? timeoutMs = null);
    public abstract void SelectYear(int? year, int? timeoutMs = null);
    public abstract void SelectMonth(int? month, int? timeoutMs = null);
    public abstract void SelectDay(int? day, int? timeoutMs = null);
    public abstract DateTime GetMinDate(int? timeoutMs = null);
    public abstract DateTime GetMaxDate(int? timeoutMs = null);
    public abstract bool IsPickerOpen();
    public abstract bool WaitPickerOpen(bool? expected, int? timeoutMs = null);
    public abstract void OpenPicker(int? timeoutMs = null);
    public abstract void ClosePicker(int? timeoutMs = null);
}
```

---

## 2. TimeControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class TimeControlBase : InteractiveControlBase, ITimeControlObject
{
    protected TimeControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    public abstract TimeSpan GetTime(int? timeoutMs = null);
    public abstract bool WaitTime(TimeSpan? expected, int? timeoutMs = null);
    public abstract void AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertTimeInRange(TimeSpan? min, TimeSpan? max, string? message = null, int? timeoutMs = null);
    public abstract void SetTime(TimeSpan? time, int? timeoutMs = null);
    public abstract void SelectHour(int? hour, int? timeoutMs = null);
    public abstract void SelectMinute(int? minute, int? timeoutMs = null);
    public abstract void SelectSecond(int? second, int? timeoutMs = null);
    public abstract TimeSpan GetMinTime(int? timeoutMs = null);
    public abstract TimeSpan GetMaxTime(int? timeoutMs = null);
    public abstract bool IsPickerOpen();
    public abstract bool WaitPickerOpen(bool? expected, int? timeoutMs = null);
    public abstract void OpenPicker(int? timeoutMs = null);
    public abstract void ClosePicker(int? timeoutMs = null);
}
```

---

## 3. MAUI Implementation

```csharp
namespace Brinell.Maui;

public class MauiDateControl : MauiInteractiveControlBase, IDateControlObject
{
    public MauiDateControl(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetDate
    public DateTime GetDate(int? timeoutMs = null)
    {
        var element = WaitForElementVisible(timeoutMs);
        if (element == null)
            ThrowCheckFailed("GetDate", $"Element '{Locator}' not visible.");
        
        var text = element!.Text;
        if (DateTime.TryParse(text, out var date))
            return date;
        
        Log($"GetDate: parsed '{text}' as {date:yyyy-MM-dd}");
        return DateTime.MinValue;
    }

    // Full implementation for SetDate with logging
    public void SetDate(DateTime? date, int? timeoutMs = null)
    {
        if (date == null) return;
        
        EnsureEnabled(timeoutMs);
        var element = WaitForElementVisible(timeoutMs);
        if (element == null)
            ThrowCheckFailed("SetDate", $"Element '{Locator}' not visible.");
        
        element!.Click();
        LogAction("SetDate", date.Value.ToString("yyyy-MM-dd"));
        
        // Platform-specific date picker interaction
        SelectYear(date.Value.Year, timeoutMs);
        SelectMonth(date.Value.Month, timeoutMs);
        SelectDay(date.Value.Day, timeoutMs);
    }

    // Method signatures only
    public bool WaitDate(DateTime? expected, int? timeoutMs = null);
    public void AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null);
    public void AssertDateInRange(DateTime? min, DateTime? max, string? message = null, int? timeoutMs = null);
    public void SelectYear(int? year, int? timeoutMs = null);
    public void SelectMonth(int? month, int? timeoutMs = null);
    public void SelectDay(int? day, int? timeoutMs = null);
    public DateTime GetMinDate(int? timeoutMs = null);
    public DateTime GetMaxDate(int? timeoutMs = null);
    public bool IsPickerOpen();
    public bool WaitPickerOpen(bool? expected, int? timeoutMs = null);
    public void OpenPicker(int? timeoutMs = null);
    public void ClosePicker(int? timeoutMs = null);
}

public class MauiTimeControl : MauiInteractiveControlBase, ITimeControlObject
{
    public MauiTimeControl(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public TimeSpan GetTime(int? timeoutMs = null);
    public bool WaitTime(TimeSpan? expected, int? timeoutMs = null);
    public void AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null);
    public void AssertTimeInRange(TimeSpan? min, TimeSpan? max, string? message = null, int? timeoutMs = null);
    public void SetTime(TimeSpan? time, int? timeoutMs = null);
    public void SelectHour(int? hour, int? timeoutMs = null);
    public void SelectMinute(int? minute, int? timeoutMs = null);
    public void SelectSecond(int? second, int? timeoutMs = null);
    public TimeSpan GetMinTime(int? timeoutMs = null);
    public TimeSpan GetMaxTime(int? timeoutMs = null);
    public bool IsPickerOpen();
    public bool WaitPickerOpen(bool? expected, int? timeoutMs = null);
    public void OpenPicker(int? timeoutMs = null);
    public void ClosePicker(int? timeoutMs = null);
}
```

---

## 4. Blazor Implementation

```csharp
namespace Brinell.Blazor;

public class BlazorDateControl : BlazorInteractiveControlBase, IDateControlObject
{
    public BlazorDateControl(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetDate
    public DateTime GetDate(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var value = locator.InputValueAsync().GetAwaiter().GetResult();
        
        if (DateTime.TryParse(value, out var date))
        {
            Log($"GetDate: parsed '{value}' as {date:yyyy-MM-dd}");
            return date;
        }
        
        return DateTime.MinValue;
    }

    // Full implementation for SetDate with logging
    public void SetDate(DateTime? date, int? timeoutMs = null)
    {
        if (date == null) return;
        
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        
        // HTML date input uses yyyy-MM-dd format
        var value = date.Value.ToString("yyyy-MM-dd");
        locator.FillAsync(value).GetAwaiter().GetResult();
        LogAction("SetDate", value);
    }

    // Method signatures only
    public bool WaitDate(DateTime? expected, int? timeoutMs = null);
    public void AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null);
    public void AssertDateInRange(DateTime? min, DateTime? max, string? message = null, int? timeoutMs = null);
    public void SelectYear(int? year, int? timeoutMs = null);
    public void SelectMonth(int? month, int? timeoutMs = null);
    public void SelectDay(int? day, int? timeoutMs = null);
    public DateTime GetMinDate(int? timeoutMs = null);
    public DateTime GetMaxDate(int? timeoutMs = null);
    public bool IsPickerOpen();
    public bool WaitPickerOpen(bool? expected, int? timeoutMs = null);
    public void OpenPicker(int? timeoutMs = null);
    public void ClosePicker(int? timeoutMs = null);
}

public class BlazorTimeControl : BlazorInteractiveControlBase, ITimeControlObject
{
    public BlazorTimeControl(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public TimeSpan GetTime(int? timeoutMs = null);
    public bool WaitTime(TimeSpan? expected, int? timeoutMs = null);
    public void AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null);
    public void AssertTimeInRange(TimeSpan? min, TimeSpan? max, string? message = null, int? timeoutMs = null);
    public void SetTime(TimeSpan? time, int? timeoutMs = null);
    public void SelectHour(int? hour, int? timeoutMs = null);
    public void SelectMinute(int? minute, int? timeoutMs = null);
    public void SelectSecond(int? second, int? timeoutMs = null);
    public TimeSpan GetMinTime(int? timeoutMs = null);
    public TimeSpan GetMaxTime(int? timeoutMs = null);
    public bool IsPickerOpen();
    public bool WaitPickerOpen(bool? expected, int? timeoutMs = null);
    public void OpenPicker(int? timeoutMs = null);
    public void ClosePicker(int? timeoutMs = null);
}
```

---

**Next:** [SPEC-006-002h: Collection Classes](SPEC-006-002-CLASSES-COLLECTION.md)

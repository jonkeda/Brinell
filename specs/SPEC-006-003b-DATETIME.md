# SPEC-006-003b: DateTime Controls

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Parent:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

---

## 1. MAUI DateTime Classes

### 1.1 DateControlBase

```csharp
public abstract class DateControlBase : ControlObjectBase, IDateControlObject
{
    protected DateControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected DateControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Get/Set Date (Example: GetDate)

    public virtual DateTime GetDate(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var value = element.GetAttribute("Value.Value") ?? element.Text;
        return DateTime.TryParse(value, out var result) ? result : DateTime.MinValue;
    }

    public virtual bool WaitDate(DateTime? expected, int? timeoutMs = null);
    public virtual void AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null);
    public virtual void AssertDateInRange(DateTime? min, DateTime? max, string? message = null, int? timeoutMs = null);

    public virtual void SetDate(DateTime? date, int? timeoutMs = null)
    {
        if (date is null) return;
        Log($"SetDate({date:yyyy-MM-dd})");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        OpenPicker(timeoutMs);
        SelectYear(date.Value.Year, timeoutMs);
        SelectMonth(date.Value.Month, timeoutMs);
        SelectDay(date.Value.Day, timeoutMs);
        ClosePicker(timeoutMs);
    }

    public virtual void SelectYear(int? year, int? timeoutMs = null);
    public virtual void SelectMonth(int? month, int? timeoutMs = null);
    public virtual void SelectDay(int? day, int? timeoutMs = null);

    #endregion

    #region Date Range

    public virtual DateTime GetMinDate(int? timeoutMs = null);
    public virtual DateTime GetMaxDate(int? timeoutMs = null);

    #endregion

    #region Picker (Example: IsPickerOpen)

    public virtual bool IsPickerOpen(int? timeoutMs = null)
    {
        try
        {
            var popup = Driver.FindElement(MobileBy.ClassName("CalendarDatePicker"));
            return popup?.Displayed ?? false;
        }
        catch { return false; }
    }

    public virtual bool WaitPickerOpen(bool? expected, int? timeoutMs = null);
    public virtual void OpenPicker(int? timeoutMs = null);
    public virtual void ClosePicker(int? timeoutMs = null);

    #endregion
}
```

### 1.2 TimeControlBase

```csharp
public abstract class TimeControlBase : ControlObjectBase, ITimeControlObject
{
    protected TimeControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected TimeControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Get/Set Time (Example: GetTime)

    public virtual TimeSpan GetTime(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var value = element.GetAttribute("Value.Value") ?? element.Text;
        return TimeSpan.TryParse(value, out var result) ? result : TimeSpan.Zero;
    }

    public virtual bool WaitTime(TimeSpan? expected, int? timeoutMs = null);
    public virtual void AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null);
    public virtual void AssertTimeInRange(TimeSpan? min, TimeSpan? max, string? message = null, int? timeoutMs = null);

    public virtual void SetTime(TimeSpan? time, int? timeoutMs = null);
    public virtual void SelectHour(int? hour, int? timeoutMs = null);
    public virtual void SelectMinute(int? minute, int? timeoutMs = null);
    public virtual void SelectSecond(int? second, int? timeoutMs = null);

    #endregion

    #region Time Range

    public virtual TimeSpan GetMinTime(int? timeoutMs = null);
    public virtual TimeSpan GetMaxTime(int? timeoutMs = null);

    #endregion

    #region Picker

    public virtual bool IsPickerOpen(int? timeoutMs = null);
    public virtual bool WaitPickerOpen(bool? expected, int? timeoutMs = null);
    public virtual void OpenPicker(int? timeoutMs = null);
    public virtual void ClosePicker(int? timeoutMs = null);

    #endregion
}
```

### 1.3 Concrete MAUI Controls

```csharp
public class DatePickerControl : DateControlBase
{
    public DatePickerControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public DatePickerControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class TimePickerControl : TimeControlBase
{
    public TimePickerControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public TimePickerControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}
```

---

## 2. Blazor DateTime Classes

### 2.1 AsyncDateControlBase

```csharp
public abstract class AsyncDateControlBase : AsyncControlObjectBase
{
    protected AsyncDateControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncDateControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Get/Set Date (Example: GetDateAsync)

    public virtual async Task<DateTime> GetDateAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var value = await GetLocator().InputValueAsync();
        return DateTime.TryParse(value, out var result) ? result : DateTime.MinValue;
    }

    public virtual Task AssertDateAsync(DateTime? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    public virtual async Task SetDateAsync(DateTime? date, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (date is null) return;
        Log($"SetDateAsync({date:yyyy-MM-dd})");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        // HTML date input expects yyyy-MM-dd format
        await GetLocator().FillAsync(date.Value.ToString("yyyy-MM-dd"));
    }

    #endregion
}
```

### 2.2 AsyncTimeControlBase

```csharp
public abstract class AsyncTimeControlBase : AsyncControlObjectBase
{
    protected AsyncTimeControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncTimeControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Get/Set Time (Example: GetTimeAsync)

    public virtual async Task<TimeSpan> GetTimeAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var value = await GetLocator().InputValueAsync();
        return TimeSpan.TryParse(value, out var result) ? result : TimeSpan.Zero;
    }

    public virtual Task AssertTimeAsync(TimeSpan? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    public virtual async Task SetTimeAsync(TimeSpan? time, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (time is null) return;
        Log($"SetTimeAsync({time})");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        // HTML time input expects HH:mm format
        await GetLocator().FillAsync(time.Value.ToString(@"hh\:mm"));
    }

    #endregion
}
```

### 2.3 Concrete Blazor Controls

```csharp
/// <summary>HTML input type="date" element.</summary>
public class DateInputControl : AsyncDateControlBase
{
    public DateInputControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public DateInputControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>HTML input type="time" element.</summary>
public class TimeInputControl : AsyncTimeControlBase
{
    public TimeInputControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public TimeInputControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>HTML input type="datetime-local" element.</summary>
public class DateTimeInputControl : AsyncControlObjectBase
{
    public DateTimeInputControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public DateTimeInputControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    public virtual Task<DateTime> GetDateTimeAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task SetDateTimeAsync(DateTime? dateTime, int? timeoutMs = null, CancellationToken ct = default);
}
```

---

## 3. Inheritance Summary

```
MAUI:
DateControlBase : ControlObjectBase, IDateControlObject
└── DatePickerControl

TimeControlBase : ControlObjectBase, ITimeControlObject
└── TimePickerControl

Blazor:
AsyncDateControlBase : AsyncControlObjectBase
└── DateInputControl

AsyncTimeControlBase : AsyncControlObjectBase
└── TimeInputControl

DateTimeInputControl : AsyncControlObjectBase
```

---

**Next:** [SPEC-006-003b-COLLECTION](SPEC-006-003b-COLLECTION.md)

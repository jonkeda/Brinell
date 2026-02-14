# SPEC-006-003b: Display Controls

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Parent:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

---

## 1. MAUI Display Classes

### 1.1 ImageControlBase

```csharp
public abstract class ImageControlBase : ControlObjectBase, IImageControlObject
{
    protected ImageControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ImageControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Image Source (Example: GetSource)

    public virtual string? GetSource(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.GetAttribute("Source") ?? element.GetAttribute("Name");
    }

    public virtual void AssertSource(string? expected, string? message = null, int? timeoutMs = null);
    public virtual bool HasSource(int? timeoutMs = null);

    #endregion

    #region Dimensions (Example: GetDimensions)

    public virtual (int width, int height) GetDimensions(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return (element.Size.Width, element.Size.Height);
    }

    public virtual void AssertDimensions(int? expectedWidth, int? expectedHeight, string? message = null, int? timeoutMs = null);

    #endregion

    #region Loading State (Example: IsLoading)

    public virtual bool IsLoading(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.GetAttribute("IsLoading") == "True";
    }

    public virtual bool WaitLoaded(int? timeoutMs = null);
    public virtual void AssertLoaded(string? message = null, int? timeoutMs = null);

    #endregion
}
```

### 1.2 ProgressControlBase

```csharp
public abstract class ProgressControlBase : ControlObjectBase, IProgressControlObject
{
    protected ProgressControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ProgressControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Progress Value (Example: GetProgress)

    public virtual double GetProgress(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var value = element.GetAttribute("RangeValue.Value");
        return double.TryParse(value, out var v) ? v : 0;
    }

    public virtual bool WaitProgress(double? expected, int? timeoutMs = null);
    public virtual void AssertProgress(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);

    #endregion

    #region Progress Range (Example: GetMinMax)

    public virtual (double min, double max) GetMinMax(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var min = double.Parse(element.GetAttribute("RangeValue.Minimum") ?? "0");
        var max = double.Parse(element.GetAttribute("RangeValue.Maximum") ?? "1");
        return (min, max);
    }

    public virtual double GetProgressPercent(int? timeoutMs = null);

    #endregion

    #region Completion State (Example: IsComplete)

    public virtual bool IsComplete(int? timeoutMs = null)
    {
        var (_, max) = GetMinMax(timeoutMs);
        return Math.Abs(GetProgress(timeoutMs) - max) < 0.001;
    }

    public virtual bool WaitComplete(int? timeoutMs = null);
    public virtual void AssertComplete(string? message = null, int? timeoutMs = null);

    #endregion
}
```

### 1.3 ActivityIndicatorControlBase

```csharp
public abstract class ActivityIndicatorControlBase : ControlObjectBase, IActivityIndicatorControlObject
{
    protected ActivityIndicatorControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ActivityIndicatorControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Running State (Example: IsRunning)

    public virtual bool IsRunning(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.GetAttribute("IsRunning") == "True" || element.Displayed;
    }

    public virtual bool WaitRunning(bool? expected, int? timeoutMs = null);
    public virtual void AssertRunning(bool? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Wait Helpers (Example: WaitUntilStopped)

    public virtual void WaitUntilStopped(int? timeoutMs = null)
    {
        Log("WaitUntilStopped()");
        WaitRunning(false, timeoutMs);
    }

    public virtual void WaitUntilStarted(int? timeoutMs = null);

    #endregion
}
```

### 1.4 Concrete MAUI Controls

```csharp
public class ImageControl : ImageControlBase
{
    public ImageControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public ImageControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class ProgressBarControl : ProgressControlBase
{
    public ProgressBarControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public ProgressBarControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class ActivityIndicatorControl : ActivityIndicatorControlBase
{
    public ActivityIndicatorControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public ActivityIndicatorControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

/// <summary>LabelControl is in FOUNDATION - inherits from TextControlBase.</summary>
```

---

## 2. Blazor Display Classes

### 2.1 AsyncImageControlBase

```csharp
public abstract class AsyncImageControlBase : AsyncControlObjectBase
{
    protected AsyncImageControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncImageControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Image Source (Example: GetSourceAsync)

    public virtual async Task<string?> GetSourceAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().GetAttributeAsync("src");
    }

    public virtual Task AssertSourceAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<bool> HasSourceAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Alt Text (Example: GetAltTextAsync)

    public virtual async Task<string?> GetAltTextAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().GetAttributeAsync("alt");
    }

    public virtual Task AssertAltTextAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Loading State (Example: IsLoadedAsync)

    public virtual async Task<bool> IsLoadedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().EvaluateAsync<bool>("img => img.complete && img.naturalWidth > 0");
    }

    public virtual Task WaitLoadedAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.2 AsyncProgressControlBase

```csharp
public abstract class AsyncProgressControlBase : AsyncControlObjectBase
{
    protected AsyncProgressControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncProgressControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Progress Value (Example: GetProgressAsync)

    public virtual async Task<double> GetProgressAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var value = await GetLocator().GetAttributeAsync("value");
        return double.TryParse(value, out var v) ? v : 0;
    }

    public virtual Task AssertProgressAsync(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Progress Range (Example: GetMaxAsync)

    public virtual async Task<double> GetMaxAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var max = await GetLocator().GetAttributeAsync("max");
        return double.TryParse(max, out var v) ? v : 100;
    }

    public virtual Task<double> GetProgressPercentAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Indeterminate State (Example: IsIndeterminateAsync)

    public virtual async Task<bool> IsIndeterminateAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        // Progress without value attribute is indeterminate
        var value = await GetLocator().GetAttributeAsync("value");
        return string.IsNullOrEmpty(value);
    }

    public virtual Task AssertIndeterminateAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.3 AsyncSpinnerControlBase

```csharp
public abstract class AsyncSpinnerControlBase : AsyncControlObjectBase
{
    protected AsyncSpinnerControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncSpinnerControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Visibility State (Example: IsSpinningAsync)

    public virtual async Task<bool> IsSpinningAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await IsVisibleAsync(timeoutMs, ct);
    }

    public virtual Task AssertSpinningAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Wait Helpers (Example: WaitUntilHiddenAsync)

    public virtual async Task WaitUntilHiddenAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("WaitUntilHiddenAsync()");
        await WaitVisibleAsync(false, timeoutMs, ct);
    }

    public virtual Task WaitUntilVisibleAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.4 Concrete Blazor Controls

```csharp
/// <summary>HTML img element.</summary>
public class ImageControl : AsyncImageControlBase
{
    public ImageControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public ImageControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>HTML progress element.</summary>
public class ProgressControl : AsyncProgressControlBase
{
    public ProgressControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public ProgressControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>Loading spinner element.</summary>
public class SpinnerControl : AsyncSpinnerControlBase
{
    public SpinnerControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public SpinnerControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>HTML meter element.</summary>
public class MeterControl : AsyncProgressControlBase
{
    public MeterControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public MeterControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    public async Task<double> GetMinAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var min = await GetLocator().GetAttributeAsync("min");
        return double.TryParse(min, out var v) ? v : 0;
    }

    public async Task<double> GetLowAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var low = await GetLocator().GetAttributeAsync("low");
        return double.TryParse(low, out var v) ? v : 0;
    }

    public Task<double> GetHighAsync(int? timeoutMs = null, CancellationToken ct = default);
    public Task<double> GetOptimumAsync(int? timeoutMs = null, CancellationToken ct = default);
}

/// <summary>LabelControl is in FOUNDATION - inherits from AsyncTextControlBase.</summary>
```

---

## 3. Inheritance Summary

```
MAUI:
ImageControlBase : ControlObjectBase, IImageControlObject
└── ImageControl

ProgressControlBase : ControlObjectBase, IProgressControlObject
└── ProgressBarControl

ActivityIndicatorControlBase : ControlObjectBase, IActivityIndicatorControlObject
└── ActivityIndicatorControl

(LabelControl in FOUNDATION: TextControlBase → LabelControl)

Blazor:
AsyncImageControlBase : AsyncControlObjectBase
└── ImageControl

AsyncProgressControlBase : AsyncControlObjectBase
├── ProgressControl
└── MeterControl

AsyncSpinnerControlBase : AsyncControlObjectBase
└── SpinnerControl

(LabelControl in FOUNDATION: AsyncTextControlBase → LabelControl)
```

---

**Next:** [SPEC-006-003b-NAVIGATION](SPEC-006-003b-NAVIGATION.md)

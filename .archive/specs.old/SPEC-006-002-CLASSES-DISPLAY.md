# SPEC-006-002j: Display Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. LabelControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class LabelControlBase : ControlBase, ILabelControlObject
{
    protected LabelControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetText with logging
    public override string? GetText(int? timeoutMs = null)
    {
        var element = WaitForElementVisible(timeoutMs);
        if (element == null) return null;
        
        var text = GetTextFromElement(element);
        Log($"GetText: '{text}'");
        return text;
    }

    // Full implementation for AssertText with logging
    public virtual void AssertText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        CheckVisible(true, timeoutMs);
        var actual = GetText(timeoutMs);
        if (actual != expected)
        {
            ThrowAssertionFailed("Text", actual, expected,
                message ?? $"Expected text '{expected}' but was '{actual}'.");
        }
        LogAssertPass("Text", actual, expected);
    }

    // Abstract core method
    protected abstract string? GetTextFromElement(object element);

    // Method signatures only
    public abstract bool WaitText(string? expected, int? timeoutMs = null);
    public abstract void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null);
    public abstract void AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 2. ImageControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class ImageControlBase : ControlBase, IImageControlObject
{
    protected ImageControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsLoaded
    public virtual bool IsLoaded(int? timeoutMs = null)
    {
        var element = WaitForElementVisible(timeoutMs);
        if (element == null) return false;
        
        var loaded = GetLoadedState(element);
        Log($"IsLoaded: {loaded}");
        return loaded;
    }

    // Full implementation for AssertLoaded
    public virtual void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        CheckVisible(true, timeoutMs);
        var actual = IsLoaded(timeoutMs);
        if (actual != expected.Value)
        {
            ThrowAssertionFailed("Loaded", actual.ToString(), expected.Value.ToString(),
                message ?? $"Expected image loaded={expected.Value} but was {actual}.");
        }
        LogAssertPass("Loaded", actual.ToString(), expected.Value.ToString());
    }

    // Abstract core method
    protected abstract bool GetLoadedState(object element);

    // Method signatures only
    public abstract string? GetSource(int? timeoutMs = null);
    public abstract string? GetAltText(int? timeoutMs = null);
    public abstract bool WaitLoaded(bool? expected, int? timeoutMs = null);
    public abstract void AssertSource(string? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertSourceContains(string? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertAltText(string? expected, string? message = null, int? timeoutMs = null);
    public abstract (int Width, int Height) GetDimensions(int? timeoutMs = null);
    public abstract void AssertDimensions(int? width, int? height, string? message = null, int? timeoutMs = null);
}
```

---

## 3. ProgressControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class ProgressControlBase : ControlBase, IProgressControlObject
{
    protected ProgressControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetProgress
    public virtual double GetProgress(int? timeoutMs = null)
    {
        var element = WaitForElementVisible(timeoutMs);
        if (element == null) return 0;
        
        var progress = GetProgressValue(element);
        Log($"GetProgress: {progress:F2}");
        return progress;
    }

    // Full implementation for AssertProgress
    public virtual void AssertProgress(double? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        CheckVisible(true, timeoutMs);
        var actual = GetProgress(timeoutMs);
        var tolerance = 0.01;
        if (Math.Abs(actual - expected.Value) > tolerance)
        {
            ThrowAssertionFailed("Progress", actual.ToString("F2"), expected.Value.ToString("F2"),
                message ?? $"Expected progress {expected.Value:F2} but was {actual:F2}.");
        }
        LogAssertPass("Progress", actual.ToString("F2"), expected.Value.ToString("F2"));
    }

    // Abstract core method
    protected abstract double GetProgressValue(object element);

    // Method signatures only
    public abstract bool IsIndeterminate(int? timeoutMs = null);
    public abstract bool WaitProgress(double? expected, int? timeoutMs = null);
    public abstract bool WaitProgressAtLeast(double? minimum, int? timeoutMs = null);
    public abstract bool WaitProgressComplete(int? timeoutMs = null);
    public abstract void AssertProgressInRange(double? min, double? max, string? message = null, int? timeoutMs = null);
    public abstract void AssertIndeterminate(bool? expected, string? message = null, int? timeoutMs = null);
    public abstract double GetMinimum(int? timeoutMs = null);
    public abstract double GetMaximum(int? timeoutMs = null);
}
```

---

## 4. ActivityIndicatorControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class ActivityIndicatorControlBase : ControlBase, IActivityIndicatorControlObject
{
    protected ActivityIndicatorControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsRunning
    public virtual bool IsRunning(int? timeoutMs = null)
    {
        var element = WaitForElementVisible(timeoutMs);
        if (element == null) return false;
        
        var running = GetRunningState(element);
        Log($"IsRunning: {running}");
        return running;
    }

    // Abstract core method
    protected abstract bool GetRunningState(object element);

    // Method signatures only
    public abstract bool WaitRunning(bool? expected, int? timeoutMs = null);
    public abstract void AssertRunning(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 5. MAUI Implementation

```csharp
namespace Brinell.Maui;

public class MauiLabel : MauiControlBase, ILabelControlObject
{
    public MauiLabel(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetText
    public override string? GetText(int? timeoutMs = null)
    {
        var element = WaitForElementVisible(timeoutMs);
        if (element == null) return null;
        
        var text = element.Text;
        Log($"GetText: '{text}'");
        return text;
    }

    // Method signatures only
    public bool WaitText(string? expected, int? timeoutMs = null);
    public void AssertText(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null);
    public void AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null);
}

public class MauiImage : MauiControlBase, IImageControlObject
{
    public MauiImage(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public bool IsLoaded(int? timeoutMs = null);
    public string? GetSource(int? timeoutMs = null);
    public string? GetAltText(int? timeoutMs = null);
    public bool WaitLoaded(bool? expected, int? timeoutMs = null);
    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertSource(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertSourceContains(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertAltText(string? expected, string? message = null, int? timeoutMs = null);
    public (int Width, int Height) GetDimensions(int? timeoutMs = null);
    public void AssertDimensions(int? width, int? height, string? message = null, int? timeoutMs = null);
}

public class MauiProgressBar : MauiControlBase, IProgressControlObject
{
    public MauiProgressBar(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetProgress
    public double GetProgress(int? timeoutMs = null)
    {
        var element = WaitForElementVisible(timeoutMs);
        if (element == null) return 0;
        
        // MAUI ProgressBar uses content-desc for progress value on Android
        var contentDesc = element.GetAttribute("content-desc");
        if (double.TryParse(contentDesc, out var progress))
        {
            Log($"GetProgress: {progress:F2}");
            return progress;
        }
        
        return 0;
    }

    // Method signatures only
    public bool IsIndeterminate(int? timeoutMs = null);
    public bool WaitProgress(double? expected, int? timeoutMs = null);
    public bool WaitProgressAtLeast(double? minimum, int? timeoutMs = null);
    public bool WaitProgressComplete(int? timeoutMs = null);
    public void AssertProgress(double? expected, string? message = null, int? timeoutMs = null);
    public void AssertProgressInRange(double? min, double? max, string? message = null, int? timeoutMs = null);
    public void AssertIndeterminate(bool? expected, string? message = null, int? timeoutMs = null);
    public double GetMinimum(int? timeoutMs = null);
    public double GetMaximum(int? timeoutMs = null);
}

public class MauiActivityIndicator : MauiControlBase, IActivityIndicatorControlObject
{
    public MauiActivityIndicator(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public bool IsRunning(int? timeoutMs = null);
    public bool WaitRunning(bool? expected, int? timeoutMs = null);
    public void AssertRunning(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 6. Blazor Implementation

```csharp
namespace Brinell.Blazor;

public class BlazorSpan : BlazorControlBase, ILabelControlObject
{
    public BlazorSpan(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetText
    public override string? GetText(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var text = locator.InnerTextAsync().GetAwaiter().GetResult();
        Log($"GetText: '{text}'");
        return text;
    }

    // Method signatures only
    public bool WaitText(string? expected, int? timeoutMs = null);
    public void AssertText(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null);
    public void AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null);
}

public class BlazorImage : BlazorControlBase, IImageControlObject
{
    public BlazorImage(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetSource
    public string? GetSource(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var src = locator.GetAttributeAsync("src").GetAwaiter().GetResult();
        Log($"GetSource: '{src}'");
        return src;
    }

    // Full implementation for IsLoaded
    public bool IsLoaded(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var loaded = locator.EvaluateAsync<bool>("img => img.complete && img.naturalHeight > 0").GetAwaiter().GetResult();
        Log($"IsLoaded: {loaded}");
        return loaded;
    }

    // Method signatures only
    public string? GetAltText(int? timeoutMs = null);
    public bool WaitLoaded(bool? expected, int? timeoutMs = null);
    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertSource(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertSourceContains(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertAltText(string? expected, string? message = null, int? timeoutMs = null);
    public (int Width, int Height) GetDimensions(int? timeoutMs = null);
    public void AssertDimensions(int? width, int? height, string? message = null, int? timeoutMs = null);
}

public class BlazorProgress : BlazorControlBase, IProgressControlObject
{
    public BlazorProgress(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetProgress
    public double GetProgress(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var value = locator.GetAttributeAsync("value").GetAwaiter().GetResult();
        var max = locator.GetAttributeAsync("max").GetAwaiter().GetResult() ?? "100";
        
        if (double.TryParse(value, out var v) && double.TryParse(max, out var m) && m > 0)
        {
            var progress = v / m;
            Log($"GetProgress: {progress:F2}");
            return progress;
        }
        
        return 0;
    }

    // Method signatures only
    public bool IsIndeterminate(int? timeoutMs = null);
    public bool WaitProgress(double? expected, int? timeoutMs = null);
    public bool WaitProgressAtLeast(double? minimum, int? timeoutMs = null);
    public bool WaitProgressComplete(int? timeoutMs = null);
    public void AssertProgress(double? expected, string? message = null, int? timeoutMs = null);
    public void AssertProgressInRange(double? min, double? max, string? message = null, int? timeoutMs = null);
    public void AssertIndeterminate(bool? expected, string? message = null, int? timeoutMs = null);
    public double GetMinimum(int? timeoutMs = null);
    public double GetMaximum(int? timeoutMs = null);
}

public class BlazorSpinner : BlazorControlBase, IActivityIndicatorControlObject
{
    public BlazorSpinner(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public bool IsRunning(int? timeoutMs = null);
    public bool WaitRunning(bool? expected, int? timeoutMs = null);
    public void AssertRunning(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

**Next:** [SPEC-006-002k: Media Classes](SPEC-006-002-CLASSES-MEDIA.md)

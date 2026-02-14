# SPEC-006-002k: Media Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. MediaControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class MediaControlBase : ControlBase, IMediaControlObject
{
    protected MediaControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for Play with logging
    public virtual void Play(int? timeoutMs = null)
    {
        EnsureVisible(timeoutMs);
        if (!IsPlaying(timeoutMs))
        {
            PlayCore();
            LogAction("Play");
        }
    }

    // Full implementation for GetDuration
    public virtual TimeSpan GetDuration(int? timeoutMs = null)
    {
        var element = WaitForElementVisible(timeoutMs);
        if (element == null) return TimeSpan.Zero;
        
        var duration = GetDurationValue(element);
        Log($"GetDuration: {duration}");
        return duration;
    }

    // Abstract core methods
    protected abstract void PlayCore();
    protected abstract void PauseCore();
    protected abstract void StopCore();
    protected abstract TimeSpan GetDurationValue(object element);
    protected abstract TimeSpan GetPositionValue(object element);

    // Method signatures only
    public abstract void Pause(int? timeoutMs = null);
    public abstract void Stop(int? timeoutMs = null);
    public abstract bool IsPlaying(int? timeoutMs = null);
    public abstract bool IsPaused(int? timeoutMs = null);
    public abstract bool IsStopped(int? timeoutMs = null);
    public abstract TimeSpan GetPosition(int? timeoutMs = null);
    public abstract void SeekTo(TimeSpan position, int? timeoutMs = null);
    public abstract void SeekToPercentage(double percentage, int? timeoutMs = null);
    public abstract double GetVolume(int? timeoutMs = null);
    public abstract void SetVolume(double? volume, int? timeoutMs = null);
    public abstract bool IsMuted(int? timeoutMs = null);
    public abstract void Mute(int? timeoutMs = null);
    public abstract void Unmute(int? timeoutMs = null);
    public abstract bool WaitPlaying(bool? expected, int? timeoutMs = null);
    public abstract bool WaitPosition(TimeSpan? expected, int? timeoutMs = null);
    public abstract void AssertPlaying(bool? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertDuration(TimeSpan? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertVolume(double? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertMuted(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 2. WebViewControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class WebViewControlBase : ControlBase, IWebViewControlObject
{
    protected WebViewControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for NavigateTo with logging
    public virtual void NavigateTo(string? url, int? timeoutMs = null)
    {
        if (url == null) return;
        
        EnsureVisible(timeoutMs);
        NavigateToCore(url);
        LogAction("NavigateTo", url);
    }

    // Full implementation for GetUrl
    public virtual string? GetUrl(int? timeoutMs = null)
    {
        var element = WaitForElementVisible(timeoutMs);
        if (element == null) return null;
        
        var url = GetUrlFromElement(element);
        Log($"GetUrl: '{url}'");
        return url;
    }

    // Abstract core methods
    protected abstract void NavigateToCore(string url);
    protected abstract string? GetUrlFromElement(object element);
    protected abstract string? GetTitleFromElement(object element);

    // Method signatures only
    public abstract string? GetTitle(int? timeoutMs = null);
    public abstract bool IsLoading(int? timeoutMs = null);
    public abstract void Reload(int? timeoutMs = null);
    public abstract void GoBack(int? timeoutMs = null);
    public abstract void GoForward(int? timeoutMs = null);
    public abstract bool CanGoBack(int? timeoutMs = null);
    public abstract bool CanGoForward(int? timeoutMs = null);
    public abstract string? ExecuteScript(string? script, int? timeoutMs = null);
    public abstract T? ExecuteScript<T>(string? script, int? timeoutMs = null);
    public abstract bool WaitUrl(string? expected, int? timeoutMs = null);
    public abstract bool WaitUrlContains(string? expected, int? timeoutMs = null);
    public abstract bool WaitLoaded(int? timeoutMs = null);
    public abstract void AssertUrl(string? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertUrlContains(string? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 3. MAUI Implementation

```csharp
namespace Brinell.Maui;

public class MauiMediaElement : MauiControlBase, IMediaControlObject
{
    public MauiMediaElement(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for Play
    public void Play(int? timeoutMs = null)
    {
        EnsureVisible(timeoutMs);
        var element = WaitForElementVisible(timeoutMs);
        if (element == null)
            ThrowCheckFailed("Play", $"Element '{Locator}' not visible.");
        
        // Find and click play button within media controls
        var playButton = element!.FindElement(OpenQA.Selenium.By.XPath(".//android.widget.ImageButton[@content-desc='Play']"));
        playButton?.Click();
        LogAction("Play");
    }

    // Method signatures only
    public void Pause(int? timeoutMs = null);
    public void Stop(int? timeoutMs = null);
    public bool IsPlaying(int? timeoutMs = null);
    public bool IsPaused(int? timeoutMs = null);
    public bool IsStopped(int? timeoutMs = null);
    public TimeSpan GetDuration(int? timeoutMs = null);
    public TimeSpan GetPosition(int? timeoutMs = null);
    public void SeekTo(TimeSpan position, int? timeoutMs = null);
    public void SeekToPercentage(double percentage, int? timeoutMs = null);
    public double GetVolume(int? timeoutMs = null);
    public void SetVolume(double? volume, int? timeoutMs = null);
    public bool IsMuted(int? timeoutMs = null);
    public void Mute(int? timeoutMs = null);
    public void Unmute(int? timeoutMs = null);
    public bool WaitPlaying(bool? expected, int? timeoutMs = null);
    public bool WaitPosition(TimeSpan? expected, int? timeoutMs = null);
    public void AssertPlaying(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertDuration(TimeSpan? expected, string? message = null, int? timeoutMs = null);
    public void AssertVolume(double? expected, string? message = null, int? timeoutMs = null);
    public void AssertMuted(bool? expected, string? message = null, int? timeoutMs = null);
}

public class MauiWebView : MauiControlBase, IWebViewControlObject
{
    public MauiWebView(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for NavigateTo
    public void NavigateTo(string? url, int? timeoutMs = null)
    {
        if (url == null) return;
        
        EnsureVisible(timeoutMs);
        
        // Switch to webview context for navigation
        var contexts = _context.Driver.Contexts;
        var webviewContext = contexts.FirstOrDefault(c => c.Contains("WEBVIEW"));
        if (webviewContext != null)
        {
            _context.Driver.Context = webviewContext;
            ((IJavaScriptExecutor)_context.Driver).ExecuteScript($"window.location.href = '{url}'");
            _context.Driver.Context = "NATIVE_APP";
        }
        
        LogAction("NavigateTo", url);
    }

    // Method signatures only
    public string? GetUrl(int? timeoutMs = null);
    public string? GetTitle(int? timeoutMs = null);
    public bool IsLoading(int? timeoutMs = null);
    public void Reload(int? timeoutMs = null);
    public void GoBack(int? timeoutMs = null);
    public void GoForward(int? timeoutMs = null);
    public bool CanGoBack(int? timeoutMs = null);
    public bool CanGoForward(int? timeoutMs = null);
    public string? ExecuteScript(string? script, int? timeoutMs = null);
    public T? ExecuteScript<T>(string? script, int? timeoutMs = null);
    public bool WaitUrl(string? expected, int? timeoutMs = null);
    public bool WaitUrlContains(string? expected, int? timeoutMs = null);
    public bool WaitLoaded(int? timeoutMs = null);
    public void AssertUrl(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertUrlContains(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 4. Blazor Implementation

```csharp
namespace Brinell.Blazor;

public class BlazorVideo : BlazorControlBase, IMediaControlObject
{
    public BlazorVideo(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for Play
    public void Play(int? timeoutMs = null)
    {
        EnsureVisible(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.EvaluateAsync("video => video.play()").GetAwaiter().GetResult();
        LogAction("Play");
    }

    // Full implementation for GetDuration
    public TimeSpan GetDuration(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var seconds = locator.EvaluateAsync<double>("video => video.duration").GetAwaiter().GetResult();
        var duration = TimeSpan.FromSeconds(seconds);
        Log($"GetDuration: {duration}");
        return duration;
    }

    // Full implementation for IsPlaying
    public bool IsPlaying(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var playing = locator.EvaluateAsync<bool>("video => !video.paused && !video.ended").GetAwaiter().GetResult();
        Log($"IsPlaying: {playing}");
        return playing;
    }

    // Method signatures only
    public void Pause(int? timeoutMs = null);
    public void Stop(int? timeoutMs = null);
    public bool IsPaused(int? timeoutMs = null);
    public bool IsStopped(int? timeoutMs = null);
    public TimeSpan GetPosition(int? timeoutMs = null);
    public void SeekTo(TimeSpan position, int? timeoutMs = null);
    public void SeekToPercentage(double percentage, int? timeoutMs = null);
    public double GetVolume(int? timeoutMs = null);
    public void SetVolume(double? volume, int? timeoutMs = null);
    public bool IsMuted(int? timeoutMs = null);
    public void Mute(int? timeoutMs = null);
    public void Unmute(int? timeoutMs = null);
    public bool WaitPlaying(bool? expected, int? timeoutMs = null);
    public bool WaitPosition(TimeSpan? expected, int? timeoutMs = null);
    public void AssertPlaying(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertDuration(TimeSpan? expected, string? message = null, int? timeoutMs = null);
    public void AssertVolume(double? expected, string? message = null, int? timeoutMs = null);
    public void AssertMuted(bool? expected, string? message = null, int? timeoutMs = null);
}

public class BlazorAudio : BlazorControlBase, IMediaControlObject
{
    public BlazorAudio(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Method signatures only (same interface as BlazorVideo)
    public void Play(int? timeoutMs = null);
    public void Pause(int? timeoutMs = null);
    public void Stop(int? timeoutMs = null);
    public bool IsPlaying(int? timeoutMs = null);
    public bool IsPaused(int? timeoutMs = null);
    public bool IsStopped(int? timeoutMs = null);
    public TimeSpan GetDuration(int? timeoutMs = null);
    public TimeSpan GetPosition(int? timeoutMs = null);
    public void SeekTo(TimeSpan position, int? timeoutMs = null);
    public void SeekToPercentage(double percentage, int? timeoutMs = null);
    public double GetVolume(int? timeoutMs = null);
    public void SetVolume(double? volume, int? timeoutMs = null);
    public bool IsMuted(int? timeoutMs = null);
    public void Mute(int? timeoutMs = null);
    public void Unmute(int? timeoutMs = null);
    public bool WaitPlaying(bool? expected, int? timeoutMs = null);
    public bool WaitPosition(TimeSpan? expected, int? timeoutMs = null);
    public void AssertPlaying(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertDuration(TimeSpan? expected, string? message = null, int? timeoutMs = null);
    public void AssertVolume(double? expected, string? message = null, int? timeoutMs = null);
    public void AssertMuted(bool? expected, string? message = null, int? timeoutMs = null);
}

public class BlazorIFrame : BlazorControlBase, IWebViewControlObject
{
    public BlazorIFrame(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for NavigateTo
    public void NavigateTo(string? url, int? timeoutMs = null)
    {
        if (url == null) return;
        
        EnsureVisible(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.EvaluateAsync($"iframe => iframe.src = '{url}'").GetAwaiter().GetResult();
        LogAction("NavigateTo", url);
    }

    // Full implementation for GetUrl
    public string? GetUrl(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var url = locator.GetAttributeAsync("src").GetAwaiter().GetResult();
        Log($"GetUrl: '{url}'");
        return url;
    }

    // Method signatures only
    public string? GetTitle(int? timeoutMs = null);
    public bool IsLoading(int? timeoutMs = null);
    public void Reload(int? timeoutMs = null);
    public void GoBack(int? timeoutMs = null);
    public void GoForward(int? timeoutMs = null);
    public bool CanGoBack(int? timeoutMs = null);
    public bool CanGoForward(int? timeoutMs = null);
    public string? ExecuteScript(string? script, int? timeoutMs = null);
    public T? ExecuteScript<T>(string? script, int? timeoutMs = null);
    public bool WaitUrl(string? expected, int? timeoutMs = null);
    public bool WaitUrlContains(string? expected, int? timeoutMs = null);
    public bool WaitLoaded(int? timeoutMs = null);
    public void AssertUrl(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertUrlContains(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
}
```

---

**Next:** [SPEC-006-002l: Navigation Classes](SPEC-006-002-CLASSES-NAVIGATION.md)

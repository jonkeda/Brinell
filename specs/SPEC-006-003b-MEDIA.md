# SPEC-006-003b: Media Controls

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Parent:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

---

## 1. MAUI Media Classes

### 1.1 MediaElementControlBase

```csharp
public abstract class MediaElementControlBase : ControlObjectBase, IMediaControlObject
{
    protected MediaElementControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected MediaElementControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Playback State (Example: IsPlaying)

    public virtual bool IsPlaying(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.GetAttribute("CurrentState") == "Playing";
    }

    public virtual bool WaitPlaying(bool? expected, int? timeoutMs = null);
    public virtual void AssertPlaying(bool? expected, string? message = null, int? timeoutMs = null);
    public virtual bool IsPaused(int? timeoutMs = null);
    public virtual bool IsStopped(int? timeoutMs = null);

    #endregion

    #region Playback Controls (Example: Play)

    public virtual void Play(int? timeoutMs = null)
    {
        Log("Play()");
        var element = FindElementRequired(timeoutMs);
        // Invoke play through automation or click play button
        element.SendKeys(" "); // Space to toggle play
    }

    public virtual void Pause(int? timeoutMs = null);
    public virtual void Stop(int? timeoutMs = null);
    public virtual void TogglePlayPause(int? timeoutMs = null);

    #endregion

    #region Position & Duration (Example: GetPosition)

    public virtual TimeSpan GetPosition(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var position = element.GetAttribute("Position");
        return TimeSpan.TryParse(position, out var p) ? p : TimeSpan.Zero;
    }

    public virtual TimeSpan GetDuration(int? timeoutMs = null);
    public virtual double GetPositionPercent(int? timeoutMs = null);
    public virtual void SeekTo(TimeSpan? position, int? timeoutMs = null);
    public virtual void SeekToPercent(double? percent, int? timeoutMs = null);

    #endregion

    #region Volume (Example: GetVolume)

    public virtual double GetVolume(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var volume = element.GetAttribute("Volume");
        return double.TryParse(volume, out var v) ? v : 1.0;
    }

    public virtual void SetVolume(double? volume, int? timeoutMs = null);
    public virtual bool IsMuted(int? timeoutMs = null);
    public virtual void Mute(int? timeoutMs = null);
    public virtual void Unmute(int? timeoutMs = null);
    public virtual void ToggleMute(int? timeoutMs = null);

    #endregion

    #region Source (Example: GetSource)

    public virtual string? GetSource(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.GetAttribute("Source");
    }

    public virtual void AssertSource(string? expected, string? message = null, int? timeoutMs = null);

    #endregion
}
```

### 1.2 WebViewControlBase

```csharp
public abstract class WebViewControlBase : ControlObjectBase, IWebViewControlObject
{
    protected WebViewControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected WebViewControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region URL (Example: GetCurrentUrl)

    public virtual string? GetCurrentUrl(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.GetAttribute("Source") ?? element.GetAttribute("Url");
    }

    public virtual void AssertCurrentUrl(string? expected, string? message = null, int? timeoutMs = null);
    public virtual void AssertUrlContains(string? substring, string? message = null, int? timeoutMs = null);

    #endregion

    #region Navigation (Example: NavigateTo)

    public virtual void NavigateTo(string? url, int? timeoutMs = null)
    {
        if (url is null) return;
        Log($"NavigateTo({url})");
        var element = FindElementRequired(timeoutMs);
        // Set Source property or use JavaScript
    }

    public virtual void GoBack(int? timeoutMs = null);
    public virtual void GoForward(int? timeoutMs = null);
    public virtual void Refresh(int? timeoutMs = null);
    public virtual bool CanGoBack(int? timeoutMs = null);
    public virtual bool CanGoForward(int? timeoutMs = null);

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

    #region Title (Example: GetTitle)

    public virtual string? GetTitle(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.GetAttribute("Title");
    }

    public virtual void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region JavaScript (Example: ExecuteJavaScript)

    public virtual string? ExecuteJavaScript(string? script, int? timeoutMs = null)
    {
        if (script is null) return null;
        Log($"ExecuteJavaScript({script.Substring(0, Math.Min(50, script.Length))}...)");
        // Execute through EvaluateJavaScriptAsync or similar
        return null; // Return result as string
    }

    #endregion
}
```

### 1.3 Concrete MAUI Controls

```csharp
public class MediaElementControl : MediaElementControlBase
{
    public MediaElementControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public MediaElementControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class WebViewControl : WebViewControlBase
{
    public WebViewControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public WebViewControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}
```

---

## 2. Blazor Media Classes

### 2.1 AsyncVideoControlBase

```csharp
public abstract class AsyncVideoControlBase : AsyncControlObjectBase
{
    protected AsyncVideoControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncVideoControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Playback State (Example: IsPlayingAsync)

    public virtual async Task<bool> IsPlayingAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().EvaluateAsync<bool>("v => !v.paused && !v.ended");
    }

    public virtual Task AssertPlayingAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<bool> IsPausedAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<bool> IsEndedAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Playback Controls (Example: PlayAsync)

    public virtual async Task PlayAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("PlayAsync()");
        await GetLocator().EvaluateAsync("v => v.play()");
    }

    public virtual Task PauseAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task TogglePlayPauseAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Position & Duration (Example: GetCurrentTimeAsync)

    public virtual async Task<double> GetCurrentTimeAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().EvaluateAsync<double>("v => v.currentTime");
    }

    public virtual Task<double> GetDurationAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task SeekToAsync(double? seconds, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task SeekToPercentAsync(double? percent, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Volume (Example: GetVolumeAsync)

    public virtual async Task<double> GetVolumeAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().EvaluateAsync<double>("v => v.volume");
    }

    public virtual Task SetVolumeAsync(double? volume, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<bool> IsMutedAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task MuteAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task UnmuteAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Source (Example: GetSourceAsync)

    public virtual async Task<string?> GetSourceAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().GetAttributeAsync("src") ??
               await GetLocator().EvaluateAsync<string>("v => v.currentSrc");
    }

    #endregion
}
```

### 2.2 AsyncAudioControlBase

```csharp
public abstract class AsyncAudioControlBase : AsyncControlObjectBase
{
    protected AsyncAudioControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncAudioControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Playback State (Example: IsPlayingAsync)

    public virtual async Task<bool> IsPlayingAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().EvaluateAsync<bool>("a => !a.paused && !a.ended");
    }

    public virtual Task<bool> IsPausedAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Playback Controls (Example: PlayAsync)

    public virtual async Task PlayAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("PlayAsync()");
        await GetLocator().EvaluateAsync("a => a.play()");
    }

    public virtual Task PauseAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Position & Duration

    public virtual Task<double> GetCurrentTimeAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<double> GetDurationAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task SeekToAsync(double? seconds, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Volume

    public virtual Task<double> GetVolumeAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task SetVolumeAsync(double? volume, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<bool> IsMutedAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.3 AsyncIFrameControlBase

```csharp
public abstract class AsyncIFrameControlBase : AsyncControlObjectBase
{
    protected AsyncIFrameControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncIFrameControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Source (Example: GetSourceAsync)

    public virtual async Task<string?> GetSourceAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().GetAttributeAsync("src");
    }

    public virtual Task AssertSourceAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Frame Content (Example: GetFrameLocatorAsync)

    public virtual async Task<IFrameLocator> GetFrameLocatorAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return GetLocator().FrameLocator(":scope");
    }

    public virtual Task<string?> GetFrameTitleAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Loaded State (Example: IsLoadedAsync)

    public virtual async Task<bool> IsLoadedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        // Check if iframe content is accessible
        try
        {
            var frame = GetLocator().FrameLocator(":scope");
            await frame.Locator("body").WaitForAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
            return true;
        }
        catch { return false; }
    }

    public virtual Task WaitLoadedAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.4 Concrete Blazor Controls

```csharp
/// <summary>HTML video element.</summary>
public class VideoControl : AsyncVideoControlBase
{
    public VideoControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public VideoControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>HTML audio element.</summary>
public class AudioControl : AsyncAudioControlBase
{
    public AudioControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public AudioControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>HTML iframe element.</summary>
public class IFrameControl : AsyncIFrameControlBase
{
    public IFrameControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public IFrameControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>HTML canvas element.</summary>
public class CanvasControl : AsyncControlObjectBase
{
    public CanvasControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public CanvasControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    #region Dimensions (Example: GetDimensionsAsync)

    public virtual async Task<(int width, int height)> GetDimensionsAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var width = await GetLocator().EvaluateAsync<int>("c => c.width");
        var height = await GetLocator().EvaluateAsync<int>("c => c.height");
        return (width, height);
    }

    #endregion

    #region Screenshot (Example: ToDataUrlAsync)

    public virtual async Task<string> ToDataUrlAsync(string? type = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        type ??= "image/png";
        return await GetLocator().EvaluateAsync<string>($"c => c.toDataURL('{type}')");
    }

    #endregion
}
```

---

## 3. Inheritance Summary

```
MAUI:
MediaElementControlBase : ControlObjectBase, IMediaControlObject
└── MediaElementControl

WebViewControlBase : ControlObjectBase, IWebViewControlObject
└── WebViewControl

Blazor:
AsyncVideoControlBase : AsyncControlObjectBase
└── VideoControl

AsyncAudioControlBase : AsyncControlObjectBase
└── AudioControl

AsyncIFrameControlBase : AsyncControlObjectBase
└── IFrameControl

CanvasControl : AsyncControlObjectBase
```

---

**Next:** [SPEC-006-003b-PAGE](SPEC-006-003b-PAGE.md)

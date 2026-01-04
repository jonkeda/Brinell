using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Video control for Blazor.
/// Wraps &lt;video&gt; elements.
/// </summary>
public class VideoControl : AsyncClickableControlBase
{
    /// <summary>
    /// Creates a new Video control.
    /// </summary>
    public VideoControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Video control using TestId.
    /// </summary>
    public VideoControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    #region Playback Control

    /// <summary>
    /// Plays the video.
    /// </summary>
    public virtual async Task PlayAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("PlayAsync()");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync("video => video.play()");
    }

    /// <summary>
    /// Pauses the video.
    /// </summary>
    public virtual async Task PauseAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("PauseAsync()");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync("video => video.pause()");
    }

    /// <summary>
    /// Gets whether the video is playing.
    /// </summary>
    public virtual async Task<bool> IsPlayingAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<bool>("video => !video.paused && !video.ended");
    }

    /// <summary>
    /// Gets whether the video is paused.
    /// </summary>
    public virtual async Task<bool> IsPausedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<bool>("video => video.paused");
    }

    /// <summary>
    /// Gets whether the video has ended.
    /// </summary>
    public virtual async Task<bool> IsEndedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<bool>("video => video.ended");
    }

    #endregion

    #region Time Control

    /// <summary>
    /// Gets the current playback time in seconds.
    /// </summary>
    public virtual async Task<double> GetCurrentTimeAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<double>("video => video.currentTime");
    }

    /// <summary>
    /// Sets the current playback time in seconds.
    /// </summary>
    public virtual async Task SeekAsync(double seconds, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"SeekAsync({seconds})");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync($"video => video.currentTime = {seconds}");
    }

    /// <summary>
    /// Gets the total duration of the video in seconds.
    /// </summary>
    public virtual async Task<double> GetDurationAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<double>("video => video.duration");
    }

    #endregion

    #region Volume Control

    /// <summary>
    /// Gets the current volume (0-1).
    /// </summary>
    public virtual async Task<double> GetVolumeAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<double>("video => video.volume");
    }

    /// <summary>
    /// Sets the volume (0-1).
    /// </summary>
    public virtual async Task SetVolumeAsync(double volume, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"SetVolumeAsync({volume})");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync($"video => video.volume = {Math.Clamp(volume, 0, 1)}");
    }

    /// <summary>
    /// Gets whether the video is muted.
    /// </summary>
    public virtual async Task<bool> IsMutedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<bool>("video => video.muted");
    }

    /// <summary>
    /// Mutes the video.
    /// </summary>
    public virtual async Task MuteAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("MuteAsync()");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync("video => video.muted = true");
    }

    /// <summary>
    /// Unmutes the video.
    /// </summary>
    public virtual async Task UnmuteAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("UnmuteAsync()");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync("video => video.muted = false");
    }

    #endregion

    #region Source

    /// <summary>
    /// Gets the video source URL.
    /// </summary>
    public virtual async Task<string?> GetSourceAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        // Try src attribute first, then currentSrc
        var src = await GetLocator().GetAttributeAsync("src");
        if (!string.IsNullOrEmpty(src))
            return src;

        return await GetLocator().EvaluateAsync<string>("video => video.currentSrc");
    }

    /// <summary>
    /// Gets the poster image URL.
    /// </summary>
    public virtual async Task<string?> GetPosterAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().GetAttributeAsync("poster");
    }

    #endregion

    #region Assertions

    /// <summary>
    /// Asserts the video is playing.
    /// </summary>
    public virtual async Task AssertPlayingAsync(string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (!await IsPlayingAsync(timeoutMs, ct))
        {
            throw new AssertionException(
                message ?? "Expected video to be playing",
                Locator.Value,
                "AssertPlaying");
        }
    }

    /// <summary>
    /// Asserts the video is paused.
    /// </summary>
    public virtual async Task AssertPausedAsync(string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (!await IsPausedAsync(timeoutMs, ct))
        {
            throw new AssertionException(
                message ?? "Expected video to be paused",
                Locator.Value,
                "AssertPaused");
        }
    }

    #endregion
}

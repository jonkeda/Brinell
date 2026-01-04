using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Audio control for Blazor.
/// Wraps &lt;audio&gt; elements.
/// </summary>
public class AudioControl : AsyncClickableControlBase
{
    /// <summary>
    /// Creates a new Audio control.
    /// </summary>
    public AudioControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Audio control using TestId.
    /// </summary>
    public AudioControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    #region Playback Control

    /// <summary>
    /// Plays the audio.
    /// </summary>
    public virtual async Task PlayAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("PlayAsync()");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync("audio => audio.play()");
    }

    /// <summary>
    /// Pauses the audio.
    /// </summary>
    public virtual async Task PauseAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("PauseAsync()");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync("audio => audio.pause()");
    }

    /// <summary>
    /// Gets whether the audio is playing.
    /// </summary>
    public virtual async Task<bool> IsPlayingAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<bool>("audio => !audio.paused && !audio.ended");
    }

    /// <summary>
    /// Gets whether the audio is paused.
    /// </summary>
    public virtual async Task<bool> IsPausedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<bool>("audio => audio.paused");
    }

    /// <summary>
    /// Gets whether the audio has ended.
    /// </summary>
    public virtual async Task<bool> IsEndedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<bool>("audio => audio.ended");
    }

    #endregion

    #region Time Control

    /// <summary>
    /// Gets the current playback time in seconds.
    /// </summary>
    public virtual async Task<double> GetCurrentTimeAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<double>("audio => audio.currentTime");
    }

    /// <summary>
    /// Sets the current playback time in seconds.
    /// </summary>
    public virtual async Task SeekAsync(double seconds, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"SeekAsync({seconds})");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync($"audio => audio.currentTime = {seconds}");
    }

    /// <summary>
    /// Gets the total duration of the audio in seconds.
    /// </summary>
    public virtual async Task<double> GetDurationAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<double>("audio => audio.duration");
    }

    #endregion

    #region Volume Control

    /// <summary>
    /// Gets the current volume (0-1).
    /// </summary>
    public virtual async Task<double> GetVolumeAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<double>("audio => audio.volume");
    }

    /// <summary>
    /// Sets the volume (0-1).
    /// </summary>
    public virtual async Task SetVolumeAsync(double volume, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"SetVolumeAsync({volume})");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync($"audio => audio.volume = {Math.Clamp(volume, 0, 1)}");
    }

    /// <summary>
    /// Gets whether the audio is muted.
    /// </summary>
    public virtual async Task<bool> IsMutedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<bool>("audio => audio.muted");
    }

    /// <summary>
    /// Mutes the audio.
    /// </summary>
    public virtual async Task MuteAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("MuteAsync()");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync("audio => audio.muted = true");
    }

    /// <summary>
    /// Unmutes the audio.
    /// </summary>
    public virtual async Task UnmuteAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("UnmuteAsync()");
        await CheckExistsAsync(true, timeoutMs, ct);
        await GetLocator().EvaluateAsync("audio => audio.muted = false");
    }

    #endregion

    #region Source

    /// <summary>
    /// Gets the audio source URL.
    /// </summary>
    public virtual async Task<string?> GetSourceAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        // Try src attribute first, then currentSrc
        var src = await GetLocator().GetAttributeAsync("src");
        if (!string.IsNullOrEmpty(src))
            return src;

        return await GetLocator().EvaluateAsync<string>("audio => audio.currentSrc");
    }

    #endregion

    #region Assertions

    /// <summary>
    /// Asserts the audio is playing.
    /// </summary>
    public virtual async Task AssertPlayingAsync(string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (!await IsPlayingAsync(timeoutMs, ct))
        {
            throw new AssertionException(
                message ?? "Expected audio to be playing",
                Locator.Value,
                "AssertPlaying");
        }
    }

    /// <summary>
    /// Asserts the audio is paused.
    /// </summary>
    public virtual async Task AssertPausedAsync(string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (!await IsPausedAsync(timeoutMs, ct))
        {
            throw new AssertionException(
                message ?? "Expected audio to be paused",
                Locator.Value,
                "AssertPaused");
        }
    }

    #endregion
}

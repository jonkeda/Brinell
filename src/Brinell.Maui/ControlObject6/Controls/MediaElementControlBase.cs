using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for media element controls in MAUI.
/// </summary>
public abstract class MediaElementControlBase : ControlObjectBase, IMediaControlObject
{
    /// <summary>
    /// Creates a new media element control.
    /// </summary>
    protected MediaElementControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new media element control using AutomationId.
    /// </summary>
    protected MediaElementControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    #region Playback State

    /// <inheritdoc/>
    public virtual bool IsPlaying(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var state = element.GetAttribute("CurrentState");
        var result = state == "Playing";
        Log($"IsPlaying: {result}");
        return result;
    }

    /// <inheritdoc/>
    public virtual bool WaitPlaying(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            if (IsPlaying(timeoutMs) == expected.Value)
                return true;

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertPlaying(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = IsPlaying(timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected media to be {(expected.Value ? "playing" : "not playing")} but was {(actual ? "playing" : "not playing")}";
            throw new AssertionException(msg, Locator.Value, "AssertPlaying");
        }
    }

    /// <inheritdoc/>
    public virtual bool IsPaused(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var state = element.GetAttribute("CurrentState");
        var result = state == "Paused";
        Log($"IsPaused: {result}");
        return result;
    }

    /// <inheritdoc/>
    public virtual bool IsStopped(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var state = element.GetAttribute("CurrentState");
        var result = state == "Stopped" || state == "None";
        Log($"IsStopped: {result}");
        return result;
    }

    #endregion

    #region Playback Controls

    /// <inheritdoc/>
    public virtual void Play(int? timeoutMs = null)
    {
        Log("Play()");
        if (!IsPlaying(timeoutMs))
        {
            var element = FindElementRequired(timeoutMs);
            element.SendKeys(" ");
        }
    }

    /// <inheritdoc/>
    public virtual void Pause(int? timeoutMs = null)
    {
        Log("Pause()");
        if (IsPlaying(timeoutMs))
        {
            var element = FindElementRequired(timeoutMs);
            element.SendKeys(" ");
        }
    }

    /// <inheritdoc/>
    public virtual void Stop(int? timeoutMs = null)
    {
        Log("Stop()");
        Pause(timeoutMs);
        SeekTo(TimeSpan.Zero, timeoutMs);
    }

    /// <inheritdoc/>
    public virtual void TogglePlayPause(int? timeoutMs = null)
    {
        Log("TogglePlayPause()");
        var element = FindElementRequired(timeoutMs);
        element.SendKeys(" ");
    }

    #endregion

    #region Position & Duration

    /// <inheritdoc/>
    public virtual TimeSpan GetPosition(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var position = element.GetAttribute("Position");
        if (TimeSpan.TryParse(position, out var p))
        {
            Log($"GetPosition: {p}");
            return p;
        }
        return TimeSpan.Zero;
    }

    /// <inheritdoc/>
    public virtual TimeSpan GetDuration(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var duration = element.GetAttribute("Duration");
        if (TimeSpan.TryParse(duration, out var d))
        {
            Log($"GetDuration: {d}");
            return d;
        }
        return TimeSpan.Zero;
    }

    /// <inheritdoc/>
    public virtual double GetPositionPercent(int? timeoutMs = null)
    {
        var position = GetPosition(timeoutMs);
        var duration = GetDuration(timeoutMs);
        if (duration.TotalSeconds > 0)
        {
            return (position.TotalSeconds / duration.TotalSeconds) * 100;
        }
        return 0;
    }

    /// <inheritdoc/>
    public virtual void SeekTo(TimeSpan? position, int? timeoutMs = null)
    {
        if (position is null) return;
        Log($"SeekTo({position})");
    }

    /// <inheritdoc/>
    public virtual void SeekToPercent(double? percent, int? timeoutMs = null)
    {
        if (percent is null) return;
        Log($"SeekToPercent({percent})");
        var duration = GetDuration(timeoutMs);
        var position = TimeSpan.FromSeconds(duration.TotalSeconds * (percent.Value / 100));
        SeekTo(position, timeoutMs);
    }

    #endregion

    #region Volume

    /// <inheritdoc/>
    public virtual double GetVolume(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var volume = element.GetAttribute("Volume");
        if (double.TryParse(volume, out var v))
        {
            Log($"GetVolume: {v}");
            return v;
        }
        return 1.0;
    }

    /// <inheritdoc/>
    public virtual void SetVolume(double? volume, int? timeoutMs = null)
    {
        if (volume is null) return;
        Log($"SetVolume({volume})");
    }

    /// <inheritdoc/>
    public virtual bool IsMuted(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var muted = element.GetAttribute("IsMuted");
        var result = muted == "True" || muted == "true";
        Log($"IsMuted: {result}");
        return result;
    }

    /// <inheritdoc/>
    public virtual void Mute(int? timeoutMs = null)
    {
        Log("Mute()");
        if (!IsMuted(timeoutMs))
        {
            ToggleMute(timeoutMs);
        }
    }

    /// <inheritdoc/>
    public virtual void Unmute(int? timeoutMs = null)
    {
        Log("Unmute()");
        if (IsMuted(timeoutMs))
        {
            ToggleMute(timeoutMs);
        }
    }

    /// <inheritdoc/>
    public virtual void ToggleMute(int? timeoutMs = null)
    {
        Log("ToggleMute()");
        var element = FindElementRequired(timeoutMs);
        element.SendKeys("m");
    }

    #endregion

    #region Source

    /// <inheritdoc/>
    public virtual string? GetSource(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var source = element.GetAttribute("Source");
        Log($"GetSource: {source}");
        return source;
    }

    /// <inheritdoc/>
    public virtual void AssertSource(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSource(timeoutMs);
        if (actual != expected)
        {
            var msg = message ?? $"Expected source '{expected}' but was '{actual}'";
            throw new AssertionException(msg, Locator.Value, "AssertSource");
        }
    }

    #endregion
}

namespace Brinell.Maui.Controls.Media;

/// <summary>
/// MAUI MediaElement control for audio/video playback.
/// Provides methods for media playback control and state inspection.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiMediaElementControl<TScope> : MauiControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a MediaElement control with locator.
    /// </summary>
    public MauiMediaElementControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a MediaElement control with automation ID.
    /// </summary>
    public MauiMediaElementControl(IMauiScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    #region Media State Methods

    /// <summary>
    /// Checks if the media is currently playing.
    /// </summary>
    /// <returns>True if playing, false otherwise, null if unknown.</returns>
    public bool? IsPlaying()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var state = element.GetAttribute("CurrentState");
        if (!string.IsNullOrEmpty(state))
        {
            return state.Equals("Playing", StringComparison.OrdinalIgnoreCase);
        }

        return null;
    }

    /// <summary>
    /// Checks if the media is paused.
    /// </summary>
    /// <returns>True if paused, false otherwise, null if unknown.</returns>
    public bool? IsPaused()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var state = element.GetAttribute("CurrentState");
        if (!string.IsNullOrEmpty(state))
        {
            return state.Equals("Paused", StringComparison.OrdinalIgnoreCase);
        }

        return null;
    }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    /// <returns>The current state string, or null if not available.</returns>
    public string? GetPlaybackState()
    {
        var element = TryFindElement();
        if (element == null) return null;

        return element.GetAttribute("CurrentState");
    }

    /// <summary>
    /// Gets the current playback position in seconds.
    /// </summary>
    /// <returns>The position in seconds, or null if not available.</returns>
    public double? GetPosition()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var attr = element.GetAttribute("Position");
        if (!string.IsNullOrEmpty(attr))
        {
            // Position is typically in TimeSpan format or ticks
            if (TimeSpan.TryParse(attr, out var ts))
            {
                return ts.TotalSeconds;
            }

            if (double.TryParse(attr, out var val))
            {
                return val;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the total duration of the media in seconds.
    /// </summary>
    /// <returns>The duration in seconds, or null if not available.</returns>
    public double? GetDuration()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var attr = element.GetAttribute("Duration");
        if (!string.IsNullOrEmpty(attr))
        {
            if (TimeSpan.TryParse(attr, out var ts))
            {
                return ts.TotalSeconds;
            }

            if (double.TryParse(attr, out var val))
            {
                return val;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the current volume level (0.0 to 1.0).
    /// </summary>
    /// <returns>The volume level, or null if not available.</returns>
    public double? GetVolume()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var attr = element.GetAttribute("Volume");
        if (!string.IsNullOrEmpty(attr) && double.TryParse(attr, out var val))
        {
            return val;
        }

        return null;
    }

    /// <summary>
    /// Checks if the media is muted.
    /// </summary>
    /// <returns>True if muted, false otherwise, null if unknown.</returns>
    public bool? IsMuted()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var attr = element.GetAttribute("IsMuted");
        if (!string.IsNullOrEmpty(attr))
        {
            return attr.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return null;
    }

    #endregion

    #region Assertions

    /// <summary>
    /// Asserts that the media is currently playing.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertPlaying(string? message = null)
    {
        return RunAssert(nameof(AssertPlaying), true, () => IsPlaying(), message);
    }

    /// <summary>
    /// Asserts that the media is paused.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertPaused(string? message = null)
    {
        return RunAssert(nameof(AssertPaused), true, () => IsPaused(), message);
    }

    #endregion
}

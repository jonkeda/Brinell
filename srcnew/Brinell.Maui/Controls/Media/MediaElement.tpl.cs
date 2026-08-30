namespace Brinell.Maui.Controls.Media;

/// <summary>
/// MAUI MediaElement control for audio/video playback.
/// Provides methods for media playback control and state inspection.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class MediaElement<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a MediaElement control with locator.
    /// </summary>
    public MediaElement(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a MediaElement control with automation ID.
    /// </summary>
    public MediaElement(IMauiScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Checks if the media is currently playing.
    /// </summary>
    /// <remarks>
    /// Null when the platform reports no state at all — "unknown" is a different answer from
    /// "not playing", and a test asserting the latter should not pass on the former.
    /// </remarks>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if playing, false if not, null if unknown.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsPlayingCore(IMauiElement? element)
        => MatchesState(element, "Playing");

    /// <summary>
    /// Checks if the media is paused.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if paused, false if not, null if unknown.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsPausedCore(IMauiElement? element)
        => MatchesState(element, "Paused");

    /// <summary>
    /// Checks if the media is muted.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if muted, false if not, null if unknown.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsMutedCore(IMauiElement? element)
    {
        var attr = element?.GetAttribute("IsMuted");
        if (string.IsNullOrEmpty(attr)) return null;

        return attr.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The current state string, or null if not available.</returns>
    [AbsenceTolerant]
    protected virtual string? GetPlaybackStateCore(IMauiElement? element)
        => element?.GetAttribute("CurrentState");

    /// <summary>
    /// Gets the current playback position in seconds.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The position in seconds, or null if not available.</returns>
    [AbsenceTolerant]
    protected virtual double? GetPositionCore(IMauiElement? element)
        => ReadSeconds(element, "Position");

    /// <summary>
    /// Gets the total duration of the media in seconds.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The duration in seconds, or null if not available.</returns>
    [AbsenceTolerant]
    protected virtual double? GetDurationCore(IMauiElement? element)
        => ReadSeconds(element, "Duration");

    /// <summary>
    /// Gets the current volume level (0.0 to 1.0).
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The volume level, or null if not available.</returns>
    [AbsenceTolerant]
    protected virtual double? GetVolumeCore(IMauiElement? element)
    {
        var attr = element?.GetAttribute("Volume");
        if (!string.IsNullOrEmpty(attr) && double.TryParse(attr, out var val))
        {
            return val;
        }

        return null;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Whether the element's <c>CurrentState</c> equals the given state.
    /// </summary>
    /// <remarks>
    /// Playing and paused are two readings of one attribute, so they share this rather than
    /// repeating the lookup and the null handling.
    /// </remarks>
    private static bool? MatchesState(IMauiElement? element, string state)
    {
        var actual = element?.GetAttribute("CurrentState");
        if (string.IsNullOrEmpty(actual)) return null;

        return actual.Equals(state, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads a time-valued attribute as seconds.
    /// </summary>
    /// <remarks>
    /// Platforms report these either as a TimeSpan or as a bare number, so both are accepted;
    /// position and duration share the parsing.
    /// </remarks>
    private static double? ReadSeconds(IMauiElement? element, string attributeName)
    {
        var attr = element?.GetAttribute(attributeName);
        if (string.IsNullOrEmpty(attr)) return null;

        if (TimeSpan.TryParse(attr, out var ts))
        {
            return ts.TotalSeconds;
        }

        if (double.TryParse(attr, out var val))
        {
            return val;
        }

        return null;
    }

    #endregion
}

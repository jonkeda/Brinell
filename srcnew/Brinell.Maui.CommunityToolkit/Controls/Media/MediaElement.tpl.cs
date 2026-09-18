using System.Text.RegularExpressions;
using Brinell.Core.Utilities;

namespace Brinell.Maui.CommunityToolkit.Controls.Media;

/// <summary>
/// CommunityToolkit.Maui <c>MediaElement</c>: audio and video playback with platform transport
/// controls.
/// </summary>
/// <remarks>
/// <para>
/// <b>Driven and read through its transport controls, not through the element.</b> On Windows the
/// media element is not in the tree by its AutomationId, but the WinUI transport controls are,
/// flattened into the page: <c>PlayPauseButton</c> answers Invoke, the seek slider publishes
/// RangeValue, and the elapsed and remaining times are text. Everything here goes through those,
/// with no bridge.
/// </para>
/// <para>
/// <b>Not the toolkit's <c>CurrentState</c>.</b> Measured on toolkit 10.0.0 / Windows: after the
/// transport controls start playback, <c>CurrentState</c> stays <c>Opening</c> and
/// <c>StateChanged</c> never fires, while the player plays. A state read from the app would be
/// wrong, so this control reads what the user sees. See <c>.my/communitytoolkit/probe.md</c>.
/// </para>
/// <para>
/// <b>Limits.</b> One media element per scope on Windows: the transport ids are fixed, so put each
/// media element in its own container. Playback controls must be shown
/// (<c>ShouldShowPlaybackControls</c>). <c>IsPlaying</c> reads the play/pause button's name, which
/// Windows localizes; it matches the English "Pause". Android publishes none of these ids yet.
/// </para>
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class MediaElement<TScope> : Brinell.Maui.Controls.Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    private const string PlayPauseButtonId = "PlayPauseButton";
    private const string TimeElapsedId = "TimeElapsedElement";
    private const string TimeRemainingId = "TimeRemainingElement";
    private const string ProgressSliderId = "ProgressSlider";

    private static readonly Regex Clock = new(@"(\d+):(\d{2}):(\d{2})\s*$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Creates a media element control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the media element.</param>
    public MediaElement(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a media element control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID).</param>
    public MediaElement(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Element Finding

    /// <summary>
    /// The element by its locator where the platform shows it; otherwise its play/pause button,
    /// which stands in for it as the Stepper's buttons stand in for a Stepper.
    /// </summary>
    protected override IMauiElement? TryFindElement()
        => base.TryFindElement() ?? Part(PlayPauseButtonId);

    /// <inheritdoc />
    protected override IMauiElement FindElement()
        => TryFindElement()
           ?? throw new ElementNotFoundException(
               $"MediaElement was not found by '{Locator}' or by its '{PlayPauseButtonId}'. The "
               + "transport controls must be shown (ShouldShowPlaybackControls).");

    #endregion

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Reads whether media is playing: the transport shows "Pause" while it plays.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True while playing, null where there is no play/pause button.</returns>
    protected virtual bool? IsPlayingCore(IMauiElement? element)
        => element == null
            ? null
            : Part(PlayPauseButtonId)?.Name is { } name
                ? name.StartsWith("Pause", StringComparison.OrdinalIgnoreCase)
                : null;

    /// <summary>
    /// Reads how far playback has got, from 0 to 100, from the seek slider's RangeValue pattern.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The progress, or null where there is no seek slider.</returns>
    protected virtual double? GetProgressCore(IMauiElement? element)
        => element == null ? null : Part(ProgressSliderId)?.RangeValue;

    /// <summary>
    /// Reads the media's length in whole seconds: elapsed plus remaining.
    /// </summary>
    /// <remarks>
    /// Read while the player is not running: during playback Windows drops the clock from the
    /// times' names (it stays only in their text pattern, which the driver does not read), and this
    /// answers null.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The duration, or null while either time shows no clock.</returns>
    protected virtual double? GetDurationCore(IMauiElement? element)
        => element == null ? null : ReadClock(TimeElapsedId) + ReadClock(TimeRemainingId);

    /// <summary>
    /// Starts playback through the transport controls. No-op when already playing.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void PlayCore(IMauiElement element, int? timeoutMs = null)
        => SetPlayingCore(element, true, timeoutMs);

    /// <summary>
    /// Pauses playback through the transport controls. No-op when not playing.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void PauseCore(IMauiElement element, int? timeoutMs = null)
        => SetPlayingCore(element, false, timeoutMs);

    /// <summary>
    /// Presses play/pause when the state differs, then waits for the transport to show the change.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="playing">True to play, false to pause. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SetPlayingCore(IMauiElement element, bool? playing, int? timeoutMs = null)
    {
        if (playing == null || IsPlayingCore(element) == playing)
        {
            return;
        }

        var button = Part(PlayPauseButtonId)
            ?? throw new NotSupportedException(
                $"MediaElement '{Locator.Value}' has no '{PlayPauseButtonId}' in this scope. The "
                + "transport controls must be shown, and on Android they publish no id this control "
                + "can find.");

        // A press before the media has opened is dropped by the player: failed once under the full
        // suite, where the page was pressed straight after it arrived. The transport shows a
        // duration once the media is open, so wait for that first.
        if (playing.Value
            && !WaitHelper.WaitFor(
                () => GetDurationCore(element),
                duration => duration > 0,
                timeoutMs: timeoutMs ?? DefaultTimeoutMs,
                pollingIntervalMs: PollingIntervalMs))
        {
            throw new TimeoutException(
                $"MediaElement '{Locator.Value}' never showed a duration, so its media did not open.");
        }

        button.Invoke();

        if (!WaitHelper.WaitFor(
                () => IsPlayingCore(element),
                actual => actual == playing,
                timeoutMs: timeoutMs ?? DefaultTimeoutMs,
                pollingIntervalMs: PollingIntervalMs))
        {
            throw new TimeoutException(
                $"MediaElement '{Locator.Value}' did not {(playing.Value ? "start playing" : "pause")}.");
        }
    }

    #endregion

    #region Hand-written Members

    // Hand-written: generated waits compare for equality, and playing progress or a loading
    // duration is only ever "past" a value, never equal to one.

    /// <summary>
    /// Waits until playback progress, from 0 to 100, is past the given value.
    /// </summary>
    /// <param name="percent">The progress to pass.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True when progress passed it within the timeout.</returns>
    public bool WaitProgressPasses(double percent, int? timeoutMs = null)
        => RunWait(() => GetProgressCore(TryFindElement()) > percent, timeoutMs);

    /// <summary>
    /// Waits until the media has opened far enough to show a non-zero duration.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True when the duration became known within the timeout.</returns>
    public bool WaitDurationKnown(int? timeoutMs = null)
        => RunWait(() => GetDurationCore(TryFindElement()) > 0, timeoutMs);

    #endregion

    #region Helpers

    private IMauiElement? Part(string automationId)
        => MauiScope.TryFindElement(Locator.ByAutomationId(automationId));

    /// <summary>
    /// The trailing h:mm:ss of a transport time, in seconds. The label drops its clock for a moment
    /// while it updates, which reads as null rather than zero.
    /// </summary>
    private double? ReadClock(string automationId)
    {
        if (Part(automationId)?.Name is not { } name || Clock.Match(name) is not { Success: true } match)
        {
            return null;
        }

        return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) * 3600
               + int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) * 60
               + int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
    }

    #endregion
}

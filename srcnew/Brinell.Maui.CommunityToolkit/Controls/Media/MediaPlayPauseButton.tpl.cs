using Brinell.Maui.Controls.Buttons;

namespace Brinell.Maui.CommunityToolkit.Controls.Media;

/// <summary>
/// The media transport's play/pause button: one button whose name says what pressing it will do.
/// </summary>
/// <remarks>
/// <para>
/// <b>Windows</b>: the WinUI transport's <c>PlayPauseButton</c> answers Invoke and is named
/// "Play" while paused and "Pause" while playing. The name is localized; this matches English.
/// </para>
/// <para>
/// The button is enabled before the media has opened, and a press made then is dropped by the
/// player (probed 2026-09-18: enabled at 311 ms, media opened at 393 ms). This control cannot see
/// the media's state, so a component that owns this button waits for the media first; see
/// <see cref="MediaElement{TScope}"/>.
/// </para>
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class MediaPlayPauseButton<TScope> : Button<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a play/pause button within the specified scope.
    /// </summary>
    /// <param name="scope">The scope providing element finding.</param>
    /// <param name="locator">The locator for the button.</param>
    public MediaPlayPauseButton(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a play/pause button within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID).</param>
    public MediaPlayPauseButton(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Reads whether media is playing: the button offers "Pause" while it plays.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True while playing, false while not, null when the button has no name.</returns>
    protected virtual bool? IsPlayingCore(IMauiElement? element)
        => element?.Name is { Length: > 0 } name
            ? name.StartsWith("Pause", StringComparison.OrdinalIgnoreCase)
            : null;

    /// <summary>
    /// Starts playback. No-op when already playing.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void PlayCore(IMauiElement element, int? timeoutMs = null)
        => SetPlayingCore(element, true, timeoutMs);

    /// <summary>
    /// Pauses playback. No-op when not playing.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void PauseCore(IMauiElement element, int? timeoutMs = null)
        => SetPlayingCore(element, false, timeoutMs);

    /// <summary>
    /// Presses the button when the state differs, then waits for its name to show the change.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="playing">True to play, false to pause. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <exception cref="TimeoutException">The press was accepted and the state did not change.</exception>
    protected virtual void SetPlayingCore(IMauiElement element, bool? playing, int? timeoutMs = null)
    {
        if (playing == null || IsPlayingCore(element) == playing)
        {
            return;
        }

        ClickCore(element, timeoutMs);

        if (!Until(() => IsPlayingCore(element), actual => actual == playing, timeoutMs, out var lastError))
        {
            throw new TimeoutException(
                $"'{Locator.Value}' was pressed and did not {(playing.Value ? "start playing" : "pause")}.",
                lastError);
        }
    }

    #endregion
}

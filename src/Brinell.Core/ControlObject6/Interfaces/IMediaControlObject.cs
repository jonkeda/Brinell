namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for media controls (video/audio players).
/// </summary>
public interface IMediaControlObject : IControlObject
{
    /// <summary>
    /// Gets whether the media is currently playing.
    /// </summary>
    bool IsPlaying(int? timeoutMs = null);

    /// <summary>
    /// Waits for the playing state to match the expected value.
    /// </summary>
    bool WaitPlaying(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts the playing state matches the expected value.
    /// </summary>
    void AssertPlaying(bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets whether the media is paused.
    /// </summary>
    bool IsPaused(int? timeoutMs = null);

    /// <summary>
    /// Gets whether the media is stopped.
    /// </summary>
    bool IsStopped(int? timeoutMs = null);

    /// <summary>
    /// Starts playback.
    /// </summary>
    void Play(int? timeoutMs = null);

    /// <summary>
    /// Pauses playback.
    /// </summary>
    void Pause(int? timeoutMs = null);

    /// <summary>
    /// Stops playback.
    /// </summary>
    void Stop(int? timeoutMs = null);

    /// <summary>
    /// Toggles between play and pause.
    /// </summary>
    void TogglePlayPause(int? timeoutMs = null);

    /// <summary>
    /// Gets the current playback position.
    /// </summary>
    TimeSpan GetPosition(int? timeoutMs = null);

    /// <summary>
    /// Gets the total duration.
    /// </summary>
    TimeSpan GetDuration(int? timeoutMs = null);

    /// <summary>
    /// Gets the position as a percentage (0-100).
    /// </summary>
    double GetPositionPercent(int? timeoutMs = null);

    /// <summary>
    /// Seeks to the specified position.
    /// </summary>
    void SeekTo(TimeSpan? position, int? timeoutMs = null);

    /// <summary>
    /// Seeks to the specified percentage (0-100).
    /// </summary>
    void SeekToPercent(double? percent, int? timeoutMs = null);

    /// <summary>
    /// Gets the current volume (0-1).
    /// </summary>
    double GetVolume(int? timeoutMs = null);

    /// <summary>
    /// Sets the volume (0-1).
    /// </summary>
    void SetVolume(double? volume, int? timeoutMs = null);

    /// <summary>
    /// Gets whether the media is muted.
    /// </summary>
    bool IsMuted(int? timeoutMs = null);

    /// <summary>
    /// Mutes the media.
    /// </summary>
    void Mute(int? timeoutMs = null);

    /// <summary>
    /// Unmutes the media.
    /// </summary>
    void Unmute(int? timeoutMs = null);

    /// <summary>
    /// Toggles the mute state.
    /// </summary>
    void ToggleMute(int? timeoutMs = null);

    /// <summary>
    /// Gets the media source URL.
    /// </summary>
    string? GetSource(int? timeoutMs = null);

    /// <summary>
    /// Asserts the media source matches the expected value.
    /// </summary>
    void AssertSource(string? expected, string? message = null, int? timeoutMs = null);
}

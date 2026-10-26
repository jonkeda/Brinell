using System.Runtime.InteropServices;
using Windows.Media.SpeechSynthesis;

[assembly: Bruinet.Narrator.SpeechSynthesizer(typeof(Bruinet.Narrator.Windows.WindowsSpeech))]

namespace Bruinet.Narrator.Windows;

/// <summary>Windows' own voices (Windows.Media.SpeechSynthesis): offline, the ones in Settings, Time and language, Speech.</summary>
public sealed class WindowsSpeech : ISpeechSynthesizer
{
    /// <inheritdoc />
    public bool IsAvailable => TryVoices()?.Count > 0;

    /// <summary>The voices on this machine, by display name.</summary>
    public static IReadOnlyList<string> Voices => TryVoices()?.Select(voice => voice.DisplayName).ToList() ?? [];

    /// <inheritdoc />
    public async Task<TimeSpan> SynthesizeAsync(string text, string? voice, string wavPath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(wavPath);
        using var synthesizer = new SpeechSynthesizer();
        if (voice is not null)
        {
            synthesizer.Voice = SpeechSynthesizer.AllVoices.FirstOrDefault(candidate => candidate.DisplayName.Contains(voice, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"No voice '{voice}' here. The voices: {string.Join(", ", Voices)}.", nameof(voice));
        }

        using (var speech = await synthesizer.SynthesizeTextToStreamAsync(text).AsTask(cancellationToken).ConfigureAwait(false))
        await using (var file = File.Create(wavPath))
        {
            await speech.AsStreamForRead().CopyToAsync(file, cancellationToken).ConfigureAwait(false);
        }

        return Speech.WavDuration(wavPath);
    }

    /// <summary>Plays a clip aloud, to its end; for Speak aloud, while a recording waits for the line.</summary>
    /// <param name="clip">The line, with its WAV.</param>
    /// <param name="cancellationToken">Stops it.</param>
    public static async Task PlayAsync(SpeechClip clip, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (clip.WavPath is null)
        {
            return;
        }

        // The source is disposed too: it holds the file open, and the next clip may be written to it.
        using var source = global::Windows.Media.Core.MediaSource.CreateFromUri(new Uri(clip.WavPath));
        using var player = new global::Windows.Media.Playback.MediaPlayer { Source = source };
        var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        player.MediaEnded += (_, _) => ended.TrySetResult();
        player.MediaFailed += (_, failed) => ended.TrySetException(new InvalidOperationException($"The line could not be played: {failed.ErrorMessage}"));
        player.Play();
        using (cancellationToken.Register(() => ended.TrySetCanceled(cancellationToken)))
        {
            // The clip's own length bounds the wait: a player that never says it ended does not hold the run.
            await Task.WhenAny(ended.Task, Task.Delay(clip.Duration + TimeSpan.FromSeconds(1), cancellationToken)).ConfigureAwait(false);
        }
    }

    // No speech runtime (a server without the feature) means no voices, not a failed run.
    private static IReadOnlyList<VoiceInformation>? TryVoices()
    {
        try
        {
            return SpeechSynthesizer.AllVoices;
        }
        catch (COMException exception)
        {
            System.Diagnostics.Debug.WriteLine($"Speech: Windows' voices are not available ({exception.Message}).");
            return null;
        }
    }
}

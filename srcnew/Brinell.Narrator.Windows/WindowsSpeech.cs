using System.Runtime.InteropServices;
using Windows.Media.SpeechSynthesis;

[assembly: Brinell.Narrator.SpeechSynthesizer(typeof(Brinell.Narrator.Windows.WindowsSpeech))]

namespace Brinell.Narrator.Windows;

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

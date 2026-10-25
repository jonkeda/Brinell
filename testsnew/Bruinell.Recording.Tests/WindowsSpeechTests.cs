using Bruinell.Narrator;
using Bruinell.Narrator.Windows;
using Xunit;

namespace Bruinell.Recording.Tests;

public sealed class WindowsSpeechTests
{
    [Fact]
    public async Task A_line_is_spoken_into_a_wav_whose_length_is_known()
    {
        var speech = new WindowsSpeech();
        Assert.True(speech.IsAvailable, "Windows has no voices here: Settings, Time and language, Speech.");
        var path = Path.Combine(Path.GetTempPath(), $"bruinell-speech-{Guid.NewGuid():N}.wav");
        try
        {
            var duration = await speech.SynthesizeAsync("You tap Sync to fetch it.", voice: null, path, CancellationToken.None);

            Assert.InRange(duration.TotalSeconds, 0.8, 5);
            Assert.Equal(duration, Speech.WavDuration(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task An_unknown_voice_names_the_voices_there_are()
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(() => new WindowsSpeech().SynthesizeAsync("Hi.", "No Such Voice", Path.GetTempFileName(), CancellationToken.None));

        Assert.Contains(WindowsSpeech.Voices[0], error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Discover_finds_the_Windows_voice()
    {
        Assert.IsType<WindowsSpeech>(Speech.Discover());
    }
}

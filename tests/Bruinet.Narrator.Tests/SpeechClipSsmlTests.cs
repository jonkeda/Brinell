namespace Bruinet.Narrator.Tests;

/// <summary>A clip's words are the captions' words; a voice that follows SSML gets the markup, any other the reduced line.</summary>
public sealed class SpeechClipSsmlTests
{
    private const string Marked = "and save.<break time=\"400ms\"/> Welcome to <sub alias=\"Brew-ee-net\">Bruinet</sub>.";

    [Fact]
    public async Task AVoiceThatFollowsSsml_GetsTheLine_AndTheClipHoldsOnlyItsWords()
    {
        var voice = new SsmlVoice();

        var clip = await Narrator.Speech.ClipAsync(voice, Marked, "Zira", "a.wav", default);

        Assert.Equal("and save. Welcome to Bruinet.", clip.Text);
        Assert.Equal(["and save.<break time=\"400ms\"/> Welcome to <sub alias=\"Brew-ee-net\">Bruinet</sub>."], voice.Ssml);
        Assert.Empty(voice.Plain);
        Assert.Equal("Zira", voice.Voice);
    }

    [Fact]
    public async Task AVoiceThatDoesNot_GetsThePlainReducedLine_WithAliases()
    {
        var voice = new PlainVoice();

        var clip = await Narrator.Speech.ClipAsync(voice, Marked, null, "a.wav", default);

        Assert.Equal(["and save. Welcome to Brew-ee-net."], voice.Spoken);
        Assert.Equal("and save. Welcome to Bruinet.", clip.Text);
    }

    [Fact]
    public async Task APlainLine_GoesAsItIs_EvenToAVoiceThatFollowsSsml()
    {
        var ssml = new SsmlVoice();
        var plain = new PlainVoice();

        var a = await Narrator.Speech.ClipAsync(ssml, "Tom & Jerry x < 5.", null, "a.wav", default);
        var b = await Narrator.Speech.ClipAsync(plain, "Tom & Jerry x < 5.", null, "b.wav", default);

        Assert.Equal(["Tom & Jerry x < 5."], ssml.Plain);
        Assert.Empty(ssml.Ssml);
        Assert.Equal(["Tom & Jerry x < 5."], plain.Spoken);
        Assert.Equal("Tom & Jerry x < 5.", a.Text);
        Assert.Equal("Tom & Jerry x < 5.", b.Text);
    }

    [Fact]
    public async Task WithNoVoice_TheLengthIsEstimatedFromTheWordsNotTheTags()
    {
        var marked = await Narrator.Speech.ClipAsync(null, "one<break time=\"5s\"/> two three", null, "a.wav", default);
        var plain = await Narrator.Speech.ClipAsync(null, "one two three", null, "a.wav", default);

        Assert.Equal(plain.Duration, marked.Duration);
        Assert.Equal("one two three", marked.Text);
        Assert.Null(marked.WavPath);
    }

    private sealed class SsmlVoice : ISsmlSynthesizer
    {
        public List<string> Ssml { get; } = [];

        public List<string> Plain { get; } = [];

        public string? Voice { get; private set; }

        public bool IsAvailable => true;

        public IReadOnlySet<string> SupportedElements => SsmlSupport.Windows;

        public Task<TimeSpan> SynthesizeAsync(string text, string? voice, string wavPath, CancellationToken cancellationToken)
        {
            Plain.Add(text);
            return Task.FromResult(TimeSpan.FromSeconds(1));
        }

        public Task<TimeSpan> SynthesizeLineAsync(SpokenLine line, string? voice, string wavPath, CancellationToken cancellationToken)
        {
            Ssml.Add(line.ToSsml(SupportedElements));
            Voice = voice;
            return Task.FromResult(TimeSpan.FromSeconds(1));
        }
    }

    private sealed class PlainVoice : ISpeechSynthesizer
    {
        public List<string> Spoken { get; } = [];

        public bool IsAvailable => true;

        public Task<TimeSpan> SynthesizeAsync(string text, string? voice, string wavPath, CancellationToken cancellationToken)
        {
            Spoken.Add(text);
            return Task.FromResult(TimeSpan.FromSeconds(1));
        }
    }
}

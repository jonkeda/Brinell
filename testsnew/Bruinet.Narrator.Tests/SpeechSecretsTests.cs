using System.Text.Json;

namespace Bruinet.Narrator.Tests;

/// <summary>Azure Speech's settings: the <c>speech</c> object of voice.secrets.json, beside the Azure OpenAI voice's.</summary>
public sealed class SpeechSecretsTests : IDisposable
{
    private const string Key = "speech-key-987";
    private const string OpenAiKey = "openai-key-123";
    private const string OpenAiEndpoint = "https://voice.openai.azure.com";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "Bruinet", "SpeechSecretsTests", Guid.NewGuid().ToString("N"));
    private readonly string _home;
    private readonly string _suite;

    public SpeechSecretsTests()
    {
        _home = Path.Combine(_root, "home");
        _suite = Path.Combine(_root, "repo", "tests", "Suite");
        Directory.CreateDirectory(Path.Combine(_root, "repo", ".git"));
        Directory.CreateDirectory(_suite);
        Directory.CreateDirectory(_home);
    }

    private string SuiteFile => Path.Combine(_suite, VoiceSecrets.FileName);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void TheSpeechObject_IsFound_WithItsDefaults()
    {
        File.WriteAllText(SuiteFile, """{ "speech": { "endpoint": "eastus2", "key": "speech-key-987" } }""");

        var secrets = SpeechSecrets.Find(_suite, NoVariables, _home);

        Assert.NotNull(secrets);
        Assert.Equal("eastus2", secrets.Endpoint);
        Assert.Null(secrets.Voice);
        Assert.Equal(SpeechSecrets.DefaultSampleRate, secrets.SampleRate);
        Assert.Null(VoiceSecrets.Find(_suite, NoVariables, _home));
    }

    [Fact]
    public void TheEnvironment_WinsOverTheFile()
    {
        File.WriteAllText(SuiteFile, """{ "speech": { "endpoint": "eastus2", "key": "file-key", "voice": "nl-NL-FennaNeural" } }""");
        var agent = new Dictionary<string, string>
        {
            [SpeechSecrets.EndpointVariable] = "westeurope",
            [SpeechSecrets.KeyVariable] = "agent-key",
            [SpeechSecrets.VoiceVariable] = "en-GB-Emily:MAI-Voice-2.1",
            [SpeechSecrets.SampleRateVariable] = "48000",
        };

        var secrets = SpeechSecrets.Find(_suite, name => agent.GetValueOrDefault(name), _home);

        Assert.Equal(("westeurope", "en-GB-Emily:MAI-Voice-2.1", 48000), (secrets!.Endpoint, secrets.Voice, secrets.SampleRate));
    }

    [Theory]
    [InlineData("""{ "speech": { "endpoint": "eastus2" } }""")]
    [InlineData("""{ "speech": { "key": "speech-key-987" } }""")]
    [InlineData("""{ "speech": {} }""")]
    public void AHalfFilledSection_IsNotSetUp(string json)
    {
        File.WriteAllText(SuiteFile, json);

        Assert.Null(SpeechSecrets.Find(_suite, NoVariables, _home));
        Assert.False(SpeechSecrets.Describe(_suite, NoVariables, _home).IsSetUp);
    }

    [Theory]
    [InlineData("""{ "speech": { "endpoint": "eastus2", "key": "k", "sampleRate": 16000 } }""")]
    [InlineData("""{ "speech": { "endpoint": "eastus2", "key": "k", "sampleRate": "loud" } }""")]
    public void ASampleRate_OtherThan24000Or48000_IsRefused_NamingBoth(string json)
    {
        File.WriteAllText(SuiteFile, json);

        var failure = Assert.Throws<InvalidDataException>(() => SpeechSecrets.Find(_suite, NoVariables, _home));

        Assert.Contains("24000", failure.Message, StringComparison.Ordinal);
        Assert.Contains("48000", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSampleRate_CanBeANumberInTheFile_AndAVariable()
    {
        File.WriteAllText(SuiteFile, """{ "speech": { "endpoint": "eastus2", "key": "k", "sampleRate": 48000 } }""");
        Assert.Equal(48000, SpeechSecrets.Find(_suite, NoVariables, _home)!.SampleRate);

        var refused = Assert.Throws<InvalidDataException>(() => SpeechSecrets.Find(_suite, name => name == SpeechSecrets.SampleRateVariable ? "44100" : null, _home));
        Assert.Contains("44100", refused.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("eastus2", "https://eastus2.tts.speech.microsoft.com/cognitiveservices/v1", "https://eastus2.tts.speech.microsoft.com/cognitiveservices/voices/list")]
    [InlineData("EastUS2", "https://eastus2.tts.speech.microsoft.com/cognitiveservices/v1", "https://eastus2.tts.speech.microsoft.com/cognitiveservices/voices/list")]
    [InlineData("https://eastus2.tts.speech.microsoft.com", "https://eastus2.tts.speech.microsoft.com/cognitiveservices/v1", "https://eastus2.tts.speech.microsoft.com/cognitiveservices/voices/list")]
    [InlineData("https://res.cognitiveservices.azure.com", "https://res.cognitiveservices.azure.com/cognitiveservices/v1", "https://res.cognitiveservices.azure.com/tts/cognitiveservices/voices/list")]
    [InlineData("https://res.cognitiveservices.azure.com/speech/path?api-version=1", "https://res.cognitiveservices.azure.com/cognitiveservices/v1", "https://res.cognitiveservices.azure.com/tts/cognitiveservices/voices/list")]
    public void TheEndpoint_AsARegion_AResourceUrl_OrAPastedUrl_NamesTheSameHost(string endpoint, string synthesis, string voices)
    {
        var secrets = new SpeechSecrets(endpoint, Key);

        Assert.Equal(synthesis, secrets.SynthesisUrl);
        Assert.Equal(voices, secrets.VoicesUrl);
    }

    [Theory]
    [InlineData("http://res.cognitiveservices.azure.com")]
    [InlineData("not a url")]
    [InlineData("")]
    public void AnEndpointThatIsNeitherARegionNorAnHttpsUrl_HasAProblem(string endpoint)
        => Assert.NotNull(SpeechSecrets.EndpointProblem(endpoint));

    [Fact]
    public void SavingOneService_KeepsTheOther()
    {
        VoiceSecrets.Save(SuiteFile, OpenAiEndpoint, null, null, OpenAiKey, "coral");
        SpeechSecrets.Save(SuiteFile, "eastus2", Key, "en-GB-Emily:MAI-Voice-2.1", 48000);

        var openAi = VoiceSecrets.Find(_suite, NoVariables, _home)!;
        var speech = SpeechSecrets.Find(_suite, NoVariables, _home)!;
        Assert.Equal((OpenAiEndpoint, "coral"), (openAi.Endpoint, openAi.Voice));
        Assert.Equal(("eastus2", "en-GB-Emily:MAI-Voice-2.1", 48000), (speech.Endpoint, speech.Voice, speech.SampleRate));

        VoiceSecrets.Save(SuiteFile, "https://other.openai.azure.com", null, null, "", null);
        Assert.Equal("eastus2", SpeechSecrets.Find(_suite, NoVariables, _home)?.Endpoint);

        SpeechSecrets.Save(SuiteFile, "westeurope", "", null);
        Assert.Equal("https://other.openai.azure.com", VoiceSecrets.Find(_suite, NoVariables, _home)?.Endpoint);
        Assert.Equal(Key, SpeechSecrets.Find(_suite, NoVariables, _home)?.Key);
    }

    [Fact]
    public void RemovingOneService_KeepsTheOther_AndTheFileGoesWithTheLast()
    {
        VoiceSecrets.Save(SuiteFile, OpenAiEndpoint, null, null, OpenAiKey, null);
        SpeechSecrets.Save(SuiteFile, "eastus2", Key, null);

        VoiceSecrets.Delete(SuiteFile);
        Assert.Null(VoiceSecrets.Find(_suite, NoVariables, _home));
        Assert.NotNull(SpeechSecrets.Find(_suite, NoVariables, _home));

        VoiceSecrets.Save(SuiteFile, OpenAiEndpoint, null, null, OpenAiKey, null);
        SpeechSecrets.Delete(SuiteFile);
        Assert.Null(SpeechSecrets.Find(_suite, NoVariables, _home));
        Assert.NotNull(VoiceSecrets.Find(_suite, NoVariables, _home));

        VoiceSecrets.Delete(SuiteFile);
        Assert.False(File.Exists(SuiteFile));
        SpeechSecrets.Delete(SuiteFile);
    }

    [Fact]
    public void ASavedSection_HoldsTheDefaultsOnlyWhenTheyAreSet()
    {
        SpeechSecrets.Save(SuiteFile, "eastus2", Key, null);

        using var json = JsonDocument.Parse(File.ReadAllText(SuiteFile));
        var section = json.RootElement.GetProperty("speech");
        Assert.False(section.TryGetProperty("voice", out _));
        Assert.False(section.TryGetProperty("sampleRate", out _));
        Assert.Equal(SpeechSecrets.DefaultSampleRate, SpeechSecrets.Find(_suite, NoVariables, _home)!.SampleRate);
    }

    [Fact]
    public void SavingWithoutAKey_NeedsOneAlreadySaved_AndABadRateOrEndpointIsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => SpeechSecrets.Save(SuiteFile, "eastus2", "", null));
        Assert.Throws<InvalidOperationException>(() => SpeechSecrets.Save(SuiteFile, "eastus2", Key, null, 16000));
        Assert.Throws<InvalidOperationException>(() => SpeechSecrets.Save(SuiteFile, "http://insecure.example", Key, null));
        Assert.False(File.Exists(SuiteFile));
    }

    [Fact]
    public void TheKey_IsInNoDescriptionAndNoMessage()
    {
        SpeechSecrets.Save(SuiteFile, "eastus2", Key, null);

        Assert.DoesNotContain(Key, SpeechSecrets.Find(_suite, NoVariables, _home)!.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(Key, SpeechSecrets.Describe(_suite, NoVariables, _home).ToString(), StringComparison.Ordinal);
        File.WriteAllText(SuiteFile, "{ \"speech\": { \"key\": \"" + Key + "\", ");
        var broken = Assert.Throws<InvalidDataException>(() => SpeechSecrets.Find(_suite, NoVariables, _home));
        Assert.DoesNotContain(Key, broken.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDescription_SaysWhereEachValueIsFrom()
    {
        SpeechSecrets.Save(SuiteFile, "eastus2", Key, null);

        var fromFile = SpeechSecrets.Describe(_suite, NoVariables, _home);
        Assert.True(fromFile.IsSetUp);
        Assert.Equal(SuiteFile, fromFile.File);
        Assert.Empty(fromFile.FromEnvironment);

        var fromAgent = SpeechSecrets.Describe(_suite, name => name == SpeechSecrets.KeyVariable ? "agent-key" : null, _home);
        Assert.Equal([SpeechSecrets.KeyVariable], fromAgent.FromEnvironment);
    }

    private static string? NoVariables(string name) => null;
}

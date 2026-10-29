using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Bruinet.Narrator.Tests;

/// <summary>The Azure Speech voice: its request, its styles, its clips and its voices list. No test calls the real service.</summary>
public sealed class AzureSpeechTests : IDisposable
{
    private const string Key = "speech-key-987";
    private const string Emily = "azure-speech-service:en-GB-Emily:MAI-Voice-2.1";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "Bruinet", "AzureSpeechTests", Guid.NewGuid().ToString("N"));
    private readonly string _cache;
    private readonly string _home;
    private readonly string _suite;

    public AzureSpeechTests()
    {
        _cache = Path.Combine(_root, "cache");
        _home = Path.Combine(_root, "home");
        _suite = Path.Combine(_root, "repo", "tests", "Suite");
        Directory.CreateDirectory(Path.Combine(_root, "repo", ".git"));
        Directory.CreateDirectory(_suite);
        Directory.CreateDirectory(_home);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Theory]
    [InlineData("eastus2", "https://eastus2.tts.speech.microsoft.com/cognitiveservices/v1")]
    [InlineData("https://res.cognitiveservices.azure.com", "https://res.cognitiveservices.azure.com/cognitiveservices/v1")]
    [InlineData("https://eastus2.tts.speech.microsoft.com/cognitiveservices/v1?x=1", "https://eastus2.tts.speech.microsoft.com/cognitiveservices/v1")]
    public async Task ALine_IsPostedAsSsml_ToTheEndpointsHost_WithTheKeyInTheHeaderOnly(string endpoint, string url)
    {
        var service = new FakeService(Respond(Wav(1)));
        var voice = new AzureSpeech(new SpeechSecrets(endpoint, Key), service, _cache);

        await voice.SynthesizeAsync("Hello there", Emily, Output(), default);

        var sent = Assert.Single(service.Requests);
        Assert.Equal(url, sent.Url);
        Assert.Equal(HttpMethod.Post, sent.Method);
        Assert.Equal(Key, sent.Key);
        Assert.Equal("application/ssml+xml", sent.ContentType);
        Assert.Equal("riff-24khz-16bit-mono-pcm", sent.Format);
        Assert.Equal("Bruinet.Narrator", sent.UserAgent);
        Assert.DoesNotContain(Key, sent.Url + sent.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheVoiceNameGoesWhole_TheModelSuffixIncluded_AndTheLanguageIsItsFirstTwoParts()
    {
        var service = new FakeService(Respond(Wav(1)), Respond(Wav(1)));
        var voice = new AzureSpeech(new SpeechSecrets("eastus2", Key), service, _cache);

        await voice.SynthesizeAsync("Hello", Emily, Output(), default);
        await voice.SynthesizeAsync("Hallo", "azure-speech-service:nl-NL-FennaNeural", Output(), default);

        Assert.Contains("<voice name=\"en-GB-Emily:MAI-Voice-2.1\">", service.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("xml:lang=\"en-GB\"", service.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("<voice name=\"nl-NL-FennaNeural\">", service.Requests[1].Body, StringComparison.Ordinal);
        Assert.Contains("xml:lang=\"nl-NL\"", service.Requests[1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithNoVoiceNamed_TheSettingsVoiceSpeaks_ElseEmily()
    {
        var service = new FakeService(Respond(Wav(1)), Respond(Wav(1)));

        await new AzureSpeech(new SpeechSecrets("eastus2", Key) { Voice = "nl-NL-FennaNeural" }, service, _cache).SynthesizeAsync("Hallo", null, Output(), default);
        await new AzureSpeech(new SpeechSecrets("eastus2", Key), service, _cache).SynthesizeAsync("Hello", null, Output(), default);

        Assert.Contains("name=\"nl-NL-FennaNeural\"", service.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("name=\"en-GB-Emily:MAI-Voice-2.1\"", service.Requests[1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public void TheText_IsEscaped_SoATomAndJerryLineIsStillSsml()
    {
        var ssml = AzureSpeech.Ssml("Tom & Jerry <say> \"hi\" 'there'", "en-GB-Emily:MAI-Voice-2.1", null);

        Assert.Contains("Tom &amp; Jerry &lt;say&gt; &quot;hi&quot; &apos;there&apos;", ssml, StringComparison.Ordinal);
        Assert.DoesNotContain("<say>", ssml, StringComparison.Ordinal);
        Assert.DoesNotContain("express-as", ssml, StringComparison.Ordinal);
        System.Xml.Linq.XDocument.Parse(ssml);
    }

    [Fact]
    public void TheInstructions_AreAStyle_SentAsExpressAs()
    {
        var ssml = AzureSpeech.Ssml("Hello", "en-GB-Emily:MAI-Voice-2.1", "narrator");

        var document = System.Xml.Linq.XDocument.Parse(ssml);
        var style = document.Descendants(System.Xml.Linq.XName.Get("express-as", "http://www.w3.org/2001/mstts")).Single();
        Assert.Equal("narrator", style.Attribute("style")!.Value);
        Assert.Equal("Hello", style.Value);
    }

    [Fact]
    public async Task AStyleSetOnTheVoice_ReachesTheRequest_AndIsPartOfTheClip()
    {
        var service = new FakeService(Respond(Wav(1)), Respond(Wav(1)));

        await new AzureSpeech(new SpeechSecrets("eastus2", Key), service, _cache) { Instructions = "narrator" }.SynthesizeAsync("Hello", Emily, Output(), default);
        await new AzureSpeech(new SpeechSecrets("eastus2", Key), service, _cache) { Instructions = "educational" }.SynthesizeAsync("Hello", Emily, Output(), default);

        Assert.Equal(2, service.Requests.Count);
        Assert.Contains("style=\"narrator\"", service.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("style=\"educational\"", service.Requests[1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ALineWithMarkup_IsPostedAsItsSsml_UnderTheStyle_WithTheTextEscaped()
    {
        var service = new FakeService(Respond(Wav(1)));
        var voice = new AzureSpeech(new SpeechSecrets("eastus2", Key), service, _cache) { Instructions = "narrator" };
        var pronunciations = Pronunciations.Parse("| Term | Say |\n| --- | --- |\n| Bruinet | Brew-ee-net |\n", "p.md");
        var text = pronunciations.Apply("Tom & Jerry meet Bruinet.<break time=\"400ms\"/> Your <emphasis level=\"strong\">todo</emphasis> is saved.");

        var clip = await Narrator.Speech.ClipAsync(voice, text, Emily, Output(), default);

        Assert.Equal("Tom & Jerry meet Bruinet. Your todo is saved.", clip.Text);
        Assert.Equal(
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"http://www.w3.org/2001/mstts\" xml:lang=\"en-GB\">"
            + "<voice name=\"en-GB-Emily:MAI-Voice-2.1\"><mstts:express-as style=\"narrator\">"
            + "Tom &amp; Jerry meet <sub alias=\"Brew-ee-net\">Bruinet</sub>.<break time=\"400ms\"/> Your <emphasis level=\"strong\">todo</emphasis> is saved."
            + "</mstts:express-as></voice></speak>",
            Assert.Single(service.Requests).Body);
    }

    [Fact]
    public async Task ALinesOwnStyle_WinsForTheLine_AndAPlainLineStillGoesAsText()
    {
        var service = new FakeService(Respond(Wav(1)), Respond(Wav(1)));
        var voice = new AzureSpeech(new SpeechSecrets("eastus2", Key), service, _cache) { Instructions = "narrator" };

        await Narrator.Speech.ClipAsync(voice, "<mstts:express-as style=\"excited\">Hurray</mstts:express-as> and done.", Emily, Output(), default);
        await Narrator.Speech.ClipAsync(voice, "Plain <3 line.", Emily, Output(), default);

        Assert.Equal(1, service.Requests[0].Body.Split("express-as style").Length - 1);
        Assert.Contains("<mstts:express-as style=\"excited\">Hurray</mstts:express-as> and done.", service.Requests[0].Body, StringComparison.Ordinal);
        Assert.DoesNotContain("style=\"narrator\"", service.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("<mstts:express-as style=\"narrator\">Plain &lt;3 line.</mstts:express-as>", service.Requests[1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AMarkedLine_AndTheSameWordsPlain_AreDifferentClips_AndEachIsAskedForOnce()
    {
        var service = new FakeService(Respond(Wav(2)), Respond(Wav(1)));
        var voice = new AzureSpeech(new SpeechSecrets("eastus2", Key), service, _cache);

        await Narrator.Speech.ClipAsync(voice, "Wait.<break time=\"1s\"/> Go.", Emily, Output(), default);
        await Narrator.Speech.ClipAsync(voice, "Wait. Go.", Emily, Output(), default);
        await Narrator.Speech.ClipAsync(voice, "Wait.<break time=\"1s\"/> Go.", Emily, Output(), default);

        Assert.Equal(2, service.Requests.Count);
    }

    [Fact]
    public async Task AzureOpenAi_GetsTheReducedLine_TagsGone_AliasesSpoken_AndTheCacheKeyIsTheReducedText()
    {
        var service = new FakeService(Respond(Wav(1)));
        var voice = new AzureOpenAiSpeech(new VoiceSecrets("https://voice.openai.azure.com", VoiceSecrets.DefaultDeployment, VoiceSecrets.DefaultApiVersion, Key), service, _cache);

        var first = await Narrator.Speech.ClipAsync(voice, "Welcome to <sub alias=\"Brew-ee-net\">Bruinet</sub>.<break time=\"1s\"/> Go.", "azure:coral", Output(), default);
        var same = await Narrator.Speech.ClipAsync(voice, "Welcome to Brew-ee-net. Go.", "azure:coral", Output(), default);

        Assert.Equal("Welcome to Bruinet. Go.", first.Text);
        Assert.Equal("Welcome to Brew-ee-net. Go.", same.Text);
        var sent = Assert.Single(service.Requests);
        Assert.Contains("\"input\":\"Welcome to Brew-ee-net. Go.\"", sent.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("<", sent.Body.Replace("\\u003C", "<", StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(24000, "riff-24khz-16bit-mono-pcm")]
    [InlineData(48000, "riff-48khz-16bit-mono-pcm")]
    public async Task TheSampleRateSetting_IsTheFormatAskedFor(int rate, string format)
    {
        var service = new FakeService(Respond(Wav(1, rate)));

        await new AzureSpeech(new SpeechSecrets("eastus2", Key) { SampleRate = rate }, service, _cache).SynthesizeAsync("Hello", Emily, Output(), default);

        Assert.Equal(format, service.Requests.Single().Format);
    }

    [Fact]
    public async Task AFortyEightKilohertzClip_IsMeasuredFromItsHeader()
    {
        var voice = new AzureSpeech(new SpeechSecrets("eastus2", Key) { SampleRate = 48000 }, new FakeService(Respond(Wav(2, 48000))), _cache);

        var length = await voice.SynthesizeAsync("Hello", Emily, Output(), default);

        Assert.Equal(2, length.TotalSeconds, precision: 3);
    }

    [Fact]
    public async Task ALineIsAskedForOnce_AndAnotherRateIsAnotherClip()
    {
        var service = new FakeService(Respond(Wav(2)), Respond(Wav(2, 48000)));
        var low = new AzureSpeech(new SpeechSecrets("eastus2", Key), service, _cache);
        var high = new AzureSpeech(new SpeechSecrets("eastus2", Key) { SampleRate = 48000 }, service, _cache);

        var first = await low.SynthesizeAsync("Hello", Emily, Output(), default);
        var second = await low.SynthesizeAsync("Hello", Emily, Output(), default);
        await high.SynthesizeAsync("Hello", Emily, Output(), default);

        Assert.Equal(first, second);
        Assert.Equal(2, service.Requests.Count);
    }

    [Fact]
    public async Task TheCacheKey_HoldsNoSecret()
    {
        await new AzureSpeech(new SpeechSecrets("eastus2", Key), new FakeService(Respond(Wav(1))), _cache).SynthesizeAsync("Hello", Emily, Output(), default);

        var names = string.Join("\n", Directory.GetFiles(_cache).Select(Path.GetFileName));
        Assert.DoesNotContain(Key, names, StringComparison.Ordinal);
        Assert.Single(Directory.GetFiles(_cache, "*.wav"));
        Assert.Empty(Directory.GetFiles(_cache, "*.part"));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "another region")]
    [InlineData(HttpStatusCode.BadRequest, "voice name")]
    [InlineData(HttpStatusCode.UnsupportedMediaType, "content type")]
    [InlineData(HttpStatusCode.InternalServerError, "500")]
    public async Task AFailure_SaysWhatToDo_NeverTheKey(HttpStatusCode status, string says)
    {
        var voice = new AzureSpeech(new SpeechSecrets("eastus2", Key), new FakeService(new HttpResponseMessage(status) { Content = new StringContent("nope") }), _cache);

        var failure = await Assert.ThrowsAsync<VoiceServiceException>(() => voice.SynthesizeAsync("Hello", Emily, Output(), default));

        Assert.Contains(says, failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Key, failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ABusyService_IsAskedAgain_ThenFailsNamingIt(HttpStatusCode status)
    {
        var service = new FakeService(Busy(status), Respond(Wav(1)));
        await new AzureSpeech(new SpeechSecrets("eastus2", Key), service, _cache).SynthesizeAsync("Hello", Emily, Output(), default);
        Assert.Equal(2, service.Requests.Count);

        var always = new FakeService(Busy(status), Busy(status), Busy(status), Busy(status));
        var failure = await Assert.ThrowsAsync<VoiceServiceException>(() => new AzureSpeech(new SpeechSecrets("eastus2", Key), always, Path.Combine(_root, "other")).SynthesizeAsync("Other", Emily, Output(), default));
        Assert.Contains("busy", failure.Message, StringComparison.Ordinal);
        Assert.Equal(4, always.Requests.Count);
    }

    [Fact]
    public async Task AnUnreachableService_SaysSo()
    {
        var voice = new AzureSpeech(new SpeechSecrets("eastus2", Key), new Unreachable(), _cache);

        var failure = await Assert.ThrowsAsync<VoiceServiceException>(() => voice.SynthesizeAsync("Hello", Emily, Output(), default));

        Assert.Contains("cannot be reached", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Key, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnAzureSpeechName_IsNotAnAzureOpenAiName_AndTheOtherWayAround()
    {
        Assert.True(AzureSpeech.Owns(Emily));
        Assert.True(AzureSpeech.Owns("AZURE-SPEECH-SERVICE:nl-NL-FennaNeural"));
        Assert.False(AzureSpeech.Owns("azure:coral"));
        Assert.False(AzureOpenAiSpeech.Owns(Emily));
        Assert.False(AzureSpeech.Owns("Microsoft Zira"));
        Assert.False(AzureSpeech.Owns(null));
    }

    [Fact]
    public void FindingAVoice_ByItsPrefix_NeedsItsSettings_AndNamesWhereItLooked()
    {
        var failure = Assert.Throws<VoiceNotConfiguredException>(() => Narrator.Speech.Find(Emily, folder: _suite, read: NoVariables, home: _home));
        Assert.Contains(SpeechSecrets.KeyVariable, failure.Message, StringComparison.Ordinal);
        Assert.Contains(Path.Combine(_home, ".bruinet", VoiceSecrets.FileName), failure.Message, StringComparison.Ordinal);

        Assert.IsType<AzureSpeech>(Narrator.Speech.Find(Emily, folder: _suite, read: SpeechVariables, home: _home));
    }

    [Fact]
    public void WithNoVoiceNamed_AzureOpenAiComesFirst_ThenAzureSpeech_ThenTheWindowsVoice()
    {
        Assert.IsNotType<AzureSpeech>(Narrator.Speech.Find(null, folder: _suite, read: NoVariables, home: _home));
        Assert.IsType<AzureSpeech>(Narrator.Speech.Find(null, folder: _suite, read: SpeechVariables, home: _home));

        string? Both(string name) => name switch
        {
            VoiceSecrets.KeyVariable => "openai-key",
            VoiceSecrets.EndpointVariable => "https://voice.openai.azure.com",
            _ => SpeechVariables(name),
        };

        Assert.IsType<AzureOpenAiSpeech>(Narrator.Speech.Find(null, folder: _suite, read: Both, home: _home));
        Assert.IsType<AzureOpenAiSpeech>(Narrator.Speech.Find("azure:coral", folder: _suite, read: Both, home: _home));
        Assert.IsType<AzureSpeech>(Narrator.Speech.Find(Emily, folder: _suite, read: Both, home: _home));
    }

    [Fact]
    public void AWindowsVoiceName_WithSpeechSetUp_StaysAWindowsVoice()
        => Assert.IsNotType<AzureSpeech>(Narrator.Speech.Find("Microsoft Zira", folder: _suite, read: SpeechVariables, home: _home));

    [Fact]
    public void TheScriptsStyle_IsCheckedAgainstTheKeptVoicesList()
    {
        KeepVoices();

        Assert.Empty(Check(Script("narrator")).Diagnostics);

        var unknown = Assert.Single(Check(Script("whispering")).Diagnostics);
        Assert.Equal(BddVideoScriptCheck.UnknownStyleCode, unknown.Code);
        Assert.Equal(BddDiagnosticSeverity.Warning, unknown.Severity);
        Assert.Equal(4, unknown.Location.LineNumber);
        Assert.Contains("narrator", unknown.Message, StringComparison.Ordinal);

        var free = Assert.Single(Check(Script("calm and friendly, a product demo")).Diagnostics);
        Assert.Equal(BddVideoScriptCheck.UnknownStyleCode, free.Code);
    }

    [Fact]
    public void WithoutAKeptList_OrSettings_OrAnotherVoice_NothingIsSaidAboutTheStyle()
    {
        Assert.Empty(Check(Script("whispering")).Diagnostics);

        KeepVoices();
        Assert.Empty(Check(Script("whispering", voice: "azure-speech-service:xx-XX-NobodyNeural")).Diagnostics);
        Assert.Empty(Check(Script("whispering", voice: "azure:coral")).Diagnostics);
        Assert.Empty(Check(Script("whispering", voice: "Microsoft Zira")).Diagnostics);
        Assert.Empty(BddVideoScriptCheck.Check(Script("whispering"), Path.Combine(_suite, "a.video.md"), _suite, NoVariables, _home, _cache).Diagnostics);
    }

    [Fact]
    public void AVoiceWithNoStyles_SaysTheInstructionsAreIgnored()
    {
        KeepVoices();

        var diagnostic = Assert.Single(Check(Script("narrator", voice: "azure-speech-service:nl-NL-FennaNeural")).Diagnostics);

        Assert.Equal(BddVideoScriptCheck.UnknownStyleCode, diagnostic.Code);
        Assert.Contains("no styles", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheVoicesList_IsAskedForWithTheKey_ByLocale_AndKept()
    {
        var service = new FakeService(Json(VoicesJson));
        var voices = new AzureSpeechVoices(new SpeechSecrets("eastus2", Key), service, _cache);

        var british = await voices.ListAsync("en-GB", default);
        var dutch = await voices.ListAsync("nl-NL", default);
        var all = await voices.ListAsync(null, default);

        Assert.Equal(["en-GB-Emily:MAI-Voice-2.1", "en-GB-Harry:MAI-Voice-2.1"], british.Select(voice => voice.ShortName));
        Assert.Equal(["nl-NL-FennaNeural"], dutch.Select(voice => voice.ShortName));
        Assert.Equal(3, all.Count);
        var sent = Assert.Single(service.Requests);
        Assert.Equal("https://eastus2.tts.speech.microsoft.com/cognitiveservices/voices/list", sent.Url);
        Assert.Equal(HttpMethod.Get, sent.Method);
        Assert.Equal(Key, sent.Key);
        Assert.Contains("narrator", british[0].Styles);
        Assert.Equal("Preview", british[0].Status);
    }

    [Fact]
    public async Task AKeptListIsUsedWithoutACall_ButAStaleOneIsAskedForAgain()
    {
        var secrets = new SpeechSecrets("eastus2", Key);
        await new AzureSpeechVoices(secrets, new FakeService(Json(VoicesJson)), _cache).ListAsync(null, default);
        var file = Assert.Single(Directory.GetFiles(_cache, "voices-*.json"));
        Assert.Equal(3, AzureSpeechVoices.Kept(secrets, _cache)!.Count);

        var offline = new FakeService();
        Assert.Equal(3, (await new AzureSpeechVoices(secrets, offline, _cache).ListAsync(null, default)).Count);
        Assert.Empty(offline.Requests);

        File.SetLastWriteTimeUtc(file, DateTime.UtcNow - AzureSpeechVoices.Fresh - TimeSpan.FromHours(1));
        Assert.Null(AzureSpeechVoices.Kept(secrets, _cache));
        var again = new FakeService(Json(VoicesJson));
        await new AzureSpeechVoices(secrets, again, _cache).ListAsync(null, default);
        Assert.Single(again.Requests);
    }

    [Fact]
    public async Task ABrokenKeptList_IsAskedForAgain()
    {
        var secrets = new SpeechSecrets("eastus2", Key);
        await new AzureSpeechVoices(secrets, new FakeService(Json(VoicesJson)), _cache).ListAsync(null, default);
        File.WriteAllText(Directory.GetFiles(_cache, "voices-*.json").Single(), "{ not a list");
        Assert.Null(AzureSpeechVoices.Kept(secrets, _cache));

        var again = new FakeService(Json(VoicesJson));
        Assert.Equal(3, (await new AzureSpeechVoices(secrets, again, _cache).ListAsync(null, default)).Count);
        Assert.Single(again.Requests);
        Assert.Equal(3, AzureSpeechVoices.Kept(secrets, _cache)!.Count);
    }

    [Fact]
    public async Task EachEndpointKeepsItsOwnList_AndAResourceHostAsksItsOwnPath()
    {
        var east = new SpeechSecrets("eastus2", Key);
        var resource = new SpeechSecrets("https://res.cognitiveservices.azure.com", Key);
        await new AzureSpeechVoices(east, new FakeService(Json(VoicesJson)), _cache).ListAsync(null, default);
        Assert.Null(AzureSpeechVoices.Kept(resource, _cache));

        var service = new FakeService(Json(VoicesJson));
        await new AzureSpeechVoices(resource, service, _cache).ListAsync(null, default);
        Assert.Equal("https://res.cognitiveservices.azure.com/tts/cognitiveservices/voices/list", service.Requests.Single().Url);
        Assert.Equal(2, Directory.GetFiles(_cache, "voices-*.json").Length);
    }

    [Fact]
    public async Task AWrongKeyOrAnAnswerThatIsNoList_IsAFailureThatNamesIt()
    {
        var secrets = new SpeechSecrets("eastus2", Key);

        var refused = await Assert.ThrowsAsync<VoiceServiceException>(() => new AzureSpeechVoices(secrets, new FakeService(new HttpResponseMessage(HttpStatusCode.Unauthorized)), _cache).ListAsync(null, default));
        Assert.Contains("another region", refused.Message, StringComparison.Ordinal);

        var notAList = await Assert.ThrowsAsync<VoiceServiceException>(() => new AzureSpeechVoices(secrets, new FakeService(Json("{\"a\":1}")), _cache).ListAsync(null, default));
        Assert.Contains("list of voices", notAList.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Key, refused.Message + notAList.Message, StringComparison.Ordinal);
        Assert.Empty(Directory.Exists(_cache) ? Directory.GetFiles(_cache, "voices-*") : []);
    }

    [Fact]
    public void AVoice_IsFoundByItsName_WithOrWithoutTheModelSuffix()
    {
        var kept = AzureSpeechVoices.Of(Parsed(), null);

        Assert.Equal("en-GB-Emily:MAI-Voice-2.1", AzureSpeechVoices.Find(kept, "en-GB-Emily:MAI-Voice-2.1")!.ShortName);
        Assert.Equal("nl-NL-FennaNeural", AzureSpeechVoices.Find(kept, "NL-nl-fennaneural")!.ShortName);
        Assert.Null(AzureSpeechVoices.Find(kept, "en-GB-Nobody"));
    }

    private BddVideoScriptCheck Check(string text) => BddVideoScriptCheck.Check(text, Path.Combine(_suite, "a.video.md"), _suite, SpeechVariables, _home, _cache);

    private static string Script(string instructions, string voice = Emily)
        => $"# Video: Speaking\n\n| Field | Value |\n| Instructions | {instructions} |\n| Voice | {voice} |\n";

    private void KeepVoices()
    {
        var secrets = new SpeechSecrets("eastus2", Key);
        Directory.CreateDirectory(_cache);
        new AzureSpeechVoices(secrets, new FakeService(Json(VoicesJson)), _cache).ListAsync(null, default).GetAwaiter().GetResult();
    }

    private IReadOnlyList<SpeechVoice> Parsed()
    {
        KeepVoices();
        return AzureSpeechVoices.Kept(new SpeechSecrets("eastus2", Key), _cache)!;
    }

    private string Output() => Path.Combine(_root, "out", Guid.NewGuid().ToString("N") + ".wav");

    private static string? NoVariables(string name) => null;

    private static string? SpeechVariables(string name) => name switch
    {
        SpeechSecrets.EndpointVariable => "eastus2",
        SpeechSecrets.KeyVariable => Key,
        _ => null,
    };

    private static HttpResponseMessage Respond(byte[] wav) => new(HttpStatusCode.OK) { Content = new ByteArrayContent(wav) };

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Busy(HttpStatusCode status)
    {
        var busy = new HttpResponseMessage(status);
        busy.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);
        return busy;
    }

    private const string VoicesJson = """
        [
          { "ShortName": "en-GB-Emily:MAI-Voice-2.1", "Locale": "en-GB", "Gender": "Female", "VoiceType": "Neural", "Status": "Preview", "StyleList": ["narrator", "educational", "audiobook"] },
          { "ShortName": "en-GB-Harry:MAI-Voice-2.1", "Locale": "en-GB", "Gender": "Male", "VoiceType": "Neural", "Status": "Preview", "StyleList": ["narrator", "educational"] },
          { "ShortName": "nl-NL-FennaNeural", "Locale": "nl-NL", "Gender": "Female", "VoiceType": "Neural", "Status": "GA" },
          { "Locale": "xx-XX" }
        ]
        """;

    // 16-bit mono PCM at a sample rate, as the service sends it; streaming, its sizes are unknown (0xFFFFFFFF).
    private static byte[] Wav(double seconds, int sampleRate = 24_000)
    {
        var bytesPerSecond = sampleRate * 2;
        var data = (int)(bytesPerSecond * seconds);
        var wav = new byte[44 + data];
        Encoding.ASCII.GetBytes("RIFF").CopyTo(wav, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(4), uint.MaxValue);
        Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(wav, 8);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(16), 16);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(20), 1);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(22), 1);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(24), sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(28), bytesPerSecond);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(32), 2);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(34), 16);
        Encoding.ASCII.GetBytes("data").CopyTo(wav, 36);
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(40), uint.MaxValue);
        return wav;
    }

    private sealed record SentRequest(string Url, HttpMethod Method, string? Key, string? ContentType, string? Format, string? UserAgent, string Body);

    // The service, answering from a queue, and keeping what was asked.
    private sealed class FakeService(params HttpResponseMessage[] answers) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _answers = new(answers);

        public List<SentRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? Header(string name) => request.Headers.TryGetValues(name, out var values) ? string.Join(" ", values) : null;
            Requests.Add(new SentRequest(
                request.RequestUri!.ToString(),
                request.Method,
                Header("Ocp-Apim-Subscription-Key"),
                request.Content?.Headers.ContentType?.MediaType,
                Header("X-Microsoft-OutputFormat"),
                Header("User-Agent"),
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
            return _answers.Dequeue();
        }
    }

    private sealed class Unreachable : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("no route to host");
    }
}

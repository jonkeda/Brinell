using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Brinell.Narrator.Tests;

/// <summary>The cloud voice (Azure OpenAI text to speech): its settings and key, its requests, its clips.</summary>
public sealed class CloudVoiceTests : IDisposable
{
    private const string Key = "secret-key-123";
    private const string Endpoint = "https://voice.openai.azure.com";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "Brinell", "CloudVoiceTests", Guid.NewGuid().ToString("N"));
    private readonly string _home;
    private readonly string _suite;

    public CloudVoiceTests()
    {
        _home = Path.Combine(_root, "home");
        _suite = Path.Combine(_root, "repo", "tests", "Suite");
        Directory.CreateDirectory(Path.Combine(_root, "repo", ".git"));
        Directory.CreateDirectory(_suite);
        Directory.CreateDirectory(_home);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void TheSuitesFile_IsFound_FromAFolderBelowIt_UpToTheRepositoryRoot()
    {
        WriteSecrets(Path.Combine(_root, "repo"), Endpoint, Key);

        var secrets = VoiceSecrets.Find(_suite, NoVariables, _home);

        Assert.NotNull(secrets);
        Assert.Equal(Endpoint, secrets.Endpoint);
        Assert.Equal(VoiceSecrets.DefaultDeployment, secrets.Deployment);
        Assert.Equal(VoiceSecrets.DefaultApiVersion, secrets.ApiVersion);
    }

    [Fact]
    public void AboveTheRepositoryRoot_NothingIsRead_ButTheUsersFolderIs()
    {
        WriteSecrets(_root, "https://above.openai.azure.com", Key);
        Assert.Null(VoiceSecrets.Find(_suite, NoVariables, _home));

        WriteSecrets(Path.Combine(_home, ".brinell"), "https://home.openai.azure.com", Key);
        Assert.Equal("https://home.openai.azure.com", VoiceSecrets.Find(_suite, NoVariables, _home)?.Endpoint);
        Assert.Equal(Path.Combine(_home, ".brinell", VoiceSecrets.FileName), VoiceSecrets.Places(_suite, _home)[^1]);
    }

    [Fact]
    public void TheSuitesFile_WinsOverTheUsers_AndTheEnvironment_OverBoth()
    {
        WriteSecrets(Path.Combine(_home, ".brinell"), "https://home.openai.azure.com", "home-key");
        WriteSecrets(_suite, "https://suite.openai.azure.com", "suite-key");
        Assert.Equal("https://suite.openai.azure.com", VoiceSecrets.Find(_suite, NoVariables, _home)?.Endpoint);

        var fromAgent = VoiceSecrets.Find(_suite, Variables((VoiceSecrets.KeyVariable, "agent-key"), (VoiceSecrets.DeploymentVariable, "tts")), _home);

        Assert.Equal("agent-key", fromAgent?.Key);
        Assert.Equal("tts", fromAgent?.Deployment);
        Assert.Equal("https://suite.openai.azure.com", fromAgent?.Endpoint);
    }

    [Fact]
    public void WithoutAKeyOrAnEndpoint_ThereAreNoSettings()
    {
        WriteSecrets(_suite, Endpoint, key: null);

        Assert.Null(VoiceSecrets.Find(_suite, NoVariables, _home));
        Assert.Null(VoiceSecrets.Find(null, Variables((VoiceSecrets.KeyVariable, Key)), _home));
    }

    [Fact]
    public void TheKey_IsNotShown_InTheSettingsText_OrABrokenFilesMessage()
    {
        WriteSecrets(_suite, Endpoint, Key);
        Assert.DoesNotContain(Key, VoiceSecrets.Find(_suite, NoVariables, _home)!.ToString(), StringComparison.Ordinal);

        File.WriteAllText(Path.Combine(_suite, VoiceSecrets.FileName), "{ \"key\": \"" + Key + "\", ");
        var broken = Assert.Throws<InvalidDataException>(() => VoiceSecrets.Find(_suite, NoVariables, _home));
        Assert.DoesNotContain(Key, broken.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GitIgnoresTheFile_InASuiteFolder()
    {
        var repository = FindRepositoryRoot();
        var suiteFile = Path.Combine(repository, "samples", "Todo", "tests", "Brinell.Samples.Todo.Uat", VoiceSecrets.FileName);

        using var git = Process.Start(new ProcessStartInfo("git", $"check-ignore -q \"{suiteFile}\"") { WorkingDirectory = repository, UseShellExecute = false })!;
        git.WaitForExit();

        Assert.Equal(0, git.ExitCode);
    }

    [Fact]
    public async Task ALine_IsAskedFor_WithTheKeyOnlyInItsHeader_AndItsWav_IsMeasured()
    {
        var service = new FakeService(Respond(Wav(seconds: 1.5)));
        var speech = Speech(service, instructions: "calm and friendly");
        var wav = Path.Combine(_root, "line.wav");

        var duration = await speech.SynthesizeAsync("The app opens on your list.", "azure:coral", wav, CancellationToken.None);

        var request = Assert.Single(service.Requests);
        Assert.Equal($"{Endpoint}/openai/deployments/gpt-4o-mini-tts/audio/speech?api-version=2025-03-01-preview", request.Url);
        Assert.Equal(Key, request.ApiKey);
        Assert.DoesNotContain(Key, request.Url, StringComparison.Ordinal);
        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal("coral", body.RootElement.GetProperty("voice").GetString());
        Assert.Equal("The app opens on your list.", body.RootElement.GetProperty("input").GetString());
        Assert.Equal("wav", body.RootElement.GetProperty("response_format").GetString());
        Assert.Equal("calm and friendly", body.RootElement.GetProperty("instructions").GetString());
        Assert.Equal(TimeSpan.FromSeconds(1.5), duration);
    }

    [Fact]
    public async Task ALineSaidBefore_IsTakenFromTheCache_AndAnotherToneIsAskedFor()
    {
        var service = new FakeService(Respond(Wav(1)), Respond(Wav(1)));
        var wav = Path.Combine(_root, "line.wav");

        await Speech(service).SynthesizeAsync("Hello.", "azure:coral", wav, CancellationToken.None);
        await Speech(service).SynthesizeAsync("Hello.", "azure:coral", wav, CancellationToken.None);
        Assert.Single(service.Requests);

        await Speech(service, instructions: "excited").SynthesizeAsync("Hello.", "azure:coral", wav, CancellationToken.None);
        Assert.Equal(2, service.Requests.Count);
    }

    [Fact]
    public async Task ABusyService_IsAskedAgain_AfterTheWaitItGives()
    {
        var busy = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        busy.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);
        var service = new FakeService(busy, Respond(Wav(1)));

        await Speech(service).SynthesizeAsync("Hello.", null, Path.Combine(_root, "line.wav"), CancellationToken.None);

        Assert.Equal(2, service.Requests.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "refused the key")]
    [InlineData(HttpStatusCode.NotFound, "has no deployment 'gpt-4o-mini-tts'")]
    [InlineData(HttpStatusCode.InternalServerError, "answered 500")]
    public async Task AFailure_SaysWhatToDo_AndNeverShowsTheKey(HttpStatusCode status, string says)
    {
        var service = new FakeService(new HttpResponseMessage(status) { Content = new StringContent("echo " + Key) });

        var failure = await Assert.ThrowsAsync<VoiceServiceException>(() => Speech(service).SynthesizeAsync("Hello.", null, Path.Combine(_root, "line.wav"), CancellationToken.None));

        Assert.Contains(says, failure.Message, StringComparison.Ordinal);
        if (status != HttpStatusCode.InternalServerError)
        {
            Assert.DoesNotContain(Key, failure.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AStreamingHeader_GetsItsSizes_FromTheFilesLength()
    {
        var wav = AzureOpenAiSpeech.FixHeader(Wav(seconds: 2, streaming: true));

        Assert.Equal((uint)(wav.Length - 8), BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(4)));
        Assert.Equal(48_000u * 2, BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(40)));
    }

    [Fact]
    public void ACloudVoice_IsChosenByItsName_AndWithoutSettings_FailsNamingWhereItLooked()
    {
        var failure = Assert.Throws<VoiceNotConfiguredException>(() => Narrator.Speech.Find("azure:coral", folder: _suite, read: NoVariables, home: _home));
        Assert.Contains(Path.Combine(_suite, VoiceSecrets.FileName), failure.Message, StringComparison.Ordinal);

        var agent = Variables((VoiceSecrets.KeyVariable, Key), (VoiceSecrets.EndpointVariable, Endpoint));
        Assert.IsType<AzureOpenAiSpeech>(Narrator.Speech.Find("azure:coral", folder: _suite, read: agent, home: _home));
        Assert.IsType<AzureOpenAiSpeech>(Narrator.Speech.Find(null, folder: _suite, read: agent, home: _home));
        Assert.IsNotType<AzureOpenAiSpeech>(Narrator.Speech.Find("Microsoft Zira", folder: _suite, read: agent, home: _home));
    }

    [Fact]
    public void AScriptsInstructions_AreRead_AsAVideoField()
    {
        const string Script = """
            # Video: Tone

            | Field | Value |
            | --- | --- |
            | Voice | azure:coral |
            | Instructions | calm and friendly |
            """;

        var parse = BddVideoScriptParser.Parse(Script, "tone.video.md");

        Assert.Equal("calm and friendly", parse.Script?.Instructions);
        Assert.DoesNotContain(parse.Diagnostics, diagnostic => diagnostic.Code == BddVideoScriptParser.UnknownFieldCode);
    }

    [Fact]
    public async Task AnAudioChatModel_IsAskedThroughChatCompletions_ToReadTheLine_AndItsAudio_IsDecoded()
    {
        // The URL as Foundry shows it: the resource is what is before /openai.
        const string Copied = "https://voice.services.ai.azure.com/openai/v1/chat/completions";
        var service = new FakeService(Chat(Wav(1.5), "The app opens on your list."));
        var speech = Speech(service, instructions: "calm and friendly", endpoint: Copied, deployment: "gpt-audio-1.5");
        var wav = Path.Combine(_root, "line.wav");

        var duration = await speech.SynthesizeAsync("The app opens on your list.", "azure:marin", wav, CancellationToken.None);

        var request = Assert.Single(service.Requests);
        Assert.Equal("https://voice.services.ai.azure.com/openai/v1/chat/completions", request.Url);
        Assert.Equal(Key, request.ApiKey);
        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal("gpt-audio-1.5", body.RootElement.GetProperty("model").GetString());
        Assert.Equal(["text", "audio"], body.RootElement.GetProperty("modalities").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("marin", body.RootElement.GetProperty("audio").GetProperty("voice").GetString());
        Assert.Equal("wav", body.RootElement.GetProperty("audio").GetProperty("format").GetString());
        var messages = body.RootElement.GetProperty("messages");
        Assert.Contains("exactly as written", messages[0].GetProperty("content").GetString(), StringComparison.Ordinal);
        Assert.Contains("calm and friendly", messages[0].GetProperty("content").GetString(), StringComparison.Ordinal);
        Assert.Equal("The app opens on your list.", messages[1].GetProperty("content").GetString());
        Assert.Equal(TimeSpan.FromSeconds(1.5), duration);
    }

    [Fact]
    public async Task AnAudioChatModel_ThatMisreadsTheLine_IsAskedAgain_ThenFails_SayingWhatItSaid()
    {
        const string Line = "Tap Add to make a new todo.";
        var misread = new FakeService(Chat(Wav(1), "Sure! What should the todo be called?"), Chat(Wav(1), Line));
        var speech = Speech(misread, deployment: "gpt-audio-1.5");

        await speech.SynthesizeAsync(Line, "azure:coral", Path.Combine(_root, "line.wav"), CancellationToken.None);
        Assert.Equal(2, misread.Requests.Count);

        var answers = Enumerable.Range(0, 3).Select(_ => Chat(Wav(1), "Sure! What should the todo be called?")).ToArray();
        var failure = await Assert.ThrowsAsync<VoiceServiceException>(() => Speech(new FakeService(answers), deployment: "gpt-audio-1.5")
            .SynthesizeAsync("Another line, never read.", "azure:coral", Path.Combine(_root, "other.wav"), CancellationToken.None));
        Assert.Contains("did not read the line as written, 3 times", failure.Message, StringComparison.Ordinal);
        Assert.Contains("What should the todo be called?", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Version 2 adds 3 filters.", "Version two adds three filters.", true)]
    [InlineData("The app opens on your list.", "The app opens on your list!", true)]
    [InlineData("The app opens on your list.", "Okay. The app opens on your list, as you asked, and then I will explain more.", false)]
    [InlineData("Tap Add to make a new todo.", "Tap Add.", false)]
    public void ALineIsSaid_WhenNearlyAllItsWords_AreSaidInOrder(string line, string transcript, bool said)
        => Assert.Equal(said, AzureOpenAiSpeech.SaidTheLine(line, transcript));

    [Theory]
    [InlineData("https://voice.openai.azure.com/", "https://voice.openai.azure.com", false)]
    [InlineData("https://voice.services.ai.azure.com/openai/v1/chat/completions", "https://voice.services.ai.azure.com", true)]
    [InlineData("https://voice.openai.azure.com/openai/deployments/tts/audio/speech?api-version=x", "https://voice.openai.azure.com", false)]
    public void AnEndpoint_CopiedWithItsPath_NamesTheResource(string endpoint, string resource, bool chat)
    {
        var secrets = new VoiceSecrets(endpoint, VoiceSecrets.DefaultDeployment, VoiceSecrets.DefaultApiVersion, Key);

        Assert.Equal(resource, secrets.Resource);
        Assert.Equal(chat, secrets.IsChatAudio);
        Assert.Null(VoiceSecrets.EndpointProblem(endpoint));
    }

    [Fact]
    public void ARealtimeUrl_IsNotSaved_AndSaysWhy()
    {
        const string Realtime = "wss://voice.openai.azure.com/openai/v1/realtime?model=gpt-realtime-mini";
        var path = Path.Combine(_home, ".brinell", VoiceSecrets.FileName);

        var failure = Assert.Throws<InvalidOperationException>(() => VoiceSecrets.Save(path, Realtime, null, null, Key, null));

        Assert.Contains("Realtime", failure.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(path));
        Assert.True(new VoiceSecrets(Endpoint, "gpt-audio-1.5", VoiceSecrets.DefaultApiVersion, Key).IsChatAudio);
    }

    private AzureOpenAiSpeech Speech(FakeService service, string? instructions = null, string endpoint = Endpoint, string deployment = VoiceSecrets.DefaultDeployment)
        => new(new VoiceSecrets(endpoint, deployment, VoiceSecrets.DefaultApiVersion, Key), service, Path.Combine(_root, "cache")) { Instructions = instructions };

    // A chat completion with audio, as an audio chat model answers.
    private static HttpResponseMessage Chat(byte[] wav, string transcript)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                choices = new[] { new { message = new { role = "assistant", audio = new { id = "audio_1", data = Convert.ToBase64String(wav), transcript } } } },
            })),
        };

    private static string? NoVariables(string name) => null;

    private static Func<string, string?> Variables(params (string Name, string Value)[] set)
        => name => set.FirstOrDefault(variable => variable.Name == name).Value;

    private static void WriteSecrets(string folder, string endpoint, string? key)
    {
        Directory.CreateDirectory(folder);
        var fields = new Dictionary<string, string> { ["endpoint"] = endpoint };
        if (key is not null)
        {
            fields["key"] = key;
        }

        File.WriteAllText(Path.Combine(folder, VoiceSecrets.FileName), JsonSerializer.Serialize(fields));
    }

    private static HttpResponseMessage Respond(byte[] wav) => new(HttpStatusCode.OK) { Content = new ByteArrayContent(wav) };

    // 24 kHz, 16-bit mono PCM, as the service sends it; streaming, its sizes are unknown (0xFFFFFFFF).
    private static byte[] Wav(double seconds, bool streaming = true)
    {
        const int BytesPerSecond = 48_000;
        var data = (int)(BytesPerSecond * seconds);
        var wav = new byte[44 + data];
        Encoding.ASCII.GetBytes("RIFF").CopyTo(wav, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(4), streaming ? uint.MaxValue : (uint)(36 + data));
        Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(wav, 8);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(16), 16);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(20), 1);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(22), 1);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(24), 24_000);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(28), BytesPerSecond);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(32), 2);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(34), 16);
        Encoding.ASCII.GetBytes("data").CopyTo(wav, 36);
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(40), streaming ? uint.MaxValue : (uint)data);
        return wav;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Brinell.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("No Brinell.sln above the test output.");
    }

    private sealed record SentRequest(string Url, string? ApiKey, string Body);

    // The service, answering from a queue, and keeping what was asked.
    private sealed class FakeService(params HttpResponseMessage[] answers) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _answers = new(answers);

        public List<SentRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.Headers.TryGetValues("api-key", out var values) ? values.Single() : null;
            Requests.Add(new SentRequest(request.RequestUri!.ToString(), key, await request.Content!.ReadAsStringAsync(cancellationToken)));
            return _answers.Dequeue();
        }
    }
}

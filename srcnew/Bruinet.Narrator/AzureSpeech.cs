using System.Net;
using System.Security;
using System.Text;

namespace Bruinet.Narrator;

/// <summary>
/// A cloud voice: Azure AI Speech, a named voice for each language and region, neural or MAI
/// (<c>nl-NL-FennaNeural</c>, <c>en-GB-Emily:MAI-Voice-2.1</c>). A script names it
/// <c>azure-speech-service:&lt;voice&gt;</c>. The line goes as SSML to the service's text-to-speech endpoint, with
/// the key in a header; <see cref="Instructions"/> is a style (<c>narrator</c>, <c>educational</c>), sent as
/// <c>mstts:express-as</c>. Each line is asked for once: clips are kept in the speech cache, by the service,
/// the voice, the style, the sample rate and the text.
/// </summary>
public sealed class AzureSpeech : ISpeechSynthesizer
{
    /// <summary>What an Azure Speech voice's name starts with.</summary>
    public const string Prefix = "azure-speech-service:";

    private const string Namespace = "http://www.w3.org/2001/10/synthesis";
    private const string StyleNamespace = "http://www.w3.org/2001/mstts";

    // One client for the process: a client per synthesizer would exhaust sockets.
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromMinutes(2) };

    private readonly SpeechSecrets _secrets;
    private readonly HttpClient _client;
    private readonly string _cache;

    /// <summary>Creates the voice.</summary>
    /// <param name="secrets">Where the service is, and its key.</param>
    /// <param name="handler">Sends the requests; the network by default (tests pass their own).</param>
    /// <param name="cacheFolder">Where clips are kept; <c>%LOCALAPPDATA%\Bruinet\speech-cache</c> by default.</param>
    public AzureSpeech(SpeechSecrets secrets, HttpMessageHandler? handler = null, string? cacheFolder = null)
    {
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _client = handler is null ? SharedClient : new HttpClient(handler, disposeHandler: false);
        _cache = cacheFolder ?? CloudVoice.DefaultCacheFolder;
    }

    /// <summary>The style the lines are said in, e.g. <c>narrator</c>; null for the voice's own way.</summary>
    public string? Instructions { get; init; }

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <summary>Whether a voice's name is an Azure Speech voice's.</summary>
    /// <param name="voice">The name.</param>
    public static bool Owns(string? voice) => voice?.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>The voice a name asks for: what follows the prefix, whole (the MAI model suffix included).</summary>
    /// <param name="voice">The name as a script writes it.</param>
    public static string VoiceOf(string voice)
    {
        ArgumentNullException.ThrowIfNull(voice);
        return Owns(voice) ? voice[Prefix.Length..].Trim() : voice.Trim();
    }

    /// <summary>The language of a voice: its name's first two parts (<c>en-GB</c> of <c>en-GB-Emily:MAI-Voice-2.1</c>).</summary>
    /// <param name="voice">The voice, without the prefix.</param>
    public static string LanguageOf(string voice)
    {
        var parts = voice.Split('-');
        return parts.Length >= 3 ? parts[0] + "-" + parts[1] : "en-US";
    }

    /// <summary>The audio format asked for at a sample rate.</summary>
    /// <param name="sampleRate">24000 or 48000.</param>
    public static string OutputFormat(int sampleRate) => $"riff-{sampleRate / 1000}khz-16bit-mono-pcm";

    /// <summary>The SSML for one line: the text escaped, one voice, and the style when there is one.</summary>
    /// <param name="text">What is said.</param>
    /// <param name="voice">The voice, without the prefix.</param>
    /// <param name="style">The style; null for the voice's own way.</param>
    public static string Ssml(string text, string voice, string? style)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(voice);
        var said = SecurityElement.Escape(text);
        var inner = string.IsNullOrWhiteSpace(style) ? said : $"<mstts:express-as style=\"{SecurityElement.Escape(style.Trim())}\">{said}</mstts:express-as>";
        return $"<speak version=\"1.0\" xmlns=\"{Namespace}\" xmlns:mstts=\"{StyleNamespace}\" xml:lang=\"{LanguageOf(voice)}\"><voice name=\"{SecurityElement.Escape(voice)}\">{inner}</voice></speak>";
    }

    /// <inheritdoc />
    public async Task<TimeSpan> SynthesizeAsync(string text, string? voice, string wavPath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(wavPath);
        var name = voice is null ? _secrets.Voice ?? SpeechSecrets.DefaultVoice : VoiceOf(voice);
        var style = string.IsNullOrWhiteSpace(Instructions) ? null : Instructions.Trim();
        var hash = CloudVoice.Hash("azure-speech-service", _secrets.Host, name, style ?? string.Empty, _secrets.SampleRate.ToString(System.Globalization.CultureInfo.InvariantCulture), text);
        return await CloudVoice.ClipAsync(_cache, hash, wavPath, () => RequestAsync(Ssml(text, name, style), cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    private Task<byte[]> RequestAsync(string ssml, CancellationToken cancellationToken)
        => CloudVoice.SendAsync(
            _client,
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _secrets.SynthesisUrl)
                {
                    Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml"),
                };
                request.Headers.Add("Ocp-Apim-Subscription-Key", _secrets.Key);
                request.Headers.Add("X-Microsoft-OutputFormat", OutputFormat(_secrets.SampleRate));
                request.Headers.TryAddWithoutValidation("User-Agent", "Bruinet.Narrator");
                return request;
            },
            _secrets.Host,
            Explain,
            cancellationToken,
            CloudVoice.IsBusy);

    private string? Explain(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => $"Azure Speech at {_secrets.Host} refused the key: it is wrong, or belongs to another region. Check the key and the endpoint in {VoiceSecrets.FileName}.",
        HttpStatusCode.BadRequest => $"Azure Speech at {_secrets.Host} refused the voice name or the text: check the name against the voices the service lists.",
        HttpStatusCode.UnsupportedMediaType => $"Azure Speech at {_secrets.Host} refused the content type.",
        HttpStatusCode.TooManyRequests or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable
            => $"Azure Speech at {_secrets.Host} is busy: it was asked {CloudVoice.BusyRetries + 1} times.",
        _ => null,
    };
}

using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace Brinell.Narrator;

/// <summary>
/// A cloud voice: Azure OpenAI text to speech (<c>gpt-4o-mini-tts</c>). A script names it
/// <c>azure:&lt;voice&gt;</c> (<c>azure:coral</c>). Each line is asked for once: clips are kept in
/// <c>%LOCALAPPDATA%\Brinell\speech-cache</c>, by the voice, its instructions and the text.
/// </summary>
public sealed class AzureOpenAiSpeech : ISpeechSynthesizer
{
    /// <summary>What a cloud voice's name starts with.</summary>
    public const string Prefix = "azure:";

    /// <summary>The voice when neither the script nor the settings name one.</summary>
    public const string DefaultVoice = "alloy";

    // A busy service (429) is asked again after the wait it gives, this many times.
    private const int BusyRetries = 3;
    private static readonly TimeSpan BusyWait = TimeSpan.FromSeconds(2);

    // One client for the process: a client per synthesizer would exhaust sockets.
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromMinutes(2) };

    private readonly VoiceSecrets _secrets;
    private readonly HttpClient _client;
    private readonly string _cache;

    /// <summary>Creates the voice.</summary>
    /// <param name="secrets">Where the service is, and its key.</param>
    /// <param name="handler">Sends the requests; the network by default (tests pass their own).</param>
    /// <param name="cacheFolder">Where clips are kept; <c>%LOCALAPPDATA%\Brinell\speech-cache</c> by default.</param>
    public AzureOpenAiSpeech(VoiceSecrets secrets, HttpMessageHandler? handler = null, string? cacheFolder = null)
    {
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _client = handler is null ? SharedClient : new HttpClient(handler, disposeHandler: false);
        _cache = cacheFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brinell", "speech-cache");
    }

    /// <summary>How the lines are said, e.g. "calm and friendly, a product demo"; null for the voice's own way.</summary>
    public string? Instructions { get; init; }

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <summary>Whether a voice's name is a cloud voice's.</summary>
    /// <param name="voice">The name.</param>
    public static bool Owns(string? voice) => voice?.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) == true;

    /// <inheritdoc />
    public async Task<TimeSpan> SynthesizeAsync(string text, string? voice, string wavPath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(wavPath);
        var name = Owns(voice) ? voice![Prefix.Length..] : voice ?? _secrets.Voice ?? DefaultVoice;
        var cached = Path.Combine(_cache, Hash(name, text) + ".wav");
        if (!File.Exists(cached))
        {
            var wav = FixHeader(await RequestAsync(text, name, cancellationToken).ConfigureAwait(false));
            Directory.CreateDirectory(_cache);

            // Written whole, then moved: a cancelled or failed write never leaves a clip behind.
            var partial = cached + "." + Guid.NewGuid().ToString("N") + ".part";
            await File.WriteAllBytesAsync(partial, wav, cancellationToken).ConfigureAwait(false);
            File.Move(partial, cached, overwrite: true);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(wavPath))!);
        File.Copy(cached, wavPath, overwrite: true);
        return Speech.WavDuration(wavPath);
    }

    /// <summary>
    /// A WAV with its RIFF and data sizes set from its length: a service that streams its answer
    /// writes the header before it knows them (0 or 0xFFFFFFFF).
    /// </summary>
    /// <param name="wav">The WAV as received.</param>
    public static byte[] FixHeader(byte[] wav)
    {
        ArgumentNullException.ThrowIfNull(wav);
        if (wav.Length < 12 || Encoding.ASCII.GetString(wav, 0, 4) != "RIFF" || Encoding.ASCII.GetString(wav, 8, 4) != "WAVE")
        {
            throw new VoiceServiceException("The cloud voice did not answer with a WAV file.");
        }

        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(4), (uint)(wav.Length - 8));
        var position = 12;
        while (position + 8 <= wav.Length)
        {
            var chunk = Encoding.ASCII.GetString(wav, position, 4);
            if (chunk == "data")
            {
                BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(position + 4), (uint)(wav.Length - position - 8));
                return wav;
            }

            var size = BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(position + 4));
            position += 8 + (int)size + (int)(size & 1);
        }

        throw new VoiceServiceException("The cloud voice's WAV has no data.");
    }

    private async Task<byte[]> RequestAsync(string text, string voice, CancellationToken cancellationToken)
    {
        var url = $"{_secrets.Endpoint.TrimEnd('/')}/openai/deployments/{Uri.EscapeDataString(_secrets.Deployment)}/audio/speech?api-version={Uri.EscapeDataString(_secrets.ApiVersion)}";
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(new Dictionary<string, string?>
                {
                    ["model"] = _secrets.Deployment,
                    ["input"] = text,
                    ["voice"] = voice,
                    ["response_format"] = "wav",
                    ["instructions"] = Instructions,
                }.Where(field => field.Value is not null).ToDictionary()),
            };
            request.Headers.Add("api-key", _secrets.Key);

            HttpResponseMessage response;
            try
            {
                response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException exception)
            {
                throw new VoiceServiceException($"The cloud voice at {_secrets.Endpoint} cannot be reached: {exception.Message}");
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < BusyRetries)
                {
                    await Task.Delay(response.Headers.RetryAfter?.Delta ?? BusyWait, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw new VoiceServiceException(response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => $"The cloud voice at {_secrets.Endpoint} refused the key: check the key and the endpoint in {VoiceSecrets.FileName}.",
                    HttpStatusCode.NotFound => $"The cloud voice at {_secrets.Endpoint} has no deployment '{_secrets.Deployment}'.",
                    HttpStatusCode.TooManyRequests => $"The cloud voice at {_secrets.Endpoint} is busy: it was asked {BusyRetries + 1} times.",
                    _ => $"The cloud voice at {_secrets.Endpoint} answered {(int)response.StatusCode}: {await Body(response, cancellationToken).ConfigureAwait(false)}",
                });
            }
        }
    }

    private static async Task<string> Body(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return body.Length > 300 ? body[..300] + "…" : body;
    }

    // The clip's name: everything that changes what is said, and nothing secret.
    private string Hash(string voice, string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", _secrets.Endpoint, _secrets.Deployment, voice, Instructions ?? string.Empty, text))))[..32];
}

/// <summary>The cloud voice could not make a line.</summary>
/// <param name="message">What went wrong, and what to do; never the key.</param>
public sealed class VoiceServiceException(string message) : Exception(message);

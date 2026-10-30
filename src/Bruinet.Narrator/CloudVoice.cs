using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Bruinet.Narrator;

/// <summary>
/// What the cloud voices (<see cref="AzureOpenAiSpeech"/>, <see cref="AzureSpeech"/>) share: the clip cache
/// (<c>%LOCALAPPDATA%\Bruinet\speech-cache</c>), a streaming WAV's header, the retry on a busy service.
/// </summary>
internal static class CloudVoice
{
    // A busy service (429) is asked again after the wait it gives, this many times.
    internal const int BusyRetries = 3;

    private static readonly TimeSpan BusyWait = TimeSpan.FromSeconds(2);

    /// <summary>Where clips are kept when the voice is not told otherwise.</summary>
    internal static string DefaultCacheFolder
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Bruinet", "speech-cache");

    /// <summary>A clip's name: everything that changes what is said, and nothing secret.</summary>
    /// <param name="parts">The service, the voice, how it says the line, the text.</param>
    internal static string Hash(params string[] parts)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", parts))))[..32];

    /// <summary>Copies the clip called <paramref name="name"/> to <paramref name="wavPath"/>, making it with <paramref name="make"/> when the cache has none.</summary>
    internal static async Task<TimeSpan> ClipAsync(string cache, string name, string wavPath, Func<Task<byte[]>> make, CancellationToken cancellationToken)
    {
        var cached = Path.Combine(cache, name + ".wav");
        if (!File.Exists(cached))
        {
            var wav = FixHeader(await make().ConfigureAwait(false));
            Directory.CreateDirectory(cache);

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
    internal static byte[] FixHeader(byte[] wav)
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

    /// <summary>
    /// Sends a request and returns the answer's body; a busy service (429) is asked again after its
    /// <c>Retry-After</c>, a few times. Any other failure is a <see cref="VoiceServiceException"/>.
    /// </summary>
    /// <param name="client">Sends the request.</param>
    /// <param name="create">Makes the request; called again for each attempt.</param>
    /// <param name="endpoint">Where the service is, for the messages; never the key.</param>
    /// <param name="explain">The service's own message for a status, or null for the generic one.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <param name="retry">Whether a status is a busy service, asked again; only 429 by default.</param>
    internal static async Task<byte[]> SendAsync(HttpClient client, Func<HttpRequestMessage> create, string endpoint, Func<HttpStatusCode, string?> explain, CancellationToken cancellationToken, Func<HttpStatusCode, bool>? retry = null)
    {
        retry ??= status => status == HttpStatusCode.TooManyRequests;
        for (var attempt = 0; ; attempt++)
        {
            using var request = create();
            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException exception)
            {
                throw new VoiceServiceException($"The cloud voice at {endpoint} cannot be reached: {exception.Message}");
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                }

                if (retry(response.StatusCode) && attempt < BusyRetries)
                {
                    await Task.Delay(response.Headers.RetryAfter?.Delta ?? BusyWait, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw new VoiceServiceException(
                    explain(response.StatusCode)
                    ?? (response.StatusCode == HttpStatusCode.TooManyRequests
                        ? $"The cloud voice at {endpoint} is busy: it was asked {BusyRetries + 1} times."
                        : $"The cloud voice at {endpoint} answered {(int)response.StatusCode}: {await Body(response, cancellationToken).ConfigureAwait(false)}"));
            }
        }
    }

    /// <summary>Busy, or briefly gone: 429, 502 and 503 are asked again.</summary>
    /// <param name="status">The answer's status.</param>
    internal static bool IsBusy(HttpStatusCode status) => status is HttpStatusCode.TooManyRequests or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable;

    private static async Task<string> Body(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return body.Length > 300 ? body[..300] + "…" : body;
    }
}

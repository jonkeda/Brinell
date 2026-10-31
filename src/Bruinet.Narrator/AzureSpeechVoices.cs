using System.Text.Json;

namespace Bruinet.Narrator;

/// <summary>A voice the Speech service lists.</summary>
/// <param name="ShortName">The name a script uses, after the prefix.</param>
/// <param name="Locale">Its language and region, e.g. <c>en-GB</c>.</param>
/// <param name="Gender">Female or Male; null when not listed.</param>
/// <param name="Styles">The styles it can be told to speak in.</param>
/// <param name="VoiceType">Neural, or what the service calls it; null when not listed.</param>
/// <param name="Status">GA or Preview; null when not listed.</param>
public sealed record SpeechVoice(string ShortName, string Locale, string? Gender, IReadOnlyList<string> Styles, string? VoiceType, string? Status);

/// <summary>
/// The voices of an Azure Speech resource, read from <c>voices/list</c> and kept per endpoint in the speech
/// cache folder, so a picker and the script check need no call after the first. The list is the service's,
/// not a fixed one that would go stale.
/// </summary>
public sealed class AzureSpeechVoices
{
    /// <summary>How long a kept list is used before it is asked for again.</summary>
    public static readonly TimeSpan Fresh = TimeSpan.FromDays(7);

    private readonly SpeechSecrets _secrets;
    private readonly HttpClient _client;
    private readonly string _cache;

    /// <summary>Creates the list.</summary>
    /// <param name="secrets">Where the service is, and its key.</param>
    /// <param name="handler">Sends the request; the network by default (tests pass their own).</param>
    /// <param name="cacheFolder">Where the list is kept; <c>%LOCALAPPDATA%\Bruinet\speech-cache</c> by default.</param>
    public AzureSpeechVoices(SpeechSecrets secrets, HttpMessageHandler? handler = null, string? cacheFolder = null)
    {
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _client = handler is null ? new HttpClient { Timeout = TimeSpan.FromSeconds(30) } : new HttpClient(handler, disposeHandler: false);
        _cache = cacheFolder ?? CloudVoice.DefaultCacheFolder;
    }

    /// <summary>The voices, of one locale or all of them: from the kept list when it is fresh and whole, otherwise asked for and kept.</summary>
    /// <param name="locale">A locale such as <c>nl-NL</c>; null for every voice.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="VoiceServiceException">The service could not be asked, or did not answer with a list.</exception>
    public async Task<IReadOnlyList<SpeechVoice>> ListAsync(string? locale, CancellationToken cancellationToken)
    {
        var voices = Kept(_secrets, _cache) ?? await FetchAsync(cancellationToken).ConfigureAwait(false);
        return Of(voices, locale);
    }

    /// <summary>Asks the service for its voices whatever is kept, and keeps the answer.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="VoiceServiceException">The service could not be asked, or did not answer with a list.</exception>
    public Task<IReadOnlyList<SpeechVoice>> RefreshAsync(CancellationToken cancellationToken) => FetchAsync(cancellationToken);

    /// <summary>The kept list for a resource, or null when there is none, it is stale or it is broken (never a call).</summary>
    /// <param name="secrets">Where the service is.</param>
    /// <param name="cacheFolder">Where the list is kept; the speech cache by default.</param>
    public static IReadOnlyList<SpeechVoice>? Kept(SpeechSecrets secrets, string? cacheFolder = null)
    {
        ArgumentNullException.ThrowIfNull(secrets);
        var path = PathOf(secrets, cacheFolder ?? CloudVoice.DefaultCacheFolder);
        try
        {
            if (!File.Exists(path) || DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > Fresh)
            {
                return null;
            }

            using var json = JsonDocument.Parse(File.ReadAllText(path));
            return json.RootElement.ValueKind == JsonValueKind.Array ? Parse(json.RootElement) : null;
        }
        catch (Exception exception) when (exception is JsonException or IOException or InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>The voices of one locale; every voice for null.</summary>
    /// <param name="voices">The list.</param>
    /// <param name="locale">A locale such as <c>nl-NL</c>.</param>
    public static IReadOnlyList<SpeechVoice> Of(IReadOnlyList<SpeechVoice> voices, string? locale)
        => string.IsNullOrWhiteSpace(locale)
            ? voices
            : [.. voices.Where(voice => string.Equals(voice.Locale, locale.Trim(), StringComparison.OrdinalIgnoreCase))];

    /// <summary>The voice a name (as a script writes it, without the prefix) is in a list; null when it is not there.</summary>
    /// <param name="voices">The list.</param>
    /// <param name="name">The voice name; a MAI model suffix (<c>:MAI-Voice-2.1</c>) may be there or not.</param>
    public static SpeechVoice? Find(IReadOnlyList<SpeechVoice> voices, string name)
    {
        ArgumentNullException.ThrowIfNull(voices);
        ArgumentNullException.ThrowIfNull(name);
        return voices.FirstOrDefault(voice => string.Equals(voice.ShortName, name, StringComparison.OrdinalIgnoreCase))
            ?? voices.FirstOrDefault(voice => name.StartsWith(voice.ShortName + ":", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<SpeechVoice>> FetchAsync(CancellationToken cancellationToken)
    {
        var answer = await CloudVoice.SendAsync(
            _client,
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, _secrets.VoicesUrl);
                request.Headers.Add("Ocp-Apim-Subscription-Key", _secrets.Key);
                return request;
            },
            _secrets.Host,
            status => status is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden
                ? $"Azure Speech at {_secrets.Host} refused the key: it is wrong, or belongs to another region. Check the key and the endpoint in {VoiceSecrets.FileName}."
                : null,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<SpeechVoice> voices;
        try
        {
            using var json = JsonDocument.Parse(answer);
            voices = json.RootElement.ValueKind == JsonValueKind.Array
                ? Parse(json.RootElement)
                : throw new VoiceServiceException($"Azure Speech at {_secrets.Host} answered with something other than a list of voices.");
        }
        catch (JsonException)
        {
            throw new VoiceServiceException($"Azure Speech at {_secrets.Host} answered with something other than a list of voices.");
        }

        Directory.CreateDirectory(_cache);
        var path = PathOf(_secrets, _cache);
        var partial = path + "." + Guid.NewGuid().ToString("N") + ".part";
        await File.WriteAllTextAsync(partial, JsonSerializer.Serialize(voices.Select(voice => new Dictionary<string, object?>
        {
            ["ShortName"] = voice.ShortName,
            ["Locale"] = voice.Locale,
            ["Gender"] = voice.Gender,
            ["StyleList"] = voice.Styles,
            ["VoiceType"] = voice.VoiceType,
            ["Status"] = voice.Status,
        })), cancellationToken).ConfigureAwait(false);
        File.Move(partial, path, overwrite: true);
        return voices;
    }

    // The service's own shape: ShortName, Locale, Gender, StyleList, VoiceType, Status. A row without a name is skipped.
    private static List<SpeechVoice> Parse(JsonElement list)
    {
        List<SpeechVoice> voices = [];
        foreach (var row in list.EnumerateArray().Where(row => row.ValueKind == JsonValueKind.Object))
        {
            if (Text(row, "ShortName") is not { } name)
            {
                continue;
            }

            List<string> styles = [];
            if (row.TryGetProperty("StyleList", out var styleList) && styleList.ValueKind == JsonValueKind.Array)
            {
                styles.AddRange(styleList.EnumerateArray().Where(style => style.ValueKind == JsonValueKind.String).Select(style => style.GetString()!));
            }

            voices.Add(new SpeechVoice(name, Text(row, "Locale") ?? string.Join('-', name.Split('-').Take(2)), Text(row, "Gender"), styles, Text(row, "VoiceType"), Text(row, "Status")));
        }

        return voices;
    }

    private static string? Text(JsonElement row, string name)
        => row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;

    // Per host: another resource lists other voices. The key is not part of the name.
    private static string PathOf(SpeechSecrets secrets, string cache) => Path.Combine(cache, "voices-" + CloudVoice.Hash(secrets.Host) + ".json");
}

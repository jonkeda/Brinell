using System.Text.Json;

namespace Brinell.Narrator;

/// <summary>
/// Where the cloud voice (<see cref="AzureOpenAiSpeech"/>) is, and its key. Read from the environment
/// (<see cref="KeyVariable"/> and the others), over the first <see cref="FileName"/> found in a suite's
/// folder or a folder above it up to the repository's root, then in <c>%USERPROFILE%\.brinell</c>.
/// The file is ignored by git; the key is never written anywhere but the request's header.
/// </summary>
/// <param name="Endpoint">The Azure OpenAI resource, e.g. <c>https://name.openai.azure.com</c>.</param>
/// <param name="Deployment">The text-to-speech deployment's name.</param>
/// <param name="ApiVersion">The REST API version.</param>
/// <param name="Key">The resource's key.</param>
public sealed record VoiceSecrets(string Endpoint, string Deployment, string ApiVersion, string Key)
{
    /// <summary>The file's name.</summary>
    public const string FileName = "voice.secrets.json";

    /// <summary>The key, for a build agent's secret store.</summary>
    public const string KeyVariable = "BRINELL_VOICE_KEY";

    /// <summary>The endpoint, over the file's.</summary>
    public const string EndpointVariable = "BRINELL_VOICE_ENDPOINT";

    /// <summary>The deployment, over the file's.</summary>
    public const string DeploymentVariable = "BRINELL_VOICE_DEPLOYMENT";

    /// <summary>The API version, over the file's.</summary>
    public const string ApiVersionVariable = "BRINELL_VOICE_API_VERSION";

    /// <summary>The deployment when none is named.</summary>
    public const string DefaultDeployment = "gpt-4o-mini-tts";

    /// <summary>The API version when none is named.</summary>
    public const string DefaultApiVersion = "2025-03-01-preview";

    /// <summary>The voice when a script names none, e.g. <c>coral</c>; null for <see cref="AzureOpenAiSpeech.DefaultVoice"/>.</summary>
    public string? Voice { get; init; }

    /// <summary>The settings without the key, so they can be shown.</summary>
    public override string ToString() => $"{Endpoint}, deployment {Deployment}, api-version {ApiVersion}, key set";

    /// <summary>The settings, or null when no endpoint and key are found.</summary>
    /// <param name="folder">The suite's folder; null to look only in the environment and the user's folder.</param>
    /// <param name="read">Reads an environment variable; the process's by default.</param>
    /// <param name="home">The user's folder; <c>%USERPROFILE%</c> by default.</param>
    public static VoiceSecrets? Find(string? folder, Func<string, string?>? read = null, string? home = null)
    {
        read ??= Environment.GetEnvironmentVariable;
        var file = Places(folder, home).FirstOrDefault(File.Exists) is { } path ? Read(path) : null;
        var endpoint = Value(read(EndpointVariable)) ?? file?.Endpoint;
        var key = Value(read(KeyVariable)) ?? file?.Key;
        if (endpoint is null || key is null)
        {
            return null;
        }

        return new VoiceSecrets(
            endpoint,
            Value(read(DeploymentVariable)) ?? file?.Deployment ?? DefaultDeployment,
            Value(read(ApiVersionVariable)) ?? file?.ApiVersion ?? DefaultApiVersion,
            key)
        { Voice = file?.Voice };
    }

    /// <summary>Where <see cref="Find"/> looks for the file, in order.</summary>
    /// <param name="folder">The suite's folder; null for the user's folder only.</param>
    /// <param name="home">The user's folder; <c>%USERPROFILE%</c> by default.</param>
    public static IReadOnlyList<string> Places(string? folder, string? home = null)
    {
        List<string> places = [];
        for (var directory = folder is null ? null : new DirectoryInfo(folder); directory is not null; directory = directory.Parent)
        {
            places.Add(Path.Combine(directory.FullName, FileName));

            // The repository's root is the last folder looked in: a file above it belongs to no repository.
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                break;
            }
        }

        places.Add(Path.Combine(home ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".brinell", FileName));
        return places;
    }

    private static string? Value(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static FileValues Read(string path)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            return new FileValues(Text("endpoint"), Text("deployment"), Text("apiVersion"), Text("key"), Text("voice"));

            string? Text(string name)
                => json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                    ? Value(value.GetString())
                    : null;
        }
        catch (JsonException exception)
        {
            // The message names the file and the position only: the parser does not quote the text.
            throw new InvalidDataException($"{path} is not valid JSON (line {exception.LineNumber + 1}): see voice.secrets.example.json for its shape.");
        }
    }

    private sealed record FileValues(string? Endpoint, string? Deployment, string? ApiVersion, string? Key, string? Voice);
}

/// <summary>A script names a cloud voice, and no <see cref="VoiceSecrets"/> were found for it.</summary>
public sealed class VoiceNotConfiguredException : Exception
{
    /// <summary>Creates the exception, naming where was looked.</summary>
    /// <param name="voice">The voice that was asked for.</param>
    /// <param name="places">Where the settings were looked for.</param>
    public VoiceNotConfiguredException(string voice, IReadOnlyList<string> places)
        : base($"The voice '{voice}' is a cloud voice, and no key was found for it. Set {VoiceSecrets.KeyVariable} and {VoiceSecrets.EndpointVariable}, or write {VoiceSecrets.FileName} "
               + $"(as voice.secrets.example.json shows) in one of: {string.Join("; ", places)}.")
    {
    }
}

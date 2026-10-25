using System.Text.Json;

namespace Bruinell.Narrator;

/// <summary>
/// Where the cloud voice (<see cref="AzureOpenAiSpeech"/>) is, and its key. Read from the environment
/// (<see cref="KeyVariable"/> and the others), over the first <see cref="FileName"/> found in a suite's
/// folder or a folder above it up to the repository's root, then in <c>%USERPROFILE%\.bruinell</c>.
/// The file is ignored by git; the key is never written anywhere but the request's header.
/// </summary>
/// <param name="Endpoint">The Azure OpenAI resource, e.g. <c>https://name.openai.azure.com</c>; a URL copied with its path works too.</param>
/// <param name="Deployment">The deployment's name: a text-to-speech model, or an audio chat model (<see cref="IsChatAudio"/>).</param>
/// <param name="ApiVersion">The REST API version.</param>
/// <param name="Key">The resource's key.</param>
public sealed record VoiceSecrets(string Endpoint, string Deployment, string ApiVersion, string Key)
{
    /// <summary>The file's name.</summary>
    public const string FileName = "voice.secrets.json";

    /// <summary>The key, for a build agent's secret store.</summary>
    public const string KeyVariable = "BRUINELL_VOICE_KEY";

    /// <summary>The endpoint, over the file's.</summary>
    public const string EndpointVariable = "BRUINELL_VOICE_ENDPOINT";

    /// <summary>The deployment, over the file's.</summary>
    public const string DeploymentVariable = "BRUINELL_VOICE_DEPLOYMENT";

    /// <summary>The API version, over the file's.</summary>
    public const string ApiVersionVariable = "BRUINELL_VOICE_API_VERSION";

    /// <summary>The deployment when none is named.</summary>
    public const string DefaultDeployment = "gpt-4o-mini-tts";

    /// <summary>The API version when none is named.</summary>
    public const string DefaultApiVersion = "2025-03-01-preview";

    /// <summary>The voice when a script names none, e.g. <c>coral</c>; null for <see cref="AzureOpenAiSpeech.DefaultVoice"/>.</summary>
    public string? Voice { get; init; }

    /// <summary>
    /// Whether the deployment is a chat model that answers in audio (<c>gpt-audio</c>,
    /// <c>gpt-4o-audio-preview</c>), asked through chat completions: its name says <c>audio</c>, or the
    /// endpoint was copied from its chat completions URL. Otherwise it is text to speech.
    /// </summary>
    public bool IsChatAudio => Deployment.Contains("audio", StringComparison.OrdinalIgnoreCase)
        || Endpoint.Contains("/chat/completions", StringComparison.OrdinalIgnoreCase);

    /// <summary>The resource the endpoint names, without a path, a query or a trailing slash.</summary>
    public string Resource => ResourceOf(Endpoint);

    /// <summary>The settings without the key, so they can be shown.</summary>
    public override string ToString() => $"{Endpoint}, deployment {Deployment}, api-version {ApiVersion}, key set";

    /// <summary>The resource an endpoint names: what is before <c>/openai</c>, so a URL copied from Foundry works.</summary>
    /// <param name="endpoint">The endpoint as given.</param>
    public static string ResourceOf(string endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        var text = endpoint.Trim();
        if (text.IndexOf('?', StringComparison.Ordinal) is var query and >= 0)
        {
            text = text[..query];
        }

        if (text.IndexOf("/openai", StringComparison.OrdinalIgnoreCase) is var path and >= 0)
        {
            text = text[..path];
        }

        return text.TrimEnd('/');
    }

    /// <summary>Why an endpoint cannot be used, or null when it can.</summary>
    /// <param name="endpoint">The endpoint as given.</param>
    public static string? EndpointProblem(string? endpoint)
    {
        if (!Uri.TryCreate(endpoint?.Trim(), UriKind.Absolute, out var uri))
        {
            return "The endpoint is needed: https://<resource>.openai.azure.com.";
        }

        return uri.Scheme switch
        {
            "https" => null,
            "wss" or "ws" => "That is a Realtime URL, which the narrator does not use: give the resource, https://<resource>.openai.azure.com, and a text-to-speech or gpt-audio deployment.",
            _ => $"The endpoint must start with https://, not {uri.Scheme}://.",
        };
    }

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

        places.Add(Path.Combine(home ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".bruinell", FileName));
        return places;
    }

    /// <summary>
    /// What is set up, and from where, without the key: for a settings page. The values are those
    /// <see cref="Find"/> would use.
    /// </summary>
    /// <param name="folder">The suite's folder; null to look only in the environment and the user's folder.</param>
    /// <param name="read">Reads an environment variable; the process's by default.</param>
    /// <param name="home">The user's folder; <c>%USERPROFILE%</c> by default.</param>
    public static VoiceSecretsDescription Describe(string? folder, Func<string, string?>? read = null, string? home = null)
    {
        read ??= Environment.GetEnvironmentVariable;
        var path = Places(folder, home).FirstOrDefault(File.Exists);
        var file = path is null ? null : Read(path);
        string[] variables = [KeyVariable, EndpointVariable, DeploymentVariable, ApiVersionVariable];
        return new VoiceSecretsDescription(
            Value(read(EndpointVariable)) ?? file?.Endpoint,
            Value(read(DeploymentVariable)) ?? file?.Deployment,
            Value(read(ApiVersionVariable)) ?? file?.ApiVersion,
            file?.Voice,
            Value(read(KeyVariable)) is not null || file?.Key is not null,
            path,
            variables.Where(variable => Value(read(variable)) is not null).ToList());
    }

    /// <summary>
    /// Writes the file whole, readable by the current user only on Windows. An empty
    /// <paramref name="key"/> keeps the key the file has.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="endpoint">The Azure OpenAI resource.</param>
    /// <param name="deployment">The deployment; null for <see cref="DefaultDeployment"/>.</param>
    /// <param name="apiVersion">The API version; null for <see cref="DefaultApiVersion"/>.</param>
    /// <param name="key">The key; empty to keep the saved one.</param>
    /// <param name="voice">The default voice; null for none.</param>
    /// <exception cref="InvalidOperationException">No key is given and the file has none, or the endpoint cannot be used.</exception>
    public static void Save(string path, string endpoint, string? deployment, string? apiVersion, string? key, string? voice)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        if (EndpointProblem(endpoint) is { } problem)
        {
            throw new InvalidOperationException(problem);
        }

        var kept = Value(key) ?? (File.Exists(path) ? Read(path).Key : null)
            ?? throw new InvalidOperationException("A cloud voice needs a key: none was given, and there is none saved.");
        var fields = new Dictionary<string, string>
        {
            ["endpoint"] = endpoint.Trim(),
            ["deployment"] = Value(deployment) ?? DefaultDeployment,
            ["apiVersion"] = Value(apiVersion) ?? DefaultApiVersion,
            ["key"] = kept,
        };
        if (Value(voice) is { } named)
        {
            fields["voice"] = named;
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(folder);
        var partial = Path.Combine(folder, $".{FileName}.{Guid.NewGuid():N}.part");
        File.WriteAllText(partial, JsonSerializer.Serialize(fields, new JsonSerializerOptions { WriteIndented = true }));
        if (OperatingSystem.IsWindows())
        {
            OwnerOnly(partial);
        }

        File.Move(partial, path, overwrite: true);
    }

    /// <summary>Removes the file; nothing when there is none. Environment variables are not touched.</summary>
    /// <param name="path">The file.</param>
    public static void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    // Only the current user may read or change the key: inherited access is removed.
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void OwnerOnly(string path)
    {
        var user = System.Security.Principal.WindowsIdentity.GetCurrent().User!;
        var security = new System.Security.AccessControl.FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(user, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
        new FileInfo(path).SetAccessControl(security);
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

/// <summary>What a cloud voice's settings are, and where they come from; never the key itself.</summary>
/// <param name="Endpoint">The endpoint; null when none is set.</param>
/// <param name="Deployment">The deployment; null for the default.</param>
/// <param name="ApiVersion">The API version; null for the default.</param>
/// <param name="Voice">The default voice; null for none.</param>
/// <param name="HasKey">Whether a key is set.</param>
/// <param name="File">The file the values come from; null when there is none.</param>
/// <param name="FromEnvironment">The environment variables that are set, over the file.</param>
public sealed record VoiceSecretsDescription(string? Endpoint, string? Deployment, string? ApiVersion, string? Voice, bool HasKey, string? File, IReadOnlyList<string> FromEnvironment)
{
    /// <summary>Whether a cloud voice can be used: an endpoint and a key.</summary>
    public bool IsSetUp => Endpoint is not null && HasKey;
}

/// <summary>Whether git ignores <see cref="VoiceSecrets.FileName"/> in a folder.</summary>
public enum VoiceSecretsIgnore
{
    /// <summary>Git ignores it there: it is safe to save.</summary>
    Ignored,

    /// <summary>The folder is in a repository that does not ignore it.</summary>
    NotIgnored,

    /// <summary>The folder is in no git repository (or git is not installed).</summary>
    NoRepository,
}

/// <summary>Where the cloud voice's file may be saved: only where git will not commit it.</summary>
public static class VoiceSecretsPlaces
{
    /// <summary>The line a repository's <c>.gitignore</c> needs.</summary>
    public const string IgnoreRule = "**/voice.secrets.json";

    /// <summary>Whether git ignores the file in <paramref name="folder"/>.</summary>
    /// <param name="folder">The folder the file would be saved in.</param>
    public static VoiceSecretsIgnore IsIgnored(string folder)
    {
        if (Git(folder, "rev-parse --show-toplevel") is not (0, _))
        {
            return VoiceSecretsIgnore.NoRepository;
        }

        return Git(folder, $"check-ignore -q \"{Path.Combine(folder, VoiceSecrets.FileName)}\"").ExitCode == 0
            ? VoiceSecretsIgnore.Ignored
            : VoiceSecretsIgnore.NotIgnored;
    }

    /// <summary>Adds <see cref="IgnoreRule"/> to the <c>.gitignore</c> at the root of <paramref name="folder"/>'s repository.</summary>
    /// <param name="folder">A folder in the repository.</param>
    /// <returns>The <c>.gitignore</c> that was changed.</returns>
    /// <exception cref="InvalidOperationException">The folder is in no repository.</exception>
    public static string AddIgnoreRule(string folder)
    {
        var (exitCode, output) = Git(folder, "rev-parse --show-toplevel");
        if (exitCode != 0 || output.Length == 0)
        {
            throw new InvalidOperationException($"{folder} is in no git repository.");
        }

        var ignore = Path.Combine(Path.GetFullPath(output), ".gitignore");
        var text = File.Exists(ignore) ? File.ReadAllText(ignore) : string.Empty;
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var separator = text.Length == 0 || text.EndsWith('\n') ? string.Empty : newline;
        File.WriteAllText(ignore, $"{text}{separator}{newline}# The narrator's cloud voice: its key{newline}{IgnoreRule}{newline}");
        return ignore;
    }

    private static (int ExitCode, string Output) Git(string folder, string arguments)
    {
        try
        {
            using var git = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", $"-C \"{folder}\" {arguments}")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            })!;
            var output = git.StandardOutput.ReadToEnd().Trim();
            git.StandardError.ReadToEnd();
            git.WaitForExit();
            return (git.ExitCode, output);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // git is not installed: no repository can be told to ignore the file.
            return (-1, string.Empty);
        }
    }
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

using System.IO;
using ClaudeSwitcher.Models;

namespace ClaudeSwitcher.Services;

public sealed class ProfileStore
{
    private const string CredentialsFileName = "credentials.json";
    private const string ClaudeJsonFileName = "claude.json";
    private const string IndexFileName = "profiles.json";

    private readonly FileManager _files;

    public ProfileStore(FileManager files)
    {
        _files = files;

        UserHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        RootFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClaudeSwitcher");

        LiveCredentialsPath = Path.Combine(UserHome, ".claude", ".credentials.json");
        LiveClaudeJsonPath = Path.Combine(UserHome, ".claude.json");
        IndexPath = Path.Combine(RootFolder, IndexFileName);

        Directory.CreateDirectory(RootFolder);
    }

    public string UserHome { get; }
    public string RootFolder { get; }
    public string LiveCredentialsPath { get; }
    public string LiveClaudeJsonPath { get; }
    public string IndexPath { get; }

    public async Task<IReadOnlyList<ProfileEntry>> ListAsync(CancellationToken ct = default)
    {
        var index = await _files.ReadJsonAsync<ProfileIndex>(IndexPath, ct) ?? new ProfileIndex();
        var byName = index.Profiles.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var onDisk = Directory.EnumerateDirectories(RootFolder)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var merged = new List<ProfileEntry>(onDisk.Count);
        foreach (var name in onDisk)
        {
            merged.Add(byName.TryGetValue(name, out var known)
                ? known
                : new ProfileEntry { Name = name });
        }
        return merged;
    }

    public async Task CaptureAsync(string name, CancellationToken ct = default)
    {
        ValidateName(name);

        if (!_files.Exists(LiveCredentialsPath))
            throw new InvalidOperationException(
                $"Not logged in: '{LiveCredentialsPath}' does not exist.");

        var target = Path.Combine(RootFolder, name);
        Directory.CreateDirectory(target);

        await _files.CopyAsync(LiveCredentialsPath, Path.Combine(target, CredentialsFileName), ct);
        if (_files.Exists(LiveClaudeJsonPath))
            await _files.CopyAsync(LiveClaudeJsonPath, Path.Combine(target, ClaudeJsonFileName), ct);
    }

    public async Task ActivateAsync(string name, CancellationToken ct = default)
    {
        ValidateName(name);

        var source = Path.Combine(RootFolder, name);
        var creds = Path.Combine(source, CredentialsFileName);
        var meta = Path.Combine(source, ClaudeJsonFileName);

        if (!_files.Exists(creds))
            throw new FileNotFoundException($"Profile '{name}' has no credentials.json.", creds);

        Directory.CreateDirectory(Path.GetDirectoryName(LiveCredentialsPath)!);
        await _files.CopyAsync(creds, LiveCredentialsPath, ct);
        if (_files.Exists(meta))
            await _files.CopyAsync(meta, LiveClaudeJsonPath, ct);

        await StampActivatedAsync(name, ct);
    }

    public Task DeleteAsync(string name, CancellationToken ct = default)
    {
        ValidateName(name);
        var target = Path.Combine(RootFolder, name);
        if (Directory.Exists(target))
            Directory.Delete(target, recursive: true);
        return RemoveFromIndexAsync(name, ct);
    }

    public Task<ClearLiveResult> ClearLiveAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var removedCredentials = TryDelete(LiveCredentialsPath);
        var removedClaudeJson = TryDelete(LiveClaudeJsonPath);
        return Task.FromResult(new ClearLiveResult(removedCredentials, removedClaudeJson));
    }

    private static bool TryDelete(string path)
    {
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    private async Task StampActivatedAsync(string name, CancellationToken ct)
    {
        var index = await _files.ReadJsonAsync<ProfileIndex>(IndexPath, ct) ?? new ProfileIndex();
        var entry = index.Profiles.FirstOrDefault(
            p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            entry = new ProfileEntry { Name = name };
            index.Profiles.Add(entry);
        }
        entry.LastActivatedUtc = DateTime.UtcNow;
        await _files.WriteJsonAsync(IndexPath, index, ct);
    }

    private async Task RemoveFromIndexAsync(string name, CancellationToken ct)
    {
        var index = await _files.ReadJsonAsync<ProfileIndex>(IndexPath, ct);
        if (index is null) return;
        index.Profiles.RemoveAll(
            p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        await _files.WriteJsonAsync(IndexPath, index, ct);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name is required.", nameof(name));
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Profile name contains invalid characters.", nameof(name));
    }
}

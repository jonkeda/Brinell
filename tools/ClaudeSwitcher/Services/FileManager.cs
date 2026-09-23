using System.IO;
using System.Text.Json;

namespace ClaudeSwitcher.Services;

public sealed class FileManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task CopyAsync(string source, string destination, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        var tempPath = destination + ".tmp";
        await using (var input = File.OpenRead(source))
        await using (var output = File.Create(tempPath))
        {
            await input.CopyToAsync(output, ct);
        }

        if (File.Exists(destination))
            File.Replace(tempPath, destination, destinationBackupFileName: null);
        else
            File.Move(tempPath, destination);
    }

    public async Task<T?> ReadJsonAsync<T>(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path)) return default;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
    }

    public async Task WriteJsonAsync<T>(string path, T value, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, ct);
        }
        if (File.Exists(path))
            File.Replace(tempPath, path, destinationBackupFileName: null);
        else
            File.Move(tempPath, path);
    }

    public bool Exists(string path) => File.Exists(path);
}

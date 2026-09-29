using System.Text.Json;
using System.Text.Json.Nodes;

namespace Brinell.Presenter.Services;

/// <summary>
/// The step breakpoints the IDEs set (<c>%LOCALAPPDATA%\Brinell\bdd-breakpoints.json</c>, AD-022):
/// Presenter reads a feature file's lines to stop Play there, and toggles them from its step
/// list. Only the file's own list is changed; everything else in the file is kept.
/// </summary>
/// <param name="path">The breakpoints file; the IDEs' by default.</param>
public sealed class PresenterBreakpointStore(string? path = null)
{
    private const int Attempts = 10;

    /// <summary>The breakpoints file.</summary>
    public string Path { get; } = path ?? BddBreakpoints.DefaultPath;

    /// <summary>The lines with a breakpoint in a file; empty when none, or the file cannot be read.</summary>
    /// <param name="file">A feature file.</param>
    public IReadOnlySet<int> Lines(string file)
    {
        var root = Read();
        return root?["files"] is JsonObject files && Find(files, file) is { } key && files[key] is JsonArray lines
            ? lines.Select(LineOf).Where(line => line > 0).ToHashSet()
            : new HashSet<int>();
    }

    /// <summary>Sets or clears the breakpoint on a line.</summary>
    /// <param name="file">A feature file.</param>
    /// <param name="line">The line of a step or a call in it.</param>
    /// <returns>Whether the line has a breakpoint now.</returns>
    public bool Toggle(string file, int line)
    {
        var root = Read() ?? new JsonObject();
        if (root["files"] is not JsonObject files)
        {
            files = new JsonObject();
            root["files"] = files;
        }

        var key = Find(files, file) ?? System.IO.Path.GetFullPath(file);
        var lines = (files[key] as JsonArray)?.Select(LineOf).Where(value => value > 0).ToList() ?? [];
        var set = !lines.Remove(line);
        if (set)
        {
            lines.Add(line);
        }

        if (lines.Count == 0)
        {
            files.Remove(key);
        }
        else
        {
            files[key] = new JsonArray([.. lines.Order().Select(value => (JsonNode)value)]);
        }

        Write(root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return set;
    }

    private static int LineOf(JsonNode? node) => node is JsonValue value && value.TryGetValue<int>(out var line) ? line : 0;

    private static string? Find(JsonObject files, string file)
    {
        var full = System.IO.Path.GetFullPath(file);
        return files.Select(entry => entry.Key).FirstOrDefault(key => string.Equals(System.IO.Path.GetFullPath(key), full, StringComparison.OrdinalIgnoreCase));
    }

    // Another process (an IDE, a test that is reading it) may hold the file for a moment.
    private JsonObject? Read()
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return File.Exists(Path) ? JsonNode.Parse(File.ReadAllText(Path)) as JsonObject : null;
            }
            catch (JsonException)
            {
                return null;
            }
            catch (IOException) when (attempt < Attempts)
            {
                Thread.Sleep(20);
            }
        }
    }

    private void Write(string json)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.WriteAllText(Path, json);
                return;
            }
            catch (IOException) when (attempt < Attempts)
            {
                Thread.Sleep(20);
            }
        }
    }
}

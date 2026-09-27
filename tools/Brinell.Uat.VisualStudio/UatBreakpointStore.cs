using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Brinell.Uat.VisualStudio;

/// <summary>
/// The step breakpoints (R2): <c>%LOCALAPPDATA%\Brinell\uat-breakpoints.json</c>, the file
/// <c>UatBreakpoints</c> in <c>Brinell.Bdd</c> reads while a debugger is attached. The preview's
/// gutter, the editor command and the glyph margin all go through this one store. The file is read
/// on each call, so another writer (VS Code) is never overwritten with a stale copy.
/// </summary>
internal sealed class UatBreakpointStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private JObject? _cache;
    private DateTime _cachedAt;

    public UatBreakpointStore(string path) => _path = path;

    /// <summary>The store over the per-user file.</summary>
    public static UatBreakpointStore Instance { get; } = new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brinell", "uat-breakpoints.json"));

    /// <summary>A file's breakpoints changed (its path).</summary>
    public event EventHandler<string>? Changed;

    /// <summary>The lines with a breakpoint in a file, ascending.</summary>
    public IReadOnlyList<int> LinesOf(string file)
    {
        lock (_gate)
        {
            // The glyph margin asks on every layout: the file is parsed again only when it was written since.
            var written = File.Exists(_path) ? File.GetLastWriteTimeUtc(_path) : DateTime.MinValue;
            if (_cache is null || written != _cachedAt)
            {
                _cache = Read();
                _cachedAt = written;
            }

            return Lines(_cache, file).ToList();
        }
    }

    /// <summary>Sets or clears the breakpoint at a line.</summary>
    /// <returns>Whether the line has a breakpoint now.</returns>
    public bool Toggle(string file, int line)
    {
        bool set;
        lock (_gate)
        {
            var root = Read();
            var lines = Lines(root, file).ToList();
            set = !lines.Remove(line);
            if (set)
            {
                lines.Add(line);
            }

            Write(root, file, lines);
        }

        Changed?.Invoke(this, file);
        return set;
    }

    /// <summary>
    /// Keeps breakpoints on their steps while the file is edited: lines after <paramref name="afterLine"/>
    /// move by <paramref name="delta"/>, and lines a deletion removed are cleared.
    /// </summary>
    public void Shift(string file, int afterLine, int delta)
    {
        if (delta == 0)
        {
            return;
        }

        lock (_gate)
        {
            var root = Read();
            var lines = Lines(root, file).ToList();
            if (!lines.Any(line => line > afterLine))
            {
                return;
            }

            var moved = lines
                .Where(line => line <= afterLine || line > afterLine - delta)
                .Select(line => line > afterLine ? line + delta : line)
                .Distinct()
                .ToList();
            Write(root, file, moved);
        }

        Changed?.Invoke(this, file);
    }

    private JObject Read()
    {
        try
        {
            return File.Exists(_path) ? JObject.Parse(File.ReadAllText(_path)) : new JObject();
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            // A file another writer has half written, or holds just now: start from nothing rather than fail the toggle.
            return new JObject();
        }
    }

    private static IEnumerable<int> Lines(JObject root, string file)
        => root["files"] is JObject files && Entry(files, file) is { Value: JArray lines }
            ? lines.Where(token => token.Type == JTokenType.Integer).Select(token => (int)token).OrderBy(line => line)
            : [];

    // The paths are Windows paths: compared as the engine compares them, ignoring case.
    private static JProperty? Entry(JObject files, string file)
        => files.Properties().FirstOrDefault(property => string.Equals(property.Name, file, StringComparison.OrdinalIgnoreCase));

    private void Write(JObject root, string file, IReadOnlyCollection<int> lines)
    {
        if (root["files"] is not JObject files)
        {
            root["files"] = files = new JObject();
        }

        _cache = null;
        Entry(files, file)?.Remove();
        if (lines.Count > 0)
        {
            files[file] = new JArray(lines.OrderBy(line => line));
        }

        // Written whole and then moved into place: the engine never reads half a file.
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, root.ToString(Formatting.Indented));
        if (File.Exists(_path))
        {
            File.Replace(temporary, _path, destinationBackupFileName: null);
        }
        else
        {
            File.Move(temporary, _path);
        }
    }
}

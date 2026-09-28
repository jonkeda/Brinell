using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Brinell.Bdd.VisualStudio;

/// <summary>
/// The step breakpoints (R2): <c>%LOCALAPPDATA%\Brinell\bdd-breakpoints.json</c>, the file
/// <c>BddBreakpoints</c> in <c>Brinell.Bdd</c> reads while a debugger is attached. The preview's
/// gutter, the editor command and the glyph margin all go through this one store. The file is read
/// on each call, so another writer (VS Code) is never overwritten with a stale copy.
/// </summary>
internal sealed class BddBreakpointStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private JObject? _cache;
    private DateTime _cachedAt;

    public BddBreakpointStore(string path) => _path = path;

    /// <summary>The store over the per-user file.</summary>
    public static BddBreakpointStore Instance { get; } = new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brinell", "bdd-breakpoints.json"));

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

        Entry(files, file)?.Remove();
        if (lines.Count > 0)
        {
            files[file] = new JArray(lines.OrderBy(line => line));
        }

        Save(root);
    }

    /// <summary>The file that says where a debugged run is paused, beside the breakpoints (the engine writes it).</summary>
    public string PausedPath => Path.Combine(Path.GetDirectoryName(_path)!, "bdd-paused.json");

    /// <summary>
    /// Writes a one-shot move from a paused step (<c>over</c>, <c>into</c>, <c>out</c>) for the test
    /// process that paused; the engine takes it at the step it lands on.
    /// </summary>
    public void SetMove(string mode, int depth, int process)
    {
        lock (_gate)
        {
            var root = Read();
            root["next"] = new JObject { ["mode"] = mode, ["depth"] = depth, ["process"] = process };
            Save(root);
        }
    }

    /// <summary>Removes a move no run took: a run that ended, or a Continue.</summary>
    public void ClearMove()
    {
        lock (_gate)
        {
            var root = Read();
            if (root.Remove("next"))
            {
                Save(root);
            }
        }
    }

    private void Save(JObject root)
    {
        _cache = null;

        // Written whole and then moved into place: the engine never reads half a file. The engine
        // reads it at every step while debugging, so the move may meet it open: tried again.
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporary = _path + ".tmp";
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.WriteAllText(temporary, root.ToString(Formatting.Indented));
                if (File.Exists(_path))
                {
                    File.Replace(temporary, _path, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(temporary, _path);
                }

                return;
            }
            catch (Exception exception) when (attempt < SaveAttempts && exception is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(SaveRetryPause);
            }
        }
    }

    // How often, and how far apart, a save the engine's read is in the way of is tried.
    private const int SaveAttempts = 10;
    private static readonly TimeSpan SaveRetryPause = TimeSpan.FromMilliseconds(20);
}

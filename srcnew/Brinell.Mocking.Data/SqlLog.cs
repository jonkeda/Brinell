using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brinell.Mocking.Data;

/// <summary>
/// Where the commands go: JSON lines in a folder (<c>sql-&lt;run&gt;.jsonl</c>, one file a process, shared by every log of
/// it), and, when asked, kept in memory for the test that made the log. Thread-safe: tests in parallel each have their
/// own log and write to the one file under a lock.
/// </summary>
public sealed class SqlLog
{
    /// <summary>The environment variable naming the folder.</summary>
    public const string Variable = "BRINELL_SQL_LOG";

    private static readonly object Files = new();
    private static readonly Dictionary<string, StreamWriter> Writers = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly object _gate = new();
    private readonly List<SqlLogEntry>? _kept;
    private readonly string? _file;

    /// <param name="folder">The folder to write to; null writes nothing.</param>
    /// <param name="keep">Whether to keep the entries in memory as well (<see cref="Entries"/>).</param>
    public SqlLog(string? folder, bool keep = false)
    {
        _kept = keep ? [] : null;
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
            _file = Path.Combine(folder, $"sql-{Run}.jsonl");
        }
    }

    /// <summary>The log the environment asks for (<see cref="Variable"/>), or null when it asks for none.</summary>
    public static SqlLog? FromEnvironment(bool keep = false)
        => Environment.GetEnvironmentVariable(Variable) is { Length: > 0 } folder ? new SqlLog(folder, keep) : null;

    /// <summary>This process's run: its start and id, as the file's name has it.</summary>
    public static string Run { get; } = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Process.GetCurrentProcess().Id}";

    /// <summary>Raised for every entry, on the thread that sent the command.</summary>
    public event Action<SqlLogEntry>? Logged;

    /// <summary>The entries kept in memory, in the order they ended; empty unless kept.</summary>
    public IReadOnlyList<SqlLogEntry> Entries
    {
        get
        {
            lock (_gate)
            {
                return _kept is null ? [] : [.. _kept];
            }
        }
    }

    /// <summary>The time of the entries kept in memory.</summary>
    public TimeSpan Elapsed => TimeSpan.FromMilliseconds(Entries.Sum(entry => entry.Ms));

    /// <summary>Forgets the entries kept in memory; the file keeps them.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _kept?.Clear();
        }
    }

    /// <summary>Logs <paramref name="entry"/>, naming its run and, when it has none, the test of <see cref="SqlLogScope"/>.</summary>
    public void Write(SqlLogEntry entry)
    {
        entry.Run ??= Run;
        entry.Test ??= SqlLogScope.Current();
        lock (_gate)
        {
            _kept?.Add(entry);
        }

        if (_file is not null)
        {
            var line = JsonSerializer.Serialize(entry, Json);
            lock (Files)
            {
                if (!Writers.TryGetValue(_file, out var writer))
                {
                    writer = new StreamWriter(new FileStream(_file, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
                    Writers[_file] = writer;
                }

                writer.WriteLine(line);
            }
        }

        Logged?.Invoke(entry);
    }

    /// <summary>The entries of every log file in the folder <paramref name="folderOrFile"/> (<c>*.jsonl</c>), or of that one file.</summary>
    public static IEnumerable<SqlLogEntry> Read(string folderOrFile)
    {
        var files = File.Exists(folderOrFile) ? [folderOrFile] : Directory.GetFiles(folderOrFile, "*.jsonl", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (line.Length > 0 && JsonSerializer.Deserialize<SqlLogEntry>(line, Json) is { } entry)
                {
                    yield return entry;
                }
            }
        }
    }

    /// <summary>An entry for <paramref name="text"/>, its objects found, started at <paramref name="start"/>.</summary>
    public static SqlLogEntry Entry(string kind, string? text, IEnumerable<string>? parameters, DateTime start, TimeSpan elapsed) => new()
    {
        Kind = kind,
        Text = text is null ? null : Statements.Normalize(Statements.WithoutLiterals(text)),
        Parameters = parameters?.ToArray() is { Length: > 0 } names ? names : null,
        Objects = text is null ? null : Statements.Objects(text).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() is { Length: > 0 } objects ? objects : null,
        Start = start,
        Ms = elapsed.TotalMilliseconds,
    };
}

/// <summary>
/// Which test is running, for a log that can't reference the test framework (a .NET Framework app, the netstandard2.0
/// build): a suite sets it once, such as <c>SqlLogScope.Current = () =&gt; TestScope.Current.Test</c>. Null outside a test.
/// </summary>
public static class SqlLogScope
{
    public static Func<string?> Current { get; set; } = () => null;
}

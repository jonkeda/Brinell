using System.Data.SqlTypes;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bruinell.Mocking.Data.Replay;

/// <summary>How a test treats the database: untouched, recorded, or replayed with none.</summary>
public enum ReplayMode
{
    Off,
    Record,
    Replay,
}

/// <summary>One column of a recorded result set.</summary>
public sealed record RecordedColumn(string Name, string ClrType, string DataTypeName);

/// <summary>One result set: its columns and rows, each value in <see cref="Values"/>' form.</summary>
public sealed record RecordedResult(IReadOnlyList<RecordedColumn> Columns, IReadOnlyList<IReadOnlyList<JsonNode?>> Rows);

/// <summary>A recorded error: the provider's exception type and what replay needs to raise it again (<see cref="ProviderRules"/>).</summary>
public sealed record RecordedError(string Type, int Number, byte State, byte Class, string Message, string? Server);

/// <summary>One call: a command and what came back, in the order the app made it.</summary>
public sealed class Exchange
{
    public int Index { get; set; }

    /// <summary><c>reader</c>, <c>non-query</c> or <c>scalar</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>The command's text; a stored procedure called as one, as its <c>EXEC</c>.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The input parameters by name, each in <see cref="Values"/>' form.</summary>
    public Dictionary<string, JsonNode?> Parameters { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Parameters whose values differ between two recordings: bound at replay, not matched.</summary>
    public List<string> Volatile { get; set; } = [];

    public List<RecordedResult>? Results { get; set; }

    public int RecordsAffected { get; set; } = -1;

    public JsonNode? Scalar { get; set; }

    /// <summary>Output, input-output and return-value parameters as they came back, given back to the command in replay.</summary>
    public Dictionary<string, JsonNode?>? Outputs { get; set; }

    public RecordedError? Error { get; set; }

    /// <summary>Whether the command changes the database: writes are barriers, replayed in their order.</summary>
    public bool IsWrite => Statements.IsWrite(Text);

    /// <summary>A copy with other results, the rest shared.</summary>
    internal Exchange With(List<RecordedResult>? results) => new()
    {
        Index = Index,
        Kind = Kind,
        Text = Text,
        Parameters = Parameters,
        Volatile = Volatile,
        Results = results,
        RecordsAffected = RecordsAffected,
        Scalar = Scalar,
        Outputs = Outputs,
        Error = Error,
    };
}

/// <summary>
/// The schema as the app sees it now, without a database: the versions that shape the SQL (EF Core's, the provider's),
/// every table, view, procedure and function by its name (<c>dbo.places</c>) with its hash, and what each one's body
/// references (<see cref="SchemaSnapshot.ToSchema"/>).
/// </summary>
public sealed record Schema(
    IReadOnlyDictionary<string, string> Versions,
    IReadOnlyDictionary<string, string> Objects,
    IReadOnlyDictionary<string, IReadOnlyList<string>> References)
{
    /// <summary>The hash of an unknown object: a name in a command the schema doesn't hold.</summary>
    public const string Unknown = "?";

    /// <summary>
    /// The stamp of a recording whose commands named <paramref name="named"/>: those objects and every object their
    /// bodies reference, followed through views into tables, so a procedure's recording goes stale with its tables.
    /// </summary>
    public Stamp StampFor(IEnumerable<string> named)
    {
        var touched = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var waiting = new Queue<string>(named);
        while (waiting.Count > 0)
        {
            var name = waiting.Dequeue();
            if (touched.Add(name) && References.TryGetValue(name, out var referenced))
            {
                foreach (var item in referenced)
                {
                    waiting.Enqueue(item);
                }
            }
        }

        return new Stamp
        {
            Versions = new Dictionary<string, string>(Versions.ToDictionary(pair => pair.Key, pair => pair.Value), StringComparer.Ordinal),
            Objects = touched.ToDictionary(name => name, name => Objects.TryGetValue(name, out var hash) ? hash : Unknown, StringComparer.OrdinalIgnoreCase),
        };
    }
}

/// <summary>
/// What a recording was made against: the versions that shape the SQL, and each object its commands touched, named or
/// through a view's or procedure's body, with that object's hash. Only an upgrade, or a change to one of those objects,
/// makes it stale: on a schema of thousands of tables a change elsewhere leaves it alone.
/// </summary>
public sealed class Stamp
{
    public Dictionary<string, string> Versions { get; set; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> Objects { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>What differs from <paramref name="now"/>, each as one line for a person.</summary>
    public IReadOnlyList<string> Differences(Schema now)
    {
        var differences = new List<string>();
        foreach (var version in Versions)
        {
            var current = now.Versions.TryGetValue(version.Key, out var value) ? value : Schema.Unknown;
            if (current != version.Value)
            {
                differences.Add($"{version.Key}: {version.Value}, now {current}");
            }
        }

        foreach (var item in Objects)
        {
            if (!now.Objects.TryGetValue(item.Key, out var hash))
            {
                if (item.Value != Schema.Unknown)
                {
                    differences.Add($"{item.Key} is gone");
                }
            }
            else if (hash != item.Value)
            {
                differences.Add($"{item.Key} changed");
            }
        }

        return differences;
    }
}

/// <summary>A test's recording on one head: its stamp and its exchanges, in order.</summary>
public sealed class Recording
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public int Version { get; set; } = 2;

    public Stamp? Stamp { get; set; }

    public List<Exchange> Exchanges { get; set; } = [];

    public static Recording? Load(string path) => File.Exists(path) ? JsonSerializer.Deserialize<Recording>(File.ReadAllText(path), Json) : null;

    public void Save(string path)
    {
        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } folder)
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
    }

    /// <summary>
    /// Marks as volatile every parameter whose value differs from <paramref name="previous"/>'s: recording twice finds the
    /// GUIDs, clocks and ids nobody configures. Paired as replay matches: the same text, its n-th time between the same two
    /// writes; a repeat beyond the other run's count is paired with its last one. Throws when a command has no counterpart.
    /// </summary>
    public void MarkVolatile(Recording previous)
    {
        var earlier = Keys(previous.Exchanges, out _);
        Keys(Exchanges, out var keys);
        for (var i = 0; i < Exchanges.Count; i++)
        {
            var now = Exchanges[i];
            if (!Counterpart(earlier, keys[i], out var index))
            {
                throw new InvalidOperationException($"Command {i} has no counterpart in the other recording:\n{now.Text}");
            }

            var before = previous.Exchanges[index];
            now.Volatile = [.. now.Parameters.Keys
                .Where(name => !before.Parameters.TryGetValue(name, out var old) || !Values.Same(old, now.Parameters[name]))
                .Union(before.Volatile)
                .OrderBy(name => name, StringComparer.Ordinal)];
        }
    }

    // The other recording's exchange for a key: its n-th time, or the last time before n it was sent.
    private static bool Counterpart(Dictionary<string, int> keys, (string Text, int Occurrence) key, out int index)
    {
        for (var occurrence = key.Occurrence; occurrence > 0; occurrence--)
        {
            if (keys.TryGetValue(key.Text + "|" + occurrence.ToString(CultureInfo.InvariantCulture), out index))
            {
                return true;
            }
        }

        index = -1;
        return false;
    }

    // Each exchange's key: the writes before it, its text, and which time it is that text since the last write.
    private static Dictionary<string, int> Keys(List<Exchange> exchanges, out (string Text, int Occurrence)[] byIndex)
    {
        var keys = new Dictionary<string, int>(StringComparer.Ordinal);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        byIndex = new (string, int)[exchanges.Count];
        var writes = 0;
        for (var i = 0; i < exchanges.Count; i++)
        {
            var text = writes.ToString(CultureInfo.InvariantCulture) + "|" + Statements.Key(exchanges[i].Text);
            var occurrence = (seen.TryGetValue(text, out var count) ? count : 0) + 1;
            seen[text] = occurrence;
            byIndex[i] = (text, occurrence);
            keys[text + "|" + occurrence.ToString(CultureInfo.InvariantCulture)] = i;
            if (exchanges[i].IsWrite)
            {
                writes++;
            }
        }

        return keys;
    }
}

/// <summary>
/// Values as JSON with their type, so a reader gives back what the provider gave: <c>{"t":"Guid","v":"..."}</c>.
/// Compared by value, not representation: integers as one type, decimals without trailing zeros, instants in UTC.
/// </summary>
public static class Values
{
    public static JsonNode? ToJson(object? value) => value switch
    {
        null or DBNull => null,
        INullable { IsNull: true } => null,
        SqlBytes bytes => Tagged("SqlBytes", Convert.ToBase64String(bytes.Value)),
        byte[] bytes => Tagged("Bytes", Convert.ToBase64String(bytes)),
        Guid guid => Tagged("Guid", guid.ToString()),
        string text => Tagged("String", text),
        char character => Tagged("String", character.ToString()),
        bool flag => Tagged("Boolean", flag ? "true" : "false"),
        decimal number => Tagged("Decimal", number.ToString(CultureInfo.InvariantCulture)),
        double number => Tagged("Double", number.ToString("R", CultureInfo.InvariantCulture)),
        float number => Tagged("Single", number.ToString("R", CultureInfo.InvariantCulture)),
        DateTimeOffset moment => Tagged("DateTimeOffset", moment.ToString("O", CultureInfo.InvariantCulture)),
        DateTime moment => Tagged("DateTime", moment.ToString("O", CultureInfo.InvariantCulture)),
        TimeSpan span => Tagged("TimeSpan", span.ToString("c", CultureInfo.InvariantCulture)),
        byte or sbyte or short or ushort or int or uint or long => Tagged(value.GetType().Name, Convert.ToString(value, CultureInfo.InvariantCulture)!),
        Enum named => Tagged("Int64", Convert.ToInt64(named, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)),
        _ => throw new NotSupportedException($"SQL replay can't record a {value.GetType().FullName}."),
    };

    public static object? FromJson(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        var text = node["v"]!.GetValue<string>();
        return node["t"]!.GetValue<string>() switch
        {
            "SqlBytes" => new SqlBytes(Convert.FromBase64String(text)),
            "Bytes" => Convert.FromBase64String(text),
            "Guid" => Guid.Parse(text),
            "String" => text,
            "Boolean" => text == "true",
            "Decimal" => decimal.Parse(text, CultureInfo.InvariantCulture),
            "Double" => double.Parse(text, CultureInfo.InvariantCulture),
            "Single" => float.Parse(text, CultureInfo.InvariantCulture),
            "DateTimeOffset" => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            "DateTime" => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            "TimeSpan" => TimeSpan.Parse(text, CultureInfo.InvariantCulture),
            "Byte" => byte.Parse(text, CultureInfo.InvariantCulture),
            "SByte" => sbyte.Parse(text, CultureInfo.InvariantCulture),
            "Int16" => short.Parse(text, CultureInfo.InvariantCulture),
            "UInt16" => ushort.Parse(text, CultureInfo.InvariantCulture),
            "Int32" => int.Parse(text, CultureInfo.InvariantCulture),
            "UInt32" => uint.Parse(text, CultureInfo.InvariantCulture),
            "Int64" => long.Parse(text, CultureInfo.InvariantCulture),
            var type => throw new NotSupportedException($"SQL replay can't read a recorded {type}."),
        };
    }

    /// <summary>The value's identity for matching: integers as one type, decimals without trailing zeros, instants in UTC.</summary>
    public static string? Canonical(JsonNode? node)
    {
        var value = FromJson(node);
        return value switch
        {
            null => null,
            byte or sbyte or short or ushort or int or uint or long => "I:" + Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            decimal number => "D:" + number.ToString("0.############################", CultureInfo.InvariantCulture),
            DateTimeOffset moment => "T:" + moment.UtcTicks.ToString(CultureInfo.InvariantCulture),
            DateTime moment => "T:" + moment.Ticks.ToString(CultureInfo.InvariantCulture),
            _ => node!["t"]!.GetValue<string>() + ":" + node["v"]!.GetValue<string>(),
        };
    }

    public static bool Same(JsonNode? left, JsonNode? right) => Canonical(left) == Canonical(right);

    private static JsonObject Tagged(string type, string value) => new() { ["t"] = type, ["v"] = value };
}

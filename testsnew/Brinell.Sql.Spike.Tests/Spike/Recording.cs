using System.Data.SqlTypes;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Brinell.Sql.Spike.Tests.Spike;

/// <summary>How a run treats the database (<c>BRINELL_SQL_REPLAY</c>): untouched, recorded, or replayed with none.</summary>
public enum ReplayMode
{
    Off,
    Record,
    Replay,

    /// <summary>Against the database, writing only the schema snapshot: after a schema change, before replaying.</summary>
    Snapshot,
}

/// <summary>One column of a recorded result set.</summary>
public sealed record RecordedColumn(string Name, string ClrType, string DataTypeName);

/// <summary>One result set: its columns and rows, each value in <see cref="Values"/>' form.</summary>
public sealed record RecordedResult(IReadOnlyList<RecordedColumn> Columns, IReadOnlyList<IReadOnlyList<JsonNode?>> Rows);

/// <summary>A recorded error: what replay needs to raise the provider's own exception.</summary>
public sealed record RecordedError(string Type, int Number, byte State, byte Class, string Message, string? Server);

/// <summary>One call: a command and what came back, in the order the app made it.</summary>
public sealed class Exchange
{
    public int Index { get; set; }

    /// <summary><c>reader</c>, <c>non-query</c> or <c>scalar</c>.</summary>
    public required string Kind { get; set; }

    public required string Text { get; set; }

    /// <summary>The parameters by name, each in <see cref="Values"/>' form.</summary>
    public Dictionary<string, JsonNode?> Parameters { get; set; } = [];

    /// <summary>Parameters whose values differ between two recordings: bound at replay, not matched.</summary>
    public List<string> Volatile { get; set; } = [];

    public List<RecordedResult>? Results { get; set; }

    public int RecordsAffected { get; set; } = -1;

    public JsonNode? Scalar { get; set; }

    public RecordedError? Error { get; set; }

    /// <summary>Whether the command changes the database: writes are barriers, replayed in their order.</summary>
    public bool IsWrite => Statements.IsWrite(Text);
}

/// <summary>
/// The schema as the app sees it now, without a database: EF Core's and SqlClient's versions, every table, view,
/// procedure and function by its name (<c>dbo.places</c>) with its hash, and what each one's body references
/// (<see cref="SchemaSnapshot"/>).
/// </summary>
public sealed record Schema(
    string Ef,
    string SqlClient,
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
        while (waiting.TryDequeue(out var name))
        {
            if (touched.Add(name))
            {
                foreach (var referenced in References.GetValueOrDefault(name) ?? [])
                {
                    waiting.Enqueue(referenced);
                }
            }
        }

        return new Stamp
        {
            Ef = Ef,
            SqlClient = SqlClient,
            Objects = touched.ToDictionary(name => name, name => Objects.GetValueOrDefault(name, Unknown), StringComparer.OrdinalIgnoreCase),
        };
    }
}

/// <summary>
/// What a recording was made against: EF Core's and SqlClient's versions, and each object its commands touched, named or
/// through a view's or procedure's body, with that object's hash. Only an upgrade, or a change to one of those objects,
/// makes it stale: on a schema of thousands of tables a change elsewhere leaves it alone. Said before any miss, since
/// such a change reaches most of its queries.
/// </summary>
public sealed class Stamp
{
    public required string Ef { get; init; }

    public required string SqlClient { get; init; }

    public Dictionary<string, string> Objects { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>What differs from <paramref name="now"/>, as "what: recorded, now".</summary>
    public IReadOnlyList<string> Differences(Schema now) =>
    [
        .. Ef != now.Ef ? [$"EF Core: {Ef}, now {now.Ef}"] : Array.Empty<string>(),
        .. SqlClient != now.SqlClient ? [$"SqlClient: {SqlClient}, now {now.SqlClient}"] : Array.Empty<string>(),
        .. Objects
            .Where(item => now.Objects.GetValueOrDefault(item.Key, Schema.Unknown) != item.Value)
            .Select(item => now.Objects.ContainsKey(item.Key) ? $"{item.Key} changed" : $"{item.Key} is gone"),
    ];
}

/// <summary>A test's recording on one head.</summary>
public sealed class Recording
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public int Version { get; set; } = 1;

    public Stamp? Stamp { get; set; }

    public List<Exchange> Exchanges { get; set; } = [];

    public static Recording? Load(string path) => File.Exists(path) ? JsonSerializer.Deserialize<Recording>(File.ReadAllText(path), Json) : null;

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
    }

    /// <summary>
    /// Marks as volatile every parameter whose value differs from <paramref name="previous"/>'s, exchange by exchange:
    /// recording twice finds the GUIDs, clocks and ids nobody configures. Throws when a command has no counterpart: the
    /// two runs did different things.
    /// </summary>
    public void MarkVolatile(Recording previous)
    {
        // Paired as replay matches: the same text, its n-th time between the same two writes. Reads between two writes
        // may come in another order each run (on bUnit the start page's query races the page navigated to), and in the
        // browser as often as prerendering and page loads make them: a repeat beyond the other run's count is paired with
        // its last one, as replay answers it.
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
            now.Volatile = [.. now.Parameters.Keys.Where(name =>
                !before.Parameters.TryGetValue(name, out var old) || !Values.Same(old, now.Parameters[name]))
                .Union(before.Volatile)
                .Order(StringComparer.Ordinal)];
        }
    }

    // The other recording's exchange for a key: its n-th time, or the last time before n it was sent.
    private static bool Counterpart(Dictionary<string, int> keys, string key, out int index)
    {
        var cut = key.LastIndexOf('|');
        for (var occurrence = int.Parse(key[(cut + 1)..], System.Globalization.CultureInfo.InvariantCulture); occurrence > 0; occurrence--)
        {
            if (keys.TryGetValue($"{key[..cut]}|{occurrence}", out index))
            {
                return true;
            }
        }

        index = -1;
        return false;
    }

    // Each exchange's key: the writes before it, its text, and which time it is that text since the last write.
    private static Dictionary<string, int> Keys(List<Exchange> exchanges, out string[] byIndex)
    {
        var keys = new Dictionary<string, int>(StringComparer.Ordinal);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        byIndex = new string[exchanges.Count];
        var writes = 0;
        for (var i = 0; i < exchanges.Count; i++)
        {
            var text = $"{writes}|{Statements.Key(exchanges[i].Text)}";
            var occurrence = seen[text] = seen.GetValueOrDefault(text) + 1;
            byIndex[i] = $"{text}|{occurrence}";
            keys[byIndex[i]] = i;
            if (exchanges[i].IsWrite)
            {
                writes++;
            }
        }

        return keys;
    }
}

/// <summary>Command text: normalized for keys, and classified as a write.</summary>
public static partial class Statements
{
    public static string Normalize(string text) => Whitespace().Replace(text, " ").Trim();

    /// <summary>
    /// The text's identity for matching: normalized, and a leading <c>SELECT</c>'s columns sorted. Their order says
    /// nothing about the query (EF orders them by property name, so renaming a property reorders them); replay gives
    /// the recorded columns back in the order the live query asks for (<see cref="Projection"/>).
    /// </summary>
    public static string Key(string text)
    {
        var normalized = Insert().Replace(Normalize(text), insert =>
            $"INSERT INTO {insert.Groups["table"].Value} ({string.Join(", ", Split(insert.Groups["columns"].Value).Order(StringComparer.Ordinal))}) VALUES (...)");
        return Projection(normalized) is { } projection
            ? projection.Prefix + string.Join(", ", projection.Columns.Order(StringComparer.Ordinal)) + projection.Tail
            : normalized;
    }

    /// <summary>
    /// The parameter that fills each column of the text's <c>INSERT</c>s, by column: EF numbers an insert's parameters in
    /// its column order, which follows property names, so a parameter is matched by the column it fills, not its name.
    /// </summary>
    public static IReadOnlyDictionary<string, string> InsertParameters(string text)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match insert in Insert().Matches(Normalize(text)))
        {
            var table = insert.Groups["table"].Value;
            foreach (var (column, value) in Split(insert.Groups["columns"].Value).Zip(Split(insert.Groups["values"].Value)))
            {
                if (value.StartsWith('@'))
                {
                    parameters[$"{table}.{column}"] = value;
                }
            }
        }

        return parameters;
    }

    /// <summary>
    /// The objects a command names: tables and views after <c>FROM</c>, <c>JOIN</c>, <c>INTO</c>, <c>UPDATE</c> or
    /// <c>MERGE</c>, procedures after <c>EXEC</c>; as <c>schema.name</c> in lower case, <c>dbo</c> when none is written.
    /// What a view's or procedure's body touches comes from the schema's references (<see cref="Schema.StampFor"/>).
    /// </summary>
    public static IEnumerable<string> Objects(string text)
        => TableNames().Matches(Normalize(text)).Concat(Procedures().Matches(Normalize(text))).Select(match =>
        {
            var parts = match.Groups["name"].Value.Split('.').Select(part => part.Trim('[', ']').ToLowerInvariant()).ToArray();
            return parts.Length == 1 ? "dbo." + parts[0] : string.Join('.', parts[^2..]);
        });

    private static IEnumerable<string> Split(string list) => list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// A leading <c>SELECT</c>'s columns: the text before them, each column as written, and the text from <c>FROM</c>
    /// on; null for any other statement. Commas and <c>FROM</c> inside parentheses belong to a column.
    /// </summary>
    public static (string Prefix, IReadOnlyList<string> Columns, string Tail)? Projection(string normalized)
    {
        var select = SelectStart().Match(normalized);
        if (!select.Success)
        {
            return null;
        }

        var columns = new List<string>();
        var depth = 0;
        var start = select.Length;
        for (var i = start; i < normalized.Length; i++)
        {
            switch (normalized[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    columns.Add(normalized[start..i].Trim());
                    start = i + 1;
                    break;
                case ' ' when depth == 0 && normalized.AsSpan(i).StartsWith(" FROM ", StringComparison.OrdinalIgnoreCase):
                    columns.Add(normalized[start..i].Trim());
                    return (select.Value, columns, normalized[i..]);
            }
        }

        return null;
    }

    public static bool IsWrite(string text) => Writes().IsMatch(text);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"INSERT INTO (?<table>\S+) \((?<columns>[^)]*)\) VALUES \((?<values>[^)]*)\)", RegexOptions.IgnoreCase)]
    private static partial Regex Insert();

    [GeneratedRegex(@"\b(?:FROM|JOIN|INTO|UPDATE|MERGE)\s+(?<name>(?:\[[^\]]+\]|\w+)(?:\.(?:\[[^\]]+\]|\w+))*)(?!\s*\()", RegexOptions.IgnoreCase)]
    private static partial Regex TableNames();

    [GeneratedRegex(@"\bEXEC(?:UTE)?\s+(?:@\w+\s*=\s*)?(?<name>(?:\[[^\]]+\]|\w+)(?:\.(?:\[[^\]]+\]|\w+))*)", RegexOptions.IgnoreCase)]
    private static partial Regex Procedures();

    [GeneratedRegex(@"^SELECT (DISTINCT )?(TOP\([^)]*\) )?", RegexOptions.IgnoreCase)]
    private static partial Regex SelectStart();

    [GeneratedRegex(@"\b(INSERT|UPDATE|DELETE|MERGE)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Writes();
}

/// <summary>
/// Values as JSON with their type, so a reader gives back what the provider gave: <c>{"t":"Guid","v":"..."}</c>.
/// Comparison is by value, not representation (.my/sql-replay/proposal.md, parameter normalization).
/// </summary>
public static class Values
{
    public static JsonNode? ToJson(object? value) => value switch
    {
        null or DBNull => null,
        SqlBytes { IsNull: true } => null,
        SqlBytes bytes => Tagged("SqlBytes", Convert.ToBase64String(bytes.Value)),
        byte[] bytes => Tagged("Bytes", Convert.ToBase64String(bytes)),
        Guid guid => Tagged("Guid", guid.ToString()),
        string text => Tagged("String", text),
        bool flag => Tagged("Boolean", flag ? "true" : "false"),
        decimal number => Tagged("Decimal", number.ToString(CultureInfo.InvariantCulture)),
        double number => Tagged("Double", number.ToString("R", CultureInfo.InvariantCulture)),
        float number => Tagged("Single", number.ToString("R", CultureInfo.InvariantCulture)),
        DateTimeOffset moment => Tagged("DateTimeOffset", moment.ToString("O", CultureInfo.InvariantCulture)),
        DateTime moment => Tagged("DateTime", moment.ToString("O", CultureInfo.InvariantCulture)),
        TimeSpan span => Tagged("TimeSpan", span.ToString("c", CultureInfo.InvariantCulture)),
        byte or short or int or long => Tagged(value.GetType().Name, Convert.ToString(value, CultureInfo.InvariantCulture)!),
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
            "Int16" => short.Parse(text, CultureInfo.InvariantCulture),
            "Int32" => int.Parse(text, CultureInfo.InvariantCulture),
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
            byte or short or int or long => "I:" + Convert.ToInt64(value, CultureInfo.InvariantCulture),
            decimal number => "D:" + number.ToString("0.############################", CultureInfo.InvariantCulture),
            DateTimeOffset moment => "T:" + moment.UtcTicks,
            DateTime moment => "T:" + moment.Ticks,
            _ => node!["t"]!.GetValue<string>() + ":" + node["v"]!.GetValue<string>(),
        };
    }

    public static bool Same(JsonNode? left, JsonNode? right) => Canonical(left) == Canonical(right);

    private static JsonObject Tagged(string type, string value) => new() { ["t"] = type, ["v"] = value };
}

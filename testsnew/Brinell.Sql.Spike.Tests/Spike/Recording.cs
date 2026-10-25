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

/// <summary>A test's recording on one head.</summary>
public sealed class Recording
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public int Version { get; set; } = 1;

    public List<Exchange> Exchanges { get; set; } = [];

    public static Recording? Load(string path) => File.Exists(path) ? JsonSerializer.Deserialize<Recording>(File.ReadAllText(path), Json) : null;

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
    }

    /// <summary>
    /// Marks as volatile every parameter whose value differs from <paramref name="previous"/>'s, exchange by exchange:
    /// recording twice finds the GUIDs, clocks and ids nobody configures. Throws when the two runs sent different commands.
    /// </summary>
    public void MarkVolatile(Recording previous)
    {
        if (previous.Exchanges.Count != Exchanges.Count)
        {
            throw new InvalidOperationException($"The two recordings sent {previous.Exchanges.Count} and {Exchanges.Count} commands: not the same run.");
        }

        for (var i = 0; i < Exchanges.Count; i++)
        {
            var now = Exchanges[i];
            var before = previous.Exchanges[i];
            if (Statements.Normalize(now.Text) != Statements.Normalize(before.Text))
            {
                throw new InvalidOperationException($"Command {i} differs between the two recordings:\n{before.Text}\n{now.Text}");
            }

            now.Volatile = [.. now.Parameters.Keys.Where(name =>
                !before.Parameters.TryGetValue(name, out var old) || !Values.Same(old, now.Parameters[name]))
                .Union(before.Volatile)
                .Order(StringComparer.Ordinal)];
        }
    }
}

/// <summary>Command text: normalized for keys, and classified as a write.</summary>
public static partial class Statements
{
    public static string Normalize(string text) => Whitespace().Replace(text, " ").Trim();

    public static bool IsWrite(string text) => Writes().IsMatch(text);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

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

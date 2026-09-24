using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Brinell.Uat;

/// <summary>
/// Step tables as typed rows: binding a table to records, and checking real rows against a table.
/// </summary>
/// <remarks>
/// <para>
/// A column is a constructor parameter (for <see cref="Bind{T}"/>) or a property (for the
/// assertions) of <c>T</c>, matched without case and spaces: <c>Due Date</c> is <c>DueDate</c>.
/// An unknown column is an error naming the known ones, never a column that is quietly ignored.
/// </para>
/// <para>
/// Cells are read invariantly. An empty cell or <c>null</c> is <c>null</c> where the type allows
/// it. Dates may be relative to the test data's pinned now: <c>today</c>, <c>+2d</c>, <c>-1d</c>.
/// </para>
/// </remarks>
public static partial class UatTableBinding
{
    /// <summary>One <typeparamref name="T"/> per row, built through its widest public constructor.</summary>
    /// <param name="table">The step table.</param>
    /// <param name="now">What relative dates are resolved against.</param>
    /// <exception cref="InvalidOperationException">An unknown column, a missing required column, or a cell that does not fit.</exception>
    public static IReadOnlyList<T> Bind<T>(this UatTable table, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(table);

        var constructor = typeof(T).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"{typeof(T).Name} has no public constructor to bind a table to.");
        var parameters = constructor.GetParameters();
        var byColumn = MapColumns(table, parameters.Select(p => p.Name!).ToList(), typeof(T));

        var missing = parameters
            .Where(p => !p.HasDefaultValue && !byColumn.Values.Contains(p.Name!))
            .Select(p => p.Name!)
            .ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"{table.Source}: the table needs column(s) {string.Join(", ", missing.Select(Title))} for {typeof(T).Name}.");
        }

        var nullability = new NullabilityInfoContext();
        return table.Rows.Select(row =>
        {
            var arguments = parameters.Select(parameter =>
            {
                var column = byColumn.FirstOrDefault(pair => pair.Value == parameter.Name).Key;
                return column is null
                    ? parameter.DefaultValue
                    : Convert(row.Cells[column], parameter.ParameterType, nullability.Create(parameter).WriteState == NullabilityState.Nullable, now, row.Source, column);
            }).ToArray();

            return (T)constructor.Invoke(arguments);
        }).ToList();
    }

    /// <summary>Every row of the table matches an item of <paramref name="actual"/>, on the listed columns.</summary>
    /// <exception cref="InvalidOperationException">A row has no match; the message shows the expected and actual rows.</exception>
    public static void AssertContains<T>(this UatTable table, IEnumerable<T> actual, DateTimeOffset? now = null)
        => Compare(table, actual, now, exactly: false);

    /// <summary>The rows of the table and the items of <paramref name="actual"/> match one to one, on the listed columns.</summary>
    /// <exception cref="InvalidOperationException">A row has no match, or an item is not in the table.</exception>
    public static void AssertExactly<T>(this UatTable table, IEnumerable<T> actual, DateTimeOffset? now = null)
        => Compare(table, actual, now, exactly: true);

    /// <summary>No item of <paramref name="actual"/> matches a row of the table, on the listed columns.</summary>
    /// <exception cref="InvalidOperationException">An item matches; the message shows it.</exception>
    public static void AssertDoesNotContain<T>(this UatTable table, IEnumerable<T> actual, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(actual);

        var columns = PropertyColumns<T>(table);
        var expected = ExpectedRows(table, columns, now);
        var items = actual.ToList();
        var found = items.Where(item => expected.Any(row => Matches(row, item, columns))).ToList();
        if (found.Count > 0)
        {
            throw new InvalidOperationException(
                $"{table.Source}: expected no {typeof(T).Name} like these rows, but found {found.Count}." + Environment.NewLine +
                Format("found", table.Columns, found.Select(item => Cells(item, columns)), _ => null));
        }
    }

    private static void Compare<T>(UatTable table, IEnumerable<T> actual, DateTimeOffset? now, bool exactly)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(actual);

        var columns = PropertyColumns<T>(table);
        var expected = ExpectedRows(table, columns, now);
        var items = actual.ToList();
        var unused = new List<T>(items);
        var missing = new List<int>();

        for (var i = 0; i < expected.Count; i++)
        {
            var match = unused.FindIndex(item => Matches(expected[i], item, columns));
            if (match >= 0)
            {
                unused.RemoveAt(match);
            }
            else
            {
                missing.Add(i);
            }
        }

        var extra = exactly ? unused : [];
        if (missing.Count == 0 && extra.Count == 0)
        {
            return;
        }

        var summary = new List<string>();
        if (missing.Count > 0)
        {
            summary.Add($"{missing.Count} row(s) missing");
        }

        if (extra.Count > 0)
        {
            summary.Add($"{extra.Count} extra");
        }

        var message = new StringBuilder()
            .AppendLine($"{table.Source}: {typeof(T).Name} rows do not match - {string.Join(", ", summary)}.")
            .AppendLine(Format("expected", table.Columns, table.Rows.Select(r => table.Columns.Select(c => r.Cells[c]).ToList()), i => missing.Contains(i) ? "<- missing" + Closest(expected[i], items, columns) : null))
            .Append(Format("actual", table.Columns, items.Select(item => Cells(item, columns)), i => extra.Contains(items[i]) ? "<- extra" : null));

        throw new InvalidOperationException(message.ToString());
    }

    private static List<PropertyColumn> PropertyColumns<T>(UatTable table)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead).ToList();
        var byColumn = MapColumns(table, properties.Select(p => p.Name).ToList(), typeof(T));
        var nullability = new NullabilityInfoContext();
        return table.Columns.Select(column =>
        {
            var property = properties.First(p => p.Name == byColumn[column]);
            return new PropertyColumn(column, property, nullability.Create(property).ReadState == NullabilityState.Nullable);
        }).ToList();
    }

    private static List<object?[]> ExpectedRows(UatTable table, List<PropertyColumn> columns, DateTimeOffset? now)
        => table.Rows
            .Select(row => columns
                .Select(c => Convert(row.Cells[c.Column], c.Property.PropertyType, c.Nullable, now, row.Source, c.Column))
                .ToArray())
            .ToList();

    private static bool Matches<T>(object?[] expected, T item, List<PropertyColumn> columns)
        => columns.Select((column, index) => Equals(expected[index], column.Property.GetValue(item))).All(equal => equal);

    private static string Closest<T>(object?[] expected, List<T> items, List<PropertyColumn> columns)
    {
        var best = items
            .Select(item => (Item: item, Differences: columns.Where((column, index) => !Equals(expected[index], column.Property.GetValue(item))).ToList()))
            .Where(candidate => candidate.Differences.Count < columns.Count)
            .OrderBy(candidate => candidate.Differences.Count)
            .FirstOrDefault();

        return best.Item is null
            ? string.Empty
            : $" (closest differs in {string.Join(", ", best.Differences.Select(d => $"{d.Column}: {Show(d.Property.GetValue(best.Item))}"))})";
    }

    private static List<string> Cells<T>(T item, List<PropertyColumn> columns)
        => columns.Select(column => Show(column.Property.GetValue(item))).ToList();

    private static string Show(object? value) => value switch
    {
        null => "null",
        "" => "\"\"",
        string text when string.IsNullOrWhiteSpace(text) => $"\"{text}\"",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    private static string Format(string label, IReadOnlyList<string> columns, IEnumerable<IReadOnlyList<string>> rows, Func<int, string?> note)
    {
        var all = rows.ToList();
        var widths = columns.Select((c, i) => Math.Max(c.Length, all.Count == 0 ? 0 : all.Max(r => r[i].Length))).ToList();
        string Line(IReadOnlyList<string> cells) => "    | " + string.Join(" | ", cells.Select((cell, i) => cell.PadRight(widths[i]))) + " |";

        var builder = new StringBuilder().AppendLine($"  {label}:").AppendLine(Line(columns));
        for (var i = 0; i < all.Count; i++)
        {
            var suffix = note(i) is { } text ? "   " + text : string.Empty;
            builder.AppendLine(Line(all[i]) + suffix);
        }

        if (all.Count == 0)
        {
            builder.AppendLine("    (none)");
        }

        return builder.ToString().TrimEnd();
    }

    private static Dictionary<string, string> MapColumns(UatTable table, IReadOnlyList<string> members, Type type)
    {
        var byKey = members.GroupBy(Key).ToDictionary(g => g.Key, g => g.First());
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var column in table.Columns)
        {
            if (!byKey.TryGetValue(Key(column), out var member))
            {
                var suggestion = members.OrderBy(m => Distance(Key(column), Key(m))).FirstOrDefault(m => Distance(Key(column), Key(m)) <= 2);
                throw new InvalidOperationException(
                    $"{table.Source}: column '{column}' is not a field of {type.Name}." +
                    (suggestion is null ? string.Empty : $" Did you mean '{Title(suggestion)}'?") +
                    $" Fields: {string.Join(", ", members.Select(Title))}.");
            }

            map[column] = member;
        }

        return map;
    }

    private static object? Convert(string cell, Type type, bool nullableReference, DateTimeOffset? now, UatSourceLocation source, string column)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        var target = underlying ?? type;
        var text = cell.Trim();

        if ((underlying is not null || nullableReference) && (text.Length == 0 || text == "null"))
        {
            return null;
        }

        try
        {
            if (target == typeof(string))
            {
                return cell;
            }

            if (target.IsEnum)
            {
                return Enum.TryParse(target, text, ignoreCase: true, out var value)
                    ? value
                    : throw new FormatException($"'{text}' is not one of {string.Join(", ", Enum.GetNames(target))}");
            }

            if (RelativeDays(text) is { } days)
            {
                var anchor = now ?? throw new FormatException($"'{text}' is a relative date, but no 'now' was given to resolve it against");
                return target == typeof(DateOnly) ? DateOnly.FromDateTime(anchor.UtcDateTime).AddDays(days)
                    : target == typeof(DateTimeOffset) ? anchor.AddDays(days)
                    : target == typeof(DateTime) ? anchor.UtcDateTime.AddDays(days)
                    : throw new FormatException($"'{text}' is a relative date, but the field is {target.Name}");
            }

            return target switch
            {
                _ when target == typeof(DateOnly) => DateOnly.Parse(text, CultureInfo.InvariantCulture),
                _ when target == typeof(DateTimeOffset) => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture),
                _ when target == typeof(DateTime) => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                _ when target == typeof(Guid) => Guid.Parse(text),
                _ when target == typeof(TimeSpan) => TimeSpan.Parse(text, CultureInfo.InvariantCulture),
                _ when target == typeof(bool) => bool.Parse(text),
                _ when typeof(IConvertible).IsAssignableFrom(target) => System.Convert.ChangeType(text, target, CultureInfo.InvariantCulture),
                _ => throw new FormatException($"cells cannot be read as {target.Name}"),
            };
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentException)
        {
            throw new InvalidOperationException($"{source}: column '{column}': {exception.Message.TrimEnd('.')}.", exception);
        }
    }

    private static int? RelativeDays(string text)
    {
        if (text.Equals("today", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var match = RelativeDaysRegex().Match(text);
        return match.Success ? int.Parse(match.Groups["days"].Value, CultureInfo.InvariantCulture) * (match.Groups["sign"].Value == "-" ? -1 : 1) : null;
    }

    private static string Key(string name) => name.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

    private static string Title(string name) => name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];

    private static int Distance(string a, string b)
    {
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1];
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            }

            previous = current;
        }

        return previous[b.Length];
    }

    [GeneratedRegex("^(?<sign>[+-])(?<days>\\d+)d$", RegexOptions.IgnoreCase)]
    private static partial Regex RelativeDaysRegex();

    private sealed record PropertyColumn(string Column, PropertyInfo Property, bool Nullable);
}

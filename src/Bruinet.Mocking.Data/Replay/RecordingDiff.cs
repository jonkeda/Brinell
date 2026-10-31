using System.Globalization;
using System.Text.Json.Nodes;

namespace Bruinet.Mocking.Data.Replay;

/// <summary>
/// What a re-record changed, one line per change, for a person to read before the recording goes in: the stamp's objects,
/// commands added and removed, and for a command in both its rows, columns, values, rows affected, scalar, outputs and
/// error. Commands are paired by kind and key, the n-th time with the n-th time, whatever the writes between them; values
/// are compared after the volatile parameters bind old to new, as replay binds them.
/// </summary>
public static class RecordingDiff
{
    private const int Shown = 70;

    /// <summary>The changes from <paramref name="before"/> to <paramref name="after"/>; either null for a recording added or removed.</summary>
    /// <param name="before">The recording as it was.</param>
    /// <param name="after">The recording as it is.</param>
    /// <param name="volatileBefore">Values volatile in the other recordings of the suite as it was (<see cref="RecordingReview.VolatileValues(string)"/>):
    /// tests that share an app read each other's new ids and titles.</param>
    /// <param name="volatileAfter">The same, as it is.</param>
    public static IReadOnlyList<string> Compare(Recording? before, Recording? after, IReadOnlyCollection<string>? volatileBefore = null, IReadOnlyCollection<string>? volatileAfter = null)
    {
        if (before is null || after is null)
        {
            return after is not null ? [$"new recording, {after.Exchanges.Count} commands"]
                : before is not null ? ["recording removed"]
                : [];
        }

        var lines = new List<string>();
        Stamps(before.Stamp, after.Stamp, lines);

        var earlier = new Dictionary<string, Exchange>(StringComparer.Ordinal);
        foreach (var (key, exchange) in Keyed(before.Exchanges))
        {
            earlier[key] = exchange;
        }

        // A read sent more or fewer times than before, where the other recording has it too, is no change: replay answers
        // a read again between two writes.
        var beforeTexts = new HashSet<string>(before.Exchanges.Select(Text), StringComparer.Ordinal);
        var afterTexts = new HashSet<string>(after.Exchanges.Select(Text), StringComparer.Ordinal);
        var pairs = new List<(Exchange Before, Exchange After)>();
        foreach (var (key, exchange) in Keyed(after.Exchanges))
        {
            if (earlier.TryGetValue(key, out var old))
            {
                pairs.Add((old, exchange));
                earlier.Remove(key);
            }
            else if (exchange.IsWrite || !beforeTexts.Contains(Text(exchange)))
            {
                lines.Add($"+ {Describe(exchange)}");
            }
        }

        lines.AddRange(earlier.Values
            .Where(exchange => exchange.IsWrite || !afterTexts.Contains(Text(exchange)))
            .OrderBy(exchange => exchange.Index)
            .Select(exchange => $"- {Describe(exchange)}"));

        // Volatile values, old to new, from every pair's parameters: an id a write made shows in the rows that read it.
        var bindings = new Bindings(volatileBefore, volatileAfter);
        foreach (var (old, now) in pairs)
        {
            foreach (var name in now.Volatile.Union(old.Volatile))
            {
                if (old.Parameters.TryGetValue(name, out var was) && now.Parameters.TryGetValue(name, out var @is) && Values.Canonical(was) is { } from)
                {
                    bindings.Map[from] = Values.Canonical(@is);
                }
            }
        }

        foreach (var (old, now) in pairs)
        {
            var changes = Changes(old, now, bindings);
            if (changes.Count > 0)
            {
                lines.Add($"~ {Describe(now)}: {string.Join("; ", changes)}");
            }
        }

        return lines;
    }

    private static void Stamps(Stamp? before, Stamp? after, List<string> lines)
    {
        var old = before?.Objects ?? [];
        var now = after?.Objects ?? [];
        foreach (var name in now.Keys.Union(old.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(name => name, StringComparer.Ordinal))
        {
            var had = old.TryGetValue(name, out var oldHash);
            var has = now.TryGetValue(name, out var newHash);
            if (had && has && oldHash != newHash)
            {
                lines.Add($"stamp: {name} changed");
            }
            else if (had != has)
            {
                lines.Add(has ? $"stamp: + {name}" : $"stamp: - {name}");
            }
        }

        foreach (var version in (after?.Versions ?? []).Where(pair => before?.Versions.TryGetValue(pair.Key, out var was) == true && was != pair.Value))
        {
            lines.Add($"stamp: {version.Key} {before!.Versions[version.Key]} -> {version.Value}");
        }
    }

    private static List<string> Changes(Exchange before, Exchange after, Bindings bindings)
    {
        var changes = new List<string>();
        if (before.Error?.Number != after.Error?.Number)
        {
            changes.Add($"error {Error(before)} -> {Error(after)}");
        }

        if (before.RecordsAffected != after.RecordsAffected)
        {
            changes.Add($"affected {before.RecordsAffected} -> {after.RecordsAffected}");
        }

        if (!Same(before.Scalar, after.Scalar, bindings))
        {
            changes.Add($"scalar {Values.Canonical(before.Scalar) ?? "null"} -> {Values.Canonical(after.Scalar) ?? "null"}");
        }

        foreach (var name in (after.Outputs?.Keys ?? Enumerable.Empty<string>()).Union(before.Outputs?.Keys ?? Enumerable.Empty<string>()).OrderBy(name => name, StringComparer.Ordinal))
        {
            var was = before.Outputs is { } old && old.TryGetValue(name, out var value) ? value : null;
            var @is = after.Outputs is { } now && now.TryGetValue(name, out var current) ? current : null;
            if (!Same(was, @is, bindings))
            {
                changes.Add($"{name} {Values.Canonical(was) ?? "null"} -> {Values.Canonical(@is) ?? "null"}");
            }
        }

        var oldSets = before.Results ?? [];
        var newSets = after.Results ?? [];
        if (oldSets.Count != newSets.Count)
        {
            changes.Add($"result sets {oldSets.Count} -> {newSets.Count}");
        }

        for (var set = 0; set < Math.Min(oldSets.Count, newSets.Count); set++)
        {
            var label = newSets.Count > 1 ? $"set {set + 1} " : string.Empty;
            Results(oldSets[set], newSets[set], bindings, label, changes);
        }

        return changes;
    }

    // Columns by name, whatever their order; rows by position.
    private static void Results(RecordedResult before, RecordedResult after, Bindings bindings, string label, List<string> changes)
    {
        var oldColumns = before.Columns.Select(column => column.Name).ToList();
        var newColumns = after.Columns.Select(column => column.Name).ToList();
        var added = newColumns.Except(oldColumns, StringComparer.OrdinalIgnoreCase).ToList();
        var removed = oldColumns.Except(newColumns, StringComparer.OrdinalIgnoreCase).ToList();
        if (added.Count + removed.Count > 0)
        {
            changes.Add(label + "columns " + string.Join(" ", added.Select(name => "+" + name).Concat(removed.Select(name => "-" + name))));
        }

        if (before.Rows.Count != after.Rows.Count)
        {
            changes.Add($"{label}rows {before.Rows.Count} -> {after.Rows.Count}");
        }

        var shared = newColumns.Intersect(oldColumns, StringComparer.OrdinalIgnoreCase).ToList();
        var differing = 0;
        var columns = new SortedSet<string>(StringComparer.Ordinal);
        for (var row = 0; row < Math.Min(before.Rows.Count, after.Rows.Count); row++)
        {
            var changed = shared.Where(name => !Same(
                before.Rows[row][oldColumns.FindIndex(column => string.Equals(column, name, StringComparison.OrdinalIgnoreCase))],
                after.Rows[row][newColumns.FindIndex(column => string.Equals(column, name, StringComparison.OrdinalIgnoreCase))],
                bindings)).ToList();
            if (changed.Count > 0)
            {
                differing++;
                columns.UnionWith(changed);
            }
        }

        if (differing > 0)
        {
            changes.Add($"{label}values in {differing} {(differing == 1 ? "row" : "rows")} ({string.Join(", ", columns)})");
        }
    }

    private static bool Same(JsonNode? before, JsonNode? after, Bindings bindings)
    {
        var old = Values.Canonical(before);
        var now = Values.Canonical(after);
        return (old is not null && bindings.Map.TryGetValue(old, out var bound) ? bound : old) == now
            || old is not null && now is not null && bindings.Before.Contains(old) && bindings.After.Contains(now);
    }

    // Volatile values old to new from this recording's parameters, and the suite's volatile values on either side.
    private sealed class Bindings(IReadOnlyCollection<string>? before, IReadOnlyCollection<string>? after)
    {
        public Dictionary<string, string?> Map { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Before { get; } = new(before ?? [], StringComparer.Ordinal);

        public HashSet<string> After { get; } = new(after ?? [], StringComparer.Ordinal);
    }

    private static string Text(Exchange exchange) => exchange.Kind + "|" + Statements.Key(exchange.Text);

    private static IEnumerable<(string Key, Exchange Exchange)> Keyed(IEnumerable<Exchange> exchanges)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var exchange in exchanges)
        {
            var key = Text(exchange);
            var occurrence = (seen.TryGetValue(key, out var count) ? count : 0) + 1;
            seen[key] = occurrence;
            yield return (key + "|" + occurrence.ToString(CultureInfo.InvariantCulture), exchange);
        }
    }

    private static string Error(Exchange exchange) => exchange.Error is { } error ? error.Number.ToString(CultureInfo.InvariantCulture) : "none";

    private static string Describe(Exchange exchange)
    {
        var text = Statements.Normalize(exchange.Text);
        return exchange.Kind + " " + (text.Length > Shown ? text.Substring(0, Shown) + "..." : text);
    }
}

using System.Text;
using System.Text.Json.Nodes;

namespace Bruinell.Sql.Spike.Tests.Spike;

/// <summary>A command replay could not answer from the recording.</summary>
public sealed class ReplayMissException(string message) : Exception(message);

/// <summary>
/// One test's recording or replay (.my/sql-replay/proposal.md, matching and value binding). In replay a command is
/// answered by the recorded exchange with the same key and occurrence: writes in their recorded order, as barriers;
/// the reads between two writes in any order. Volatile values are bound, recorded to live, the first time they meet,
/// and recorded results are rewritten through the bindings.
/// </summary>
public sealed class ReplaySession
{
    private readonly Lock _gate = new();
    private readonly Recording _recording;
    private readonly Recording? _previous;
    private readonly int[] _epochs;
    private readonly bool[] _used;
    private readonly Dictionary<string, JsonNode?> _bindings = new(StringComparer.Ordinal);
    private readonly List<string> _misses = [];
    private int _writes;

    private readonly string? _stale;

    private readonly Schema _schema;

    public ReplaySession(ReplayMode mode, string path, Schema schema)
    {
        Mode = mode;
        Path = path;
        _schema = schema;
        if (mode == ReplayMode.Replay)
        {
            _recording = Recording.Load(path) ?? throw new ReplayMissException($"No recording at {path}: record the test first (BRUINELL_SQL_REPLAY=Record, twice).");
            // BRUINELL_SQL_STAMP=ignore: replay a stale recording anyway, to see which commands a change touched.
            var ignoreStamp = string.Equals(Environment.GetEnvironmentVariable("BRUINELL_SQL_STAMP"), "ignore", StringComparison.OrdinalIgnoreCase);
            if (!ignoreStamp && _recording.Stamp?.Differences(schema) is { Count: > 0 } differences)
            {
                _stale = $"Stale recording {System.IO.Path.GetFileName(path)}: re-record it (BRUINELL_SQL_REPLAY=Record, twice). Changed: {string.Join("; ", differences)}.";
            }
        }
        else
        {
            _recording = new Recording();
            _previous = Recording.Load(path);
            if (_previous?.Stamp?.Differences(schema) is not { Count: 0 })
            {
                // Recorded against other tables or versions: not the same run, so not a source of volatile values.
                _previous = null;
            }
        }

        _epochs = new int[_recording.Exchanges.Count];
        _used = new bool[_recording.Exchanges.Count];
        var writes = 0;
        for (var i = 0; i < _epochs.Length; i++)
        {
            _epochs[i] = writes;
            if (_recording.Exchanges[i].IsWrite)
            {
                writes++;
            }
        }
    }

    public ReplayMode Mode { get; }

    public string Path { get; }

    public IReadOnlyList<string> Misses
    {
        get
        {
            lock (_gate)
            {
                return [.. _misses];
            }
        }
    }

    /// <summary>Record mode: keeps <paramref name="exchange"/> in the order it happened.</summary>
    public void Record(Exchange exchange)
    {
        lock (_gate)
        {
            exchange.Index = _recording.Exchanges.Count;
            _recording.Exchanges.Add(exchange);
        }
    }

    /// <summary>Record mode: saves the recording, its volatile values found against the one it replaces.</summary>
    public void Finish()
    {
        if (Mode != ReplayMode.Record)
        {
            return;
        }

        if (_previous is not null)
        {
            _recording.MarkVolatile(_previous);
        }

        _recording.Stamp = _schema.StampFor(_recording.Exchanges.SelectMany(exchange => Statements.Objects(exchange.Text)));
        _recording.Save(Path);
    }

    /// <summary>Replay mode: the recorded exchange answering <paramref name="live"/>, its results rewritten through the bindings.</summary>
    public Exchange Answer(Exchange live)
    {
        lock (_gate)
        {
            if (_stale is not null)
            {
                if (_misses.Count == 0)
                {
                    _misses.Add(_stale);
                }

                throw new ReplayMissException(_stale);
            }

            var key = Statements.Key(live.Text);
            Dictionary<string, JsonNode?>? bound = null;
            var index = -1;

            // An unused exchange first; then, for a read, one already used: between two writes the same read returns the
            // same rows, so a read repeated more often than when recorded (the browser's prerender, a page opened twice)
            // is answered again rather than missed.
            foreach (var reuse in (bool[])[false, true])
            {
                for (var i = 0; i < _recording.Exchanges.Count && index < 0; i++)
                {
                    var recorded = _recording.Exchanges[i];
                    if (_used[i] != reuse || (reuse && live.IsWrite) || recorded.Kind != live.Kind || Statements.Key(recorded.Text) != key)
                    {
                        continue;
                    }

                    // A write is only the next write; a read only one between the same two writes.
                    if (live.IsWrite ? !recorded.IsWrite || _epochs[i] != _writes || NextWrite() != i : recorded.IsWrite || _epochs[i] != _writes)
                    {
                        continue;
                    }

                    if (Matches(recorded, live, out bound))
                    {
                        index = i;
                    }
                }

                if (index >= 0)
                {
                    break;
                }
            }

            if (index < 0)
            {
                var miss = Describe(live, key);
                _misses.Add(miss);
                throw new ReplayMissException(miss);
            }

            _used[index] = true;
            foreach (var (recorded, value) in bound!)
            {
                _bindings[recorded] = value;
            }

            if (live.IsWrite)
            {
                _writes++;
            }

            return Reorder(Rewrite(_recording.Exchanges[index]), live);
        }
    }

    private int NextWrite()
    {
        for (var i = 0; i < _recording.Exchanges.Count; i++)
        {
            if (_recording.Exchanges[i].IsWrite && !_used[i])
            {
                return i;
            }
        }

        return -1;
    }

    private bool Matches(Exchange recorded, Exchange live, out Dictionary<string, JsonNode?> bound)
    {
        bound = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        if (recorded.Parameters.Count != live.Parameters.Count)
        {
            return false;
        }

        var actuals = Aligned(recorded, live);
        foreach (var (name, value) in recorded.Parameters)
        {
            if (!actuals.TryGetValue(name, out var actual))
            {
                return false;
            }

            var canonical = Values.Canonical(value);
            if (!recorded.Volatile.Contains(name))
            {
                if (!Values.Same(value, actual))
                {
                    return false;
                }

                continue;
            }

            // Bound already: the live value must be the one it was bound to. Not yet: bind it once this exchange matches.
            if (canonical is not null && _bindings.TryGetValue(canonical, out var binding))
            {
                if (!Values.Same(binding, actual))
                {
                    return false;
                }
            }
            else if (canonical is not null)
            {
                bound[canonical] = actual?.DeepClone();
            }
        }

        return true;
    }

    private Exchange Rewrite(Exchange recorded)
    {
        if (_bindings.Count == 0 || recorded.Results is null)
        {
            return recorded;
        }

        return new Exchange
        {
            Index = recorded.Index,
            Kind = recorded.Kind,
            Text = recorded.Text,
            Parameters = recorded.Parameters,
            RecordsAffected = recorded.RecordsAffected,
            Scalar = recorded.Scalar,
            Error = recorded.Error,
            Results = [.. recorded.Results.Select(result => new RecordedResult(
                result.Columns,
                [.. result.Rows.Select(row => (IReadOnlyList<JsonNode?>)[.. row.Select(Bound)])]))],
        };
    }

    // The live parameters under the recorded names: an insert's by the column each fills, the others by their own name.
    private static Dictionary<string, JsonNode?> Aligned(Exchange recorded, Exchange live)
    {
        var aligned = new Dictionary<string, JsonNode?>(live.Parameters, StringComparer.Ordinal);
        var was = Statements.InsertParameters(recorded.Text);
        if (was.Count == 0)
        {
            return aligned;
        }

        foreach (var (column, liveName) in Statements.InsertParameters(live.Text))
        {
            if (was.TryGetValue(column, out var recordedName) && live.Parameters.TryGetValue(liveName, out var value))
            {
                aligned[recordedName] = value;
            }
        }

        return aligned;
    }

    // The recorded columns in the order the live query asks for them: matched as a set, read by ordinal.
    private static Exchange Reorder(Exchange recorded, Exchange live)
    {
        if (recorded.Results is not [var first, ..] results
            || Statements.Projection(Statements.Normalize(recorded.Text)) is not { } was
            || Statements.Projection(Statements.Normalize(live.Text)) is not { } now
            || was.Columns.SequenceEqual(now.Columns)
            || was.Columns.Count != first.Columns.Count)
        {
            return recorded;
        }

        var recordedColumns = was.Columns.ToList();
        var order = now.Columns.Select(column => recordedColumns.IndexOf(column)).ToArray();
        var reordered = new RecordedResult(
            [.. order.Select(i => first.Columns[i])],
            [.. first.Rows.Select(row => (IReadOnlyList<JsonNode?>)[.. order.Select(i => row[i])])]);
        return new Exchange
        {
            Index = recorded.Index,
            Kind = recorded.Kind,
            Text = recorded.Text,
            Parameters = recorded.Parameters,
            Volatile = recorded.Volatile,
            RecordsAffected = recorded.RecordsAffected,
            Scalar = recorded.Scalar,
            Error = recorded.Error,
            Results = [reordered, .. results.Skip(1)],
        };
    }

    private JsonNode? Bound(JsonNode? value)
        => Values.Canonical(value) is { } canonical && _bindings.TryGetValue(canonical, out var live) ? live?.DeepClone() : value?.DeepClone();

    private string Describe(Exchange live, string key)
    {
        var text = new StringBuilder($"SQL replay miss in {System.IO.Path.GetFileName(Path)} after {_writes} writes: no recorded {live.Kind} answers\n  {key}\n  with {Show(live.Parameters)}");
        var unused = _recording.Exchanges.Where((exchange, i) => !_used[i]).ToList();
        var same = unused.Where(exchange => Statements.Key(exchange.Text) == key).ToList();
        if (same.Count > 0)
        {
            return text.Append("\nNearest, the same text with other values:\n  ")
                .AppendJoin("\n  ", same.Select(exchange => $"#{exchange.Index} {(exchange.IsWrite ? "write" : "read")} with {Show(exchange.Parameters)}, volatile [{string.Join(", ", exchange.Volatile)}]"))
                .ToString();
        }

        // Another text: the unused command of the same kind and place that shares the longest start, and where they part.
        var nearest = unused
            .Where(exchange => exchange.Kind == live.Kind && exchange.IsWrite == live.IsWrite && _epochs[exchange.Index] == _writes)
            .MaxBy(exchange => Common(Statements.Key(exchange.Text), key));
        if (nearest is null)
        {
            return text.Append("\nNo recorded command is left at this point: the app sends a command it didn't send when recorded.").ToString();
        }

        var recorded = Statements.Key(nearest.Text);
        var at = Common(recorded, key);
        return text.Append($"\nNearest, #{nearest.Index}, parts at character {at}:\n  recorded ...{Around(recorded, at)}...\n  now      ...{Around(key, at)}...").ToString();
    }

    private static int Common(string left, string right)
    {
        var i = 0;
        while (i < left.Length && i < right.Length && left[i] == right[i])
        {
            i++;
        }

        return i;
    }

    private static string Around(string text, int at)
    {
        var start = Math.Max(0, at - 20);
        return text.Substring(start, Math.Min(text.Length - start, 60));
    }

    private static string Show(Dictionary<string, JsonNode?> parameters)
        => "{" + string.Join(", ", parameters.Select(pair => $"{pair.Key}={Values.Canonical(pair.Value) ?? "null"}")) + "}";
}

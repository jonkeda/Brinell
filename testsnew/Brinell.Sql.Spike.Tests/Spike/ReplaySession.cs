using System.Text;
using System.Text.Json.Nodes;

namespace Brinell.Sql.Spike.Tests.Spike;

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

    public ReplaySession(ReplayMode mode, string path, Stamp stamp)
    {
        Mode = mode;
        Path = path;
        if (mode == ReplayMode.Replay)
        {
            _recording = Recording.Load(path) ?? throw new ReplayMissException($"No recording at {path}: record the test first (BRINELL_SQL_REPLAY=Record, twice).");
            // BRINELL_SQL_STAMP=ignore: replay a stale recording anyway, to see which commands a change touched.
            var ignoreStamp = string.Equals(Environment.GetEnvironmentVariable("BRINELL_SQL_STAMP"), "ignore", StringComparison.OrdinalIgnoreCase);
            if (!ignoreStamp && _recording.Stamp?.Differences(stamp) is { Count: > 0 } differences)
            {
                _stale = $"Stale recording {System.IO.Path.GetFileName(path)}: re-record it (BRINELL_SQL_REPLAY=Record, twice). Changed: {string.Join("; ", differences)}.";
            }
        }
        else
        {
            _recording = new Recording { Stamp = stamp };
            _previous = Recording.Load(path);
            if (_previous?.Stamp != stamp)
            {
                // Recorded against other migrations or versions: not the same run, so not a source of volatile values.
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

            var key = Statements.Normalize(live.Text);
            Dictionary<string, JsonNode?>? bound = null;
            var index = -1;
            for (var i = 0; i < _recording.Exchanges.Count && index < 0; i++)
            {
                var recorded = _recording.Exchanges[i];
                if (_used[i] || recorded.Kind != live.Kind || Statements.Normalize(recorded.Text) != key)
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

            return Rewrite(_recording.Exchanges[index]);
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

        foreach (var (name, value) in recorded.Parameters)
        {
            if (!live.Parameters.TryGetValue(name, out var actual))
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

    private JsonNode? Bound(JsonNode? value)
        => Values.Canonical(value) is { } canonical && _bindings.TryGetValue(canonical, out var live) ? live?.DeepClone() : value?.DeepClone();

    private string Describe(Exchange live, string key)
    {
        var text = new StringBuilder($"SQL replay miss in {System.IO.Path.GetFileName(Path)} after {_writes} writes: no recorded {live.Kind} answers\n  {key}\n  with {Show(live.Parameters)}");
        var unused = _recording.Exchanges.Where((exchange, i) => !_used[i]).ToList();
        var same = unused.Where(exchange => Statements.Normalize(exchange.Text) == key).ToList();
        if (same.Count > 0)
        {
            return text.Append("\nNearest, the same text with other values:\n  ")
                .AppendJoin("\n  ", same.Select(exchange => $"#{exchange.Index} {(exchange.IsWrite ? "write" : "read")} with {Show(exchange.Parameters)}, volatile [{string.Join(", ", exchange.Volatile)}]"))
                .ToString();
        }

        // Another text: the unused command of the same kind and place that shares the longest start, and where they part.
        var nearest = unused
            .Where(exchange => exchange.Kind == live.Kind && exchange.IsWrite == live.IsWrite && _epochs[exchange.Index] == _writes)
            .MaxBy(exchange => Common(Statements.Normalize(exchange.Text), key));
        if (nearest is null)
        {
            return text.Append("\nNo recorded command is left at this point: the app sends a command it didn't send when recorded.").ToString();
        }

        var recorded = Statements.Normalize(nearest.Text);
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

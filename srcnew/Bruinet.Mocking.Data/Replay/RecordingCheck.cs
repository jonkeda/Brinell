using System.Text.Json;

namespace Bruinet.Mocking.Data.Replay;

/// <summary>One recording against the schema now: what changed among the objects it touched, empty when it holds.</summary>
public sealed record RecordingState(string Path, IReadOnlyList<string> Objects, IReadOnlyList<string> Differences)
{
    public bool IsStale => Differences.Count > 0;
}

/// <summary>
/// Which recordings a schema change made stale, read from their stamps without replaying them: after rewriting the
/// snapshot, the tests to re-record. On a schema of thousands of tables a change reaches only the recordings that touch it.
/// </summary>
public static class RecordingCheck
{
    /// <summary>
    /// Every recording under <paramref name="folder"/> (<c>&lt;head&gt;/&lt;test&gt;.json</c>) against <paramref name="schema"/>.
    /// A recording's versions are compared only with those <paramref name="schema"/> holds: the tool doesn't know the app's.
    /// Files that aren't recordings (the snapshot itself) are skipped.
    /// </summary>
    public static IReadOnlyList<RecordingState> Check(string folder, Schema schema)
    {
        var states = new List<RecordingState>();
        foreach (var path in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
        {
            Recording? recording;
            try
            {
                recording = Recording.Load(path);
            }
            catch (JsonException)
            {
                continue;
            }

            if (recording?.Stamp is not { } stamp || recording.Exchanges.Count == 0)
            {
                continue;
            }

            var versions = new Dictionary<string, string>(stamp.Versions, StringComparer.Ordinal);
            foreach (var version in schema.Versions)
            {
                versions[version.Key] = version.Value;
            }

            var now = schema with { Versions = versions };
            states.Add(new RecordingState(path, [.. stamp.Objects.Keys.OrderBy(name => name, StringComparer.Ordinal)], stamp.Differences(now)));
        }

        return states;
    }
}

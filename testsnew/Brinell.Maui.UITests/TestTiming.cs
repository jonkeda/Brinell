using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Brinell.Core.Artifacts;
using Xunit.Sdk;

[assembly: Brinell.Maui.UITests.TestTiming]

namespace Brinell.Maui.UITests;

/// <summary>
/// Stage G step 35: notices when the suite starts waiting.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> Step 33's regression - a two-second grace period paid on every fixture
/// reset - shipped with a green suite <i>and a lower total runtime</i>, because an unrelated fix was
/// saving more than it cost. Nothing asserted how long anything took, and a person watching a run
/// was what found it. Per-area timings would have shown it at once: Buttons runs eleven tests in a
/// second, and an area an order of magnitude off that per test is worth a look before a fix.
/// </para>
/// <para>
/// <b>What it does with the numbers, which was the open decision.</b> Every test is timed from
/// here - an assembly-level attribute, so no test has to know. At the end of the run a report goes
/// to <c>TestResults/&lt;run-id&gt;/suites/&lt;suite&gt;/attachments/test-timings.md</c> per
/// <c>AD-007</c>: every test class with its count, total, mean and slowest test, set against
/// <c>timing-baseline.json</c>, and the classes that have got markedly slower listed first.
/// </para>
/// <para>
/// <b>A report and not a gate, deliberately.</b> A threshold that fails the run fails it on a
/// slower machine, a busy CI agent, or a first run that pays for JIT - and a gate that trips for
/// reasons unrelated to the change teaches everyone to ignore it. What step 33 needed was for the
/// slowdown to be <i>visible</i>; a flagged row is visible. The raw rows are also written as CSV as
/// the run goes, so a run that is killed still leaves what it measured.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class TestTimingAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest) => TestTimings.Start(methodUnderTest);

    public override void After(MethodInfo methodUnderTest) => TestTimings.Stop(methodUnderTest);
}

/// <summary>Collects durations for the run and writes the report when the process ends.</summary>
internal static class TestTimings
{
    /// <summary>A class this many times slower per test than its baseline is flagged.</summary>
    private const double SlowerFactor = 2.0;

    /// <summary>
    /// ...and only if it is also this much slower in absolute terms.
    /// </summary>
    /// <remarks>
    /// A 20 ms test that takes 50 ms is "2.5 times slower" and means nothing; step 33's regression
    /// was seconds per reset.
    /// </remarks>
    private const double SlowerByAtLeastMs = 250;

    private static readonly ConcurrentDictionary<MethodInfo, long> Started = new();
    private static readonly ConcurrentQueue<Row> Rows = new();
    private static readonly Lock CsvGate = new();
    private static readonly Lazy<string> Directory = new(CreateDirectory);
    private static int _hooked;

    internal static void Start(MethodInfo method)
    {
        HookReportOnce();
        Started[method] = Stopwatch.GetTimestamp();
    }

    internal static void Stop(MethodInfo method)
    {
        if (!Started.TryRemove(method, out var began))
        {
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(began).TotalMilliseconds;
        var row = new Row(AreaOf(method), method.DeclaringType?.Name ?? "?", method.Name, elapsed);
        Rows.Enqueue(row);

        try
        {
            lock (CsvGate)
            {
                File.AppendAllText(
                    Path.Combine(Directory.Value, "test-timings.csv"),
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"{row.Area},{row.Class},{row.Method},{row.Milliseconds:F0}{Environment.NewLine}"));
            }
        }
        catch (IOException)
        {
            // A timing that cannot be written is not worth failing a test over.
        }
    }

    private static void HookReportOnce()
    {
        if (Interlocked.Exchange(ref _hooked, 1) == 0)
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) => WriteReport();
        }
    }

    /// <remarks>
    /// The namespace segment after <c>Tests</c>: <c>Scroll</c>, <c>Background</c>. The same grain
    /// the RCA timed by hand with filters.
    /// </remarks>
    private static string AreaOf(MethodInfo method)
    {
        var ns = method.DeclaringType?.Namespace ?? string.Empty;
        var marker = ".Tests.";
        var at = ns.IndexOf(marker, StringComparison.Ordinal);
        return at < 0 ? "(root)" : ns[(at + marker.Length)..];
    }

    private static string CreateDirectory()
    {
        var paths = DefaultTestArtifactPathProvider.Create();
        paths.EnsureDirectories();
        return paths.AttachmentsDirectory;
    }

    internal static void WriteReport()
    {
        try
        {
            var rows = Rows.ToArray();
            if (rows.Length == 0)
            {
                return;
            }

            var baseline = LoadBaseline();
            File.WriteAllText(
                Path.Combine(Directory.Value, "test-timings.md"), Render(rows, baseline));
            File.WriteAllText(
                Path.Combine(Directory.Value, "test-timings-baseline-candidate.json"),
                RenderBaselineCandidate(rows));
        }
        catch (Exception)
        {
            // Process exit: nothing useful can be done with a failure here, and it must not turn a
            // passing run into a crashed one.
        }
    }

    internal static string Render(IReadOnlyCollection<Row> rows, IReadOnlyDictionary<string, double> baseline)
    {
        var classes = rows
            .GroupBy(r => $"{r.Area}/{r.Class}")
            .Select(g =>
            {
                var slowest = g.MaxBy(r => r.Milliseconds)!;
                var mean = g.Average(r => r.Milliseconds);
                var known = baseline.TryGetValue(g.Key, out var before);
                var flagged = known
                              && mean > before * SlowerFactor
                              && mean - before >= SlowerByAtLeastMs;
                return (Key: g.Key, Count: g.Count(), Total: g.Sum(r => r.Milliseconds), Mean: mean,
                    Slowest: slowest, Baseline: known ? before : (double?)null, Flagged: flagged);
            })
            .OrderByDescending(c => c.Flagged)
            .ThenByDescending(c => c.Total)
            .ToArray();

        var text = new StringBuilder();
        text.AppendLine("# Test timings");
        text.AppendLine();
        text.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{rows.Count} tests, {rows.Sum(r => r.Milliseconds) / 1000:F1} s of test time across {classes.Length} classes."));
        text.AppendLine();

        var flaggedCount = classes.Count(c => c.Flagged);
        text.AppendLine(flaggedCount == 0
            ? "No class is markedly slower than its baseline."
            : string.Create(CultureInfo.InvariantCulture,
                $"**{flaggedCount} class(es) are over {SlowerFactor:F0}x their baseline per test, and at least {SlowerByAtLeastMs:F0} ms slower.** A framework that has started waiting for something looks like this - see step 35."));
        text.AppendLine();
        text.AppendLine("| | Area / class | Tests | Total | Mean | Baseline mean | Slowest test |");
        text.AppendLine("|---|---|---:|---:|---:|---:|---|");

        foreach (var c in classes)
        {
            text.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {(c.Flagged ? "**slower**" : string.Empty)} | `{c.Key}` | {c.Count} | {c.Total / 1000:F1} s | {c.Mean:F0} ms | {(c.Baseline is { } b ? $"{b:F0} ms" : "-")} | `{c.Slowest.Method}` {c.Slowest.Milliseconds:F0} ms |"));
        }

        return text.ToString();
    }

    private static string RenderBaselineCandidate(IEnumerable<Row> rows)
        => JsonSerializer.Serialize(
            rows.GroupBy(r => $"{r.Area}/{r.Class}")
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => Math.Round(g.Average(r => r.Milliseconds))),
            new JsonSerializerOptions { WriteIndented = true });

    /// <remarks>
    /// Next to the test assembly, copied from <c>timing-baseline.json</c> in the project. Refreshing
    /// it is deliberate: copy a good run's <c>test-timings-baseline-candidate.json</c> over it.
    /// </remarks>
    private static IReadOnlyDictionary<string, double> LoadBaseline()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "timing-baseline.json");
        if (!File.Exists(path))
        {
            return new Dictionary<string, double>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, double>>(File.ReadAllText(path))
               ?? new Dictionary<string, double>();
    }

    internal readonly record struct Row(string Area, string Class, string Method, double Milliseconds);
}

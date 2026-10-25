using System.Diagnostics;
using System.Globalization;
using Brinell.Core.Testing;

namespace Brinell.Core.Logging;

/// <summary>
/// A run's timings (.my/test-selection/speed-plan.md, T1): one row per control call and per span of the work between
/// calls (the host's start, a fixture, a scenario's binding and lifecycle, a step, a page load, an arrangement), each
/// with its test, scenario and step (<see cref="TestScope"/>). Written only when <c>BRINELL_TIMINGS</c> names a folder:
/// <c>timings-&lt;run&gt;.csv</c> there, one file per test process. The same rows are the run's touch map: which test
/// used which page and control.
/// </summary>
public static class Timing
{
    /// <summary>The environment variable naming the folder the timings go to.</summary>
    public const string Variable = "BRINELL_TIMINGS";

    private const string Header = "Run;Head;Test;Scenario;Step;Kind;Name;Page;Control;Action;StartMs;DurationMs;Attempts;Result";

    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static readonly object Gate = new();
    private static readonly Lazy<StreamWriter?> Writer = new(Open, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly string Run = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}";

    /// <summary>Whether this run writes timings.</summary>
    public static bool Enabled { get; } = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable));

    /// <summary>
    /// A span of work, written when disposed: <paramref name="kind"/> says what sort (<c>host</c>, <c>fixture</c>,
    /// <c>bind</c>, <c>lifecycle</c>, <c>step</c>, <c>navigate</c>, <c>arrange</c>), <paramref name="name"/> which one.
    /// </summary>
    public static IDisposable Span(string kind, string name)
        => Enabled ? new SpanScope(kind, name, TestScope.Current, Clock.ElapsedMilliseconds) : NoSpan.Instance;

    /// <summary>One control call, as <c>ControlCall</c> ends it.</summary>
    public static void Call(string page, string control, string action, long startMs, int durationMs, int attempts, string result)
    {
        if (Enabled)
        {
            Write(TestScope.Current, "call", action, page, control, action, startMs, durationMs, attempts, result);
        }
    }

    /// <summary>Milliseconds since the run began, the rows' <c>StartMs</c>.</summary>
    public static long NowMs => Clock.ElapsedMilliseconds;

    private static void Write(TestScope scope, string kind, string name, string? page, string? control, string? action,
        long startMs, long durationMs, int? attempts, string? result)
    {
        if (Writer.Value is not { } writer)
        {
            return;
        }

        var line = string.Join(';',
            Run, WebHeads.Current.ToString().ToLowerInvariant(), Cell(scope.Test), Cell(scope.Scenario), Cell(scope.Step),
            kind, Cell(name), Cell(page), Cell(control), Cell(action),
            startMs.ToString(CultureInfo.InvariantCulture), durationMs.ToString(CultureInfo.InvariantCulture),
            attempts?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, Cell(result));
        lock (Gate)
        {
            writer.WriteLine(line);
        }
    }

    // One field: no separator, no line break, inside it.
    private static string Cell(string? value)
        => value is null ? string.Empty : value.Replace(';', ',').Replace('\r', ' ').Replace('\n', ' ');

    private static StreamWriter? Open()
    {
        var folder = Environment.GetEnvironmentVariable(Variable);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return null;
        }

        Directory.CreateDirectory(folder);
        var writer = new StreamWriter(Path.Combine(folder, $"timings-{Run}.csv"), append: false) { AutoFlush = true };
        writer.WriteLine(Header);
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            lock (Gate)
            {
                writer.Dispose();
            }
        };
        return writer;
    }

    private sealed class SpanScope(string kind, string name, TestScope scope, long startMs) : IDisposable
    {
        private int _done;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _done, 1) == 0)
            {
                Write(scope, kind, name, null, null, null, startMs, Clock.ElapsedMilliseconds - startMs, null, null);
            }
        }
    }

    private sealed class NoSpan : IDisposable
    {
        public static readonly NoSpan Instance = new();

        public void Dispose()
        {
        }
    }
}

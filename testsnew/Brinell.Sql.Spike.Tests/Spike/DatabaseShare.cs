using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Brinell.Core.Testing;
using Xunit.Sdk;

namespace Brinell.Sql.Spike.Tests.Spike;

/// <summary>
/// Writes each test's time, the app's database time and the reset before it to <c>BRINELL_DB_SHARE</c>'s folder, with
/// the test's commands beside it (.my/sql-replay/plan.md, 0.4). The test is its method: from
/// <see cref="MeasureDatabaseShareAttribute"/>'s Before to its After.
/// </summary>
/// <remarks>One test at a time: run with <c>-- xUnit.ParallelizeTestCollections=false</c>. Off when the variable is not set.</remarks>
public static class DatabaseShare
{
    public const string Variable = "BRINELL_DB_SHARE";

    private static readonly Lock Gate = new();
    private static (CommandLog? Log, TimeSpan Reset)? _prepared;
    private static long _start;

    private static string? Folder => Environment.GetEnvironmentVariable(Variable) is { Length: > 0 } folder ? folder : null;

    private static string Head => WebHeads.Current.ToString().ToLowerInvariant();

    /// <summary>The test's database is reset and its app is ready.</summary>
    public static void Prepared(CommandLog? log, TimeSpan reset)
    {
        lock (Gate)
        {
            _prepared = (log, reset);
        }
    }

    internal static void Begin()
    {
        lock (Gate)
        {
            _prepared?.Log?.Clear();
            _start = Stopwatch.GetTimestamp();
        }
    }

    internal static void End(string test)
    {
        TimeSpan elapsed;
        (CommandLog? Log, TimeSpan Reset)? prepared;
        lock (Gate)
        {
            elapsed = Stopwatch.GetElapsedTime(_start);
            prepared = _prepared;
            _prepared = null;
        }

        if (Folder is not { } folder || prepared is not { } measured)
        {
            return;
        }

        var log = measured.Log;
        Write(folder, $"{test},{Ms(elapsed)},{(log is null ? string.Empty : Ms(log.Elapsed))},{log?.Commands.Count},{Ms(measured.Reset)}");
        log?.Write(Path.Combine(folder, "log", Head, test + ".jsonl"));
    }

    private static string Ms(TimeSpan elapsed) => elapsed.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture);

    private static void Write(string folder, string row)
    {
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"db-share-{Head}.csv");
        lock (Gate)
        {
            if (!File.Exists(file))
            {
                File.AppendAllText(file, "test,testMs,dbMs,commands,resetMs" + Environment.NewLine);
            }

            File.AppendAllText(file, row + Environment.NewLine);
        }
    }
}

/// <summary>Brackets a test: its context made once its name is known, and its time for <see cref="DatabaseShare"/>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class MeasureDatabaseShareAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
        TestBase.SpikeTestBase.Current?.Begin(Name(methodUnderTest)).GetAwaiter().GetResult();
        DatabaseShare.Begin();
    }

    public override void After(MethodInfo methodUnderTest) => DatabaseShare.End(Name(methodUnderTest));

    private static string Name(MethodInfo method) => $"{method.DeclaringType?.Name}.{method.Name}";
}

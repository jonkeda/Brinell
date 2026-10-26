using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Xunit.Sdk;

namespace Bruinet.Samples.Todo.Web.UITests.Spike;

/// <summary>
/// Writes each test's time and its app's database time to <c>BRUINET_DB_SHARE</c>'s folder, for the SQL replay
/// spike (.my/sql-replay/plan.md, 0.4). A test starts at its fixture's <c>StartTest</c> and ends after its method.
/// </summary>
/// <remarks>
/// One test at a time: run with <c>-- xUnit.ParallelizeTestCollections=false</c>, so the test that started is the
/// one that ends. Off when the variable is not set.
/// </remarks>
public static class DatabaseShare
{
    public const string Variable = "BRUINET_DB_SHARE";

    private static readonly Lock Gate = new();
    private static (DatabaseClock Clock, long Start)? _current;

    private static string? Folder => Environment.GetEnvironmentVariable(Variable) is { Length: > 0 } folder ? folder : null;

    /// <summary>A test starts on the device whose <paramref name="clock"/> measures it.</summary>
    public static void Start(DatabaseClock? clock)
    {
        if (Folder is null || clock is null)
        {
            return;
        }

        clock.Reset();
        lock (Gate)
        {
            _current = (clock, Stopwatch.GetTimestamp());
        }
    }

    /// <summary>A fixture's seeding, which replay would also take away.</summary>
    public static void Seeded(string fixture, TimeSpan elapsed) => Write($"seed,{fixture},{Ms(elapsed)},,");

    internal static void End(string test)
    {
        (DatabaseClock Clock, long Start)? current;
        lock (Gate)
        {
            current = _current;
            _current = null;
        }

        if (current is { } measured)
        {
            Write($"test,{test},{Ms(Stopwatch.GetElapsedTime(measured.Start))},{Ms(measured.Clock.Elapsed)},{measured.Clock.Calls}");
        }
    }

    private static string Ms(TimeSpan elapsed) => elapsed.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture);

    private static void Write(string row)
    {
        if (Folder is not { } folder)
        {
            return;
        }

        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"db-share-{Bruinet.Core.Testing.WebHeads.Current.ToString().ToLowerInvariant()}.csv");
        lock (Gate)
        {
            if (!File.Exists(file))
            {
                File.AppendAllText(file, "kind,name,testMs,dbMs,calls" + Environment.NewLine);
            }

            File.AppendAllText(file, row + Environment.NewLine);
        }
    }
}

/// <summary>Ends <see cref="DatabaseShare"/>'s measurement of the test it is on.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class MeasureDatabaseShareAttribute : BeforeAfterTestAttribute
{
    public override void After(MethodInfo methodUnderTest)
        => DatabaseShare.End($"{methodUnderTest.DeclaringType?.Name}.{methodUnderTest.Name}");
}

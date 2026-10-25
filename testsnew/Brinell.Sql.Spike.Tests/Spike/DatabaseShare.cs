using System.Globalization;
using Brinell.Core.Testing;

namespace Brinell.Sql.Spike.Tests.Spike;

/// <summary>
/// Writes each test's time, the app's database time and the reset before it to <c>BRINELL_DB_SHARE</c>'s folder
/// (.my/sql-replay/plan.md, 0.4); the commands themselves are in the SQL log (<c>BRINELL_SQL_LOG</c>). Off when the variable is not set. Tests in parallel
/// each write their own row; the file is shared under a lock. Times measured in parallel include waiting for the
/// machine, so the share is measured one test at a time (<c>-- xUnit.ParallelizeTestCollections=false</c>).
/// </summary>
public static class DatabaseShare
{
    public const string Variable = "BRINELL_DB_SHARE";

    private static readonly Lock Gate = new();

    private static string Head => WebHeads.Current.ToString().ToLowerInvariant();

    public static void Write(string test, TimeSpan elapsed, Brinell.Mocking.Data.SqlLog log, TimeSpan reset)
    {
        if (Environment.GetEnvironmentVariable(Variable) is not { Length: > 0 } folder)
        {
            return;
        }

        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"db-share-{Head}.csv");
        lock (Gate)
        {
            if (!File.Exists(file))
            {
                File.AppendAllText(file, "test,testMs,dbMs,commands,resetMs" + Environment.NewLine);
            }

            File.AppendAllText(file, $"{test},{Ms(elapsed)},{Ms(log.Elapsed)},{log.Entries.Count(entry => entry.Kind != "open")},{Ms(reset)}" + Environment.NewLine);
        }

    }

    private static string Ms(TimeSpan elapsed) => elapsed.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture);
}

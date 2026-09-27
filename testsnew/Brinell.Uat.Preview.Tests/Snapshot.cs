using System.Runtime.CompilerServices;
using Xunit;

namespace Brinell.Uat.Preview.Tests;

/// <summary>
/// Compares text with a file under <c>Snapshots/</c>. A missing or different snapshot fails and
/// writes <c>*.received.html</c> beside it; with <c>BRINELL_UPDATE_SNAPSHOTS=1</c> the received text
/// becomes the snapshot.
/// </summary>
internal static class Snapshot
{
    public static void Match(string actual, string name, [CallerFilePath] string callerFile = "")
    {
        var folder = Path.Combine(Path.GetDirectoryName(callerFile)!, "Snapshots");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name + ".html");
        var received = Path.Combine(folder, name + ".received.html");
        actual = actual.ReplaceLineEndings("\n");

        if (Environment.GetEnvironmentVariable("BRINELL_UPDATE_SNAPSHOTS") == "1")
        {
            File.WriteAllText(path, actual);
            File.Delete(received);
            return;
        }

        var expected = File.Exists(path) ? File.ReadAllText(path).ReplaceLineEndings("\n") : null;
        if (expected == actual)
        {
            File.Delete(received);
            return;
        }

        File.WriteAllText(received, actual);
        Assert.Fail(expected is null
            ? $"No snapshot {name}.html yet. Check {received}, then run with BRINELL_UPDATE_SNAPSHOTS=1."
            : $"{name}.html differs; see {received}. Run with BRINELL_UPDATE_SNAPSHOTS=1 when the change is right.");
    }
}

using System.Diagnostics;
using System.Globalization;

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// Says what the bridge did, for an app you cannot attach a debugger to.
/// </summary>
/// <remarks>
/// <para>
/// The bridge runs inside the app under test, which is launched by a test run and has no
/// console. When it fails there, the symptom reaching the test is an app that does not answer -
/// indistinguishable from an app that has no instrumentation, from one whose automation tree
/// collapsed, and from one that crashed. Those want completely different responses, and nothing
/// else in the system can tell them apart.
/// </para>
/// <para>
/// Off unless <c>BRINELL_UIA_LOG</c> names a file. Writing per line rather than buffering,
/// because the failure being chased is often one that kills the process.
/// </para>
/// </remarks>
internal static class BridgeDiagnostics
{
    private static readonly string? LogPath =
        Environment.GetEnvironmentVariable("BRINELL_UIA_LOG");

    private static readonly Lock Gate = new();

    /// <summary>Records one line about what the bridge is doing.</summary>
    /// <param name="message">What happened.</param>
    internal static void Report(string message)
    {
        Debug.WriteLine($"[Brinell] {message}");

        if (string.IsNullOrWhiteSpace(LogPath))
        {
            return;
        }

        var line = string.Create(
            CultureInfo.InvariantCulture,
            $"{DateTime.Now:HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {message}");

        try
        {
            lock (Gate)
            {
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
        catch (IOException)
        {
            // A diagnostic that cannot write must not take the app down with it.
        }
        catch (UnauthorizedAccessException)
        {
            // Same.
        }
    }
}

using System.Diagnostics;
using System.Text.Json;

namespace Bruinell.Core.Diagnostics;

/// <summary>
/// Says which app a test started, so an IDE that debugs the test can debug the app too: while a
/// debugger is attached to the test process, each app it starts is written to
/// <c>%TEMP%\Bruinell\debugged-apps\&lt;test pid&gt;-&lt;app pid&gt;.json</c>. The IDE that attached to
/// the test watches for its test process's files and attaches to each app. Without a debugger
/// nothing is written.
/// </summary>
public static class DebuggedApps
{
    /// <summary>The folder the announcements are in.</summary>
    public static string Folder { get; } = Path.Combine(Path.GetTempPath(), "Bruinell", "debugged-apps");

    /// <summary>Announces an app this test process started, when a debugger is attached to it.</summary>
    /// <param name="app">The app's process: the app itself, not a launcher that starts it.</param>
    /// <param name="name">A name for the IDE to show.</param>
    /// <param name="attached">Whether a debugger is attached; <see cref="Debugger.IsAttached"/> by default.</param>
    /// <param name="folder">Where to write; <see cref="Folder"/> by default.</param>
    /// <returns>The file written; null when no debugger is attached or it could not be written.</returns>
    public static string? Announce(Process app, string name, Func<bool>? attached = null, string? folder = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!(attached ?? (() => Debugger.IsAttached))())
        {
            return null;
        }

        var target = folder ?? Folder;
        var file = Path.Combine(target, $"{Environment.ProcessId}-{app.Id}.json");
        try
        {
            Directory.CreateDirectory(target);
            File.WriteAllText(file, JsonSerializer.Serialize(new { testProcess = Environment.ProcessId, app = app.Id, name }));
            return file;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Debugging the app too is a convenience: a test never fails for it.
            return null;
        }
    }
}

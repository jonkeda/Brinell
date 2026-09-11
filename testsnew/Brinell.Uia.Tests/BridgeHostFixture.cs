using System.Diagnostics;
using System.Globalization;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Brinell.Uia.Tests;

/// <summary>
/// Runs one <c>Brinell.Uia.TestHost</c> process and attaches a UI Automation client to it.
/// </summary>
/// <remarks>
/// <para>
/// Out of process on purpose. UI Automation short-circuits an in-process provider, so a client
/// and a provider in the same process would exercise a direct call and prove nothing about the
/// marshalling - which is the whole risk these tests exist to measure.
/// </para>
/// <para>
/// Shared across the class so the window is created once. Every test here reads or calls; none
/// of them changes anything another test depends on, except the recording target's log, which
/// each test addresses by its own verb arguments.
/// </para>
/// </remarks>
public sealed class BridgeHostFixture : IDisposable
{
    private const int StartTimeoutMs = 20_000;
    private const int PollIntervalMs = 50;

    private readonly Process _process;
    private readonly string _readyFile;

    public BridgeHostFixture()
    {
        var runId = Guid.NewGuid().ToString("N");
        WindowTitle = $"Brinell UIA Test Host {runId}";
        _readyFile = Path.Combine(Path.GetTempPath(), $"brinell-uia-host-{runId}.ready");

        var executable = Path.Combine(AppContext.BaseDirectory, "Brinell.Uia.TestHost.exe");
        if (!File.Exists(executable))
        {
            throw new InvalidOperationException(
                $"The bridge host was not copied next to the tests ({executable}). "
                + "The project reference in Brinell.Uia.Tests.csproj is what puts it there.");
        }

        _process = Process.Start(new ProcessStartInfo(executable)
        {
            ArgumentList = { WindowTitle, _readyFile },
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("The bridge host did not start.");

        var report = WaitForReady();

        HostWindowHandle = new IntPtr(ReadValue(report, "HWND"));
        BridgeWindowHandle = new IntPtr(ReadValue(report, "BRIDGEHWND"));
        HostPatternId = (int)ReadValue(report, "PATTERNID");

        Automation = new UIA3Automation();
        HostWindow = Automation.FromHandle(HostWindowHandle);
    }

    /// <summary>The unique title of the host window, so parallel runs cannot collide.</summary>
    public string WindowTitle { get; }

    /// <summary>The host's top-level window.</summary>
    public IntPtr HostWindowHandle { get; }

    /// <summary>The bridge's own child window.</summary>
    public IntPtr BridgeWindowHandle { get; }

    /// <summary>
    /// The pattern id the host process was given.
    /// </summary>
    /// <remarks>
    /// Reported by the host rather than assumed. Comparing it with this process's id is what
    /// shows that ids are process-local and the GUID is the contract.
    /// </remarks>
    public int HostPatternId { get; }

    /// <summary>The client-side automation session.</summary>
    public UIA3Automation Automation { get; }

    /// <summary>The host's window as an automation element.</summary>
    public AutomationElement HostWindow { get; }

    private string WaitForReady()
    {
        var deadline = Stopwatch.StartNew();

        while (deadline.ElapsedMilliseconds < StartTimeoutMs)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException(
                    $"The bridge host exited with code {_process.ExitCode} before it was ready. "
                    + "It failed during window creation or pattern registration.");
            }

            if (File.Exists(_readyFile))
            {
                return File.ReadAllText(_readyFile);
            }

            Thread.Sleep(PollIntervalMs);
        }

        throw new TimeoutException(
            $"The bridge host did not report ready within {StartTimeoutMs} ms.");
    }

    private static long ReadValue(string report, string key)
    {
        foreach (var line in report.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(key + "=", StringComparison.Ordinal))
            {
                return long.Parse(
                    trimmed[(key.Length + 1)..], CultureInfo.InvariantCulture);
            }
        }

        throw new InvalidOperationException(
            $"The bridge host reported no '{key}'. What it did report was: {report}");
    }

    public void Dispose()
    {
        Automation.Dispose();

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5_000);
            }
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }

        _process.Dispose();

        try
        {
            File.Delete(_readyFile);
        }
        catch (IOException)
        {
            // A leftover temp file is not worth failing a run over.
        }
    }
}

/// <summary>Serialises the bridge tests, which share one host process.</summary>
[CollectionDefinition("BridgeHost")]
public sealed class BridgeHostCollection : ICollectionFixture<BridgeHostFixture>;

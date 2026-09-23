using System.Diagnostics;
using Brinell.Uia.TestHost;
using System.Globalization;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Brinell.Uia.Tests;

/// <summary>
/// A bridge host of one test class's own, which it may destroy and rebuild.
/// </summary>
/// <remarks>
/// <para>
/// <b>Separate from <see cref="BridgeHostFixture"/> on purpose.</b> That one is shared across a
/// collection because every test in it only reads. These tear the bridge down and put it back,
/// so sharing would make one file's assertions depend on another file's timing - the shape of
/// flake that takes a day to attribute.
/// </para>
/// <para>
/// <b>Per test, not per class.</b> Two of the lifetime tests end with the bridge deliberately
/// destroyed, so a host shared between them would hand the second one a corpse.
/// </para>
/// </remarks>
internal sealed class LifetimeHost : IDisposable
{
    private const int StartTimeoutMs = 20_000;
    private const int PollIntervalMs = 50;
    private const int CommandTimeoutMs = 10_000;

    private readonly Process _process;
    private readonly string _readyFile;
    private readonly IntPtr _hwnd;

    public LifetimeHost()
    {
        var runId = Guid.NewGuid().ToString("N");
        var title = $"Brinell UIA Lifetime Host {runId}";
        _readyFile = Path.Combine(Path.GetTempPath(), $"brinell-uia-life-{runId}.ready");

        var executable = Path.Combine(AppContext.BaseDirectory, "Brinell.Uia.TestHost.exe");
        if (!File.Exists(executable))
        {
            throw new InvalidOperationException(
                $"The bridge host was not copied next to the tests ({executable}).");
        }

        var start = new ProcessStartInfo(executable)
        {
            ArgumentList = { title, _readyFile },
            UseShellExecute = false,
        };

        start.Environment[BrinellBridgeGate.EnableVariable] = "1";

        // Redirected and never read. The host writes its report to stdout as well as to the
        // ready file, and an inherited pipe outlives a process that escapes cleanup - which
        // leaves the whole test run apparently hung, waiting on a handle nobody holds any more.
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;

        _process = Process.Start(start)
            ?? throw new InvalidOperationException("The bridge host did not start.");

        var report = WaitForReady();
        _hwnd = new IntPtr(ReadValue(report, "HWND"));

        Automation = new UIA3Automation();
        Window = BridgeClient.AttachToWindow(Automation, _hwnd);
    }

    /// <summary>The client-side automation session.</summary>
    public UIA3Automation Automation { get; }

    /// <summary>The host's top-level window.</summary>
    public AutomationElement Window { get; }

    /// <summary>
    /// Asks the host a question and returns its answer.
    /// </summary>
    /// <remarks>
    /// Sent, so the answer is the state as of now. Safe because a query makes no outgoing COM
    /// call - see <see cref="Run"/> for why that distinction decides everything here.
    /// </remarks>
    /// <param name="query">One of <c>HostCommands</c>' query messages.</param>
    /// <returns>What the host answered.</returns>
    public int Send(uint query) => (int)SendMessage(_hwnd, query, IntPtr.Zero, IntPtr.Zero);

    /// <summary>
    /// Runs one lifetime command on the host and waits for it to finish.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Posted, not sent, and the first draft of these tests got this wrong.</b> A
    /// <c>SendMessage</c> from another process is an input-synchronous call, and COM refuses an
    /// outgoing call while one is being dispatched. <c>UiaDisconnectProvider</c> has to call out
    /// to the client to revoke its interface, so every disconnect during a sent teardown failed
    /// with <c>RPC_E_CANTCALLOUT_ININPUTSYNCCALL</c> - reporting success to the caller and
    /// leaving every provider connected. The harness was manufacturing the leak it exists to
    /// detect.
    /// </para>
    /// <para>
    /// <b>Waiting on a counter rather than on a reply.</b> A posted message answers nothing, and
    /// the observable effect of a cycle is that the count does <i>not</i> change - so there is
    /// nothing to poll but the host's own tally of finished commands, read with a query.
    /// </para>
    /// </remarks>
    /// <param name="command">One of <c>HostCommands</c>' posted messages.</param>
    /// <returns>The number of live bridges once the command has finished.</returns>
    public int Run(uint command)
    {
        var before = Send(HostCommands.CommandsHandled);

        if (!PostMessage(_hwnd, command, IntPtr.Zero, IntPtr.Zero))
        {
            throw new InvalidOperationException(
                $"Could not post command {command} to the bridge host.");
        }

        var elapsed = Stopwatch.StartNew();

        while (Send(HostCommands.CommandsHandled) == before)
        {
            if (elapsed.ElapsedMilliseconds > CommandTimeoutMs)
            {
                throw new TimeoutException(
                    $"The bridge host did not finish command {command} within "
                    + $"{CommandTimeoutMs} ms.");
            }

            Thread.Sleep(1);
        }

        return Send(HostCommands.CountBridges);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(
        IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "PostMessageW",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(
        IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    private string WaitForReady()
    {
        var elapsed = Stopwatch.StartNew();

        while (elapsed.ElapsedMilliseconds < StartTimeoutMs)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException(
                    $"The bridge host exited with code {_process.ExitCode} before it was ready.");
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
                return long.Parse(trimmed[(key.Length + 1)..], CultureInfo.InvariantCulture);
            }
        }

        throw new InvalidOperationException(
            $"The bridge host reported no '{key}'. What it did report was: {report}");
    }

    /// <remarks>
    /// <b>The process first, and every step guarded.</b> Disposing the automation session used
    /// to come first, and it can throw when the window it was attached to has gone - which left
    /// the host running, one per run. A stray host is not tidy-up debt: it holds the inherited
    /// stdout pipe and the run that started it appears to hang.
    /// </remarks>
    public void Dispose()
    {
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

        try
        {
            Automation.Dispose();
        }
        catch (Exception)
        {
            // The session's window is already dead; there is nothing left to release cleanly.
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

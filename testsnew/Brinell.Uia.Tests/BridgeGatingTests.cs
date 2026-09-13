using System.Diagnostics;
using System.Globalization;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit.Abstractions;

namespace Brinell.Uia.Tests;

/// <summary>
/// Step 27: what a build without the bridge exposes, which must be nothing.
/// </summary>
/// <remarks>
/// <para>
/// <b>The bridge is a remotely invocable command channel into application logic, and UI
/// Automation has no per-caller authentication.</b> Any process at the same or higher integrity
/// level on the same desktop can enumerate the tree, find the fragment root and call the
/// pattern. There is no permission to check and no caller to identify. Absence is the only
/// control, so absence is the thing worth testing.
/// </para>
/// <para>
/// <b>These build a host and launch it, which no other test here does.</b> The claim is about a
/// build - "compiling this way produces a binary with no provider in it" - and a test that
/// inspected a binary someone else had already produced would be testing whoever ran the last
/// build. It costs a couple of seconds; the host is a window and a message loop.
/// </para>
/// <para>
/// <b><see cref="BridgeVisibilityTests"/> is the other half.</b> Everything asserted absent here
/// is asserted present there, against the same host built the ordinary way - which is what stops
/// these two tests passing because the client cannot see anything at all.
/// </para>
/// </remarks>
[Trait("Category", "Uia")]
public class BridgeGatingTests
{
    private const int BuildTimeoutMs = 300_000;
    private const int StartTimeoutMs = 20_000;
    private const int PollIntervalMs = 50;

    private readonly ITestOutputHelper _output;

    public BridgeGatingTests(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// A Release build contains no provider, so there is no fragment root to find.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Release with nothing else said, deliberately.</b> Passing
    /// <c>-p:BrinellUiaBridge=false</c> would test that the switch works; building the way a
    /// shipping build is built tests that the switch is in the right position by default, which
    /// is the property someone actually relies on. The default lives in
    /// <c>Brinell.Uia.Bridge.props</c>.
    /// </para>
    /// <para>
    /// <b>The bridge is asked for, and that matters.</b> Leaving the environment variable unset
    /// would shut the run-time gate as well, and the test would then pass on a build that had
    /// the provider in it - which is the failure it exists to catch. Written the first way, it
    /// did exactly that: forcing the constant back on in Release did not turn it red. Asking
    /// leaves compile-time absence as the only thing left to stop it.
    /// </para>
    /// </remarks>
    [Fact]
    public void ReleaseBuild_HasNoFragmentRootToFind()
    {
        var executable = BuildHost("Release");

        using var host = HostRun.Start(executable, bridgeRequested: true);
        _output.WriteLine(host.Report);

        Assert.Contains("BRIDGE=off", host.Report, StringComparison.Ordinal);

        using var automation = new UIA3Automation();
        var window = BridgeClient.AttachToWindow(automation, host.WindowHandle);

        // The control. Without it, "no bridge was found" would also be what a test that could
        // not see the process at all reports - and that is the mistake worth guarding against,
        // because it makes a broken gate look like a working one.
        Assert.Equal(host.WindowTitle, window.Properties.Name.Value);

        var raw = automation.TreeWalkerFactory.GetRawViewWalker();
        var bridge = BridgeClient.FindByClassName(raw, window, BrinellUiaIds.BridgeClassName);

        Assert.True(
            bridge is null,
            $"A build compiled without BRINELL_UIA_BRIDGE published a fragment root, even "
            + $"though {BrinellBridgeGate.EnableVariable} asked it to. UI "
            + "Automation offers no way to refuse a caller, so a shipping build exposing this "
            + "is a command channel into the app for any process on the desktop.");
    }

    /// <summary>
    /// An instrumented build that was not asked publishes nothing either.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The convenience gate, not the control.</b> It exists so one build can serve
    /// development and testing, and it is worth a test only because a gate that never says no
    /// looks exactly like a gate that works. Anything able to set an environment variable on
    /// the app could have launched a different build of it, so this protects nobody on its own -
    /// which is why the paragraph above it in <c>BrinellBridgeGate</c> says so.
    /// </para>
    /// <para>
    /// <b>The same lines the app under test runs.</b> <c>BrinellBridgeGate</c> lives in the
    /// contract because both ends compile it; if this asked a copy of the rule, it would pass
    /// while the app did the opposite.
    /// </para>
    /// </remarks>
    [Fact]
    public void InstrumentedBuild_WithoutTheVariable_PublishesNothing()
    {
        var executable = BuildHost("Debug");

        using var host = HostRun.Start(executable, bridgeRequested: false);
        _output.WriteLine(host.Report);

        Assert.Contains("BRIDGE=off", host.Report, StringComparison.Ordinal);

        using var automation = new UIA3Automation();
        var window = BridgeClient.AttachToWindow(automation, host.WindowHandle);

        Assert.Equal(host.WindowTitle, window.Properties.Name.Value);

        var raw = automation.TreeWalkerFactory.GetRawViewWalker();
        var bridge = BridgeClient.FindByClassName(raw, window, BrinellUiaIds.BridgeClassName);

        Assert.True(
            bridge is null,
            $"The host published a fragment root without {BrinellBridgeGate.EnableVariable}=1. "
            + "An app that instruments itself whenever it can would put the bridge into every "
            + "developer's run of it.");
    }

    /// <summary>
    /// Builds the bridge host in one configuration and returns what it produced.
    /// </summary>
    /// <remarks>
    /// The ordinary output path, not a scratch one: this is the same build a developer gets, and
    /// a build into somewhere unusual would be a different build. It cannot collide with the
    /// running test assembly, which is Debug and is already built by the time a test runs.
    /// </remarks>
    private string BuildHost(string configuration)
    {
        var repositoryRoot = FindRepositoryRoot();
        var project = Path.Combine(
            repositoryRoot, "testsnew", "Brinell.Uia.TestHost", "Brinell.Uia.TestHost.csproj");

        Assert.True(File.Exists(project), $"The bridge host project is not at {project}.");

        var start = new ProcessStartInfo("dotnet")
        {
            ArgumentList =
            {
                "build", project, "-c", configuration, "--nologo", "-v", "quiet",

                "-nodeReuse:false",
            },
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // MSBuild keeps its worker nodes alive for a quarter of an hour by default, and they
        // inherit the handles of whoever started them - including the pipe this method is about
        // to read to the end. A surviving node keeps that pipe open, so the read blocks long
        // after the build has finished. Belt and braces: the switch above and the variable here
        // say the same thing, and the variable is the one that cannot be misparsed.
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";

        var build = Process.Start(start)
            ?? throw new InvalidOperationException("dotnet build did not start.");

        var log = build.StandardOutput.ReadToEnd() + build.StandardError.ReadToEnd();

        Assert.True(
            build.WaitForExit(BuildTimeoutMs),
            $"Building the bridge host in {configuration} did not finish within "
            + $"{BuildTimeoutMs} ms.");

        Assert.True(build.ExitCode == 0, $"Building in {configuration} failed:{Environment.NewLine}{log}");

        var executable = Path.Combine(
            repositoryRoot, "testsnew", "Brinell.Uia.TestHost", "bin", configuration,
            "net10.0-windows", "Brinell.Uia.TestHost.exe");

        Assert.True(File.Exists(executable), $"The {configuration} build produced no {executable}.");

        return executable;
    }

    /// <remarks>
    /// By the solution file rather than by counting directories up from the test assembly, which
    /// would move the moment the target framework or the output layout changed.
    /// </remarks>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Brinell.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"No Brinell.sln above {AppContext.BaseDirectory}, so the bridge host cannot be "
            + "built from here.");
    }

    /// <summary>One run of the bridge host, and what it said on the way up.</summary>
    private sealed class HostRun : IDisposable
    {
        private readonly Process _process;
        private readonly string _readyFile;

        private HostRun(Process process, string readyFile, string title, string report)
        {
            _process = process;
            _readyFile = readyFile;
            WindowTitle = title;
            Report = report;
            WindowHandle = new IntPtr(long.Parse(
                ReadValue(report, "HWND"), CultureInfo.InvariantCulture));
        }

        public string WindowTitle { get; }

        public string Report { get; }

        public IntPtr WindowHandle { get; }

        public static HostRun Start(string executable, bool bridgeRequested)
        {
            var runId = Guid.NewGuid().ToString("N");
            var title = $"Brinell UIA Gating Host {runId}";
            var readyFile = Path.Combine(Path.GetTempPath(), $"brinell-uia-gate-{runId}.ready");

            var start = new ProcessStartInfo(executable)
            {
                ArgumentList = { title, readyFile },
                UseShellExecute = false,
            };

            // Cleared rather than left alone. A run started from a shell that happens to have
            // the variable set would otherwise quietly test the opposite of what it says.
            start.Environment[BrinellBridgeGate.EnableVariable] = bridgeRequested ? "1" : "0";

            // See LifetimeHost: an inherited stdout pipe outlives a stray process and hangs the
            // run that launched it.
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;

            var process = Process.Start(start)
                ?? throw new InvalidOperationException("The bridge host did not start.");

            return new HostRun(process, readyFile, title, WaitForReady(process, readyFile));
        }

        private static string WaitForReady(Process process, string readyFile)
        {
            var elapsed = Stopwatch.StartNew();

            while (elapsed.ElapsedMilliseconds < StartTimeoutMs)
            {
                if (process.HasExited)
                {
                    throw new InvalidOperationException(
                        $"The bridge host exited with code {process.ExitCode} before it was "
                        + "ready.");
                }

                if (File.Exists(readyFile))
                {
                    return File.ReadAllText(readyFile);
                }

                Thread.Sleep(PollIntervalMs);
            }

            throw new TimeoutException(
                $"The bridge host did not report ready within {StartTimeoutMs} ms.");
        }

        private static string ReadValue(string report, string key)
        {
            foreach (var line in report.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith(key + "=", StringComparison.Ordinal))
                {
                    return trimmed[(key.Length + 1)..];
                }
            }

            throw new InvalidOperationException(
                $"The bridge host reported no '{key}'. What it did report was: {report}");
        }

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
}

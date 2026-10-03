using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace Brinell.Bdd.VisualStudio;

/// <summary>
/// Runs a scenario, or a file, from the preview (R3) and keeps the UI test rules (R4): build the
/// solution through Visual Studio first, so a stale app never runs; then
/// <c>dotnet test &lt;project&gt; --no-build --filter BddScenario=&lt;address&gt;</c>, its output in the
/// Output pane. One run at a time; a build started during a run is cancelled; Cancel stops the test
/// process and the app it started. Debug runs the same way with the testhost waiting for a debugger,
/// and attaches Visual Studio's to it, so step breakpoints (R2) stop the run.
/// </summary>
internal sealed class BddRunController : IVsDebuggerEvents
{
    private readonly AsyncPackage _package;
    private readonly BddServerClient _server;
    private readonly Action<string> _log;
    private readonly Action<object> _post;

    // The testhost's line under VSTEST_HOST_DEBUG: "Process Id: 12345, Name: testhost".
    private static readonly Regex TesthostProcess = new(@"Process Id:\s*(?<id>\d+)", RegexOptions.CultureInvariant);

    // The .NET (CoreCLR) debug engine, which a net10 testhost needs.
    private const string CoreClrEngine = "{2E36F1D4-B23C-435D-AB41-18E608940038}";
    private TaskCompletionSource<bool>? _build;
    private Process? _process;

    // The run going now, for the preview's "running" message; and, during a Debug run, the debugger
    // whose break mode the preview shows as "paused in the debugger".
    private int? _runLine;
    private IVsDebugger? _debugger;
    private uint _debuggerCookie;

    public BddRunController(AsyncPackage package, BddServerClient server, Action<string> log, Action<object> post)
    {
        _package = package;
        _server = server;
        _log = log;
        _post = post;
    }

    /// <summary>Whether a run is going, from its build to its end.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Whether the build going now is the run's own.</summary>
    public bool OwnBuild => _build is { Task.IsCompleted: false };

    /// <summary>Runs the scenario at a heading's line, or the whole file when <paramref name="line"/> is null.</summary>
    /// <param name="path">The feature file.</param>
    /// <param name="line">The scenario heading's line.</param>
    /// <param name="suite">The suite picked for a linked file; null for the first.</param>
    /// <param name="debug">Whether to run under Visual Studio's debugger.</param>
    public async Task RunAsync(string path, int? line, string? suite, bool debug)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (IsRunning)
        {
            await StatusAsync("A BDD run is going already. Wait for it, or cancel it, before starting another.");
            return;
        }

        IsRunning = true;
        _runLine = line;
        PostRunning(paused: false);
        if (debug)
        {
            await WatchDebuggerAsync();
        }

        try
        {
            _log(string.Empty);
            _log($"Building the solution for a UAT {(debug ? "debug " : string.Empty)}run of {System.IO.Path.GetFileName(path)}{(line is null ? string.Empty : $" line {line}")}...");
            if (!await BuildAsync())
            {
                await StatusAsync("The build failed, so the BDD run did not start. See the Error List.");
                return;
            }

            await _server.BuildDoneAsync(_package.DisposalToken);
            var target = await _server.RunTargetAsync(path, line, suite, _package.DisposalToken);
            if ((bool?)target["canRun"] != true || (string?)target["project"] is not { } project || (string?)target["filter"] is not { } filter)
            {
                await StatusAsync($"This cannot be run from the preview: {(string?)target["reason"]}");
                return;
            }

            await RunTestsAsync(project, filter, debug);
        }
        finally
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync();
            UnwatchDebugger();
            IsRunning = false;
            _post(new { type = "running" });
        }
    }

    /// <summary>
    /// Records a video script, or the scenario at a heading's line (the narrator): the build, the
    /// gate run with <c>BRINELL_RECORD</c>, then the video it made, offered to watch.
    /// </summary>
    /// <param name="path">The video script, or the feature file.</param>
    /// <param name="line">The scenario heading's line, for a feature file.</param>
    /// <param name="suite">The suite picked for a linked file; null for the first.</param>
    /// <param name="options">The Recording options: the folder, the voice and the beat.</param>
    public async Task RecordAsync(string path, int? line, string? suite, BddOptionsPage options)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (IsRunning)
        {
            await StatusAsync("A BDD run is going already. Wait for it, or cancel it, before recording.");
            return;
        }

        IsRunning = true;
        _post(new { type = "running", line, file = line is null });
        try
        {
            _log(string.Empty);
            _log($"Building the solution to record {Path.GetFileName(path)}{(line is null ? string.Empty : $" line {line}")}...");
            if (!await BuildAsync())
            {
                await StatusAsync("The build failed, so the recording did not start. See the Error List.");
                return;
            }

            await _server.BuildDoneAsync(_package.DisposalToken);
            var target = await _server.RecordTargetAsync(path, line, suite, options, _package.DisposalToken);
            if ((bool?)target["canRecord"] != true || (string?)target["project"] is not { } project || (string?)target["filter"] is not { } filter)
            {
                await StatusAsync($"This cannot be recorded: {(string?)target["reason"]}");
                return;
            }

            var variables = (target["environment"] as JObject)?.Properties().ToDictionary(property => property.Name, property => (string?)property.Value ?? string.Empty) ?? [];
            _log($"Recording into {(string?)target["folder"]}...");
            var code = await RunTestsAsync(project, filter, debug: false, variables);
            var videos = await _server.RecordingsAsync(path, suite, options.RecordingsFolder, _package.DisposalToken);
            var expected = (string?)target["video"];
            var video = videos.OfType<JObject>().FirstOrDefault(candidate => expected is null || (string?)candidate["name"] == expected);
            if (video is null || (string?)video["path"] is not { } file)
            {
                await StatusAsync("The run made no video: see the Brinell BDD output.");
                return;
            }

            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            var failed = code != 0 ? " A scenario failed: the video shows where." : string.Empty;
            _log($"Recorded {file}.{failed}");
            if (VsShellUtilities.ShowMessageBox(_package, $"Recorded {Path.GetFileName(file)}.{failed}\n\nWatch it now?", "Brinell BDD", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST) == 6)
            {
                using (Process.Start(new ProcessStartInfo(file) { UseShellExecute = true }))
                {
                }
            }
        }
        finally
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync();
            IsRunning = false;
            _post(new { type = "running" });
        }
    }

    /// <inheritdoc />
    public int OnModeChange(DBGMODE dbgmodeNew)
    {
        if (IsRunning && _debugger is not null)
        {
            PostRunning(paused: (dbgmodeNew & ~DBGMODE.DBGMODE_EncMask) == DBGMODE.DBGMODE_Break);
        }

        return VSConstants.S_OK;
    }

    private void PostRunning(bool paused)
        => _post(new { type = "running", line = _runLine, file = _runLine is null, paused });

    // While a Debug run goes: the preview says "paused in the debugger" when Visual Studio breaks,
    // so a run held by an exception or a C# breakpoint does not look like one that never ends.
    private async Task WatchDebuggerAsync()
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (await _package.GetServiceAsync(typeof(SVsShellDebugger)) is IVsDebugger debugger &&
            ErrorHandler.Succeeded(debugger.AdviseDebuggerEvents(this, out var cookie)))
        {
            _debugger = debugger;
            _debuggerCookie = cookie;
        }
    }

    private void UnwatchDebugger()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _debugger?.UnadviseDebuggerEvents(_debuggerCookie);
        _debugger = null;
    }

    /// <summary>Stops the run: the test process and everything it started, the app included.</summary>
    public void Cancel()
    {
        if (_process is { HasExited: false } process)
        {
            _log("Cancelling the BDD run...");

            // The whole tree: dotnet test, its testhost, and the app the fixture started.
            using var kill = Process.Start(new ProcessStartInfo("taskkill", $"/PID {process.Id} /T /F") { UseShellExecute = false, CreateNoWindow = true });
            kill?.WaitForExit(10_000);
        }
    }

    /// <summary>The build finished; the run waiting for it goes on when it succeeded.</summary>
    public void BuildFinished(bool succeeded) => _build?.TrySetResult(succeeded);

    private async Task<bool> BuildAsync()
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (await _package.GetServiceAsync(typeof(SVsSolutionBuildManager)) is not IVsSolutionBuildManager2 builds)
        {
            return false;
        }

        _build = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = builds.StartSimpleUpdateSolutionConfiguration((uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD, 0, 0);
        if (started != Microsoft.VisualStudio.VSConstants.S_OK)
        {
            _build = null;
            return false;
        }

        // Completed by UpdateSolution_Done on the main thread, which nothing here blocks.
#pragma warning disable VSTHRD003
        return await _build.Task;
#pragma warning restore VSTHRD003
    }

    private async Task<int> RunTestsAsync(string project, string filter, bool debug, IReadOnlyDictionary<string, string>? variables = null)
    {
        var arguments = $"test \"{project}\" --no-build --filter \"{filter}\" --logger \"console;verbosity=normal\"";
        _log($"dotnet {arguments}");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo("dotnet", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(project)!,
            },
            EnableRaisingEvents = true,
        };
        foreach (var variable in variables ?? new Dictionary<string, string>())
        {
            process.StartInfo.EnvironmentVariables[variable.Key] = variable.Value;
        }

        if (debug)
        {
            // The testhost prints its process id and waits for a debugger; VSTEST_DEBUG_NOBP skips
            // the break it makes once one attaches, so the run goes on to the step breakpoints.
            process.StartInfo.EnvironmentVariables["VSTEST_HOST_DEBUG"] = "1";
            process.StartInfo.EnvironmentVariables["VSTEST_DEBUG_NOBP"] = "1";
        }

        // The testhost prints its "Process Id" line twice; a second attach to it fails and would cancel the run.
        var attaching = new HashSet<int>();
        var exited = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.OutputDataReceived += (_, e) => Line(e.Data);
        process.ErrorDataReceived += (_, e) => Line(e.Data);
        process.Exited += (_, _) => exited.TrySetResult(process.ExitCode);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _process = process;
        try
        {
            // Completed by the process's Exited event, on a thread pool thread.
#pragma warning disable VSTHRD003
            var code = await exited.Task;
#pragma warning restore VSTHRD003
            _log(code == 0 ? "The BDD run passed." : $"The BDD run ended with exit code {code}.");
            return code;
        }
        finally
        {
            _process = null;
            process.Dispose();
        }

        void Line(string? line)
        {
            if (line is null)
            {
                return;
            }

            _log(line);
            if (debug && TesthostProcess.Match(line) is { Success: true } match)
            {
                var id = int.Parse(match.Groups["id"].Value, System.Globalization.CultureInfo.InvariantCulture);
                lock (attaching)
                {
                    if (!attaching.Add(id))
                    {
                        return;
                    }
                }

                _package.JoinableTaskFactory.RunAsync(() => AttachAsync(id)).FileAndForget("Brinell/PreviewDebugAttach");
            }
        }
    }

    // Attaches Visual Studio's debugger to the waiting testhost; a testhost left waiting would hang
    // the run, so a failed attach cancels it. Then the apps the test starts are attached too.
    private async Task AttachAsync(int processId)
    {
        try
        {
            await AttachToAsync(processId);
            _log($"The debugger is attached to the testhost (process {processId}).");
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException)
        {
            await StatusAsync($"The debugger could not attach to the testhost: {exception.Message}. The run is cancelled.");
            Cancel();
            return;
        }

        await AttachAppsAsync(processId);
    }

    // While the run goes: each app the testhost announces (Brinell.Core's DebuggedApps) is attached,
    // so a breakpoint in the app's own code stops too.
    private async Task AttachAppsAsync(int testhost)
    {
        var folder = Path.Combine(Path.GetTempPath(), "Brinell", "debugged-apps");
        HashSet<int> attached = [];
        while (_process is { HasExited: false })
        {
            foreach (var file in Directory.Exists(folder) ? Directory.EnumerateFiles(folder, $"{testhost}-*.json") : [])
            {
                if (!int.TryParse(Path.GetFileNameWithoutExtension(file).Split('-').Last(), out var app) || !attached.Add(app))
                {
                    continue;
                }

                try
                {
                    await AttachToAsync(app);
                    _log($"The debugger is attached to the app too (process {app}).");
                }
                catch (Exception exception) when (exception is COMException or InvalidOperationException)
                {
                    _log($"The debugger could not attach to the app (process {app}): {exception.Message}. The run goes on.");
                }

                TryDelete(file);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), _package.DisposalToken);
        }
    }

    private async Task AttachToAsync(int processId)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (await _package.GetServiceAsync(typeof(EnvDTE.DTE)) is not EnvDTE80.DTE2 dte)
        {
            throw new InvalidOperationException("no DTE to attach with");
        }

        EnvDTE80.Process2? target = null;
        foreach (EnvDTE.Process process in dte.Debugger.LocalProcesses)
        {
            if (process.ProcessID == processId)
            {
                target = process as EnvDTE80.Process2;
                break;
            }
        }

        if (target is null)
        {
            throw new InvalidOperationException($"no process {processId} to attach to");
        }

        try
        {
            target.Attach2(CoreClrEngine);
        }
        catch (COMException)
        {
            // An engine this Visual Studio names differently: let it choose.
            target.Attach();
        }
    }

    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Left for the next run of the same testhost id, which is not one.
        }
    }
    private async Task StatusAsync(string message)
    {
        _log(message);
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (await _package.GetServiceAsync(typeof(SVsStatusbar)) is IVsStatusbar status)
        {
            status.SetText(message);
        }
    }
}

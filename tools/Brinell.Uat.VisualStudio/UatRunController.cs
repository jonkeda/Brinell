using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace Brinell.Uat.VisualStudio;

/// <summary>
/// Runs a scenario, or a file, from the preview (R3) and keeps the UI test rules (R4): build the
/// solution through Visual Studio first, so a stale app never runs; then
/// <c>dotnet test &lt;project&gt; --no-build --filter UatScenario=&lt;address&gt;</c>, its output in the
/// Output pane. One run at a time; a build started during a run is cancelled; Cancel stops the test
/// process and the app it started.
/// </summary>
internal sealed class UatRunController
{
    private readonly AsyncPackage _package;
    private readonly UatServerClient _server;
    private readonly Action<string> _log;
    private readonly Action<object> _post;
    private TaskCompletionSource<bool>? _build;
    private Process? _process;

    public UatRunController(AsyncPackage package, UatServerClient server, Action<string> log, Action<object> post)
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
    public async Task RunAsync(string path, int? line, string? suite)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (IsRunning)
        {
            await StatusAsync("A UAT run is going already. Wait for it, or cancel it, before starting another.");
            return;
        }

        IsRunning = true;
        _post(new { type = "running", line, file = line is null });
        try
        {
            _log(string.Empty);
            _log($"Building the solution for a UAT run of {System.IO.Path.GetFileName(path)}{(line is null ? string.Empty : $" line {line}")}...");
            if (!await BuildAsync())
            {
                await StatusAsync("The build failed, so the UAT run did not start. See the Error List.");
                return;
            }

            await _server.BuildDoneAsync(_package.DisposalToken);
            var target = await _server.RunTargetAsync(path, line, suite, _package.DisposalToken);
            if ((bool?)target["canRun"] != true || (string?)target["project"] is not { } project || (string?)target["filter"] is not { } filter)
            {
                await StatusAsync($"This cannot be run from the preview: {(string?)target["reason"]}");
                return;
            }

            await RunTestsAsync(project, filter);
        }
        finally
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync();
            IsRunning = false;
            _post(new { type = "running" });
        }
    }

    /// <summary>Stops the run: the test process and everything it started, the app included.</summary>
    public void Cancel()
    {
        if (_process is { HasExited: false } process)
        {
            _log("Cancelling the UAT run...");

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

    private async Task RunTestsAsync(string project, string filter)
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
            _log(code == 0 ? "The UAT run passed." : $"The UAT run ended with exit code {code}.");
        }
        finally
        {
            _process = null;
            process.Dispose();
        }

        void Line(string? line)
        {
            if (line is not null)
            {
                _log(line);
            }
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

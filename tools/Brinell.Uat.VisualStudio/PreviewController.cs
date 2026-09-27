using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace Brinell.Uat.VisualStudio;

/// <summary>
/// Joins the editor to the preview: follows the active feature or step file, sends its text to the
/// server (on open, and after a pause in typing), refreshes the preview then and after each build,
/// keeps the caret and the preview in step both ways, and passes Visual Studio's theme on. Keeps
/// the step breakpoints: the preview's gutter and the editor command toggle them, and both show them.
/// </summary>
internal sealed class PreviewController : IVsSelectionEvents, IVsUpdateSolutionEvents, IDisposable
{
    // How long typing must pause before the preview is rendered again.
    private static readonly TimeSpan TypingPause = TimeSpan.FromMilliseconds(300);

    // A step in the preview's HTML: the lines a step breakpoint can go on.
    private static readonly Regex StepLine = new("<li class=\"uat-step(?! uat-not-a-step)[^\"]*\" data-line=\"(?<line>\\d+)\"", RegexOptions.CultureInvariant);

    private readonly AsyncPackage _package;
    private readonly Dictionary<string, string> _suites = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _opened = new(StringComparer.OrdinalIgnoreCase);
    private UatServerClient? _server;
    private UatRunController? _runs;
    private IVsOutputWindowPane? _output;
    private IVsMonitorSelection? _selection;
    private IVsSolutionBuildManager? _builds;
    private IVsEditorAdaptersFactoryService? _adapters;
    private uint _selectionCookie;
    private uint _buildCookie;
    private ScenarioPreviewControl? _view;
    private IWpfTextView? _textView;
    private string? _path;
    private int _version;
    private CancellationTokenSource? _typing;
    private ScenarioPreviewControl? _explorer;
    private string _explorerView = "steps";
    private string? _explorerSuite;

    public PreviewController(AsyncPackage package) => _package = package;

    /// <summary>Whether the active document is a feature or step file.</summary>
    public bool ActiveIsFeatureOrSteps => _path is not null;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        _output = await CreateOutputPaneAsync();
        _server = new UatServerClient(UatServerClient.DefaultServerPath, Log);
        _server.ResultsChanged += OnResultsChanged;
        _runs = new UatRunController(_package, _server, Log, Post);

        _selection = await _package.GetServiceAsync(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
        Assumes.Present(_selection);
        ErrorHandler.ThrowOnFailure(_selection.AdviseSelectionEvents(this, out _selectionCookie));

        _builds = await _package.GetServiceAsync(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
        Assumes.Present(_builds);
        ErrorHandler.ThrowOnFailure(_builds.AdviseUpdateSolutionEvents(this, out _buildCookie));

        var components = await _package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
        Assumes.Present(components);
        _adapters = components.GetService<IVsEditorAdaptersFactoryService>();

        VSColorTheme.ThemeChanged += OnThemeChanged;
        UatBreakpointStore.Instance.Changed += OnBreakpointsChanged;

        ErrorHandler.ThrowOnFailure(_selection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out var frame));
        await TrackAsync(frame, cancellationToken);
    }

    /// <summary>Shows the preview in a window's control: loads the host page once, then the active file.</summary>
    public async Task AttachAsync(ScenarioPreviewControl view)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (_view != view)
        {
            _view = view;
            view.MessageReceived += OnMessage;
        }

        if (!view.IsReady)
        {
            await EnsureServerAsync(_path);
            var host = await _server!.PreviewHostAsync(_package.DisposalToken);
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            await view.InitializeAsync(host);
            view.Post(VsTheme.Message());
        }

        await RefreshAsync();
    }

    public void Dispose()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        VSColorTheme.ThemeChanged -= OnThemeChanged;
        UatBreakpointStore.Instance.Changed -= OnBreakpointsChanged;
        if (_selectionCookie != 0)
        {
            _selection?.UnadviseSelectionEvents(_selectionCookie);
        }

        if (_buildCookie != 0)
        {
            _builds?.UnadviseUpdateSolutionEvents(_buildCookie);
        }

        Detach();
        _typing?.Cancel();
        _server?.Dispose();
    }

    // The active document changed: follow it when it is a feature or step file; otherwise the
    // preview keeps showing the last one.
    private async Task TrackAsync(object? documentFrame, CancellationToken cancellationToken)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        if (documentFrame is not IVsWindowFrame frame ||
            ErrorHandler.Failed(frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out var document)) ||
            document is not string path ||
            !IsFeatureOrSteps(path))
        {
            _path = null;
            return;
        }

        var textView = VsShellUtilities.GetTextView(frame) is { } vsView ? _adapters?.GetWpfTextView(vsView) : null;
        if (textView is null || string.Equals(path, _path, StringComparison.OrdinalIgnoreCase) && textView == _textView)
        {
            _path = textView is null ? null : path;
            return;
        }

        Detach();
        _path = path;
        _textView = textView;
        textView.TextBuffer.Changed += OnTextChanged;
        textView.Caret.PositionChanged += OnCaretMoved;
        textView.Closed += OnViewClosed;

        // The step breakpoints' dots: the Markdown editor hides the glyph margin by default.
        textView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginName, true);

        await EnsureServerAsync(path);
        _version = Math.Max(_version, 1);
        var text = textView.TextSnapshot.GetText();
        if (_opened.Add(path))
        {
            await _server!.DidOpenAsync(path, _version, text, cancellationToken);
        }
        else
        {
            await _server!.DidChangeAsync(path, ++_version, text, cancellationToken);
        }

        await RefreshAsync();
    }

    private async Task EnsureServerAsync(string? path)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        var solution = await _package.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
        string? directory = null;
        if (solution is not null)
        {
            ErrorHandler.ThrowOnFailure(solution.GetSolutionInfo(out directory, out _, out _));
        }

        // A folder opened without a solution: the file's folder, or the nearest one above with a solution file.
        directory = string.IsNullOrEmpty(directory) ? SolutionFolderOf(path) : directory;
        await _server!.InitializeAsync(directory!.TrimEnd('\\'), _package.DisposalToken);
    }

    private async Task RefreshAsync()
    {
        if (_view is null || _path is not { } path || _server is null)
        {
            return;
        }

        try
        {
            var preview = await _server.PreviewAsync(path, _suites.TryGetValue(path, out var suite) ? suite : null, _package.DisposalToken);
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            preview["type"] = "update";
            if ((string?)preview["resultsHost"] is { } host && (string?)preview["resultsRoot"] is { } root)
            {
                _view.MapResults(host, root);
            }

            _view.Post(preview);
            _view.Post(Breaks(path));
            if (_textView is { } view)
            {
                _view.Post(new { type = "scrollTo", line = view.Caret.Position.BufferPosition.GetContainingLine().LineNumber + 1 });
            }
        }
        catch (Exception exception) when (exception is StreamJsonRpc.RemoteInvocationException or StreamJsonRpc.ConnectionLostException or IOException)
        {
            Log($"The preview of {Path.GetFileName(path)} failed: {exception.Message}");
        }
    }

    private void OnTextChanged(object sender, TextContentChangedEventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _typing?.Cancel();
        var typing = _typing = new CancellationTokenSource();
        var path = _path;
        var snapshot = e.After;
        if (path is not null)
        {
            // Last change first: each one's line is then still where the file had it before.
            foreach (var change in e.Changes.Reverse())
            {
                UatBreakpointStore.Instance.Shift(path, e.Before.GetLineNumberFromPosition(change.OldPosition) + 1, change.LineCountDelta);
            }
        }

        _package.JoinableTaskFactory.RunAsync(async () =>
        {
            await Task.Delay(TypingPause, typing.Token);
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(typing.Token);
            if (path is null || _server is null || typing.IsCancellationRequested)
            {
                return;
            }

            await _server.DidChangeAsync(path, ++_version, snapshot.TextBuffer.CurrentSnapshot.GetText(), typing.Token);
            await RefreshAsync();
        }).FileAndForget("Brinell/PreviewTyping");
    }

    private void OnCaretMoved(object sender, CaretPositionChangedEventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _view?.Post(new { type = "scrollTo", line = e.NewPosition.BufferPosition.GetContainingLine().LineNumber + 1 });
    }

    private void OnViewClosed(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_path is { } path && _server is not null && _opened.Remove(path))
        {
            _package.JoinableTaskFactory.RunAsync(() => _server.DidCloseAsync(path, _package.DisposalToken)).FileAndForget("Brinell/PreviewClose");
        }

        Detach();
        _path = null;
    }

    // From the page: a click on a block moves the caret there; a suite picked binds the file in it.
    private void OnMessage(object sender, JObject message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        switch ((string?)message["type"])
        {
            case "reveal" when _textView is { } view && (int?)message["line"] is { } line && line >= 1 && line <= view.TextSnapshot.LineCount:
                var start = view.TextSnapshot.GetLineFromLineNumber(line - 1).Start;
                view.Caret.MoveTo(start);
                view.ViewScroller.EnsureSpanVisible(new SnapshotSpan(start, 0), EnsureSpanVisibleOptions.AlwaysCenter);
                break;
            case "run" when _path is { } file && (int?)message["line"] is { } heading:
                Run(file, heading, (bool?)message["debug"] == true);
                break;
            case "runFile" when _path is { } file:
                Run(file, null, (bool?)message["debug"] == true);
                break;
            case "toggleBreak" when _path is { } file && (int?)message["line"] is { } step:
                UatBreakpointStore.Instance.Toggle(file, step);
                break;
            case "cancel":
                _runs?.Cancel();
                break;
            case "openScreenshot" when (string?)message["path"] is { } screenshot && File.Exists(screenshot):
                VsShellUtilities.OpenDocument(_package, screenshot);
                break;
            case "pickSuite" when _path is { } path && (string?)message["suite"] is { } suite:
                _suites[path] = suite;
                _package.JoinableTaskFactory.RunAsync(RefreshAsync).FileAndForget("Brinell/PreviewPickSuite");
                break;
        }
    }

    private void Run(string path, int? line, bool debug)
    {
        var suite = _suites.TryGetValue(path, out var picked) ? picked : null;
        _package.JoinableTaskFactory.RunAsync(() => _runs!.RunAsync(path, line, suite, debug)).FileAndForget("Brinell/PreviewRun");
    }

    /// <summary>
    /// The editor command: toggles the step breakpoint on the caret's line. A line that is not a step,
    /// by the server's parse of the text as it is now, gets none, saying so.
    /// </summary>
    public async Task ToggleBreakpointAtCaretAsync()
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (_textView is not { } view || _path is not { } path || _server is null)
        {
            return;
        }

        var line = view.Caret.Position.BufferPosition.GetContainingLine().LineNumber + 1;
        if (!UatBreakpointStore.Instance.LinesOf(path).Contains(line))
        {
            await EnsureServerAsync(path);
            _typing?.Cancel();
            await _server.DidChangeAsync(path, ++_version, view.TextSnapshot.GetText(), _package.DisposalToken);
            var preview = await _server.PreviewAsync(path, _suites.TryGetValue(path, out var suite) ? suite : null, _package.DisposalToken);
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            var steps = StepLine.Matches((string?)preview["html"] ?? string.Empty).Cast<Match>()
                .Select(match => int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture));
            if (!steps.Contains(line))
            {
                if (await _package.GetServiceAsync(typeof(SVsStatusbar)) is IVsStatusbar status)
                {
                    status.SetText($"Line {line} is not a step, so it cannot have a UAT step breakpoint.");
                }

                return;
            }
        }

        UatBreakpointStore.Instance.Toggle(path, line);
    }

    /// <summary>Shows the UAT Explorer in a window's control: loads its page once, then the view.</summary>
    public async Task AttachExplorerAsync(ScenarioPreviewControl view)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (_explorer != view)
        {
            _explorer = view;
            view.MessageReceived += OnExplorerMessage;
        }

        if (!view.IsReady)
        {
            await EnsureServerAsync(_path);
            var host = await _server!.ExplorerHostAsync(_package.DisposalToken);
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            await view.InitializeAsync(host);
            view.Post(VsTheme.Message());
        }

        await RefreshExplorerAsync();
    }

    private async Task RefreshExplorerAsync()
    {
        if (_explorer is not { } view || _server is null)
        {
            return;
        }

        try
        {
            var result = await _server.ExplorerAsync(_explorerView, _explorerSuite, _package.DisposalToken);
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            result["type"] = "update";
            view.Post(result);
        }
        catch (Exception exception) when (exception is StreamJsonRpc.RemoteInvocationException or StreamJsonRpc.ConnectionLostException or IOException)
        {
            Log($"The UAT Explorer failed: {exception.Message}");
        }
    }

    // From the explorer's page: a view picked, a suite picked, or an entry to open at its line.
    private void OnExplorerMessage(object sender, JObject message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        switch ((string?)message["type"])
        {
            case "view" when (string?)message["view"] is { } name:
                _explorerView = name;
                _package.JoinableTaskFactory.RunAsync(RefreshExplorerAsync).FileAndForget("Brinell/ExplorerView");
                break;
            case "pickSuite" when (string?)message["suite"] is { } suite:
                _explorerSuite = suite;
                _package.JoinableTaskFactory.RunAsync(RefreshExplorerAsync).FileAndForget("Brinell/ExplorerSuite");
                break;
            case "open" when (string?)message["uri"] is { } uri && Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile && File.Exists(parsed.LocalPath):
                VsShellUtilities.OpenDocument(_package, parsed.LocalPath, Guid.Empty, out _, out _, out _, out var textView);
                if (textView is not null && (int?)message["line"] is { } line && line >= 1)
                {
                    textView.SetCaretPos(line - 1, 0);
                    textView.CenterLines(line - 1, 1);
                }

                break;
        }
    }

    /// <summary>The steps that use a C# member (CodeLens), in the suites of the solution that holds the C# file.</summary>
    public async Task<JObject> UsagesAsync(string csharpFile, string symbol)
    {
        await EnsureServerAsync(csharpFile);
        return await _server!.UsagesAsync(symbol, _package.DisposalToken);
    }

    private static object Breaks(string path) => new { type = "breaks", lines = UatBreakpointStore.Instance.LinesOf(path) };

    // A breakpoint set or cleared, from the preview, the editor or an edit: the preview shows it.
    private void OnBreakpointsChanged(object sender, string path)
    {
        if (string.Equals(path, _path, StringComparison.OrdinalIgnoreCase))
        {
            _package.JoinableTaskFactory.RunAsync(async () =>
            {
                await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
                _view?.Post(Breaks(path));
            }).FileAndForget("Brinell/PreviewBreaks");
        }
    }

    // A message to the page from any thread.
    private void Post(object message)
        => _package.JoinableTaskFactory.RunAsync(async () =>
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            _view?.Post(message);
        }).FileAndForget("Brinell/PreviewPost");

    // A run wrote results for the file on show: draw them.
    private void OnResultsChanged(object sender, string path)
    {
        if (_explorerView == "results")
        {
            _package.JoinableTaskFactory.RunAsync(RefreshExplorerAsync).FileAndForget("Brinell/ExplorerResults");
        }

        if (string.Equals(path, _path, StringComparison.OrdinalIgnoreCase))
        {
            _package.JoinableTaskFactory.RunAsync(RefreshAsync).FileAndForget("Brinell/PreviewResults");
        }
    }

    private void OnThemeChanged(ThemeChangedEventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _view?.Post(VsTheme.Message());
    }

    private void Detach()
    {
        if (_textView is { } view)
        {
            view.TextBuffer.Changed -= OnTextChanged;
            view.Caret.PositionChanged -= OnCaretMoved;
            view.Closed -= OnViewClosed;
        }

        _textView = null;
    }

    private async Task<IVsOutputWindowPane?> CreateOutputPaneAsync()
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (await _package.GetServiceAsync(typeof(SVsOutputWindow)) is not IVsOutputWindow output)
        {
            return null;
        }

        var guid = new Guid("5c1b1a9e-6b1f-4f7e-9b1e-9c0b8f4e2a61");
        output.CreatePane(ref guid, "Brinell UAT", fInitVisible: 1, fClearWithSolution: 0);
        output.GetPane(ref guid, out var pane);
        return pane;
    }

    // OutputStringThreadSafe may be called from any thread; the analyzer cannot know that.
#pragma warning disable VSTHRD010
    private void Log(string line) => _output?.OutputStringThreadSafe(line + Environment.NewLine);
#pragma warning restore VSTHRD010

    internal static bool IsFeatureOrSteps(string path)
        => path.EndsWith(".feature.md", StringComparison.OrdinalIgnoreCase) ||
           path.EndsWith(".steps.md", StringComparison.OrdinalIgnoreCase) ||
           path.EndsWith(".uat.md", StringComparison.OrdinalIgnoreCase) ||
           path.EndsWith(".phrases.md", StringComparison.OrdinalIgnoreCase);

    private static string SolutionFolderOf(string? path)
    {
        var start = path is null ? Environment.CurrentDirectory : Path.GetDirectoryName(path)!;
        for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
        {
            if (folder.EnumerateFiles("*.sln").Any() || folder.EnumerateFiles("*.slnx").Any())
            {
                return folder.FullName;
            }
        }

        return start;
    }

    int IVsSelectionEvents.OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        => VSConstants.S_OK;

    int IVsSelectionEvents.OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
    {
        if (elementid == (uint)VSConstants.VSSELELEMID.SEID_DocumentFrame)
        {
            _package.JoinableTaskFactory.RunAsync(() => TrackAsync(varValueNew, _package.DisposalToken)).FileAndForget("Brinell/PreviewTrack");
        }

        return VSConstants.S_OK;
    }

    int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive) => VSConstants.S_OK;

    // R4: a build during a run would overwrite binaries under it, so it is cancelled, saying why.
    int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
    {
        if (_runs is { IsRunning: true, OwnBuild: false })
        {
            pfCancelUpdate = 1;
            Log("A UAT run is going, so the build is cancelled: it would overwrite the binaries under the run. Cancel the run first.");
        }

        return VSConstants.S_OK;
    }

    // A build finished: the suites load their new output, and the preview binds against it.
    int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
    {
        _runs?.BuildFinished(fSucceeded != 0 && fCancelCommand == 0);
        if (_server is { } server)
        {
            _package.JoinableTaskFactory.RunAsync(async () =>
            {
                await server.BuildDoneAsync(_package.DisposalToken);
                await RefreshAsync();
                await RefreshExplorerAsync();
            }).FileAndForget("Brinell/PreviewBuildDone");
        }

        return VSConstants.S_OK;
    }

    int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;

    int IVsUpdateSolutionEvents.UpdateSolution_Cancel() => VSConstants.S_OK;

    int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.S_OK;
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;

namespace Bruinet.Bdd.VisualStudio;

/// <summary>
/// The Scraper tool window's side: the page (bruinet/scraper/host) does the work; this relays its
/// bruinet/scraper/* requests and the server's bruinet/scraper/changed, and opens files, diffs and the
/// prompt for Copilot Chat. The same page is VS Code's Scraper view.
/// </summary>
internal sealed class ScraperController : IDisposable
{
    private readonly AsyncPackage _package;
    private readonly PreviewController _previews;
    private ScenarioPreviewControl? _view;
    private BddServerClient? _server;

    public ScraperController(AsyncPackage package, PreviewController previews)
    {
        _package = package;
        _previews = previews;
        VSColorTheme.ThemeChanged += OnThemeChanged;
        _previews.SolutionOpened += OnSolutionOpened;
    }

    /// <summary>Shows the Scraper in the window's control: loads the page once.</summary>
    public async Task AttachAsync(ScenarioPreviewControl view)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        var server = await _previews.ServerAsync();
        if (_server != server)
        {
            if (_server is not null)
            {
                _server.ScraperChanged -= OnScraperChanged;
            }

            _server = server;
            _server.ScraperChanged += OnScraperChanged;
        }

        if (_view != view)
        {
            _view = view;
            view.MessageReceived += OnMessage;
        }

        if (!view.IsReady)
        {
            var host = await server.ScraperHostAsync(_package.DisposalToken);
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            await view.InitializeAsync(host);
            view.Post(VsTheme.Message());
        }
    }

    public void Dispose()
    {
        VSColorTheme.ThemeChanged -= OnThemeChanged;
        _previews.SolutionOpened -= OnSolutionOpened;
        if (_server is not null)
        {
            _server.ScraperChanged -= OnScraperChanged;
        }
    }

    private void OnMessage(object sender, JObject message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        switch ((string?)message["type"])
        {
            case "ready":
                _view?.Post(VsTheme.Message());
                break;
            case "call" when (int?)message["id"] is { } id && (string?)message["method"] is { } method && method.StartsWith("bruinet/scraper/", StringComparison.Ordinal):
                _package.JoinableTaskFactory.RunAsync(() => CallAsync(id, method, message["params"])).FileAndForget("Bruinet/ScraperCall");
                break;
            case "open" when (string?)message["path"] is { } path && File.Exists(path):
                VsShellUtilities.OpenDocument(_package, path);
                break;
            case "diff" when (string?)message["left"] is { } left && (string?)message["right"] is { } right:
                Diff(left, right, (string?)message["title"]);
                break;
            case "addProject" when (string?)message["path"] is { } project && File.Exists(project):
                AddToSolution(project, (string?)message["solutionFolder"]);
                break;
            case "chat" when (string?)message["prompt"] is { } prompt:
                _package.JoinableTaskFactory.RunAsync(() => ChatAsync(prompt, (string?)message["agent"], (string?)message["model"])).FileAndForget("Bruinet/ScraperChat");
                break;
            case "agents":
                _package.JoinableTaskFactory.RunAsync(PostModelsAsync).FileAndForget("Bruinet/ScraperModels");
                break;
        }
    }

    // The prompt into Copilot Chat's conversation; where that fails, on the clipboard to paste there. The
    // model the person chose goes with it; an agent or mode cannot be chosen through Visual Studio's API
    // (the chat window's own mode is used), so the page is told.
    private async Task ChatAsync(string prompt, string? agent, string? model)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        var opened = TryOpenCopilotChat(_previews.Note);
        var sent = await CopilotChatSender.TrySendAsync(_package, prompt, model, _previews.Note, PostDone, _package.DisposalToken);
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        if (sent)
        {
            SetStatus("The prompt is sent to Copilot Chat.");
            _view?.Post(new JObject { ["type"] = "chatted", ["via"] = "sent", ["opened"] = true, ["model"] = model, ["agentIgnored"] = agent is { Length: > 0 } });
            return;
        }

        System.Windows.Clipboard.SetText(prompt);
        SetStatus("The prompt is on the clipboard: paste it into Copilot Chat in agent mode.");
        _view?.Post(new JObject { ["type"] = "chatted", ["via"] = "clipboard", ["opened"] = opened });
    }

    // The agent's answer is complete: the page checks its step again.
    private void PostDone() =>
        _package.JoinableTaskFactory.RunAsync(async () =>
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            _view?.Post(new JObject { ["type"] = "chatDone" });
        }).FileAndForget("Bruinet/ScraperChatDone");

    // The models the chat offers, for the page's model choice.
    private async Task PostModelsAsync()
    {
        var models = await CopilotChatSender.TryListModelsAsync(_package, _previews.Note, _package.DisposalToken);
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        _view?.Post(new JObject
        {
            ["type"] = "agents",
            ["models"] = new JArray(models.Select(m => new JObject { ["id"] = m.Family, ["name"] = m.Name.Length > 0 ? m.Name : m.Family })),
            ["agents"] = false,
        });
    }

    // Copilot Chat's window, found by its command's name (View.GitHub.Copilot.Chat or alike, by version);
    // false when Copilot is not installed or the command would not run.
    private static bool TryOpenCopilotChat(Action<string> log)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (Package.GetGlobalService(typeof(EnvDTE.DTE)) is not EnvDTE.DTE dte)
        {
            return false;
        }

        var names = new List<string>();
        foreach (EnvDTE.Command command in dte.Commands)
        {
            if (command.Name is { } name && name.IndexOf("Copilot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                names.Add(name);
            }
        }

        log($"Copilot Chat: commands named Copilot: {(names.Count == 0 ? "none" : string.Join(", ", names))}.");
        foreach (var name in names.Where(n => n.StartsWith("View.", StringComparison.Ordinal) && n.IndexOf("Chat", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            try
            {
                dte.ExecuteCommand(name);
                log($"Copilot Chat: opened with {name}.");
                return true;
            }
            catch (Exception exception) when (exception is COMException or ArgumentException)
            {
                log($"Copilot Chat: {name} did not run: {exception.Message}");
            }
        }

        return false;
    }

    // A new scraped project into the open solution, in a solution folder when one is named.
    private void AddToSolution(string project, string? solutionFolder)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (Package.GetGlobalService(typeof(EnvDTE.DTE)) is not EnvDTE80.DTE2 { Solution: EnvDTE80.Solution2 solution } || !solution.IsOpen)
        {
            SetStatus($"No solution is open: add {Path.GetFileName(project)} to one by hand.");
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(solutionFolder))
            {
                solution.AddFromFile(project);
            }
            else
            {
                EnvDTE.Project? folder = null;
                foreach (EnvDTE.Project candidate in solution.Projects)
                {
                    if (candidate.Kind == EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder && candidate.Name == solutionFolder)
                    {
                        folder = candidate;
                        break;
                    }
                }

                folder ??= solution.AddSolutionFolder(solutionFolder);
                ((EnvDTE80.SolutionFolder)folder.Object).AddFromFile(project);
            }

            SetStatus($"{Path.GetFileNameWithoutExtension(project)} is in the solution.");
        }
        catch (Exception exception) when (exception is COMException or ArgumentException)
        {
            _previews.Note($"Adding {project} to the solution failed: {exception.Message}");
            SetStatus($"Could not add {Path.GetFileName(project)} to the solution: {exception.Message}");
        }
    }

    // Another solution: its folder holds other projects, so the page lists them again.
    private void OnSolutionOpened(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _view?.Post(new JObject { ["type"] = "projects" });
    }

    // A request relayed; a failed one comes back as a problem the page shows.
    private async Task CallAsync(int id, string method, JToken? parameters)
    {
        JToken? value;
        try
        {
            value = await _server!.RequestAsync(method, parameters, _package.DisposalToken);
        }
        catch (Exception exception) when (exception is StreamJsonRpc.RemoteInvocationException or StreamJsonRpc.ConnectionLostException or IOException)
        {
            _previews.Note($"The Scraper's {method} failed: {exception.Message}");
            value = new JObject { ["value"] = null, ["problem"] = exception.Message };
        }

        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
        _view?.Post(new JObject { ["type"] = "result", ["id"] = id, ["value"] = value });
    }

    private void OnScraperChanged(object sender, JObject change)
        => _package.JoinableTaskFactory.RunAsync(async () =>
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            change["type"] = "changed";
            _view?.Post(change);
        }).FileAndForget("Bruinet/ScraperChanged");

    private void OnThemeChanged(ThemeChangedEventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _view?.Post(VsTheme.Message());
    }

    private void Diff(string left, string right, string? title)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (Package.GetGlobalService(typeof(SVsDifferenceService)) is IVsDifferenceService differences)
        {
            differences.OpenComparisonWindow2(left, right, title ?? "Scraper: compare captures", null, Path.GetFileName(left), Path.GetFileName(right), null, null, 0);
        }
    }

    private static void SetStatus(string text)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (Package.GetGlobalService(typeof(SVsStatusbar)) is IVsStatusbar status)
        {
            status.SetText(text);
        }
    }
}

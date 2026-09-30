using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;

namespace Brinell.Bdd.VisualStudio;

/// <summary>
/// The Scraper tool window's side: the page (brinell/scraper/host) does the work; this relays its
/// brinell/scraper/* requests and the server's brinell/scraper/changed, and opens files, diffs and the
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
            case "call" when (int?)message["id"] is { } id && (string?)message["method"] is { } method && method.StartsWith("brinell/scraper/", StringComparison.Ordinal):
                _package.JoinableTaskFactory.RunAsync(() => CallAsync(id, method, message["params"])).FileAndForget("Brinell/ScraperCall");
                break;
            case "open" when (string?)message["path"] is { } path && File.Exists(path):
                VsShellUtilities.OpenDocument(_package, path);
                break;
            case "diff" when (string?)message["left"] is { } left && (string?)message["right"] is { } right:
                Diff(left, right, (string?)message["title"]);
                break;
            case "chat" when (string?)message["prompt"] is { } prompt:
                // Copilot Chat has no command that takes a prompt: it goes to the clipboard, to paste there.
                System.Windows.Clipboard.SetText(prompt);
                SetStatus("The prompt is on the clipboard: paste it into Copilot Chat in agent mode.");
                break;
        }
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
        }).FileAndForget("Brinell/ScraperChanged");

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

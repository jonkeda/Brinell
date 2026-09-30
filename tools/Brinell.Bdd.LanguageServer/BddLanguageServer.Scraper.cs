using Brinell.Scraper.Projects;
using Brinell.Scraper.Service;
using StreamJsonRpc;

namespace Brinell.Bdd.LanguageServer;

// The Scraper: recording an app's pages into a UI test project, and reading them back, for the IDE's
// Scraper view. The work is ScraperService's; this is its JSON-RPC face. A change is sent to the
// client as brinell/scraper/changed.
public sealed partial class BddLanguageServer
{
    private ScraperService? _scraper;

    /// <summary>The Scraper over the solution's scraped projects; the MCP server shares it.</summary>
    internal ScraperService Scraper
    {
        get
        {
            lock (_gate)
            {
                if (_scraper is null)
                {
                    _scraper = new ScraperService(_workspace?.Root ?? Environment.CurrentDirectory);
                    _scraper.Changed += (_, change) => _ = _rpc?.NotifyWithParameterObjectAsync("brinell/scraper/changed", change);
                }

                return _scraper;
            }
        }
    }

    /// <summary><c>brinell/scraper/host</c>: the Scraper view's page, which an IDE loads once and relays for.</summary>
    [JsonRpcMethod("brinell/scraper/host")]
    public PreviewHostResult ScraperHost() => new(ScraperPage.Html);

    /// <summary><c>brinell/scraper/similar</c>: the nodes of the same kind as a node, on every page's newest capture.</summary>
    [JsonRpcMethod("brinell/scraper/similar")]
    public ScraperResult<IReadOnlyList<SimilarNode>> ScraperSimilar(ScraperPageNodeParams request)
        => Answer(() => Scraper.Similar(request.Project, request.Page, request.Node ?? 0));

    /// <summary><c>brinell/scraper/suggest</c>: the control and locator for a node, or for each member-worthy node of a page.</summary>
    [JsonRpcMethod("brinell/scraper/suggest")]
    public ScraperResult<IReadOnlyList<Brinell.Scraper.Html.ControlSuggestion>> ScraperSuggest(ScraperPageNodeParams request)
        => Answer(() => Scraper.Suggest(request.Project, request.Page, request.Node));

    /// <summary><c>brinell/scraper/header</c>: the first line of a file made from a page, or from one of its nodes.</summary>
    [JsonRpcMethod("brinell/scraper/header")]
    public ScraperResult<string> ScraperHeader(ScraperPageNodeParams request)
        => Answer(() => Scraper.Header(request.Project, request.Page, request.Node));

        /// <summary><c>brinell/scraper/projects</c>: the scraped projects and what each has recorded.</summary>
    [JsonRpcMethod("brinell/scraper/projects")]
    public IReadOnlyList<ScraperProjectInfo> ScraperProjects()
        => Scraper.Projects().Select(project => new ScraperProjectInfo(
            project.Name,
            project.Folder,
            project.Config.Platform,
            project.Config.StartUrl,
            project.Sessions().Select(session => session.Id).ToList())).ToList();

    /// <summary><c>brinell/scraper/state</c>: the recording in progress, or null.</summary>
    [JsonRpcMethod("brinell/scraper/state")]
    public RecordingState? ScraperState() => Scraper.Active;

    /// <summary><c>brinell/scraper/start</c>: opens the recording browser and starts a session.</summary>
    [JsonRpcMethod("brinell/scraper/start")]
    public Task<ScraperResult<RecordingState>> ScraperStart(ScraperStartParams request)
        => Answer(() => Scraper.StartRecordingAsync(request.Project, request.Session ?? "recording", request.Url, request.AutoCapture ?? true));

    /// <summary><c>brinell/scraper/capture</c>: captures the page the browser shows; null when it did not change.</summary>
    [JsonRpcMethod("brinell/scraper/capture")]
    public Task<ScraperResult<RecordingEvent?>> ScraperCapture(ScraperCaptureParams request)
        => Answer(() => Scraper.CapturePageAsync(request.Name));

    /// <summary><c>brinell/scraper/stop</c>: closes the recording browser.</summary>
    [JsonRpcMethod("brinell/scraper/stop")]
    public Task<ScraperResult<RecordingState?>> ScraperStop()
        => Answer(() => Scraper.StopRecordingAsync());

    /// <summary><c>brinell/scraper/pages</c>: a project's recorded pages, by key.</summary>
    [JsonRpcMethod("brinell/scraper/pages")]
    public ScraperResult<IReadOnlyList<ScraperPageInfo>> ScraperPages(ScraperProjectParams request)
        => Answer(() => (IReadOnlyList<ScraperPageInfo>)Scraper.Pages(request.Project).Select(page => new ScraperPageInfo(
            page.Key,
            page.Latest.Event.DisplayName,
            page.Latest.Event.Location,
            page.Captures.Select(capture => new ScraperCaptureInfo(capture.Session, capture.Event.Number, capture.Event.At, capture.Event.Nodes, Path.Combine(Scraper.Project(request.Project).RecordingsFolder, capture.Session, capture.Event.Snapshot ?? ""))).ToList())).ToList());

    /// <summary><c>brinell/scraper/outline</c>: a capture as an outline, or a node's subtree of it.</summary>
    [JsonRpcMethod("brinell/scraper/outline")]
    public ScraperResult<string> ScraperOutline(ScraperOutlineParams request)
        => Answer(() => SnapshotOutline.Render(Scraper.Snapshot(request.Project, request.Page, request.Session, request.Number).Snapshot, request.Node ?? 0, request.MaxLines ?? 400));

    /// <summary><c>brinell/scraper/highlight</c>: outlines a node of the last capture in the browser, or removes the outline.</summary>
    [JsonRpcMethod("brinell/scraper/highlight")]
    public Task<ScraperResult<bool>> ScraperHighlight(ScraperNodeParams request)
        => Answer(async () =>
        {
            await Scraper.HighlightAsync(request.Node).ConfigureAwait(false);
            return true;
        });

    /// <summary><c>brinell/scraper/pick</c>: captures, then lets the user click a node in the browser.</summary>
    [JsonRpcMethod("brinell/scraper/pick")]
    public Task<ScraperResult<ScraperPickResult>> ScraperPick()
        => Answer(async () =>
        {
            var (capture, node) = await Scraper.PickAsync().ConfigureAwait(false);
            return new ScraperPickResult(capture, node);
        });

    /// <summary><c>brinell/scraper/patterns</c>: what the recorded pages are built from.</summary>
    [JsonRpcMethod("brinell/scraper/patterns")]
    public ScraperResult<IReadOnlyList<Brinell.Scraper.Code.Pattern>> ScraperPatterns(ScraperProjectParams request)
        => Answer(() => Scraper.Patterns(request.Project));

    /// <summary><c>brinell/scraper/controls</c>: the page objects and controls a project has.</summary>
    [JsonRpcMethod("brinell/scraper/controls")]
    public ScraperResult<IReadOnlyList<Brinell.Scraper.Code.CodeClass>> ScraperControls(ScraperProjectParams request)
        => Answer(() => Scraper.Controls(request.Project));

        // A request the Scraper cannot answer comes back as a reason the view shows, not as an RPC error.
    private static ScraperResult<T> Answer<T>(Func<T> work)
    {
        try
        {
            return new ScraperResult<T>(work(), null);
        }
        catch (Exception e) when (e is ScraperProblem or FormatException or FileNotFoundException)
        {
            return new ScraperResult<T>(default, e.Message);
        }
    }

    private static async Task<ScraperResult<T>> Answer<T>(Func<Task<T>> work)
    {
        try
        {
            return new ScraperResult<T>(await work().ConfigureAwait(false), null);
        }
        catch (Exception e) when (e is ScraperProblem or FormatException or FileNotFoundException or InvalidOperationException or Microsoft.Playwright.PlaywrightException)
        {
            return new ScraperResult<T>(default, e.Message);
        }
    }

    private void DisposeScraper()
    {
        ScraperService? scraper;
        lock (_gate)
        {
            scraper = _scraper;
            _scraper = null;
        }

        scraper?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

/// <summary>An answer, or the reason there is none. Both are always sent, null or not: the view tells a result by them.</summary>
public sealed record ScraperResult<T>(
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)] T? Value,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)] string? Problem);

/// <summary>A scraped project.</summary>
public sealed record ScraperProjectInfo(string Name, string Folder, string Platform, string StartUrl, IReadOnlyList<string> Sessions);

/// <summary>A recorded page and its captures, newest first.</summary>
public sealed record ScraperPageInfo(string Key, string Name, string Location, IReadOnlyList<ScraperCaptureInfo> Captures);

/// <summary>One capture of a page.</summary>
public sealed record ScraperCaptureInfo(string Session, int Number, DateTimeOffset At, int Nodes, string File);

/// <summary>What the user picked: the capture it is in and its node.</summary>
public sealed record ScraperPickResult(RecordingEvent? Capture, int? Node);

/// <summary><c>brinell/scraper/start</c>'s parameters.</summary>
public sealed record ScraperStartParams(string? Project, string? Session, string? Url, bool? AutoCapture);

/// <summary><c>brinell/scraper/capture</c>'s parameters.</summary>
public sealed record ScraperCaptureParams(string? Name);

/// <summary>A project, by name; the only one when null.</summary>
public sealed record ScraperProjectParams(string? Project);

/// <summary><c>brinell/scraper/outline</c>'s parameters: the page's newest capture unless a session (and number) is given.</summary>
public sealed record ScraperOutlineParams(string? Project, string Page, string? Session, int? Number, int? Node, int? MaxLines);

/// <summary>A page's newest capture, and a node on it.</summary>
public sealed record ScraperPageNodeParams(string? Project, string Page, int? Node);

/// <summary>A node of the last capture; none to clear.</summary>
public sealed record ScraperNodeParams(int? Node);

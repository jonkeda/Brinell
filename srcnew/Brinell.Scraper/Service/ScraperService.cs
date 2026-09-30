using Brinell.Core.Scraping;
using Brinell.Scraper.Html;
using Brinell.Scraper.Projects;
using Brinell.Scraper.Recordings;

namespace Brinell.Scraper.Service;

/// <summary>
/// What the IDE view and an agent ask of the Scraper, over the scraped projects under a solution: one
/// recording at a time, and reading what is recorded. The language server and the MCP server are thin
/// adapters over it. It calls no model.
/// </summary>
public sealed class ScraperService : IAsyncDisposable
{
    private readonly string _solutionDir;
    private readonly Func<string, IScrapeSource> _sourceFor;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Recording? _recording;

    /// <summary>A service for the projects under <paramref name="solutionDir"/>.</summary>
    /// <param name="solutionDir">The solution's folder.</param>
    /// <param name="sourceFor">Makes the source for a platform; the Html source when null.</param>
    public ScraperService(string solutionDir, Func<string, IScrapeSource>? sourceFor = null)
    {
        _solutionDir = Path.GetFullPath(solutionDir);
        _sourceFor = sourceFor ?? DefaultSource;
    }

    /// <summary>Raised after a page is captured or the recording starts or stops.</summary>
    public event EventHandler<ScraperChange>? Changed;

    /// <summary>The recording in progress, or null.</summary>
    public RecordingState? Active => _recording?.State;

    /// <summary>The scraped projects: those with a <c>scraper.config.md</c>.</summary>
    public IReadOnlyList<ScrapeProject> Projects() => ScrapeProject.FindAll(_solutionDir);

    /// <summary>A project by name, or the only one when <paramref name="name"/> is null.</summary>
    /// <exception cref="ScraperProblem">No such project, or several and no name.</exception>
    public ScrapeProject Project(string? name)
    {
        var projects = Projects();
        if (projects.Count == 0)
            throw new ScraperProblem($"No scraped project under {_solutionDir}: add a {ScraperConfig.FileName} to a UI test project.");
        if (name is null)
            return projects.Count == 1 ? projects[0] : throw new ScraperProblem($"Say which project: {string.Join(", ", projects.Select(p => p.Name))}.");
        return projects.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ScraperProblem($"No scraped project '{name}'. They are {string.Join(", ", projects.Select(p => p.Name))}.");
    }

    /// <summary>
    /// Opens the recording browser on a project's start URL (or <paramref name="url"/>) and starts a
    /// session. With <paramref name="autoCapture"/>, each page the app moves to is captured once it settles.
    /// </summary>
    public async Task<RecordingState> StartRecordingAsync(string? projectName, string sessionName, string? url = null, bool autoCapture = true, bool visible = true, CancellationToken cancellationToken = default)
    {
        var project = Project(projectName);
        var start = url ?? project.Config.StartUrl;
        if (start.Length == 0)
            throw new ScraperProblem($"{project.Name} has no StartUrl in its {ScraperConfig.FileName}; give a url.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_recording is not null)
                throw new ScraperProblem($"A recording is in progress ({_recording.State.Session} in {_recording.State.Project}); stop it first.");
            var source = _sourceFor(project.Config.Platform);
            try
            {
                await source.StartAsync(new ScrapeStart(start, project.ProfileFolder, visible), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await source.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            var session = project.StartSession(sessionName, DateTimeOffset.Now);
            _recording = new Recording(project, session, source, autoCapture);
            if (autoCapture) source.Transitioned += OnTransitioned;
        }
        finally
        {
            _gate.Release();
        }
        Changed?.Invoke(this, new ScraperChange(ScraperChangeKind.Started, _recording.State, null));
        if (autoCapture) await CapturePageAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return _recording.State;
    }

    /// <summary>
    /// Captures the page the recording browser shows now into the session. Returns null when nothing
    /// changed since the last capture of the same page.
    /// </summary>
    public async Task<RecordingEvent?> CapturePageAsync(string? pageName = null, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        RecordingEvent? added;
        Recording recording;
        try
        {
            recording = _recording ?? throw new ScraperProblem("No recording is in progress: start one first.");
            var snapshot = await recording.Source.CaptureAsync(cancellationToken).ConfigureAwait(false);
            if (pageName is null && recording.IsUnchanged(snapshot)) return null;
            var screenshot = recording.Project.Config.Screenshots ? await recording.Source.ScreenshotAsync(cancellationToken).ConfigureAwait(false) : null;
            added = recording.Session.AddPage(snapshot, screenshot, pageName);
            recording.Remember(snapshot);
        }
        finally
        {
            _gate.Release();
        }
        Changed?.Invoke(this, new ScraperChange(ScraperChangeKind.Captured, recording.State, added));
        return added;
    }

    /// <summary>Closes the recording browser; the session stays as it is on disk.</summary>
    public async Task<RecordingState?> StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        Recording? stopped;
        try
        {
            stopped = _recording;
            _recording = null;
            if (stopped is null) return null;
            stopped.Source.Transitioned -= OnTransitioned;
            await stopped.Source.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
        Changed?.Invoke(this, new ScraperChange(ScraperChangeKind.Stopped, stopped.State, null));
        return stopped.State;
    }

    /// <summary>Outlines a node of the last capture in the recording browser, or removes the outline.</summary>
    public Task HighlightAsync(int? node, CancellationToken cancellationToken = default) =>
        (_recording ?? throw new ScraperProblem("No recording is in progress.")).Source.HighlightAsync(node, cancellationToken);

    /// <summary>Captures the page, then lets the user click a node in the recording browser; its number in that capture, or null.</summary>
    public async Task<(RecordingEvent? Capture, int? Node)> PickAsync(CancellationToken cancellationToken = default)
    {
        var capture = await CapturePageAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var recording = _recording ?? throw new ScraperProblem("No recording is in progress.");
        return (capture ?? recording.Session.Events.LastOrDefault(), await recording.Source.PickAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>A project's recorded pages: key, newest title, how many captures, and the newest capture's session and event.</summary>
    public IReadOnlyList<RecordedPage> Pages(string? projectName) => Project(projectName).Index().Pages;

    /// <summary>A capture's snapshot: the newest of a page, or a given session's event.</summary>
    /// <exception cref="ScraperProblem">No such page or capture.</exception>
    public (PageCapture Capture, ScrapeSnapshot Snapshot) Snapshot(string? projectName, string page, string? session = null, int? number = null)
    {
        var index = Project(projectName).Index();
        var recorded = index.Page(page) ?? throw new ScraperProblem($"No recorded page '{page}'. The pages are {string.Join(", ", index.Pages.Select(p => p.Key))}.");
        var capture = session is null
            ? recorded.Latest
            : recorded.Captures.FirstOrDefault(c => c.Session == session && (number is null || c.Event.Number == number))
              ?? throw new ScraperProblem($"Page '{page}' has no capture in session '{session}'{(number is null ? "" : $" numbered {number}")}.");
        return (capture, index.Read(capture));
    }

    /// <summary>The nodes on each page's newest capture with the same signature as a node (<see cref="NodeSignature"/>).</summary>
    public IReadOnlyList<SimilarNode> Similar(string? projectName, string page, int node)
    {
        var project = Project(projectName);
        var index = project.Index();
        var (_, snapshot) = Snapshot(project.Name, page);
        if (node < 1 || node > snapshot.Nodes.Count)
            throw new ScraperProblem($"Page '{page}' has nodes 1 to {snapshot.Nodes.Count}, not {node}.");
        var signature = NodeSignature.Of(snapshot, snapshot[node]);
        var found = new List<SimilarNode>();
        foreach (var recorded in index.Pages)
        {
            var other = index.Read(recorded.Latest);
            found.AddRange(other.Nodes.Where(n => NodeSignature.Of(other, n) == signature).Select(n => new SimilarNode(recorded.Key, n.Number, SnapshotOutline.Line(n))));
        }
        return found;
    }

    /// <summary>The nodes whose text, name or an attribute holds <paramref name="text"/>, on each page's newest capture.</summary>
    public IReadOnlyList<SimilarNode> Search(string? projectName, string text, int max = 100) =>
        Project(projectName).Index().Search(text).Take(max).Select(f => new SimilarNode(f.Page.Key, f.Node.Number, SnapshotOutline.Line(f.Node))).ToArray();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await StopRecordingAsync().ConfigureAwait(false);
        _gate.Dispose();
    }

    private static IScrapeSource DefaultSource(string platform) => platform switch
    {
        "html" or "blazor" => new PlaywrightScrapeSource(),
        _ => throw new ScraperProblem($"No source records '{platform}' yet: html and blazor are recorded."),
    };

    // A transition captures once the page settles; a page elsewhere (a sign-in provider) is not captured.
    private void OnTransitioned(object? sender, ScrapeTransition transition)
    {
        if (_recording is not { } recording || !recording.Project.IsOwnPage(transition.Location)) return;
        _ = CaptureOnTransitionAsync();
    }

    private async Task CaptureOnTransitionAsync()
    {
        try
        {
            await CapturePageAsync().ConfigureAwait(false);
        }
        catch (Exception e) when (e is ScraperProblem or InvalidOperationException or Microsoft.Playwright.PlaywrightException)
        {
            // The browser was closed or navigated on while capturing: the next transition captures again.
            Changed?.Invoke(this, new ScraperChange(ScraperChangeKind.Failed, _recording?.State, null, e.Message));
        }
    }

    private sealed class Recording(ScrapeProject project, RecordingSession session, IScrapeSource source, bool autoCapture)
    {
        private readonly Dictionary<string, string> _lastBody = new(StringComparer.Ordinal);

        public ScrapeProject Project { get; } = project;

        public RecordingSession Session { get; } = session;

        public IScrapeSource Source { get; } = source;

        public RecordingState State => new(Project.Name, Session.Id, Session.Folder, autoCapture, Session.Events.Count);

        public bool IsUnchanged(ScrapeSnapshot snapshot) =>
            _lastBody.TryGetValue(PageKey.Of(snapshot.Location), out var body) && body == Body(snapshot);

        public void Remember(ScrapeSnapshot snapshot) => _lastBody[PageKey.Of(snapshot.Location)] = Body(snapshot);

        // The nodes without the header: the same page captured twice, unchanged, has the same body.
        private static string Body(ScrapeSnapshot snapshot)
        {
            using var writer = new StringWriter();
            SnapshotFile.Write(snapshot with { CapturedAt = default }, writer);
            return writer.ToString();
        }
    }
}

/// <summary>A recording in progress.</summary>
public sealed record RecordingState(string Project, string Session, string Folder, bool AutoCapture, int Captures);

/// <summary>A node found on a page: its page key, its number on the page's newest capture, and its outline line.</summary>
public sealed record SimilarNode(string Page, int Node, string Line);

/// <summary>What changed in the service.</summary>
public enum ScraperChangeKind
{
    /// <summary>A recording started.</summary>
    Started,

    /// <summary>A page was captured.</summary>
    Captured,

    /// <summary>A recording stopped.</summary>
    Stopped,

    /// <summary>A capture on a transition failed.</summary>
    Failed,
}

/// <summary>A change: its kind, the recording, the captured page if any, and a message for a failure.</summary>
public sealed record ScraperChange(ScraperChangeKind Kind, RecordingState? Recording, RecordingEvent? Captured, string? Message = null);

/// <summary>A request the Scraper cannot answer; the message says why and what to do.</summary>
public sealed class ScraperProblem(string message) : Exception(message);

using Bruinell.Scraper.Code;
using Bruinell.Scraper.Explore;
using Bruinell.Scraper.Html;
using Bruinell.Scraper.Projects;
using Bruinell.Scraper.Service;
using StreamJsonRpc;

namespace Bruinell.Bdd.LanguageServer;

// The Scraper view's Explore, its settings and the config, Draft and Verify. Explore runs in the
// background: its steps and its end come as bruinell/scraper/changed (Exploring, SignIn, Explored).
public sealed partial class BddLanguageServer
{
    /// <summary><c>bruinell/scraper/exploreSummary</c>: what Explore would run with, to show before it starts.</summary>
    [JsonRpcMethod("bruinell/scraper/exploreSummary")]
    public ScraperResult<string> ScraperExploreSummary(ScraperExploreParams request)
        => Answer(() => Scraper.ExploreSummary(request.Project, request.Options()));

    /// <summary><c>bruinell/scraper/explore</c>: starts Explore; true once it runs. Its end comes as an Explored change.</summary>
    [JsonRpcMethod("bruinell/scraper/explore")]
    public ScraperResult<bool> ScraperExplore(ScraperExploreParams request)
        => Answer(() =>
        {
            if (Scraper.IsExploring) throw new ScraperProblem("Explore is running already.");
            _ = ExploreInBackgroundAsync(request);
            return true;
        });

    /// <summary><c>bruinell/scraper/exploreStop</c>: stops Explore; what it recorded stays.</summary>
    [JsonRpcMethod("bruinell/scraper/exploreStop")]
    public bool ScraperExploreStop()
    {
        Scraper.StopExploring();
        return true;
    }

    /// <summary><c>bruinell/scraper/exploreContinue</c>: goes on after the person signed in.</summary>
    [JsonRpcMethod("bruinell/scraper/exploreContinue")]
    public bool ScraperExploreContinue()
    {
        Scraper.ContinueExploring();
        return true;
    }

    /// <summary><c>bruinell/scraper/config</c>: a project's config as data, for the Explore settings.</summary>
    [JsonRpcMethod("bruinell/scraper/config")]
    public ScraperResult<ScraperConfigInfo> ScraperConfig(ScraperProjectParams request)
        => Answer(() =>
        {
            var project = Scraper.Project(request.Project);
            var c = project.Config;
            var e = c.Explore;
            return new ScraperConfigInfo(
                Path.Combine(project.Folder, Bruinell.Scraper.Projects.ScraperConfig.FileName),
                c.StartUrl,
                c.Aliases,
                e.MaxPages, e.MaxDepth, e.MaxMinutes, e.DelayMs, e.AgentMinutes, e.LoginUrl, e.Within,
                Bruinell.Scraper.Explore.Explorer.Policy(e.ClickPolicy),
                e.BlockMethods,
                e.WebsocketApps.ToString().ToLowerInvariant(),
                ExploreSettings.Categories.ToDictionary(category => category, e.IsOn),
                e.Hosts,
                e.NeverClick.Select(m => m.ToString()).ToList(),
                e.AlwaysClick.Select(m => m.ToString()).ToList(),
                e.NeverVisit,
                e.AllowRequests.Select(a => $"{a.Method} {a.Url}".Trim()).ToList());
        });

    /// <summary><c>bruinell/scraper/configEdit</c>: one change to a project's config; the sentence says what was written.</summary>
    [JsonRpcMethod("bruinell/scraper/configEdit")]
    public ScraperResult<string> ScraperConfigEdit(ScraperConfigEditParams request)
        => Answer(() => Scraper.EditConfig(request.Project, new ConfigChange(request.Action, request.Section, request.Values)).Description);

    /// <summary><c>bruinell/scraper/configCandidates</c>: the UI test projects with no config yet.</summary>
    [JsonRpcMethod("bruinell/scraper/configCandidates")]
    public IReadOnlyList<string> ScraperConfigCandidates() => Scraper.ConfigCandidates();

    /// <summary><c>bruinell/scraper/createConfig</c>: writes a config into a UI test project; the project's name.</summary>
    [JsonRpcMethod("bruinell/scraper/createConfig")]
    public ScraperResult<string> ScraperCreateConfig(ScraperCreateConfigParams request)
        => Answer(() => Scraper.CreateConfig(request.Folder, request.StartUrl).Name);

    /// <summary><c>bruinell/scraper/projectDefaults</c>: the folder and Bruinell references the new project form proposes.</summary>
    [JsonRpcMethod("bruinell/scraper/projectDefaults")]
    public ScraperResult<ScraperNewProjectDefaults> ScraperProjectDefaults(ScraperNewProjectParams request)
        => Answer(() =>
        {
            var defaults = Scraper.NewProjectDefaults(request.Name);
            return new ScraperNewProjectDefaults(defaults.Folder, defaults.References.ToString().ToLowerInvariant(), defaults.Version);
        });

    /// <summary><c>bruinell/scraper/newProjectCheck</c>: what is wrong with a new project, by field; empty when nothing is.</summary>
    [JsonRpcMethod("bruinell/scraper/newProjectCheck")]
    public ScraperResult<IReadOnlyDictionary<string, string>> ScraperNewProjectCheck(ScraperNewProjectParams request)
        => Answer(() => Scraper.NewProjectProblems(request.ToRequest()));

    /// <summary><c>bruinell/scraper/newProject</c>: makes a scraped project; its .csproj's path, for the IDE to add to the solution.</summary>
    [JsonRpcMethod("bruinell/scraper/newProject")]
    public ScraperResult<string> ScraperNewProject(ScraperNewProjectParams request)
        => Answer(() =>
        {
            var project = Scraper.NewProject(request.ToRequest());
            return Path.Combine(project.Folder, project.Name + ".csproj");
        });

    /// <summary><c>bruinell/scraper/location</c>: where the recording browser is, or null.</summary>
    [JsonRpcMethod("bruinell/scraper/location")]
    public string? ScraperLocation() => Scraper.CurrentLocation;

    /// <summary><c>bruinell/scraper/elementMatch</c>: how the config names a node, for Never click and Always click.</summary>
    [JsonRpcMethod("bruinell/scraper/elementMatch")]
    public ScraperResult<string> ScraperElementMatch(ScraperPageNodeParams request)
        => Answer(() => Scraper.ElementMatchFor(request.Project, request.Page, request.Node ?? 0));

    /// <summary><c>bruinell/scraper/draft</c>: drafts page objects for every recorded page, or one, and builds them.</summary>
    [JsonRpcMethod("bruinell/scraper/draft")]
    public Task<ScraperResult<DraftResult>> ScraperDraft(ScraperPageNodeParams request)
        => Answer(() => Scraper.DraftAsync(request.Project, string.IsNullOrEmpty(request.Page) ? null : request.Page));

    /// <summary><c>bruinell/scraper/verify</c>: looks up every page object member in the running app.</summary>
    [JsonRpcMethod("bruinell/scraper/verify")]
    public Task<ScraperResult<VerifyResult>> ScraperVerify(ScraperProjectParams request)
        => Answer(() => Scraper.VerifyAsync(request.Project));

    // Its end is the service's Explored change; a failure to run is a Failed one.
    private async Task ExploreInBackgroundAsync(ScraperExploreParams request)
    {
        try
        {
            await Scraper.ExploreAsync(request.Project, request.Options()).ConfigureAwait(false);
        }
        catch (Exception e) when (e is ScraperProblem or FormatException or FileNotFoundException or InvalidOperationException or Microsoft.Playwright.PlaywrightException)
        {
            _ = _rpc?.NotifyWithParameterObjectAsync("bruinell/scraper/changed", new ScraperChange(ScraperChangeKind.Failed, null, null, e.Message));
        }
    }
}

/// <summary><c>bruinell/scraper/explore</c>'s parameters: the project, and the run's overrides.</summary>
public sealed record ScraperExploreParams(string? Project, string? Url, bool? DryRun, int? MaxPages, int? MaxDepth, int? MaxMinutes)
{
    /// <summary>The run's options: the browser shows, and a sign-in waits for Continue.</summary>
    public ExploreOptions Options() => new()
    {
        Url = string.IsNullOrEmpty(Url) ? null : Url,
        DryRun = DryRun ?? false,
        MaxPages = MaxPages,
        MaxDepth = MaxDepth,
        MaxMinutes = MaxMinutes,
    };
}

/// <summary><c>bruinell/scraper/configEdit</c>'s parameters: set, add or remove, in a section, with its values.</summary>
public sealed record ScraperConfigEditParams(string? Project, string Action, string Section, IReadOnlyList<string> Values);

/// <summary>What the new project form proposes; <c>references</c> is <c>projects</c> or <c>packages</c>.</summary>
public sealed record ScraperNewProjectDefaults(string Folder, string References, string Version);

/// <summary>The new project form's fields; <c>references</c> is <c>projects</c> or <c>packages</c>.</summary>
public sealed record ScraperNewProjectParams(string Name, string? Folder, string? Platform, string? StartUrl, IReadOnlyList<string>? TestIds, string? References)
{
    /// <summary>As the engine takes it.</summary>
    public NewProject ToRequest() => new(
        Name,
        Folder ?? "",
        Platform ?? "html",
        StartUrl ?? "",
        TestIds ?? [],
        string.Equals(References, "packages", StringComparison.OrdinalIgnoreCase) ? BruinellReferences.Packages : BruinellReferences.Projects);
}

/// <summary><c>bruinell/scraper/createConfig</c>'s parameters.</summary>
public sealed record ScraperCreateConfigParams(string Folder, string StartUrl);

/// <summary>A project's config as the Explore settings show it; each list as the config writes its rows.</summary>
public sealed record ScraperConfigInfo(
    string Path,
    string StartUrl,
    IReadOnlyList<string> Aliases,
    int MaxPages,
    int MaxDepth,
    int MaxMinutes,
    int DelayMs,
    int AgentMinutes,
    string LoginUrl,
    string Within,
    string ClickPolicy,
    IReadOnlyList<string> BlockMethods,
    string WebsocketApps,
    IReadOnlyDictionary<string, bool> DangerWords,
    IReadOnlyList<string> Hosts,
    IReadOnlyList<string> NeverClick,
    IReadOnlyList<string> AlwaysClick,
    IReadOnlyList<string> NeverVisit,
    IReadOnlyList<string> AllowRequests);

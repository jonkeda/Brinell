using Brinell.Scraper.Code;
using Brinell.Scraper.Explore;
using Brinell.Scraper.Html;
using Brinell.Scraper.Projects;
using Brinell.Scraper.Service;
using StreamJsonRpc;

namespace Brinell.Bdd.LanguageServer;

// The Scraper view's Explore, its settings and the config, Draft and Verify. Explore runs in the
// background: its steps and its end come as brinell/scraper/changed (Exploring, SignIn, Explored).
public sealed partial class BddLanguageServer
{
    /// <summary><c>brinell/scraper/exploreSummary</c>: what Explore would run with, to show before it starts.</summary>
    [JsonRpcMethod("brinell/scraper/exploreSummary")]
    public ScraperResult<string> ScraperExploreSummary(ScraperExploreParams request)
        => Answer(() => Scraper.ExploreSummary(request.Project, request.Options()));

    /// <summary><c>brinell/scraper/explore</c>: starts Explore; true once it runs. Its end comes as an Explored change.</summary>
    [JsonRpcMethod("brinell/scraper/explore")]
    public ScraperResult<bool> ScraperExplore(ScraperExploreParams request)
        => Answer(() =>
        {
            if (Scraper.IsExploring) throw new ScraperProblem("Explore is running already.");
            _ = ExploreInBackgroundAsync(request);
            return true;
        });

    /// <summary><c>brinell/scraper/exploreStop</c>: stops Explore; what it recorded stays.</summary>
    [JsonRpcMethod("brinell/scraper/exploreStop")]
    public bool ScraperExploreStop()
    {
        Scraper.StopExploring();
        return true;
    }

    /// <summary><c>brinell/scraper/exploreContinue</c>: goes on after the person signed in.</summary>
    [JsonRpcMethod("brinell/scraper/exploreContinue")]
    public bool ScraperExploreContinue()
    {
        Scraper.ContinueExploring();
        return true;
    }

    /// <summary><c>brinell/scraper/config</c>: a project's config as data, for the Explore settings.</summary>
    [JsonRpcMethod("brinell/scraper/config")]
    public ScraperResult<ScraperConfigInfo> ScraperConfig(ScraperProjectParams request)
        => Answer(() =>
        {
            var project = Scraper.Project(request.Project);
            var c = project.Config;
            var e = c.Explore;
            return new ScraperConfigInfo(
                Path.Combine(project.Folder, Brinell.Scraper.Projects.ScraperConfig.FileName),
                c.StartUrl,
                c.Aliases,
                e.MaxPages, e.MaxDepth, e.MaxMinutes, e.DelayMs, e.AgentMinutes, e.LoginUrl, e.Within,
                Brinell.Scraper.Explore.Explorer.Policy(e.ClickPolicy),
                e.BlockMethods,
                e.WebsocketApps.ToString().ToLowerInvariant(),
                ExploreSettings.Categories.ToDictionary(category => category, e.IsOn),
                e.Hosts,
                e.NeverClick.Select(m => m.ToString()).ToList(),
                e.AlwaysClick.Select(m => m.ToString()).ToList(),
                e.NeverVisit,
                e.AllowRequests.Select(a => $"{a.Method} {a.Url}".Trim()).ToList());
        });

    /// <summary><c>brinell/scraper/configEdit</c>: one change to a project's config; the sentence says what was written.</summary>
    [JsonRpcMethod("brinell/scraper/configEdit")]
    public ScraperResult<string> ScraperConfigEdit(ScraperConfigEditParams request)
        => Answer(() => Scraper.EditConfig(request.Project, new ConfigChange(request.Action, request.Section, request.Values)).Description);

    /// <summary><c>brinell/scraper/configCandidates</c>: the UI test projects with no config yet.</summary>
    [JsonRpcMethod("brinell/scraper/configCandidates")]
    public IReadOnlyList<string> ScraperConfigCandidates() => Scraper.ConfigCandidates();

    /// <summary><c>brinell/scraper/createConfig</c>: writes a config into a UI test project; the project's name.</summary>
    [JsonRpcMethod("brinell/scraper/createConfig")]
    public ScraperResult<string> ScraperCreateConfig(ScraperCreateConfigParams request)
        => Answer(() => Scraper.CreateConfig(request.Folder, request.StartUrl).Name);

    /// <summary><c>brinell/scraper/location</c>: where the recording browser is, or null.</summary>
    [JsonRpcMethod("brinell/scraper/location")]
    public string? ScraperLocation() => Scraper.CurrentLocation;

    /// <summary><c>brinell/scraper/elementMatch</c>: how the config names a node, for Never click and Always click.</summary>
    [JsonRpcMethod("brinell/scraper/elementMatch")]
    public ScraperResult<string> ScraperElementMatch(ScraperPageNodeParams request)
        => Answer(() => Scraper.ElementMatchFor(request.Project, request.Page, request.Node ?? 0));

    /// <summary><c>brinell/scraper/draft</c>: drafts page objects for every recorded page, or one, and builds them.</summary>
    [JsonRpcMethod("brinell/scraper/draft")]
    public Task<ScraperResult<DraftResult>> ScraperDraft(ScraperPageNodeParams request)
        => Answer(() => Scraper.DraftAsync(request.Project, string.IsNullOrEmpty(request.Page) ? null : request.Page));

    /// <summary><c>brinell/scraper/verify</c>: looks up every page object member in the running app.</summary>
    [JsonRpcMethod("brinell/scraper/verify")]
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
            _ = _rpc?.NotifyWithParameterObjectAsync("brinell/scraper/changed", new ScraperChange(ScraperChangeKind.Failed, null, null, e.Message));
        }
    }
}

/// <summary><c>brinell/scraper/explore</c>'s parameters: the project, and the run's overrides.</summary>
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

/// <summary><c>brinell/scraper/configEdit</c>'s parameters: set, add or remove, in a section, with its values.</summary>
public sealed record ScraperConfigEditParams(string? Project, string Action, string Section, IReadOnlyList<string> Values);

/// <summary><c>brinell/scraper/createConfig</c>'s parameters.</summary>
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

using Bruinell.Scraper.Code;
using Bruinell.Scraper.Projects;
using Bruinell.Scraper.Service;
using StreamJsonRpc;

namespace Bruinell.Bdd.LanguageServer;

// The Scraper view's workflow: the steps' state, the app check, made controls, and page objects by choice.
public sealed partial class BddLanguageServer
{
    /// <summary><c>bruinell/scraper/progress</c>: the workflow's steps for a project and what to do next.</summary>
    [JsonRpcMethod("bruinell/scraper/progress")]
    public ScraperResult<WorkflowProgress> ScraperProgress(ScraperProjectParams request)
        => Answer(() => Scraper.Progress(request.Project));

    /// <summary><c>bruinell/scraper/check</c>: opens the start URL: answered, signed in, on a host Explore may go to.</summary>
    [JsonRpcMethod("bruinell/scraper/check")]
    public Task<ScraperResult<AppCheck>> ScraperCheck(ScraperCheckParams request)
        => Answer(() => Scraper.CheckAppAsync(request.Project, request.Visible ?? false));

    /// <summary><c>bruinell/scraper/configEdits</c>: several changes to the config, written once (the settings form's Save).</summary>
    [JsonRpcMethod("bruinell/scraper/configEdits")]
    public ScraperResult<IReadOnlyList<string>> ScraperConfigEdits(ScraperConfigEditsParams request)
        => Answer(() => (IReadOnlyList<string>)Scraper.EditConfig(request.Project, request.Changes.Select(c => new ConfigChange(c.Action, c.Section, c.Values)).ToArray())
            .Where(e => e.Changed).Select(e => e.Description).ToList());

    /// <summary><c>bruinell/scraper/controlChecks</c>: the made controls, each checked against the recordings.</summary>
    [JsonRpcMethod("bruinell/scraper/controlChecks")]
    public ScraperResult<IReadOnlyList<ControlCheck>> ScraperControlChecks(ScraperProjectParams request)
        => Answer(() => Scraper.ControlChecks(request.Project));

    /// <summary><c>bruinell/scraper/code</c>: a C# file of the project, by its path relative to the project.</summary>
    [JsonRpcMethod("bruinell/scraper/code")]
    public ScraperResult<string> ScraperCode(ScraperCodeParams request)
        => Answer(() => Scraper.ReadCode(request.Project, request.Path));

    /// <summary><c>bruinell/scraper/chatChoice</c>: the agent and model the prompts run with.</summary>
    [JsonRpcMethod("bruinell/scraper/chatChoice")]
    public ScraperResult<Bruinell.Scraper.Projects.ChatChoice> ScraperChatChoice(ScraperProjectParams request)
        => Answer(() => Scraper.ChatChoice(request.Project));

    /// <summary><c>bruinell/scraper/setChatChoice</c>: remembers the person's agent and model.</summary>
    [JsonRpcMethod("bruinell/scraper/setChatChoice")]
    public ScraperResult<bool> ScraperSetChatChoice(ScraperChatChoiceParams request)
        => Answer(() =>
        {
            Scraper.SetChatChoice(request.Project, request.Agent, request.Model);
            return true;
        });

    /// <summary><c>bruinell/scraper/acceptControl</c>: accepts a made control as it is (or takes that back).</summary>
    [JsonRpcMethod("bruinell/scraper/acceptControl")]
    public ScraperResult<bool> ScraperAcceptControl(ScraperAcceptParams request)
        => Answer(() =>
        {
            Scraper.AcceptControl(request.Project, request.Control, request.Accepted ?? true);
            return true;
        });

    /// <summary><c>bruinell/scraper/skipControls</c>: page objects without controls; the workflow stops asking (or asks again).</summary>
    [JsonRpcMethod("bruinell/scraper/skipControls")]
    public ScraperResult<bool> ScraperSkipControls(ScraperSkipParams request)
        => Answer(() =>
        {
            Scraper.SkipControls(request.Project, request.Skip ?? true);
            return true;
        });

    /// <summary><c>bruinell/scraper/draftPages</c>: drafts the page objects of the chosen pages only.</summary>
    [JsonRpcMethod("bruinell/scraper/draftPages")]
    public Task<ScraperResult<DraftResult>> ScraperDraftPages(ScraperDraftPagesParams request)
        => Answer(() => Scraper.DraftAsync(request.Project, request.Pages));

    /// <summary><c>bruinell/scraper/renamePageObject</c>: renames a page's page object (its class, draft and the person's part).</summary>
    [JsonRpcMethod("bruinell/scraper/renamePageObject")]
    public ScraperResult<RenameResult> ScraperRenamePageObject(ScraperRenameParams request)
        => Answer(() => Scraper.RenamePageObject(request.Project, request.Page, request.Name));
}

/// <summary><c>bruinell/scraper/check</c>'s parameters: the project, and whether the browser shows.</summary>
public sealed record ScraperCheckParams(string? Project, bool? Visible);

/// <summary>One change of <c>bruinell/scraper/configEdits</c>.</summary>
public sealed record ScraperConfigChangeParams(string Action, string Section, IReadOnlyList<string> Values);

/// <summary><c>bruinell/scraper/configEdits</c>'s parameters.</summary>
public sealed record ScraperConfigEditsParams(string? Project, IReadOnlyList<ScraperConfigChangeParams> Changes);

/// <summary><c>bruinell/scraper/code</c>'s parameters.</summary>
public sealed record ScraperCodeParams(string? Project, string Path);

/// <summary><c>bruinell/scraper/acceptControl</c>'s parameters.</summary>
public sealed record ScraperAcceptParams(string? Project, string Control, bool? Accepted);

/// <summary><c>bruinell/scraper/setChatChoice</c>'s parameters.</summary>
public sealed record ScraperChatChoiceParams(string? Project, string? Agent, string? Model);

/// <summary><c>bruinell/scraper/skipControls</c>'s parameters.</summary>
public sealed record ScraperSkipParams(string? Project, bool? Skip);

/// <summary><c>bruinell/scraper/draftPages</c>'s parameters: the page keys to draft.</summary>
public sealed record ScraperDraftPagesParams(string? Project, IReadOnlyList<string> Pages);

/// <summary><c>bruinell/scraper/renamePageObject</c>'s parameters: the page, and the page object's new name.</summary>
public sealed record ScraperRenameParams(string? Project, string Page, string Name);

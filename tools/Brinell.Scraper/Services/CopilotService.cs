using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using GitHub.Copilot;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class CopilotService : ICopilotService, IAsyncDisposable
{
    private readonly ILogger<CopilotService> _logger;
    private readonly CorpusTools _corpusTools;
    private readonly ISessionContext _sessionContext;
    private readonly AppSettings _settings;

    private CopilotClient? _client;
    private CopilotSession? _analyzerSession;
    private CopilotSession? _generatorSession;
    private bool _stubMode;

    public CopilotService(
        ILogger<CopilotService> logger,
        CorpusTools corpusTools,
        ISessionContext sessionContext,
        AppSettings settings)
    {
        _logger = logger;
        _corpusTools = corpusTools;
        _sessionContext = sessionContext;
        _settings = settings;
    }

    public bool IsAuthenticated =>
        !_stubMode; // && _client?.State == ConnectionState.Connected;

    public string? LastInitError { get; private set; }

    public async Task InitializeAsync(long siteId, string siteSlug, CancellationToken ct = default)
    {
        // Tear down any existing session before reinitializing.
        await DisposeSessionAsync();
        LastInitError = null;

        _sessionContext.CurrentSiteId = siteId;
        _sessionContext.CurrentSiteSlug = siteSlug;

        try
        {
            _client = new CopilotClient(new CopilotClientOptions
            {
                UseLoggedInUser = true,
                Logger = _logger,
            });

            await _client.StartAsync();
        }
        catch (Exception ex)
        {
            _stubMode = true;
            LastInitError = "Copilot CLI not authenticated";
            _logger.LogWarning(ex,
                "Copilot SDK connection failed — running in stub mode. Site: {Site} ({Slug})",
                siteId, siteSlug);
            return;
        }

        // Try configured models first, fall back to defaults if they fail.
        const string defaultAnalyzer = "claude-haiku-4.5";
        const string defaultGenerator = "claude-haiku-4.5";

        var analyzerModel = _settings.AnalyzerModel;
        var generatorModel = _settings.GeneratorModel;

        if (!await TryCreateSessionsAsync(siteId, siteSlug, analyzerModel, generatorModel))
        {
            if (analyzerModel != defaultAnalyzer || generatorModel != defaultGenerator)
            {
                _logger.LogInformation(
                    "Retrying session creation with default models ({Analyzer}, {Generator})",
                    defaultAnalyzer, defaultGenerator);

                if (await TryCreateSessionsAsync(siteId, siteSlug, defaultAnalyzer, defaultGenerator))
                {
                    // Persist the working defaults so the stale values don't recur.
                    _settings.AnalyzerModel = defaultAnalyzer;
                    _settings.GeneratorModel = defaultGenerator;
                    _settings.Save();
                }
            }
        }
    }

    private async Task<bool> TryCreateSessionsAsync(
        long siteId, string siteSlug, string analyzerModel, string generatorModel)
    {
        try
        {
            var tools = BuildTools();
            var generatorSkillName = $"{siteSlug}-controls";

            _analyzerSession = await _client!.CreateSessionAsync(new SessionConfig
            {
                Model = analyzerModel,
                Tools = tools,
                OnPermissionRequest = PermissionHandler.ApproveAll,
                SystemMessage = new SystemMessageConfig
                {
                    Mode = SystemMessageMode.Append,
                    Content = """
                        You analyze DOM snapshots to identify reusable UI patterns for the Brinell test automation framework.
                        When analyzing, output a JSON object with `proposedControls` and optional `locatorReport`.
                        Each proposed control should have: name, domSignature, frequency, confidence, exampleSnippet, suggestedProperties.
                        The locatorReport should have: stableAttributes, unstableAttributes, recommendations.
                        Use the available corpus tools to explore recorded pages when needed.
                        """
                },
            });

            _generatorSession = await _client.CreateSessionAsync(new SessionConfig
            {
                Model = generatorModel,
                Tools = tools,
                OnPermissionRequest = PermissionHandler.ApproveAll,
                SystemMessage = new SystemMessageConfig
                {
                    Mode = SystemMessageMode.Append,
                    Content = $$"""
                        You generate C# PageObject and ContainerBase classes for the Brinell test automation framework.
                        Always output complete, compilable C# classes in ```csharp fenced code blocks.
                        Follow Brinell conventions: sealed classes, expression-bodied properties, file-scoped namespaces.
                        Locator preference order: ByText > ByDataTestId > ByAriaLabel > ById > ByCss.
                        Use the per-site control library skill ({{generatorSkillName}}) and the available corpus tools to inspect page structures when needed.
                        """
                },
            });

            _stubMode = false;
            LastInitError = null;
            _logger.LogInformation(
                "Copilot SDK initialized — Site: {Site} ({Slug}), AnalyzerModel: {Analyzer}, " +
                "GeneratorModel: {Generator}, GeneratorSkill: {Skill}, " +
                "AnalyzerSession: {AnalyzerId}, GeneratorSession: {GeneratorId}",
                siteId, siteSlug, analyzerModel, generatorModel,
                generatorSkillName, _analyzerSession.SessionId, _generatorSession.SessionId);
            return true;
        }
        catch (Exception ex)
        {
            _stubMode = true;
            LastInitError = $"Model '{analyzerModel}' / '{generatorModel}': {ex.Message}";
            _logger.LogWarning(ex,
                "Copilot session creation failed — " +
                "Site: {Site} ({Slug}), AnalyzerModel: {Analyzer}, GeneratorModel: {Generator}",
                siteId, siteSlug, analyzerModel, generatorModel);
            return false;
        }
    }

    public async Task DisposeSessionAsync()
    {
        if (_analyzerSession is not null)
        {
            await _analyzerSession.DisposeAsync();
            _analyzerSession = null;
        }
        if (_generatorSession is not null)
        {
            await _generatorSession.DisposeAsync();
            _generatorSession = null;
        }
        if (_client is not null)
        {
            await _client.DisposeAsync();
            _client = null;
        }
        _stubMode = false;
    }

    public async Task<string> AnalyzeAsync(string prompt, CancellationToken ct = default)
    {
        if (_stubMode || _analyzerSession is null)
        {
            _logger.LogWarning("AnalyzeAsync called while Copilot is not configured — returning empty response.");
            return string.Empty;
        }
        return await SendAndWaitAsync(_analyzerSession, "analyzer", prompt, ct);
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        if (_stubMode || _generatorSession is null)
        {
            _logger.LogWarning("GenerateAsync called while Copilot is not configured — returning empty response.");
            return string.Empty;
        }
        return await SendAndWaitAsync(_generatorSession, "generator", prompt, ct);
    }

    private async Task<string> SendAndWaitAsync(
        CopilotSession session, string agentName, string prompt, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var content = "";
        var done = new TaskCompletionSource();

        using var ctReg = ct.Register(() => done.TrySetCanceled(ct));

        using var subscription = session.On<SessionEvent>(evt =>
        {
            switch (evt)
            {
                case AssistantMessageEvent msg:
                    content = msg.Data.Content;
                    break;
                case SessionIdleEvent:
                    done.TrySetResult();
                    break;
                case SessionErrorEvent err:
                    done.TrySetException(new InvalidOperationException(
                        $"Copilot error ({agentName}): {err.Data.Message}"));
                    break;
            }
        });

        _logger.LogDebug(
            "LLM request — Agent: {Agent}, Prompt length: {PromptLength} chars",
            agentName, prompt.Length);

        await session.SendAsync(new MessageOptions { Prompt = prompt });
        await done.Task;

        sw.Stop();
        _logger.LogInformation(
            "LLM response — Agent: {Agent}, Prompt: {PromptLength} chars, " +
            "Response: {ResponseLength} chars, Elapsed: {ElapsedMs} ms",
            agentName, prompt.Length, content.Length, sw.ElapsedMilliseconds);

        return content;
    }

    private AIFunction[] BuildTools()
    {
        return
        [
            AIFunctionFactory.Create(
                () => _corpusTools.ListRecordedPages(),
                "list_recorded_pages",
                "List all pages recorded for the current site, with element counts."),

            AIFunctionFactory.Create(
                ([Description("Page id (numeric) or page name to retrieve")] string pageIdOrName) =>
                    _corpusTools.GetPageSnapshot(pageIdOrName),
                "get_page_snapshot",
                "Get the full DOM snapshot for a recorded page (by id or name)."),

            AIFunctionFactory.Create(
                ([Description("CSS-like selector (tag, .class, #id, or substring)")] string selector) =>
                    _corpusTools.FindSimilarElements(selector),
                "find_similar_elements",
                "Find elements matching a selector across all recorded pages of the current site."),

            AIFunctionFactory.Create(
                () => _corpusTools.GetGeneratedControls(),
                "get_generated_controls",
                "List all custom controls that have been generated, with their DOM signatures and properties."),

            AIFunctionFactory.Create(
                ([Description("Free-text query against element tags, ids, attributes, and text")] string query) =>
                    _corpusTools.SearchCorpus(query),
                "search_corpus",
                "Full-text search across DOM elements of all recorded pages of the current site."),
        ];
    }

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default)
    {
        if (_stubMode || _client is null)
            return [];

        try
        {
            var models = await _client.ListModelsAsync(ct);
            return models.Select(m => m.Id).Order().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ListModelsAsync failed — returning empty list");
            return [];
        }
    }

    public string? GetCliPath()
    {
        var envPath = Environment.GetEnvironmentVariable("COPILOT_CLI_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
            return envPath;

        // The SDK's MSBuild targets place the CLI at runtimes/{rid}/native/copilot.exe.
        var appDir = AppContext.BaseDirectory;
        var rid = RuntimeInformation.RuntimeIdentifier;

        var runtimePath = Path.Combine(appDir, "runtimes", rid, "native", "copilot.exe");
        if (File.Exists(runtimePath))
            return runtimePath;

        // Fallback: portable RID (e.g. win-x64 when full RID is win10-x64).
        var parts = rid.Split('-');
        if (parts.Length >= 2)
        {
            var portableRid = $"{parts[0]}-{parts[^1]}";
            var portablePath = Path.Combine(appDir, "runtimes", portableRid, "native", "copilot.exe");
            if (File.Exists(portablePath))
                return portablePath;
        }

        return null;
    }

    public async ValueTask DisposeAsync() => await DisposeSessionAsync();
}

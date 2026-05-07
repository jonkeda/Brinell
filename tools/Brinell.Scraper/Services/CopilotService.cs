using System.ComponentModel;
using System.Diagnostics;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class CopilotService : ICopilotService, IAsyncDisposable
{
    private readonly ILogger<CopilotService> _logger;
    private readonly CorpusTools _corpusTools;
    private CopilotClient? _client;
    private CopilotSession? _analyzerSession;
    private CopilotSession? _generatorSession;

    public CopilotService(
        ILogger<CopilotService> logger,
        CorpusTools corpusTools)
    {
        _logger = logger;
        _corpusTools = corpusTools;
    }

    public bool IsAuthenticated => _client?.State == ConnectionState.Connected;

    public long CurrentSiteId { get; set; }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _client = new CopilotClient(new CopilotClientOptions
        {
            UseLoggedInUser = true,
            Logger = _logger,
        });

        await _client.StartAsync();

        var tools = BuildTools();

        _analyzerSession = await _client.CreateSessionAsync(new SessionConfig
        {
            Model = "gpt-4o-mini",
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
            Model = "gpt-4o",
            Tools = tools,
            OnPermissionRequest = PermissionHandler.ApproveAll,
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = """
                    You generate C# PageObject and ContainerBase classes for the Brinell test automation framework.
                    Always output complete, compilable C# classes in ```csharp fenced code blocks.
                    Follow Brinell conventions: sealed classes, expression-bodied properties, file-scoped namespaces.
                    Locator preference order: ByText > ByDataTestId > ByAriaLabel > ById > ByCss.
                    Use the available corpus tools to inspect page structures when needed.
                    """
            },
        });

        _logger.LogInformation(
            "Copilot SDK initialized — analyzer: {AnalyzerId}, generator: {GeneratorId}",
            _analyzerSession.SessionId, _generatorSession.SessionId);
    }

    public async Task<string> AnalyzeAsync(string prompt, CancellationToken ct = default)
    {
        EnsureInitialized();
        return await SendAndWaitAsync(_analyzerSession!, "analyzer", prompt, ct);
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        EnsureInitialized();
        return await SendAndWaitAsync(_generatorSession!, "generator", prompt, ct);
    }

    private async Task<string> SendAndWaitAsync(
        CopilotSession session, string agentName, string prompt, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var content = "";
        var done = new TaskCompletionSource();

        using var ctReg = ct.Register(() => done.TrySetCanceled(ct));

        using var subscription = session.On(evt =>
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
                ([Description("Search query for element text, tag, or attribute")] string query,
                 [Description("Optional tag filter (e.g. 'input', 'button')")] string? tag) =>
                    _corpusTools.SearchCorpus(CurrentSiteId, query, tag),
                "search_corpus",
                "Search DOM elements across all recorded pages in the corpus"),

            AIFunctionFactory.Create(
                ([Description("Name of the page to retrieve")] string pageName) =>
                    _corpusTools.GetPageSnapshot(CurrentSiteId, pageName),
                "get_page_snapshot",
                "Get the full DOM snapshot for a recorded page"),

            AIFunctionFactory.Create(
                () => _corpusTools.GetGeneratedControls(),
                "get_generated_controls",
                "List all custom controls that have been generated"),

            AIFunctionFactory.Create(
                () => _corpusTools.ListRecordedPages(CurrentSiteId),
                "list_recorded_pages",
                "List all pages recorded in the corpus with element counts"),
        ];
    }

    private void EnsureInitialized()
    {
        if (_client is null || _analyzerSession is null || _generatorSession is null)
            throw new InvalidOperationException(
                "CopilotService not initialized. Call InitializeAsync first.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_analyzerSession is not null)
            await _analyzerSession.DisposeAsync();
        if (_generatorSession is not null)
            await _generatorSession.DisposeAsync();
        if (_client is not null)
            await _client.DisposeAsync();
    }
}

# Step 5.1 — Copilot SDK Integration (Agentic Runtime)

## Objective

Integrate the GitHub Copilot SDK as an agentic runtime — managing agents, tools, skills, hooks, and session persistence. Two agents are defined: an **analyzer** (cheaper model) for pattern detection and a **generator** (smarter model) for C# code generation.

## Dependencies

- Phase 1 (DI container in `App.xaml.cs`)
- Phase 4 (corpus with stored snapshots)
- NuGet: `GitHub.Copilot.SDK`

## Implementation

### NuGet package

```xml
<PackageReference Include="GitHub.Copilot.SDK" Version="*" />
```

> Package source: https://github.com/github/copilot-sdk — follow latest install instructions for the .NET variant.

### Authentication

- Use GitHub token-based auth (PAT or GitHub App token)
- Store token in Windows Credential Manager — never in source
- Handle auth failures gracefully with "Sign in to GitHub" prompt

### ICopilotService abstraction

```csharp
// Services/ICopilotService.cs
public interface ICopilotService
{
    bool IsAuthenticated { get; }
    Task InitializeAsync(CancellationToken ct = default);
    Task<string> AnalyzeAsync(string prompt, CancellationToken ct = default);
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
}
```

### CopilotService implementation

```csharp
// Services/CopilotService.cs
public sealed class CopilotService : ICopilotService
{
    private readonly ILogger<CopilotService> _logger;
    private readonly CorpusService _corpusService;
    private CopilotClient? _client;
    private Session? _session;

    public CopilotService(
        ILogger<CopilotService> logger,
        CorpusService corpusService)
    {
        _logger = logger;
        _corpusService = corpusService;
    }

    public bool IsAuthenticated => _client is not null;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var token = GetAuthToken(); // from Credential Manager
        _client = new CopilotClient(new CopilotClientOptions
        {
            AuthToken = token,
        });

        _session = await _client.CreateSessionAsync(new SessionConfig
        {
            SkillDirectories = new[] { "./corpus/skills" },
            CustomAgents = new[]
            {
                new AgentConfig
                {
                    Name = "analyzer",
                    Description = "Analyzes DOM patterns across pages",
                    Prompt = "You analyze DOM snapshots to identify reusable UI patterns.",
                    Skills = new[] { "brinell-conventions" }
                },
                new AgentConfig
                {
                    Name = "generator",
                    Description = "Generates Brinell PageObject and ControlObject code",
                    Prompt = "You generate C# PageObject and ControlObject classes for the Brinell framework.",
                    Skills = new[] { "brinell-conventions", "{site}-controls" }
                }
            },
            CustomTools = new[]
            {
                CorpusTools.SearchCorpus,
                CorpusTools.GetPageSnapshot,
                CorpusTools.FindSimilarElements,
                CorpusTools.GetGeneratedControls,
                CorpusTools.ListRecordedPages,
            }
        }, ct);

        _logger.LogInformation("Copilot SDK initialized — session created with 2 agents, 5 tools");
    }

    public async Task<string> AnalyzeAsync(string prompt, CancellationToken ct = default)
    {
        EnsureInitialized();
        var sw = Stopwatch.StartNew();
        var response = await _session!.SendAsync("analyzer", prompt, ct);
        sw.Stop();

        _logger.LogInformation(
            "LLM request — Agent: {Agent}, Prompt length: {PromptLength} chars, Elapsed: {ElapsedMs} ms",
            "analyzer", prompt.Length, sw.ElapsedMilliseconds);

        return response;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        EnsureInitialized();
        var sw = Stopwatch.StartNew();
        var response = await _session!.SendAsync("generator", prompt, ct);
        sw.Stop();

        _logger.LogInformation(
            "LLM request — Agent: {Agent}, Prompt length: {PromptLength} chars, Elapsed: {ElapsedMs} ms",
            "generator", prompt.Length, sw.ElapsedMilliseconds);

        return response;
    }

    private void EnsureInitialized()
    {
        if (_session is null)
            throw new InvalidOperationException("CopilotService not initialized. Call InitializeAsync first.");
    }

    private static string GetAuthToken()
    {
        // Read from Windows Credential Manager
        // Target: "Brinell.Scraper:GitHub"
        throw new NotImplementedException("Implement credential retrieval");
    }
}
```

### DI registration

```csharp
// In App.xaml.cs ConfigureServices
services.AddSingleton<ICopilotService, CopilotService>();
```

### Token refresh

- Support token refresh if using OAuth flow
- On 401/403 response, prompt user to re-authenticate
- Log auth failures at `Warning` level

## Checklist

- [ ] `GitHub.Copilot.SDK` NuGet package referenced
- [ ] `ICopilotService` abstraction with `AnalyzeAsync` and `GenerateAsync`
- [ ] `CopilotService` creates session with 2 agents (analyzer, generator)
- [ ] 5 custom tools registered (`search_corpus`, `get_page_snapshot`, `find_similar_elements`, `get_generated_controls`, `list_recorded_pages`)
- [ ] Skills loaded from `./corpus/skills/`
- [ ] Auth token read from Windows Credential Manager (never from source)
- [ ] Auth failure shows "Sign in to GitHub" prompt
- [ ] Registered as singleton in DI container
- [ ] All LLM calls logged with agent, prompt length, elapsed time

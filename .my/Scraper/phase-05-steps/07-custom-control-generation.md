# Step 5.7 — Custom Control Generation (Phase 5B — Controls)

## Objective

After the user approves proposed controls from the analysis pass, the generator agent produces `ContainerBase<TParent, TScope>` classes for each custom control. Generated controls are stored in a SQLite registry and a site-specific skill is auto-generated.

## Dependencies

- Step 5.1 (Copilot SDK with generator agent)
- Step 5.2 (`SkillService` for auto-generating `{site}-controls` skill)
- Step 5.6 (analysis pass with approved `ControlProposal` items)

## Implementation

### IControlRegistry interface

```csharp
// Services/IControlRegistry.cs
public interface IControlRegistry
{
    Task<IReadOnlyList<GeneratedControl>> GetAllControlsAsync();
    Task<GeneratedControl?> GetControlAsync(string name);
    Task StoreControlAsync(GeneratedControl control);
    Task DeleteControlAsync(string name);
}
```

### GeneratedControl model

```csharp
// Models/GeneratedControl.cs
public sealed class GeneratedControl
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Namespace { get; init; } = "";
    public string Code { get; init; } = "";
    public string DomSignature { get; init; } = "";
    public double Confidence { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

### ControlRegistry (SQLite)

```csharp
// Data/ControlRegistry.cs
public sealed class ControlRegistry : IControlRegistry
{
    private readonly string _connectionString;
    private readonly ILogger<ControlRegistry> _logger;

    public ControlRegistry(string connectionString, ILogger<ControlRegistry> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
        EnsureCreated();
    }

    private void EnsureCreated()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS GeneratedControls (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Namespace TEXT NOT NULL,
                Code TEXT NOT NULL,
                DomSignature TEXT NOT NULL,
                Confidence REAL NOT NULL,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task StoreControlAsync(GeneratedControl control)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO GeneratedControls (Name, Namespace, Code, DomSignature, Confidence, CreatedAt)
            VALUES (@name, @namespace, @code, @domSignature, @confidence, @createdAt);
            """;
        cmd.Parameters.AddWithValue("@name", control.Name);
        cmd.Parameters.AddWithValue("@namespace", control.Namespace);
        cmd.Parameters.AddWithValue("@code", control.Code);
        cmd.Parameters.AddWithValue("@domSignature", control.DomSignature);
        cmd.Parameters.AddWithValue("@confidence", control.Confidence);
        cmd.Parameters.AddWithValue("@createdAt", control.CreatedAt.ToString("o"));
        await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("Control stored — Name: {ControlName}", control.Name);
    }

    // ... GetAllControlsAsync, GetControlAsync, DeleteControlAsync
}
```

### ControlGenerationService

```csharp
// Services/ControlGenerationService.cs
public sealed class ControlGenerationService
{
    private readonly ICopilotService _copilotService;
    private readonly IControlRegistry _controlRegistry;
    private readonly SkillService _skillService;
    private readonly ILogger<ControlGenerationService> _logger;

    public async Task<GeneratedControl> GenerateControlAsync(
        ControlProposal proposal,
        string siteNamespace,
        CancellationToken ct = default)
    {
        var prompt = BuildControlPrompt(proposal, siteNamespace);
        var response = await _copilotService.GenerateAsync(prompt, ct);
        var codeBlocks = CodeBlockParser.ExtractCSharpBlocks(response);

        if (codeBlocks.Count == 0)
            throw new InvalidOperationException(
                $"No C# code blocks in LLM response for control '{proposal.Name}'");

        var code = codeBlocks[0];

        // Validate with Roslyn
        var validation = CodeValidator.Validate(code);
        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "Generated control has errors, retrying — Name: {ControlName}, Errors: {ErrorCount}",
                proposal.Name, validation.Errors.Count);

            // Auto-retry with error feedback
            code = await RetryWithFeedbackAsync(prompt, code, validation, ct);
        }

        var control = new GeneratedControl
        {
            Name = proposal.Name,
            Namespace = $"{siteNamespace}.Controls",
            Code = code,
            DomSignature = proposal.DomSignature,
            Confidence = proposal.Confidence,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _controlRegistry.StoreControlAsync(control);

        _logger.LogInformation(
            "Generation — Control: {ControlName}, Custom controls used: {ControlNames}",
            proposal.Name, proposal.Name);

        return control;
    }

    public async Task GenerateAllApprovedAsync(
        IReadOnlyList<ControlProposal> proposals,
        string siteNamespace,
        string siteName,
        CancellationToken ct = default)
    {
        var approved = proposals.Where(p => p.IsApproved).ToList();

        _logger.LogInformation(
            "Generating {Count} approved custom controls", approved.Count);

        var generated = new List<GeneratedControl>();
        foreach (var proposal in approved)
        {
            var control = await GenerateControlAsync(proposal, siteNamespace, ct);
            generated.Add(control);
        }

        // Auto-generate {site}-controls skill
        _skillService.GenerateSiteControlsSkill(siteName, generated);

        _logger.LogInformation(
            "All approved controls generated — Count: {Count}", generated.Count);
    }

    private static string BuildControlPrompt(
        ControlProposal proposal, string siteNamespace)
    {
        return $"""
            Generate a Brinell custom control class with the following details:

            Control Name: {proposal.Name}
            Namespace: {siteNamespace}.Controls
            DOM Signature: {proposal.DomSignature}

            ## Example DOM

            {proposal.ExampleSnippet}

            ## Suggested Properties

            {string.Join(", ", proposal.SuggestedProperties)}

            Generate a sealed class inheriting from ContainerBase<TParent, {proposal.Name}Container<TParent>>.
            Use expression-bodied properties for each child control.
            Choose the most appropriate control type and locator strategy for each property.
            Follow the locator preference order: ByText > ByDataTestId > ByAriaLabel > ById > ByCss.
            """;
    }

    private async Task<string> RetryWithFeedbackAsync(
        string originalPrompt, string failedCode,
        ValidationResult validation, CancellationToken ct)
    {
        var retryPrompt = $"""
            The generated code has these errors:

            {string.Join("\n", validation.Errors.Select(e => $"  Line {e.Line}: {e.Message}"))}

            Original code:
            ```csharp
            {failedCode}
            ```

            Please fix the errors and regenerate the complete class.
            """;

        var response = await _copilotService.GenerateAsync(retryPrompt, ct);
        var blocks = CodeBlockParser.ExtractCSharpBlocks(response);
        return blocks.Count > 0 ? blocks[0] : failedCode; // fallback to original on failure
    }
}
```

### ControlsManagerViewModel (wire up existing stub)

```csharp
// Key properties and commands:
public ObservableCollection<GeneratedControl> Controls { get; }
public GeneratedControl? SelectedControl { get; set; }
public string CodePreview { get; }  // code of selected control

public ICommand GeneratePendingCommand { get; }
public ICommand RegenerateCommand { get; }
public ICommand EditCommand { get; }
public ICommand SaveToProjectCommand { get; }
```

## Checklist

- [ ] `IControlRegistry` abstraction for CRUD on generated controls
- [ ] `ControlRegistry` stores controls in SQLite `GeneratedControls` table
- [ ] `GeneratedControl` model with Id, Name, Namespace, Code, DomSignature, Confidence, CreatedAt
- [ ] `ControlGenerationService.GenerateControlAsync()` sends proposal to generator agent
- [ ] Generated code validated with Roslyn — auto-retry on failure (max 2 retries)
- [ ] `GenerateAllApprovedAsync()` batch-generates all approved proposals
- [ ] Auto-generates `{site}-controls/SKILL.md` after control generation
- [ ] `ControlsManagerViewModel` wired up for the Custom Controls Manager UI
- [ ] Control generation logged with name, namespace, and any retry attempts
- [ ] Controls stored with `INSERT OR REPLACE` to support re-generation

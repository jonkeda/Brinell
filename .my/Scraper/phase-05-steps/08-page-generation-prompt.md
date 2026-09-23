# Step 5.8 — Page Generation Prompt (Phase 5B — Pages)

## Objective

Build the user prompt for PageObject generation. The prompt includes previously generated custom controls and site-specific patterns from the analysis pass. The generator agent produces `HtmlPageObjectBase<TSelf>` classes that reference custom controls when DOM patterns match.

## Dependencies

- Step 5.1 (Copilot SDK with generator agent)
- Step 5.7 (custom controls in registry + `{site}-controls` skill)
- Step 5.6 (analysis pass with locator report)
- Phase 4 (DOM snapshots with selected elements)

## Implementation

### PromptBuilder

```csharp
// Services/PromptBuilder.cs
public sealed class PromptBuilder
{
    public string BuildPagePrompt(
        string className,
        string namespaceName,
        string pageUrl,
        string pageTitle,
        IReadOnlyList<DomElement> selectedElements,
        IReadOnlyList<GeneratedControl> customControls,
        LocatorReport? locatorReport = null,
        IReadOnlyList<ControlGroupSuggestion>? containerGroups = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Generate a Brinell page object class with the following details:");
        sb.AppendLine();
        sb.AppendLine($"Class Name: {className}");
        sb.AppendLine($"Namespace: {namespaceName}");
        sb.AppendLine($"Page URL: {pageUrl}");
        sb.AppendLine($"Page Title: {pageTitle}");
        sb.AppendLine();

        // Custom controls section
        if (customControls.Count > 0)
        {
            sb.AppendLine("## Available Custom Controls");
            sb.AppendLine();
            sb.AppendLine("Use these site-specific controls when their DOM patterns are detected:");
            sb.AppendLine();
            foreach (var ctrl in customControls)
            {
                sb.AppendLine($"- **{ctrl.Name}** — matches: `{ctrl.DomSignature}`");
            }
            sb.AppendLine();
        }

        // Site-specific patterns
        if (locatorReport is not null)
        {
            sb.AppendLine("## Site-Specific Patterns");
            sb.AppendLine();
            sb.AppendLine($"Stable attributes: {string.Join(", ", locatorReport.StableAttributes)}");
            sb.AppendLine($"Unstable attributes: {string.Join(", ", locatorReport.UnstableAttributes)}");
            sb.AppendLine($"Recommendations: {locatorReport.Recommendations}");
            sb.AppendLine();
        }

        // Selected elements
        sb.AppendLine("## Page Elements");
        sb.AppendLine();
        sb.AppendLine("The page contains these elements (selected for automation):");
        sb.AppendLine();
        sb.AppendLine("```html");
        foreach (var el in selectedElements)
            FormatElement(sb, el, indent: 0);
        sb.AppendLine("```");
        sb.AppendLine();

        // Container instructions
        if (containerGroups is { Count: > 0 })
        {
            sb.AppendLine("## Container Groups");
            sb.AppendLine();
            sb.AppendLine($"The following element groups should be generated as " +
                $"ContainerBase<{className}, TContainer> classes:");
            sb.AppendLine();
            foreach (var group in containerGroups)
            {
                sb.AppendLine($"### Group \"{group.Name}\" (root: `{group.RootTag}`)");
                sb.AppendLine();
                sb.AppendLine("```html");
                FormatElement(sb, group.RootElement, indent: 0);
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        sb.AppendLine($"Generate a sealed class inheriting from HtmlPageObjectBase<{className}> " +
            "with expression-bodied control properties for each element. " +
            "Use custom controls when their DOM signature matches. " +
            "Choose the most appropriate control type and locator strategy for each element.");

        return sb.ToString();
    }

    private static void FormatElement(StringBuilder sb, DomElement el, int indent)
    {
        var pad = new string(' ', indent * 2);
        sb.Append($"{pad}<{el.Tag}");

        if (el.Id is not null) sb.Append($" id=\"{el.Id}\"");
        if (el.ClassName is not null) sb.Append($" class=\"{el.ClassName}\"");
        if (el.Name is not null) sb.Append($" name=\"{el.Name}\"");
        if (el.Type is not null) sb.Append($" type=\"{el.Type}\"");
        if (el.DataTestId is not null) sb.Append($" data-testid=\"{el.DataTestId}\"");
        if (el.Role is not null) sb.Append($" role=\"{el.Role}\"");
        if (el.AriaLabel is not null) sb.Append($" aria-label=\"{el.AriaLabel}\"");
        if (el.Placeholder is not null) sb.Append($" placeholder=\"{el.Placeholder}\"");

        if (el.Children.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(el.TextContent))
                sb.AppendLine($">{el.TextContent}</{el.Tag}>");
            else
                sb.AppendLine(" />");
        }
        else
        {
            sb.AppendLine(">");
            if (!string.IsNullOrWhiteSpace(el.TextContent))
                sb.AppendLine($"{pad}  {el.TextContent}");
            foreach (var child in el.Children)
                FormatElement(sb, child, indent + 1);
            sb.AppendLine($"{pad}</{el.Tag}>");
        }
    }
}
```

### PageGenerationService

```csharp
// Services/PageGenerationService.cs
public sealed class PageGenerationService
{
    private readonly ICopilotService _copilotService;
    private readonly IControlRegistry _controlRegistry;
    private readonly PromptBuilder _promptBuilder;
    private readonly ILogger<PageGenerationService> _logger;

    public async Task<PageGenerationResult> GeneratePageAsync(
        DomSnapshot snapshot,
        string className,
        string namespaceName,
        LocatorReport? locatorReport,
        IReadOnlyList<ControlGroupSuggestion>? containerGroups,
        CancellationToken ct = default)
    {
        var customControls = await _controlRegistry.GetAllControlsAsync();

        var prompt = _promptBuilder.BuildPagePrompt(
            className, namespaceName,
            snapshot.PageUrl, snapshot.PageTitle,
            snapshot.SelectedElements.Count > 0
                ? snapshot.SelectedElements
                : FlattenElements(snapshot.RootElement),
            customControls,
            locatorReport,
            containerGroups);

        _logger.LogInformation(
            "Generation — Page: {PageName}, Custom controls available: {ControlNames}",
            className, string.Join(", ", customControls.Select(c => c.Name)));

        var response = await _copilotService.GenerateAsync(prompt, ct);
        var codeBlocks = CodeBlockParser.ExtractCSharpBlocks(response);

        if (codeBlocks.Count == 0)
            throw new InvalidOperationException(
                $"No C# code blocks in LLM response for page '{className}'");

        // First block = main PageObject, subsequent = containers
        var mainCode = codeBlocks[0];
        var containerCodes = codeBlocks.Skip(1).ToList();

        // Validate main class
        var validation = CodeValidator.Validate(mainCode);
        if (!validation.IsValid)
        {
            mainCode = await RetryWithFeedbackAsync(mainCode, validation, ct);
            validation = CodeValidator.Validate(mainCode);
        }

        return new PageGenerationResult
        {
            ClassName = className,
            Namespace = namespaceName,
            MainCode = mainCode,
            ContainerCodes = containerCodes,
            Validation = validation,
            CustomControlsUsed = DetectUsedControls(mainCode, customControls)
        };
    }

    private static IReadOnlyList<DomElement> FlattenElements(DomElement root)
    {
        var result = new List<DomElement>();
        Flatten(root, result);
        return result;

        static void Flatten(DomElement el, List<DomElement> list)
        {
            // Only include actionable elements (inputs, buttons, links, etc.)
            if (IsActionable(el.Tag))
                list.Add(el);
            foreach (var child in el.Children)
                Flatten(child, list);
        }
    }

    private static bool IsActionable(string tag) =>
        tag is "input" or "button" or "select" or "textarea" or "a" or "img"
            or "label" or "form" or "nav" or "table";
}
```

### PageGenerationResult model

```csharp
// Models/PageGenerationResult.cs
public sealed class PageGenerationResult
{
    public string ClassName { get; init; } = "";
    public string Namespace { get; init; } = "";
    public string MainCode { get; init; } = "";
    public List<string> ContainerCodes { get; init; } = [];
    public ValidationResult Validation { get; init; } = new();
    public List<string> CustomControlsUsed { get; init; } = [];
}
```

### Batch generation

```csharp
// Called from GenerationViewModel
public async Task<IReadOnlyList<PageGenerationResult>> GenerateBatchAsync(
    IReadOnlyList<DomSnapshot> snapshots,
    string namespaceName,
    LocatorReport? locatorReport,
    CancellationToken ct = default)
{
    var results = new List<PageGenerationResult>();

    foreach (var snapshot in snapshots)
    {
        var className = DeriveClassName(snapshot.PageName);
        var result = await GeneratePageAsync(
            snapshot, className, namespaceName, locatorReport, null, ct);
        results.Add(result);
    }

    _logger.LogInformation(
        "Generation batch — Completed: {CompletedCount}, Failed: {FailedCount}",
        results.Count(r => r.Validation.IsValid),
        results.Count(r => !r.Validation.IsValid));

    return results;
}
```

## Checklist

- [ ] `PromptBuilder.BuildPagePrompt()` assembles prompt with class name, namespace, URL, title
- [ ] Prompt includes custom controls section with DOM signatures
- [ ] Prompt includes site-specific patterns (locator report) when available
- [ ] Prompt includes selected elements formatted as simplified HTML
- [ ] Container group instructions appended when auto-detected groups exist
- [ ] `PageGenerationService.GeneratePageAsync()` sends prompt to generator agent
- [ ] First code block = main PageObject, subsequent = container classes
- [ ] Code validated with Roslyn, auto-retry on failure
- [ ] `PageGenerationResult` tracks: class name, code, validation, custom controls used
- [ ] Batch generation processes multiple snapshots sequentially
- [ ] Generation logged with page name and custom controls available/used

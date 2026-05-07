using System.Text.RegularExpressions;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class PageGenerationService
{
    private readonly ICopilotService _copilotService;
    private readonly IControlRegistry _controlRegistry;
    private readonly PromptBuilder _promptBuilder;
    private readonly ILogger<PageGenerationService> _logger;

    public PageGenerationService(
        ICopilotService copilotService,
        IControlRegistry controlRegistry,
        PromptBuilder promptBuilder,
        ILogger<PageGenerationService> logger)
    {
        _copilotService = copilotService;
        _controlRegistry = controlRegistry;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    public async Task<PageGenerationResult> GeneratePageAsync(
        DomSnapshot snapshot,
        string className,
        string namespaceName,
        LocatorReport? locatorReport,
        IReadOnlyList<ControlGroupSuggestion>? containerGroups,
        CancellationToken ct = default)
    {
        var customControls = _controlRegistry.GetAllControls();

        var selectedElements = snapshot.SelectedElements.Count > 0
            ? snapshot.SelectedElements
            : FlattenActionableElements(snapshot.RootElement);

        var prompt = _promptBuilder.BuildPagePrompt(
            className, namespaceName,
            snapshot.PageUrl, snapshot.PageTitle,
            selectedElements,
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

        var mainCode = codeBlocks[0];
        var containerCodes = codeBlocks.Skip(1).ToList();

        var validation = CodeValidator.ValidateWithRegistry(mainCode, _controlRegistry);
        if (!validation.IsValid)
        {
            mainCode = await RetryWithFeedbackAsync(mainCode, validation, ct);
            validation = CodeValidator.ValidateWithRegistry(mainCode, _controlRegistry);
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

    private async Task<string> RetryWithFeedbackAsync(
        string failedCode, ValidationResult validation, CancellationToken ct)
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
        return blocks.Count > 0 ? blocks[0] : failedCode;
    }

    private static List<DomElement> FlattenActionableElements(DomElement root)
    {
        var result = new List<DomElement>();
        Flatten(root, result);
        return result;

        static void Flatten(DomElement el, List<DomElement> list)
        {
            if (IsActionable(el.Tag))
                list.Add(el);
            foreach (var child in el.Children)
                Flatten(child, list);
        }
    }

    private static bool IsActionable(string tag) =>
        tag is "input" or "button" or "select" or "textarea" or "a" or "img"
            or "label" or "form" or "nav" or "table";

    private static List<string> DetectUsedControls(
        string code, IReadOnlyList<GeneratedControl> customControls)
    {
        return customControls
            .Where(c => code.Contains(c.Name, StringComparison.Ordinal))
            .Select(c => c.Name)
            .ToList();
    }

    private static string DeriveClassName(string pageName)
    {
        var cleaned = Regex.Replace(pageName, @"[^a-zA-Z0-9]", "");
        if (!cleaned.EndsWith("Page", StringComparison.Ordinal))
            cleaned += "Page";
        return cleaned;
    }
}

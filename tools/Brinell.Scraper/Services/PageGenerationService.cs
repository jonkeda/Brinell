using System.Diagnostics;
using System.Text.RegularExpressions;
using Brinell.Scraper.Exceptions;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class PageGenerationService
{
    // Char budget for the prompt before we attempt truncation. ~24k chars ≈ 6k tokens.
    private const int PromptCharBudget = 24_000;

    private readonly ICopilotService _copilotService;
    private readonly IControlRegistry _controlRegistry;
    private readonly ControlObjectMatcher _matcher;
    private readonly PromptBuilder _promptBuilder;
    private readonly LlmRetryHelper _retryHelper;
    private readonly ILogger<PageGenerationService> _logger;

    public PageGenerationService(
        ICopilotService copilotService,
        IControlRegistry controlRegistry,
        ControlObjectMatcher matcher,
        PromptBuilder promptBuilder,
        LlmRetryHelper retryHelper,
        ILogger<PageGenerationService> logger)
    {
        _copilotService = copilotService;
        _controlRegistry = controlRegistry;
        _matcher = matcher;
        _promptBuilder = promptBuilder;
        _retryHelper = retryHelper;
        _logger = logger;
    }

    public async Task<PageGenerationResult> GeneratePageAsync(
        DomSnapshot snapshot,
        string targetNamespace,
        LocatorReport? locatorReport,
        List<ControlGroupSuggestion>? containerGroups,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fallbackClassName = DeriveClassName(snapshot.PageName);
        var registeredControls = _controlRegistry.GetAllControls();

        var actionable = snapshot.SelectedElements.Count > 0
            ? snapshot.SelectedElements
            : FlattenActionableElements(snapshot.RootElement);

        var matches = _matcher.MatchAll(snapshot, registeredControls);

        var prompt = _promptBuilder.BuildPageObjectPrompt(
            snapshot, actionable, containerGroups, matches,
            registeredControls, locatorReport, targetNamespace);

        _logger.LogInformation(
            "PageObject generation start — Page: {PageName}, Namespace: {Namespace}, " +
            "Controls: {ControlCount}, Matches: {MatchCount}, PromptLength: {PromptLength}",
            snapshot.PageName, targetNamespace,
            registeredControls.Count, matches.Count, prompt.Length);

        string mainCode;
        List<string> containerCodes;
        ValidationResult validation;

        try
        {
            (mainCode, containerCodes, validation) = await GenerateWithRetryAsync(
                snapshot, prompt, registeredControls, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (LlmAuthRequiredException)
        {
            throw;
        }
        catch (LlmTokenLimitException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "PageObject generation — token limit, no truncation possible. Page: {PageName}",
                snapshot.PageName);
            return ErrorResult(fallbackClassName, targetNamespace, matches,
                $"Token limit exceeded: {ex.Message}");
        }
        catch (LlmRateLimitedException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "PageObject generation — rate limited. Page: {PageName}", snapshot.PageName);
            return ErrorResult(fallbackClassName, targetNamespace, matches,
                $"Rate limited: {ex.Message}");
        }
        catch (LlmValidationException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "PageObject generation failed validation — Page: {PageName}, {Message}",
                snapshot.PageName, ex.Message);
            return ErrorResult(fallbackClassName, targetNamespace, matches, ex.Message);
        }

        var (parsedClass, parsedNamespace) = ExtractClassAndNamespace(mainCode);

        stopwatch.Stop();
        _logger.LogInformation(
            "PageObject generation done — Page: {PageName}, Elapsed: {ElapsedMs} ms, " +
            "Errors: {ErrorCount}, Warnings: {WarningCount}",
            snapshot.PageName, stopwatch.ElapsedMilliseconds,
            validation.Errors.Count, validation.Warnings.Count);

        return new PageGenerationResult
        {
            ClassName = !string.IsNullOrEmpty(parsedClass) ? parsedClass : fallbackClassName,
            Namespace = !string.IsNullOrEmpty(parsedNamespace) ? parsedNamespace : targetNamespace,
            MainCode = mainCode,
            ContainerCodes = containerCodes,
            UsedControlObjects = ToReferences(matches),
            CustomControlsUsed = DetectUsedControls(mainCode, registeredControls),
            Validation = validation,
            Status = validation.IsValid ? PageObjectStatus.Generated : PageObjectStatus.Error,
            GeneratedAt = DateTimeOffset.UtcNow,
        };
    }

    private async Task<(string MainCode, List<string> Containers, ValidationResult Validation)>
        GenerateWithRetryAsync(
            DomSnapshot snapshot,
            string prompt,
            IReadOnlyList<GeneratedControl> registeredControls,
            CancellationToken ct)
    {
        return await _retryHelper.WithRetryAsync<(string Code, List<string> Containers, ValidationResult Validation)>(
            call: async (p, c) =>
            {
                try
                {
                    return await _copilotService.GenerateWithErrorHandlingAsync(p, _logger, c);
                }
                catch (LlmTokenLimitException)
                {
                    var truncated = PromptTruncator.TruncatePageObjectPrompt(
                        p, snapshot, PromptCharBudget);
                    if (truncated is null)
                    {
                        _logger.LogWarning(
                            "PageObject generation — prompt cannot be truncated below budget. Page: {PageName}",
                            snapshot.PageName);
                        throw;
                    }
                    _logger.LogInformation(
                        "PageObject generation — prompt truncated from {Original} to {Truncated} chars. Page: {PageName}",
                        p.Length, truncated.Length, snapshot.PageName);
                    return await _copilotService.GenerateWithErrorHandlingAsync(truncated, _logger, c);
                }
            },
            initialPrompt: prompt,
            validate: response =>
            {
                var blocks = CodeBlockParser.ExtractCSharpBlocks(response);
                if (blocks.Count == 0)
                    return (false,
                        ("", new List<string>(), new ValidationResult
                        {
                            Errors = [new CodeError("No C# code blocks in LLM response", 0, 0)]
                        }),
                        "No C# code blocks in LLM response");

                var code = blocks[0];
                var containers = blocks.Skip(1).ToList();
                var v = CodeValidator.ValidateWithRegistry(code, registeredControls, containers);
                if (!v.IsValid)
                {
                    var summary = string.Join("; ",
                        v.Errors.Select(e => $"L{e.Line}: {e.Message}"));
                    return (false, (code, containers, v), summary);
                }
                return (true, (code, containers, v), null);
            },
            maxRetries: 1,
            ct: ct,
            operation: $"GeneratePage:{snapshot.PageName}");
    }

    private static PageGenerationResult ErrorResult(
        string fallbackClassName,
        string targetNamespace,
        IReadOnlyList<ControlObjectMatch> matches,
        string errorMessage)
    {
        var validation = new ValidationResult();
        validation.Errors.Add(new CodeError(errorMessage, 0, 0));
        return new PageGenerationResult
        {
            ClassName = fallbackClassName,
            Namespace = targetNamespace,
            MainCode = "",
            ContainerCodes = [],
            UsedControlObjects = ToReferences(matches),
            CustomControlsUsed = [],
            Validation = validation,
            Status = PageObjectStatus.Error,
            GeneratedAt = DateTimeOffset.UtcNow,
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
            var result = await GeneratePageAsync(
                snapshot, namespaceName, locatorReport, null, ct);
            results.Add(result);
        }

        _logger.LogInformation(
            "Generation batch — Completed: {CompletedCount}, Failed: {FailedCount}",
            results.Count(r => r.Validation.IsValid),
            results.Count(r => !r.Validation.IsValid));

        return results;
    }

    private static List<ControlObjectReference> ToReferences(IReadOnlyList<ControlObjectMatch> matches) =>
        matches
            .GroupBy(m => m.Control.Name, StringComparer.Ordinal)
            .Select(g => new ControlObjectReference
            {
                Name = g.Key,
                DomSignature = g.First().Control.DomSignature,
            })
            .ToList();

    private static (string ClassName, string Namespace) ExtractClassAndNamespace(string code)
    {
        var nsMatch = Regex.Match(code, @"namespace\s+([\w\.]+)");
        var classMatch = Regex.Match(code, @"\bclass\s+([A-Za-z_]\w*)");
        return (
            classMatch.Success ? classMatch.Groups[1].Value : "",
            nsMatch.Success ? nsMatch.Groups[1].Value : "");
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
        string code, IReadOnlyList<GeneratedControl> customControls) =>
        customControls
            .Where(c => code.Contains(c.Name, StringComparison.Ordinal))
            .Select(c => c.Name)
            .ToList();

    private static string DeriveClassName(string pageName)
    {
        var cleaned = Regex.Replace(pageName ?? "", @"[^a-zA-Z0-9]", "");
        if (string.IsNullOrEmpty(cleaned))
            cleaned = "Page";
        if (!cleaned.EndsWith("Page", StringComparison.Ordinal))
            cleaned += "Page";
        return cleaned;
    }
}

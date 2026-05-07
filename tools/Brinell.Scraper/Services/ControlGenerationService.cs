using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class ControlGenerationService
{
    private readonly ICopilotService _copilotService;
    private readonly IControlRegistry _controlRegistry;
    private readonly SkillService _skillService;
    private readonly ILogger<ControlGenerationService> _logger;

    public ControlGenerationService(
        ICopilotService copilotService,
        IControlRegistry controlRegistry,
        SkillService skillService,
        ILogger<ControlGenerationService> logger)
    {
        _copilotService = copilotService;
        _controlRegistry = controlRegistry;
        _skillService = skillService;
        _logger = logger;
    }

    public async Task<GeneratedControl> GenerateControlAsync(
        ControlProposal proposal,
        string siteNamespace,
        CancellationToken ct = default)
    {
        var prompt = PromptBuilder.BuildControlPrompt(proposal, siteNamespace);
        var response = await _copilotService.GenerateAsync(prompt, ct);
        var codeBlocks = CodeBlockParser.ExtractCSharpBlocks(response);

        if (codeBlocks.Count == 0)
            throw new InvalidOperationException(
                $"No C# code blocks in LLM response for control '{proposal.Name}'");

        var code = codeBlocks[0];

        var validation = CodeValidator.Validate(code);
        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "Generated control has errors, retrying — Name: {ControlName}, Errors: {ErrorCount}",
                proposal.Name, validation.Errors.Count);

            code = await RetryWithFeedbackAsync(code, validation, ct);
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

        _controlRegistry.StoreControl(control);

        _logger.LogInformation(
            "Generation — Control: {ControlName}", proposal.Name);

        return control;
    }

    public async Task<IReadOnlyList<GeneratedControl>> GenerateAllApprovedAsync(
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

        _skillService.GenerateSiteControlsSkill(siteName, generated);

        _logger.LogInformation(
            "All approved controls generated — Count: {Count}", generated.Count);

        return generated;
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
}

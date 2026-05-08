using System.Diagnostics;
using Brinell.Scraper.Exceptions;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class ControlGenerationService
{
    private readonly ICopilotService _copilotService;
    private readonly IControlRegistry _controlRegistry;
    private readonly SkillService _skillService;
    private readonly LlmRetryHelper _retryHelper;
    private readonly ILogger<ControlGenerationService> _logger;

    public ControlGenerationService(
        ICopilotService copilotService,
        IControlRegistry controlRegistry,
        SkillService skillService,
        LlmRetryHelper retryHelper,
        ILogger<ControlGenerationService> logger)
    {
        _copilotService = copilotService;
        _controlRegistry = controlRegistry;
        _skillService = skillService;
        _retryHelper = retryHelper;
        _logger = logger;
    }

    public async Task<GeneratedControl> GenerateControlAsync(
        ControlProposal proposal,
        string siteNamespace,
        CancellationToken ct = default)
    {
        var control = await GenerateOneAsync(proposal, siteNamespace, locatorReport: null, ct);
        if (control is null)
            throw new InvalidOperationException(
                $"Failed to generate control '{proposal.Name}' after retry");

        _controlRegistry.StoreControl(control);
        proposal.GenerationStatus = ControlGenerationStatus.Generated;
        return control;
    }

    public async Task<List<GeneratedControl>> GenerateAllApprovedAsync(
        List<ControlProposal> approvedProposals,
        string targetNamespace,
        LocatorReport? locatorReport,
        CancellationToken ct = default)
    {
        var results = new List<GeneratedControl>();

        foreach (var proposal in approvedProposals)
        {
            if (!proposal.IsApproved && proposal.Status != ControlObjectStatus.Approved)
                continue;

            var control = await GenerateOneAsync(proposal, targetNamespace, locatorReport, ct);
            if (control is not null)
            {
                _controlRegistry.StoreControl(control);
                proposal.GenerationStatus = ControlGenerationStatus.Generated;
                results.Add(control);
            }
            else
            {
                proposal.GenerationStatus = ControlGenerationStatus.Failed;
            }
        }

        _logger.LogInformation(
            "Approved control generation finished — Generated: {Generated}, Failed: {Failed}",
            results.Count, approvedProposals.Count(p => p.IsApproved || p.Status == ControlObjectStatus.Approved) - results.Count);

        return results;
    }

    private async Task<GeneratedControl?> GenerateOneAsync(
        ControlProposal proposal,
        string targetNamespace,
        LocatorReport? locatorReport,
        CancellationToken ct)
    {
        _ = locatorReport; // reserved for future prompt enrichment

        _logger.LogInformation(
            "Generating control {Name} (signature={DomSignature}, confidence={Confidence})",
            proposal.Name, proposal.DomSignature, proposal.Confidence);

        var stopwatch = Stopwatch.StartNew();
        var prompt = PromptBuilder.BuildControlPrompt(proposal, targetNamespace);

        try
        {
            var (code, _) = await _retryHelper.WithRetryAsync<(string Code, ValidationResult Validation)>(
                call: (p, c) => _copilotService.GenerateWithErrorHandlingAsync(p, _logger, c),
                initialPrompt: prompt,
                validate: ValidateControlResponse,
                maxRetries: 1,
                ct: ct,
                operation: $"GenerateControl:{proposal.Name}");

            var control = new GeneratedControl
            {
                Name = proposal.Name,
                Namespace = $"{targetNamespace}.Controls",
                Code = code,
                DomSignature = proposal.DomSignature,
                Confidence = proposal.Confidence,
                CreatedAt = DateTimeOffset.UtcNow
            };

            stopwatch.Stop();
            _logger.LogInformation(
                "Generated control {Name} ({CodeLength} chars) in {Elapsed} ms",
                control.Name, control.Code.Length, stopwatch.ElapsedMilliseconds);

            return control;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (LlmAuthRequiredException)
        {
            // Auth failures abort the stage; surface to caller.
            throw;
        }
        catch (LlmValidationException ex)
        {
            _logger.LogWarning(
                "Generation failed for {Name} after retries — {Message}",
                proposal.Name, ex.Message);
            return null;
        }
        catch (LlmRateLimitedException ex)
        {
            _logger.LogWarning(ex,
                "Generation failed for {Name} due to rate limiting", proposal.Name);
            return null;
        }
        catch (LlmTokenLimitException ex)
        {
            _logger.LogWarning(ex,
                "Generation failed for {Name} due to token limit", proposal.Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Generation failed for {Name} due to exception", proposal.Name);
            return null;
        }
    }

    private static (bool ok, (string Code, ValidationResult Validation) result, string? error)
        ValidateControlResponse(string response)
    {
        var blocks = CodeBlockParser.ExtractCSharpBlocks(response);
        if (blocks.Count == 0)
        {
            return (false,
                ("", new ValidationResult
                {
                    Errors = [new CodeError("No C# code blocks in LLM response", 0, 0)]
                }),
                "No C# code blocks in LLM response");
        }

        var code = blocks[0];
        var validation = CodeValidator.Validate(code);
        if (!validation.IsValid)
        {
            var errorSummary = string.Join("; ",
                validation.Errors.Select(e => $"L{e.Line}: {e.Message}"));
            return (false, (code, validation), errorSummary);
        }

        return (true, (code, validation), null);
    }
}

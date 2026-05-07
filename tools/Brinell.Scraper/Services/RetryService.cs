using System.Text;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class RetryService
{
    private readonly ICopilotService _copilotService;
    private readonly ILogger<RetryService> _logger;
    private const int MaxRetries = 2;

    public RetryService(ICopilotService copilotService, ILogger<RetryService> logger)
    {
        _copilotService = copilotService;
        _logger = logger;
    }

    public async Task<(string Code, ValidationResult Validation)> ValidateWithRetryAsync(
        string code,
        IControlRegistry registry,
        CancellationToken ct = default)
    {
        var validation = CodeValidator.ValidateWithRegistry(code, registry);

        for (var attempt = 0; attempt < MaxRetries && !validation.IsValid; attempt++)
        {
            _logger.LogWarning(
                "Code validation failed (attempt {Attempt}/{Max}), retrying — Errors: {ErrorCount}",
                attempt + 1, MaxRetries, validation.Errors.Count);

            var retryPrompt = BuildRetryPrompt(code, validation);
            var response = await _copilotService.GenerateAsync(retryPrompt, ct);
            var blocks = CodeBlockParser.ExtractCSharpBlocks(response);

            if (blocks.Count == 0)
            {
                _logger.LogWarning("Retry produced no code blocks, keeping original");
                break;
            }

            code = blocks[0];
            validation = CodeValidator.ValidateWithRegistry(code, registry);
        }

        if (!validation.IsValid)
        {
            _logger.LogError(
                "Code validation failed after {MaxRetries} retries — Errors: {Errors}",
                MaxRetries,
                string.Join("; ", validation.Errors.Select(e => e.Message)));
        }

        return (code, validation);
    }

    private static string BuildRetryPrompt(string failedCode, ValidationResult validation)
    {
        var sb = new StringBuilder();
        sb.AppendLine("The generated code has these errors. Please fix and regenerate the complete class:");
        sb.AppendLine();
        foreach (var error in validation.Errors)
        {
            sb.AppendLine($"  Line {error.Line}, Col {error.Column}: {error.Message}");
        }
        sb.AppendLine();
        sb.AppendLine("Original code:");
        sb.AppendLine("```csharp");
        sb.AppendLine(failedCode);
        sb.AppendLine("```");
        return sb.ToString();
    }
}

using Brinell.Scraper.Exceptions;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class LlmRetryHelper
{
    private readonly ILogger<LlmRetryHelper> _logger;

    public LlmRetryHelper(ILogger<LlmRetryHelper> logger)
    {
        _logger = logger;
    }

    public async Task<T> WithRetryAsync<T>(
        Func<string, CancellationToken, Task<string>> call,
        string initialPrompt,
        Func<string, (bool ok, T? result, string? error)> validate,
        int maxRetries = 1,
        CancellationToken ct = default,
        string? operation = null)
    {
        var prompt = initialPrompt;
        string? lastError = null;
        var op = operation ?? "(unspecified)";

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "LLM call start — Operation: {Operation}, Attempt: {Attempt}/{Max}",
                op, attempt + 1, maxRetries + 1);

            var response = await call(prompt, ct);
            var (ok, result, error) = validate(response);

            if (ok)
            {
                _logger.LogInformation(
                    "LLM call ok — Operation: {Operation}, Attempt: {Attempt}",
                    op, attempt + 1);
                return result!;
            }

            lastError = error;
            _logger.LogWarning(
                "LLM attempt {Attempt}/{Max} failed for {Operation}: {Error}",
                attempt + 1, maxRetries + 1, op, error);

            if (attempt < maxRetries)
            {
                prompt = initialPrompt
                    + "\n\nPrevious attempt had errors:\n" + (error ?? "")
                    + "\nFix these errors and respond again.";
            }
        }

        _logger.LogError(
            "LLM max retries exceeded — Operation: {Operation}, LastError: {Error}",
            op, lastError);
        throw new LlmValidationException(
            $"Max retries exceeded for {op}: {lastError}");
    }
}

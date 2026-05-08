using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Brinell.Scraper.Exceptions;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

/// <summary>
/// Extensions that wrap <see cref="ICopilotService"/> calls with consistent
/// error classification: 401/403 → Auth, 429 → rate-limit retry, token-limit → typed exception.
/// In stub mode (no real SDK) the wrapper passes through unchanged.
/// </summary>
public static class CopilotServiceExtensions
{
    private const int MaxRateLimitRetries = 3;

    private static readonly Regex TokenLimitPattern = new(
        @"context length|context window|token(s)?\s*(limit|exceeded)|maximum context|too many tokens",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static Task<string> AnalyzeWithErrorHandlingAsync(
        this ICopilotService svc, string prompt, ILogger logger, CancellationToken ct = default) =>
        ExecuteWithErrorHandlingAsync(c => svc.AnalyzeAsync(prompt, c), logger, ct);

    public static Task<string> GenerateWithErrorHandlingAsync(
        this ICopilotService svc, string prompt, ILogger logger, CancellationToken ct = default) =>
        ExecuteWithErrorHandlingAsync(c => svc.GenerateAsync(prompt, c), logger, ct);

    private static async Task<string> ExecuteWithErrorHandlingAsync(
        Func<CancellationToken, Task<string>> call,
        ILogger logger,
        CancellationToken ct)
    {
        for (var attempt = 0; attempt <= MaxRateLimitRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return await call(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (LlmAuthRequiredException) { throw; }
            catch (LlmRateLimitedException) { throw; }
            catch (LlmTokenLimitException) { throw; }
            catch (Exception ex)
            {
                var classified = Classify(ex);
                if (classified is null) throw; // stub mode / unknown — pass through

                if (classified is LlmRateLimitedException rl && attempt < MaxRateLimitRetries)
                {
                    var delay = rl.RetryAfter ?? TimeSpan.FromMilliseconds(
                        200 * Math.Pow(2, attempt));
                    logger.LogWarning(
                        "LLM rate-limited — waiting {DelayMs} ms (attempt {Attempt}/{Max})",
                        (int)delay.TotalMilliseconds, attempt + 1, MaxRateLimitRetries);
                    await Task.Delay(delay, ct);
                    continue;
                }

                if (classified is LlmAuthRequiredException)
                    logger.LogError(ex, "LLM auth required");
                else if (classified is LlmTokenLimitException)
                    logger.LogWarning("LLM token limit hit: {Message}", ex.Message);
                else
                    logger.LogWarning("LLM rate limit exhausted after retries");

                throw classified;
            }
        }

        throw new LlmRateLimitedException(
            $"LLM rate limit exceeded after {MaxRateLimitRetries} retries");
    }

    public static Exception? Classify(Exception ex)
    {
        // Walk the exception chain looking for recognizable signals.
        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            var status = TryGetStatusCode(cur);
            if (status == HttpStatusCode.Unauthorized || status == HttpStatusCode.Forbidden)
                return new LlmAuthRequiredException(
                    $"Authentication required ({(int)status.Value}): {ex.Message}", ex);

            if (status == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = TryGetRetryAfter(cur);
                return new LlmRateLimitedException(
                    $"Rate limited (429): {ex.Message}", ex)
                {
                    RetryAfter = retryAfter,
                };
            }

            if (!string.IsNullOrEmpty(cur.Message) && TokenLimitPattern.IsMatch(cur.Message))
                return new LlmTokenLimitException(
                    $"Token limit exceeded: {cur.Message}", ex);
        }

        // Fallback: scan the top-level message text for HTTP status hints (some SDKs
        // throw a plain Exception with the status embedded in the message).
        if (Regex.IsMatch(ex.Message, @"\b(401|403)\b"))
            return new LlmAuthRequiredException($"Authentication required: {ex.Message}", ex);
        if (Regex.IsMatch(ex.Message, @"\b429\b"))
            return new LlmRateLimitedException($"Rate limited (429): {ex.Message}", ex);

        return null;
    }

    private static HttpStatusCode? TryGetStatusCode(Exception ex) => ex switch
    {
        HttpRequestException hre when hre.StatusCode.HasValue => hre.StatusCode,
        _ => TryGetStatusCodeViaReflection(ex),
    };

    private static HttpStatusCode? TryGetStatusCodeViaReflection(Exception ex)
    {
        var prop = ex.GetType().GetProperty("StatusCode");
        if (prop is null) return null;
        var value = prop.GetValue(ex);
        return value switch
        {
            HttpStatusCode code => code,
            int i => (HttpStatusCode)i,
            _ => null,
        };
    }

    private static TimeSpan? TryGetRetryAfter(Exception ex)
    {
        var prop = ex.GetType().GetProperty("RetryAfter");
        var value = prop?.GetValue(ex);
        if (value is TimeSpan ts) return ts;
        if (value is int seconds) return TimeSpan.FromSeconds(seconds);
        if (value is string s &&
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sec))
            return TimeSpan.FromSeconds(sec);
        return null;
    }
}

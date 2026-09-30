namespace Brinell.Scraper.Exceptions;

public sealed class LlmRateLimitedException : Exception
{
    public TimeSpan? RetryAfter { get; init; }

    public LlmRateLimitedException(string message) : base(message) { }
    public LlmRateLimitedException(string message, Exception inner) : base(message, inner) { }
}

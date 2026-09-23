namespace Brinell.Scraper.Exceptions;

public sealed class LlmTokenLimitException : Exception
{
    public LlmTokenLimitException(string message) : base(message) { }
    public LlmTokenLimitException(string message, Exception inner) : base(message, inner) { }
}

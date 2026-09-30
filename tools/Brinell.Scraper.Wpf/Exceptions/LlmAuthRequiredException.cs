namespace Brinell.Scraper.Exceptions;

public sealed class LlmAuthRequiredException : Exception
{
    public LlmAuthRequiredException(string message) : base(message) { }
    public LlmAuthRequiredException(string message, Exception inner) : base(message, inner) { }
}

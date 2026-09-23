namespace Brinell.Scraper.Exceptions;

public sealed class LlmValidationException : Exception
{
    public LlmValidationException(string message) : base(message) { }
    public LlmValidationException(string message, Exception inner) : base(message, inner) { }
}

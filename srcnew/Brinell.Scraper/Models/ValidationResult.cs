namespace Brinell.Scraper.Models;

public sealed class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<CodeError> Errors { get; init; } = [];
    public List<CodeWarning> Warnings { get; init; } = [];
}

public sealed record CodeError(string Message, int Line, int Column);

public sealed record CodeWarning(string Message, int Line, int Column);

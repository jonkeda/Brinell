namespace Brinell.Scraper.Models;

public sealed class GeneratedControl
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Namespace { get; init; } = "";
    public string Code { get; init; } = "";
    public string DomSignature { get; init; } = "";
    public double Confidence { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

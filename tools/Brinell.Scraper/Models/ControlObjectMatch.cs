namespace Brinell.Scraper.Models;

public sealed class ControlObjectMatch
{
    public DomElement Element { get; set; } = default!;
    public GeneratedControl Control { get; set; } = default!;
    public double Score { get; set; }
    public string Reason { get; set; } = "";
    public string XPath { get; set; } = "";
}

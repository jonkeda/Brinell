namespace Brinell.Scraper.Models;

public sealed class ValidationEntry
{
    public string Category { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";

    public string Icon => Severity switch
    {
        "Error" => "❌",
        "Warning" => "⚠️",
        _ => "✅",
    };
}

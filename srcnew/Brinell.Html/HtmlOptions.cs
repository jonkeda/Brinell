namespace Brinell.Html;

/// <summary>
/// HTML application-specific configuration options.
/// </summary>
public class HtmlOptions
{
    /// <summary>
    /// Base URL of the HTML application (e.g., https://localhost:3000).
    /// Default: https://localhost:3000
    /// </summary>
    public string AppUrl { get; set; } = "https://localhost:3000";

    /// <summary>
    /// Full path to the HTML application project or binary.
    /// Null = assume application is already running at AppUrl.
    /// </summary>
    public string? AppPath { get; set; }
}

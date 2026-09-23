namespace Brinell.Core.Configuration;

/// <summary>
/// Browser-based testing configuration (Blazor, HTML, etc.).
/// </summary>
public class BrowserOptions
{
    /// <summary>
    /// Run browser in headless mode (no visible window).
    /// Default: true
    /// </summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Browser engine type: "chromium", "firefox", or "webkit".
    /// Default: "chromium"
    /// </summary>
    public string BrowserType { get; set; } = "chromium";
}

namespace Brinell.Blazor;

/// <summary>
/// Blazor application-specific configuration options.
/// </summary>
public class BlazorOptions
{
    /// <summary>
    /// Base URL of the Blazor application (e.g., https://localhost:5001).
    /// Default: https://localhost:5001
    /// </summary>
    public string AppUrl { get; set; } = "https://localhost:5001";

    /// <summary>
    /// Full path to the Blazor application project or binary.
    /// Null = assume application is already running at AppUrl.
    /// </summary>
    public string? AppPath { get; set; }

    /// <summary>
    /// Timeout in seconds to wait for application to be ready.
    /// Default: 30
    /// </summary>
    public int ReadyTimeoutSeconds { get; set; } = 30;
}

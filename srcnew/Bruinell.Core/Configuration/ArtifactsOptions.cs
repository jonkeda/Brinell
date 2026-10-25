namespace Bruinell.Core.Configuration;

/// <summary>
/// Shared artifact configuration across all platforms.
/// </summary>
public class ArtifactsOptions
{
    /// <summary>
    /// Root directory for test artifacts and logs.
    /// Default: {AppData}/bruinell-artifacts
    /// </summary>
    public string RootDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "bruinell-artifacts");

    /// <summary>
    /// Optional run ID for organizing artifacts by test run.
    /// </summary>
    public string? RunId { get; set; }

    /// <summary>
    /// Optional suite name for organizing artifacts by test suite.
    /// </summary>
    public string? Suite { get; set; }
}

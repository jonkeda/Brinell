namespace Bruinell.Core.Interfaces;

/// <summary>
/// A test context that can take its own screenshots, so a fixture holding it gets a failure's
/// screenshot without making a service of its own (UAT looks for it on the fixture's properties).
/// </summary>
public interface IScreenshotSource
{
    /// <summary>Screenshots of what this context shows, saved in the run's screenshots folder.</summary>
    IScreenshotService Screenshots { get; }
}

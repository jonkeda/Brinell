namespace Brinell.Core.Scraping;

/// <summary>
/// Where the Scraper records from: a browser for Html, and later an app's UI Automation tree. One per
/// recording session. Node numbers in <see cref="HighlightAsync"/> and <see cref="PickAsync"/> are
/// those of the last <see cref="CaptureAsync"/>.
/// </summary>
public interface IScrapeSource : IAsyncDisposable
{
    /// <summary>The platform name its snapshots carry.</summary>
    string Platform { get; }

    /// <summary>Raised when the page or window changed and has settled: a snapshot can be taken.</summary>
    event EventHandler<ScrapeTransition>? Transitioned;

    /// <summary>Opens the app or page to record.</summary>
    Task StartAsync(ScrapeStart start, CancellationToken cancellationToken = default);

    /// <summary>Waits until the page or window has settled, then captures it.</summary>
    Task<ScrapeSnapshot> CaptureAsync(CancellationToken cancellationToken = default);

    /// <summary>Outlines a node in the app, or removes the outline when <paramref name="number"/> is null.</summary>
    Task HighlightAsync(int? number, CancellationToken cancellationToken = default);

    /// <summary>Lets the user click a node in the app; returns its number, or null when they cancel.</summary>
    Task<int?> PickAsync(CancellationToken cancellationToken = default);
}

/// <summary>What a recording starts on.</summary>
/// <param name="Location">The page's address, or the app to start.</param>
/// <param name="ProfileFolder">Where the source keeps its login and cookies between sessions; outside the repository.</param>
/// <param name="Visible">True for a window the user works in; false only for a capture of our own sites in tests.</param>
public sealed record ScrapeStart(string Location, string ProfileFolder, bool Visible = true);

/// <summary>The page or window changed.</summary>
/// <param name="Location">Where it is now.</param>
public sealed record ScrapeTransition(string Location);

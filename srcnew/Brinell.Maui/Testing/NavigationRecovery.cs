using Brinell.Core.Interfaces;
using Brinell.Maui.Interfaces;

namespace Brinell.Maui.Testing;

/// <summary>
/// Recovers app navigation state by unwinding pages until a registered root state is reached.
/// </summary>
public class NavigationRecovery
{
    private readonly IMauiTestContext _context;
    private readonly IScreenshotService? _screenshotService;
    private readonly HashSet<INavigablePage> _navigablePages = new();
    private readonly List<Func<bool>> _rootChecks = [];
    private readonly int _pageDetectionTimeoutMs;
    private readonly int _postNavigationDelayMs;

    public NavigationRecovery(
        IMauiTestContext context,
        IScreenshotService? screenshotService = null,
        int pageDetectionTimeoutMs = 500,
        int postNavigationDelayMs = 300)
    {
        _context = context;
        _screenshotService = screenshotService;
        _pageDetectionTimeoutMs = pageDetectionTimeoutMs;
        _postNavigationDelayMs = postNavigationDelayMs;
    }

    /// <summary>
    /// Registers pages that can be detected and left during recovery.
    /// </summary>
    /// <param name="pages">Navigable page instances.</param>
    public void RegisterPages(params INavigablePage[] pages)
    {
        foreach (var page in pages)
        {
            _navigablePages.Add(page);
        }
    }

    /// <summary>
    /// Registers a root-state predicate.
    /// </summary>
    /// <param name="isRoot">Predicate that returns true when app is in a valid root state.</param>
    public void RegisterRootCheck(Func<bool> isRoot)
    {
        _rootChecks.Add(isRoot);
    }

    /// <summary>
    /// Attempts to return to a registered root state.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of unwind attempts.</param>
    /// <returns>True when root state is reached.</returns>
    public bool TryReturnToRoot(int maxAttempts = 5)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (IsAtRoot())
            {
                return true;
            }

            var currentPage = _navigablePages.FirstOrDefault(page => page.IsLoaded(_pageDetectionTimeoutMs));
            if (currentPage != null)
            {
                if (!currentPage.TryLeave())
                {
                    _context.NavigateBack();
                }
            }
            else
            {
                _context.NavigateBack();
            }

            Thread.Sleep(_postNavigationDelayMs);
        }

        var recovered = IsAtRoot();
        if (!recovered)
        {
            _screenshotService?.Capture("NavigationRecovery", nameof(TryReturnToRoot), "recovery_failed_after_max_attempts");
        }

        return recovered;
    }

    private bool IsAtRoot()
    {
        if (_rootChecks.Count == 0)
        {
            return false;
        }

        return _rootChecks.Any(rootCheck => rootCheck());
    }
}
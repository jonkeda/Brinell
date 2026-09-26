using Brinell.Html.Exceptions;
using Microsoft.Playwright;

namespace Brinell.Html.Playwright;

/// <summary>
/// Which Playwright failures mean "the element is gone" and which mean "the browser is gone".
/// </summary>
/// <remarks>
/// Playwright reports both as <see cref="PlaywrightException"/>, told apart only by the message
/// (differences-from-maui.md items 1 and 2). Anything else is not a signal and stays as it is.
/// </remarks>
internal static class PlaywrightErrors
{
    private static readonly string[] ClosedMarkers =
    [
        "Target page, context or browser has been closed",
        "Target closed",
        "Browser has been closed",
        "Browser closed",
        "Connection closed",
    ];

    private static readonly string[] DetachedMarkers =
    [
        "Element is not attached to the DOM",
        "JavaScript handle is disposed",
        "Execution context was destroyed",
    ];

    internal static bool IsClosed(Exception error)
        => error is PlaywrightException && ClosedMarkers.Any(marker => error.Message.Contains(marker, StringComparison.OrdinalIgnoreCase));

    internal static bool IsDetached(Exception error)
        => error is PlaywrightException && DetachedMarkers.Any(marker => error.Message.Contains(marker, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Runs a Playwright call, reporting a closed browser as <see cref="AppUnavailableException"/>
    /// and a detached element as <see cref="StaleElementException"/>.
    /// </summary>
    internal static T Live<T>(Func<T> touch)
    {
        try
        {
            return touch();
        }
        catch (PlaywrightException error) when (IsClosed(error))
        {
            throw new AppUnavailableException("the browser, its context or the page has closed.", error);
        }
        catch (PlaywrightException error) when (IsDetached(error))
        {
            throw new StaleElementException(platformError: error);
        }
    }

    /// <inheritdoc cref="Live{T}(Func{T})"/>
    internal static void Live(Action touch) => Live(() =>
    {
        touch();
        return true;
    });
}

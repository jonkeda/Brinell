using Bruinell.Core.Exceptions;
using Microsoft.Playwright;

namespace Bruinell.Html.Playwright;

/// <summary>
/// Which Playwright failures mean "the element is gone" and which mean "the browser is gone".
/// </summary>
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

    /// <summary>
    /// Runs a Playwright action through <see cref="Live(Action)"/>, its actionability wait given
    /// to Playwright. A timeout before the action ran is <see cref="ElementNotReadyException"/>
    /// with Playwright's reason, so the call's poll keeps its budget and its message. A timeout
    /// after the action ran (Playwright waiting for the navigation it started) is the action done:
    /// it is not repeated (R0), and the next call waits for what it started.
    /// </summary>
    internal static void Act(Bruinell.Core.Locators.Locator locator, Action action)
    {
        try
        {
            Live(action);
        }
        catch (TimeoutException error) when (ActionRan(error.Message))
        {
        }
        catch (TimeoutException error)
        {
            var (reason, detail) = NotReadyOf(error.Message);
            throw new ElementNotReadyException(locator, reason, detail);
        }
    }

    /// <summary>Whether Playwright's call log says the action itself was performed ("click action done").</summary>
    internal static bool ActionRan(string message)
        => CallLog(message).Any(line => line.EndsWith(" action done", StringComparison.Ordinal));

    /// <summary>What Playwright's call log says the element was short of: its last line that says.</summary>
    internal static (NotReadyReason Reason, string? Detail) NotReadyOf(string message)
    {
        var lines = CallLog(message).ToList();
        var reasonLine = lines.LastOrDefault(line =>
            line.StartsWith("element is not ", StringComparison.Ordinal)
            || line.Contains("intercepts pointer events", StringComparison.Ordinal)
            || line.StartsWith("element is outside of the viewport", StringComparison.Ordinal));
        var reason = reasonLine switch
        {
            null => NotReadyReason.Other,
            _ when reasonLine.StartsWith("element is not visible", StringComparison.Ordinal) => NotReadyReason.NotVisible,
            _ when reasonLine.StartsWith("element is not enabled", StringComparison.Ordinal) => NotReadyReason.Disabled,
            _ => NotReadyReason.Other,
        };
        return (reason, reasonLine ?? lines.LastOrDefault() ?? message.Split('\n')[0].Trim());
    }

    private static IEnumerable<string> CallLog(string message)
        => message.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
            .Select(line => line[2..].Trim());
}

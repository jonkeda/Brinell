using OpenQA.Selenium;

namespace Brinell.Appium;

/// <summary>
/// How Appium reports an element or a session that is gone, or a command the server does not have.
/// </summary>
public static class AppiumErrors
{
    /// <summary>Whether <paramref name="error"/> says the element is no longer in the tree.</summary>
    public static bool IsElementGone(Exception error) => error is StaleElementReferenceException;

    /// <summary>Whether <paramref name="error"/> says the driver session has ended.</summary>
    public static bool IsSessionGone(Exception error)
        => error is WebDriverException
           && (error.Message.Contains("invalid session id", StringComparison.OrdinalIgnoreCase)
               || error.Message.Contains("session is either terminated or not started", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Whether <paramref name="error"/> says the server did not answer within the client's command
    /// timeout: the server or the device is stuck, and the session cannot be trusted.
    /// </summary>
    public static bool IsNoAnswer(Exception error)
        => error is WebDriverException
           && error.Message.Contains("HTTP request to the remote WebDriver server", StringComparison.OrdinalIgnoreCase)
           && error.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether <paramref name="error"/> is either: a catch that means "absent" must let it through.</summary>
    public static bool IsGone(Exception error) => IsElementGone(error) || IsSessionGone(error);

    /// <summary>
    /// Whether <paramref name="error"/> says the server has no such command, as a <c>mobile:</c>
    /// extension an older driver lacks: asking again cannot help.
    /// </summary>
    public static bool IsUnknownCommand(Exception error)
        => error is UnknownMethodException
           || (error is WebDriverException
               && (error.Message.Contains("unknown command", StringComparison.OrdinalIgnoreCase)
                   || error.Message.Contains("Unknown mobile command", StringComparison.OrdinalIgnoreCase)
                   || error.Message.Contains("is not supported", StringComparison.OrdinalIgnoreCase)));
}

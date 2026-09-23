using OpenQA.Selenium;

namespace Brinell.Maui.Appium;

/// <summary>
/// How Appium reports an element or a session that is gone.
/// </summary>
internal static class AppiumErrors
{
    /// <summary>Whether <paramref name="error"/> says the element is no longer in the tree.</summary>
    internal static bool IsElementGone(Exception error) => error is StaleElementReferenceException;

    /// <summary>Whether <paramref name="error"/> says the driver session has ended.</summary>
    internal static bool IsSessionGone(Exception error)
        => error is WebDriverException
           && (error.Message.Contains("invalid session id", StringComparison.OrdinalIgnoreCase)
               || error.Message.Contains("session is either terminated or not started", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether <paramref name="error"/> is either: a catch that means "absent" must let it through.</summary>
    internal static bool IsGone(Exception error) => IsElementGone(error) || IsSessionGone(error);
}

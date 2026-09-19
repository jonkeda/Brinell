using OpenQA.Selenium;

namespace Brinell.Maui.Appium;

/// <summary>
/// How Appium reports an element or a session that is gone.
/// </summary>
/// <remarks>
/// <para>
/// A replaced element answers any call with Selenium's <see cref="StaleElementReferenceException"/>.
/// An ended session answers with a <see cref="WebDriverException"/> carrying the W3C
/// "invalid session id" error, whose message says so in fixed words. Only those fixed words are
/// matched: a broad word such as "closed" or "not found" appears in ordinary errors too, and would
/// report a live app as gone.
/// </para>
/// <para>
/// This is the one place the driver recognizes either. <see cref="AppiumMauiElement"/> turns them
/// into <c>StaleElementException</c> and <c>AppUnavailableException</c>, so Selenium's types stay
/// in this project (<c>.my/stale-readiness/design.md</c>, R5).
/// </para>
/// </remarks>
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

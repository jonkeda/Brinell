using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for the test context that manages the test session.
/// Provides configuration, navigation, and logging.
/// </summary>
/// <remarks>
/// Control creation uses the 'new' pattern directly in PageObjects:
/// <code>
/// var button = new ButtonControl(context, "SubmitBtn", this);
/// </code>
/// </remarks>
public interface ITestContext
{
    /// <summary>
    /// Default timeout in milliseconds for wait operations.
    /// </summary>
    int DefaultTimeoutMs { get; set; }

    /// <summary>
    /// Default polling interval in milliseconds for wait operations.
    /// </summary>
    int DefaultPollingIntervalMs { get; set; }

    /// <summary>
    /// The current page object (if any).
    /// </summary>
    IPageObject? CurrentPage { get; }

    /// <summary>
    /// Navigates to a route/URL.
    /// If route is null, does nothing (skip operation).
    /// </summary>
    /// <param name="route">The route or URL to navigate to.</param>
    /// <param name="timeoutMs">Timeout for navigation.</param>
    void NavigateTo(string? route, int? timeoutMs = null);

    /// <summary>
    /// Navigates to a page and returns the page object.
    /// </summary>
    /// <typeparam name="TPage">The page object type.</typeparam>
    /// <param name="timeoutMs">Timeout for navigation.</param>
    TPage NavigateTo<TPage>(int? timeoutMs = null) where TPage : IPageObject;

    /// <summary>
    /// Takes a screenshot of the current state.
    /// </summary>
    /// <param name="filename">Optional filename (without extension).</param>
    void TakeScreenshot(string? filename);

    /// <summary>
    /// Logs a message.
    /// </summary>
    void Log(string? message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    void LogError(string? message);
}

using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for page objects that represent a view/screen in the application.
/// Provides page state verification and control access.
/// </summary>
/// <remarks>
/// Controls should be created using the 'new' pattern in derived page classes:
/// <code>
/// public ButtonControl SubmitButton => new(Context, "SubmitBtn", this);
/// public EntryControl UsernameEntry => new(Context, "Username", this);
/// </code>
/// </remarks>
public interface IPageObject
{
    /// <summary>
    /// The name of this page (for logging and identification).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Immediately checks if the page is loaded/displayed.
    /// Does not wait or retry.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for any element checks.</param>
    bool IsLoaded(int? timeoutMs = null);

    /// <summary>
    /// Waits for the page to be loaded or unloaded.
    /// If expected is null, returns true immediately (skip operation).
    /// </summary>
    bool WaitLoaded(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts the page is loaded/unloaded.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets the page title.
    /// </summary>
    /// <param name="timeoutMs">Timeout for operation.</param>
    string GetTitle(int? timeoutMs = null);

    /// <summary>
    /// Asserts the page title equals expected.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Checks if a control exists on this page.
    /// </summary>
    bool ControlExists(ControlLocator locator, int? timeoutMs = null);

    /// <summary>
    /// Waits for a control to exist or not exist.
    /// If expected is null, returns true immediately (skip operation).
    /// </summary>
    bool WaitControlExists(ControlLocator locator, bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts a control exists or doesn't exist.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertControlExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Takes a screenshot of the current page.
    /// </summary>
    /// <param name="filename">Optional filename (without extension).</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    void TakeScreenshot(string? filename, int? timeoutMs = null);

    /// <summary>
    /// Scrolls to make a control visible.
    /// If locator is null, does nothing (skip operation).
    /// </summary>
    void ScrollToControl(ControlLocator? locator, int? timeoutMs = null);
}

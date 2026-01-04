namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for navigation page controls.
/// </summary>
public interface INavigationPageControlObject : IControlObject
{
    /// <summary>
    /// Gets the current page title.
    /// </summary>
    string? GetCurrentPageTitle(int? timeoutMs = null);

    /// <summary>
    /// Asserts the current page title matches the expected value.
    /// </summary>
    void AssertCurrentPageTitle(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Checks if the back button is available.
    /// </summary>
    bool CanGoBack(int? timeoutMs = null);

    /// <summary>
    /// Navigates back.
    /// </summary>
    void GoBack(int? timeoutMs = null);

    /// <summary>
    /// Waits for navigation to complete.
    /// </summary>
    void WaitNavigationComplete(int? timeoutMs = null);
}

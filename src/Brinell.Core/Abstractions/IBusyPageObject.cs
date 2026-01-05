namespace Brinell.Core.Abstractions;

/// <summary>
/// Interface for page objects that track busy/loading state.
/// Use this for pages that display loading indicators during async operations.
/// </summary>
public interface IBusyPageObject : IPageObject
{
    /// <summary>
    /// Check if the page is currently busy (showing loading indicator).
    /// </summary>
    /// <returns>True if the page is busy.</returns>
    bool IsBusy();

    /// <summary>
    /// Wait for the page to not be busy.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if page became not busy within timeout.</returns>
    bool WaitForNotBusy(int? timeoutMs = null);

    /// <summary>
    /// Assert the page is not busy.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertNotBusy(string? message = null);
}

namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for refreshable controls (pull-to-refresh pattern).
/// </summary>
public interface IRefreshableControlObject : IControlObject
{
    /// <summary>
    /// Gets whether the control is currently refreshing.
    /// </summary>
    bool IsRefreshing(int? timeoutMs = null);

    /// <summary>
    /// Waits for the refreshing state to match the expected value.
    /// </summary>
    bool WaitRefreshing(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts the refreshing state matches the expected value.
    /// </summary>
    void AssertRefreshing(bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Triggers a refresh (pull-to-refresh gesture).
    /// </summary>
    void Refresh(int? timeoutMs = null);

    /// <summary>
    /// Waits for the refresh operation to complete.
    /// </summary>
    void WaitRefreshComplete(int? timeoutMs = null);
}

using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Gestures;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI RefreshView control wrapper.
/// Provides pull-to-refresh functionality.
/// </summary>
public class RefreshViewControl : ControlBase
{
    public RefreshViewControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public RefreshViewControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the view is currently refreshing.
    /// </summary>
    public bool IsRefreshing()
    {
        var element = FindElement();
        if (element == null) return false;
        
        var refreshing = element.GetAttribute("isRefreshing") ?? element.GetAttribute("refreshing");
        return refreshing?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Perform pull-to-refresh gesture.
    /// </summary>
    public void PullToRefresh()
    {
        LogAction("PullToRefresh");
        
        // Swipe down from the top to trigger refresh
        SwipeDown(300);
    }

    /// <summary>
    /// Wait for refresh to complete.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    public bool WaitForRefreshComplete(int? timeoutMs = null)
    {
        Log("WaitForRefreshComplete()");
        return _context.WaitFor(() => !IsRefreshing(), timeoutMs, "refresh complete");
    }

    /// <summary>
    /// Perform pull-to-refresh and wait for completion.
    /// </summary>
    /// <param name="timeoutMs">Timeout for refresh completion.</param>
    public void RefreshAndWait(int? timeoutMs = null)
    {
        PullToRefresh();
        Thread.Sleep(500); // Wait for refresh to start
        WaitForRefreshComplete(timeoutMs);
    }

    #region Assert Methods

    /// <summary>
    /// Assert the view is refreshing.
    /// </summary>
    public void AssertRefreshing(string? message = null)
    {
        if (!IsRefreshing())
        {
            ThrowAssertionFailed("Refreshing", "false", "true",
                message ?? "Expected view to be refreshing.");
        }
        LogAssertPass("Refreshing", "true", "true");
    }

    /// <summary>
    /// Assert the view is not refreshing.
    /// </summary>
    public void AssertNotRefreshing(string? message = null)
    {
        if (IsRefreshing())
        {
            ThrowAssertionFailed("NotRefreshing", "true", "false",
                message ?? "Expected view to not be refreshing.");
        }
        LogAssertPass("NotRefreshing", "false", "false");
    }

    #endregion
}

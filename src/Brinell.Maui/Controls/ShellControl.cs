using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Shell control wrapper.
/// Provides navigation and flyout menu functionality.
/// </summary>
public class ShellControl : ControlBase
{
    public ShellControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ShellControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the flyout menu is currently open.
    /// </summary>
    public bool IsFlyoutOpen()
    {
        var flyout = _context.Driver.FindElementDirect("FlyoutMenu");
        return flyout?.Displayed ?? false;
    }

    /// <summary>
    /// Open the flyout menu.
    /// </summary>
    public void OpenFlyout()
    {
        LogAction("OpenFlyout");
        if (!IsFlyoutOpen())
        {
            // Swipe from left edge to open flyout
            SwipeRight(300);
        }
    }

    /// <summary>
    /// Close the flyout menu.
    /// </summary>
    public void CloseFlyout()
    {
        LogAction("CloseFlyout");
        if (IsFlyoutOpen())
        {
            // Tap outside flyout or swipe left to close
            SwipeLeft(300);
        }
    }

    /// <summary>
    /// Navigate to a Shell route.
    /// </summary>
    /// <param name="route">The route path (e.g., "//main/settings").</param>
    public void NavigateToRoute(string route)
    {
        LogAction("NavigateToRoute", route);
        // Shell navigation is typically done through code, not UI automation
        // This is a placeholder for platform-specific navigation
        Log($"NavigateToRoute: '{route}' - requires app-side Shell.Current.GoToAsync");
    }

    /// <summary>
    /// Get a flyout item by its title.
    /// </summary>
    /// <param name="title">The title of the flyout item.</param>
    public FlyoutItemControl GetFlyoutItem(string title)
    {
        OpenFlyout();
        return new FlyoutItemControl(_context, this.Page, title);
    }

    /// <summary>
    /// Get the tab bar control.
    /// </summary>
    public TabBarControl GetTabBar()
    {
        return new TabBarControl(_context, this.Page, "ShellTabBar");
    }

    #region Assert Methods

    /// <summary>
    /// Assert the flyout is open.
    /// </summary>
    public void AssertFlyoutOpen(string? message = null)
    {
        if (!IsFlyoutOpen())
        {
            ThrowAssertionFailed("FlyoutOpen", "false", "true",
                message ?? "Expected flyout to be open but it was closed.");
        }
        LogAssertPass("FlyoutOpen", "true", "true");
    }

    /// <summary>
    /// Assert the flyout is closed.
    /// </summary>
    public void AssertFlyoutClosed(string? message = null)
    {
        if (IsFlyoutOpen())
        {
            ThrowAssertionFailed("FlyoutClosed", "true", "false",
                message ?? "Expected flyout to be closed but it was open.");
        }
        LogAssertPass("FlyoutClosed", "false", "false");
    }

    #endregion
}

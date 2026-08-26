using Brinell.Maui.AppSupport.Handlers;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Brinell.Samples.Maui.App.Platforms.Windows.Handlers;

// ---------------------------------------------------------------------------
// PHASE 0 NEGATIVE RESULT - DO NOT REGISTER THESE HANDLERS.
//
// SwipeView and RefreshView map to the WinUI SwipeControl and RefreshContainer,
// which already supply their own AutomationPeers. Overriding OnCreateAutomationPeer
// on them does not merely fail to help - it collapses the ENTIRE UIA tree for the
// app: with these registered, every probe subject including the AutomationContainer
// control group became unaddressable, and the app window exposed nothing.
//
// The code is kept, unregistered, so the next person does not spend the same hour
// rediscovering it. If SwipeView/RefreshView scoping is ever needed, the route is
// almost certainly to wrap their CONTENT in an AutomationContainer rather than to
// touch the peer of the WinUI control itself.
// ---------------------------------------------------------------------------

/// <summary>Do not register. See the file header.</summary>
public class AutomationSwipeControl : SwipeControl
{
    protected override AutomationPeer OnCreateAutomationPeer()
        => new ContentViewAutomationPeer(this);
}

/// <summary>Do not register. See the file header.</summary>
public class AutomationSwipeViewHandler : SwipeViewHandler
{
    protected override SwipeControl CreatePlatformView() => new AutomationSwipeControl();
}

/// <summary>Do not register. See the file header.</summary>
public class AutomationRefreshContainer : RefreshContainer
{
    protected override AutomationPeer OnCreateAutomationPeer()
        => new ContentViewAutomationPeer(this);
}

/// <summary>Do not register. See the file header.</summary>
public class AutomationRefreshViewHandler : RefreshViewHandler
{
    protected override RefreshContainer CreatePlatformView() => new AutomationRefreshContainer();
}

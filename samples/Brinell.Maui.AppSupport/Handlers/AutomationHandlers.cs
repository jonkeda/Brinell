using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Brinell.Maui.AppSupport.Handlers;

/// <summary>
/// Layout handler returning a peer-bearing panel, so every layout type (Grid,
/// VerticalStackLayout, HorizontalStackLayout, StackLayout, FlexLayout,
/// AbsoluteLayout) exposes its AutomationId.
/// </summary>
/// <remarks>
/// Registered against the base <c>Layout</c> type, which covers every subclass at
/// once — one registration, all layouts.
/// </remarks>
public class AutomationLayoutHandler : LayoutHandler
{
    protected override LayoutPanel CreatePlatformView() => new AutomationLayoutPanel();
}

/// <summary>
/// ContentView handler returning a peer-bearing panel.
/// </summary>
public class AutomationContentViewHandler : ContentViewHandler
{
    protected override ContentPanel CreatePlatformView() => new AutomationContentPanel();
}

/// <summary>
/// Border handler returning a peer-bearing panel.
/// </summary>
/// <remarks>
/// Border maps to <c>ContentPanel</c> — the same platform view ContentView uses —
/// but MAUI registers <c>BorderHandler</c> separately, so the ContentView
/// registration does not cover it. Without this, a Border's AutomationId is
/// invisible.
/// </remarks>
public class AutomationBorderHandler : BorderHandler
{
    protected override ContentPanel CreatePlatformView() => new AutomationContentPanel();
}

/// <summary>
/// Page handler returning a peer-bearing panel, so a page's own AutomationId is visible.
/// </summary>
/// <remarks>
/// <c>ContentPage</c> maps to the same <c>ContentPanel</c> that ContentView and Border use, and
/// like them it carries no AutomationPeer of its own. Without this a page cannot be located by
/// its <c>AutomationId</c>, so a page object has to identify itself by some child control
/// instead — which fails as soon as that child scrolls out of view.
/// </remarks>
public class AutomationPageHandler : PageHandler
{
    protected override ContentPanel CreatePlatformView() => new AutomationContentPanel();
}

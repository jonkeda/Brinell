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

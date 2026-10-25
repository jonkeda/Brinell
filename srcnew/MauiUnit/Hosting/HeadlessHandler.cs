using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace MauiUnit.Hosting;

/// <summary>
/// A handler that maps nothing and makes no platform view. It exists to carry the context's
/// <see cref="IMauiContext"/>, where MAUI looks for services from an element: the application's (for
/// <c>FindDispatcher</c>), and a Shell's, because Shell makes a <c>ContentTemplate</c>'s page with
/// <c>ActivatorUtilities</c> over the nearest handler's services and stops looking at the window.
/// </summary>
/// <remarks>
/// It is never put on a <c>NavigationPage</c>: with a handler, a push waits for the platform to
/// report it finished. Controls get no handler at all; fake handlers for them (measure, layout) are
/// the roadmap's Phase 6.
/// </remarks>
internal sealed class HeadlessHandler(IMauiContext mauiContext) : IViewHandler
{
    public object? PlatformView => null;

    public object? ContainerView => null;

    public bool HasContainer { get; set; }

    public IElement? VirtualView { get; private set; }

    IView? IViewHandler.VirtualView => VirtualView as IView;

    public IMauiContext? MauiContext { get; private set; } = mauiContext;

    public void SetMauiContext(IMauiContext mauiContext) => MauiContext = mauiContext;

    public void SetVirtualView(IElement view) => VirtualView = view;

    public void UpdateValue(string property)
    {
    }

    public void Invoke(string command, object? args = null)
    {
    }

    public void DisconnectHandler() => VirtualView = null;

    public Size GetDesiredSize(double widthConstraint, double heightConstraint) => Size.Zero;

    public void PlatformArrange(Rect frame)
    {
    }
}

/// <summary>The context's services as MAUI's <see cref="IMauiContext"/>; no handlers are registered.</summary>
internal sealed class HeadlessMauiContext(IServiceProvider services) : IMauiContext
{
    public IServiceProvider Services { get; } = services;

    public IMauiHandlersFactory Handlers
        => throw new NotSupportedException("MauiUnit makes no control handlers (see the proposal's Phase 6).");
}

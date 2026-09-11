using Brinell.Uia;
using Brinell.Uia.Provider;
using Microsoft.Maui.Controls;

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// One MAUI element, as the bridge sees it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Holds the element weakly.</b> The bridge outlives any single page, and a strong reference
/// here would keep every element that ever declared a verb alive for the life of the window -
/// along with its page, its view model and whatever they reference. The unregister on
/// <c>Unloaded</c> is the tidy path; the weak reference is what makes the untidy paths harmless.
/// </para>
/// <para>
/// Every method is called from a UI Automation dispatch on an arbitrary thread. Marshalling to
/// the UI thread, and bounding the wait, is <see cref="MauiVerbDispatcher"/>'s job.
/// </para>
/// </remarks>
internal sealed class MauiVerbTarget : IBrinellVerbTarget
{
    private readonly WeakReference<VisualElement> _element;
    private readonly WeakReference<IBrinellGestureSink>? _sink;

    internal MauiVerbTarget(
        VisualElement element,
        string automationId,
        IReadOnlyCollection<BrinellVerb> capabilities,
        IBrinellGestureSink? sink)
    {
        _element = new WeakReference<VisualElement>(element);
        _sink = sink is null ? null : new WeakReference<IBrinellGestureSink>(sink);

        AutomationId = automationId;
        Capabilities = capabilities;
    }

    /// <inheritdoc/>
    public string AutomationId { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<BrinellVerb> Capabilities { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// A handler is the test that the element is realised on this platform. An element that is
    /// constructed but never laid out has none, and acting on it would succeed against
    /// something no user could see.
    /// </remarks>
    public bool IsAvailable
        => _element.TryGetTarget(out var element) && element.Handler is not null;

    /// <inheritdoc/>
    public int Invoke(BrinellVerb verb, int arg1, int arg2)
    {
        if (!_element.TryGetTarget(out var element))
        {
            return HResults.UIA_E_ELEMENTNOTAVAILABLE;
        }

        return MauiVerbDispatcher.Invoke(element, Sink(), verb, arg1, arg2);
    }

    /// <inheritdoc/>
    public int Exchange(BrinellVerb verb, string argument, out string result)
    {
        result = string.Empty;

        if (!_element.TryGetTarget(out var element))
        {
            return HResults.UIA_E_ELEMENTNOTAVAILABLE;
        }

        return MauiVerbDispatcher.Exchange(element, Sink(), verb, argument, out result);
    }


    /// <summary>
    /// Whether this target stands for that element.
    /// </summary>
    /// <remarks>
    /// Asked when an element unloads, so a page leaving the screen cannot remove the
    /// registration that the page arriving in its place has already made under the same
    /// <c>AutomationId</c>. See <c>BrinellUiaBridge.Unregister</c>.
    /// </remarks>
    /// <param name="element">The element being withdrawn.</param>
    /// <returns>Whether this target is for that element.</returns>
    internal bool Owns(VisualElement element)
        => _element.TryGetTarget(out var mine) && ReferenceEquals(mine, element);

    private IBrinellGestureSink? Sink()
        => _sink is not null && _sink.TryGetTarget(out var sink) ? sink : null;
}

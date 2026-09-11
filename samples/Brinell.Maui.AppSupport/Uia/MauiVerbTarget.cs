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
    private readonly VerbPlan _plan;

    internal MauiVerbTarget(VisualElement element, string automationId, VerbPlan plan)
    {
        _element = new WeakReference<VisualElement>(element);
        _plan = plan;

        AutomationId = automationId;
    }

    /// <inheritdoc/>
    public string AutomationId { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// The verbs that <b>bound</b>, not the verbs that were declared. Those were the same thing
    /// until the bindings existed, which is why an element could once advertise a verb its
    /// recognizers could not serve and then refuse it on use.
    /// </remarks>
    public IReadOnlyCollection<BrinellVerb> Capabilities => _plan.Capabilities;

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

        return MauiVerbDispatcher.Invoke(element, _plan, verb, arg1, arg2);
    }

    /// <inheritdoc/>
    public int Exchange(BrinellVerb verb, string argument, out string result)
    {
        result = string.Empty;

        if (!_element.TryGetTarget(out var element))
        {
            return HResults.UIA_E_ELEMENTNOTAVAILABLE;
        }

        return MauiVerbDispatcher.Exchange(element, _plan, verb, argument, out result);
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
}

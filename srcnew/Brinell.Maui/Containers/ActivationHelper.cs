namespace Brinell.Maui.Containers;

/// <summary>
/// The activation ladder: how an element is asked to perform its action.
/// </summary>
/// <remarks>
/// <para>
/// Windows UIA reaches a control's command more reliably than a synthetic pointer click,
/// which can be swallowed by an overlay or land on the wrong visual child. On platforms
/// without these patterns - Appium on Android and iOS - every probe reports unsupported and
/// the caller falls through to <see cref="IElement{TSelf}.Click"/>, which is the correct
/// mobile behaviour.
/// </para>
/// <para>
/// A static helper for the same reason as <see cref="ScrollHelper"/>: controls, collections
/// and items all need this ladder and C# allows one base class, so the mechanics live here
/// and each base delegates.
/// </para>
/// <para>
/// LegacyIAccessible is deliberately <b>not</b> in the ladder. A WinUI toggle advertises it
/// and its <c>DoDefaultAction</c> reports success without changing the control's state, so
/// including it makes a click silently do nothing on a Switch.
/// </para>
/// </remarks>
public static class ActivationHelper
{
    /// <summary>
    /// Activates the element through an automation pattern, when the platform exposes one.
    /// </summary>
    /// <remarks>
    /// Deliberately does not catch exceptions: a pattern that is present but fails is a real
    /// fault, and swallowing it turns a broken click into an unrelated assertion failure
    /// later. A caller walking a list of candidates - where a failure means "wrong element"
    /// rather than "broken" - catches around this call.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True when a pattern was available and reported success.</returns>
    public static bool TryActivateByPattern(IMauiElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (element is ISelectionItemPatternElement { SupportsSelectionItemPattern: true } selectionItem
            && selectionItem.SelectItemPattern())
        {
            return true;
        }

        if (element is IInvokePatternElement { SupportsInvokePattern: true } invoke
            && invoke.InvokePattern())
        {
            return true;
        }

        return false;
    }
}

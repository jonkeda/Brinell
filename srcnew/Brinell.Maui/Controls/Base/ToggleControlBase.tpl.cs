using Brinell.Maui.Configuration;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// Base class for MAUI controls with toggle capability.
/// Implements IToggleControlObject with Toggle, Check, Uncheck, SetChecked.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class ToggleControlBase<TScope> : ClickableControlBase<TScope>,
    IToggleControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new toggle control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    protected ToggleControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new toggle control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    protected ToggleControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// A toggle is toggled, not invoked.
    /// </summary>
    /// <remarks>
    /// MAUI's <c>Switch</c> and <c>CheckBox</c> expose Toggle and neither Invoke nor
    /// SelectionItem, so the inherited <see cref="IMauiElement.Invoke"/> would be the wrong
    /// question to ask. <c>RadioButton</c> shares this base and overrides again, because being
    /// chosen from a group is not the same operation as being flipped.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected override void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);
        element.Toggle();
    }

    /// <summary>
    /// Performs toggle on pre-found element, and confirms the state actually changed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One operation and one check, where this used to be three rungs - the Toggle pattern, then
    /// the activation ladder, then a Space keystroke - each tried in turn until the state moved.
    /// </para>
    /// <para>
    /// <b>The check stays and the rungs go, and the difference matters.</b> Verifying the
    /// outcome is not a fallback: it is how this control catches a platform that accepts the
    /// call and does nothing, which is exactly what <c>LegacyIAccessible</c> did to a Switch and
    /// what a <c>ToolbarItem</c> does to Invoke. Trying a different route afterwards is what made
    /// it a ladder, and that part is gone: if a toggle does not toggle, that is a fact worth
    /// reporting rather than working around.
    /// </para>
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ToggleCore(IMauiElement element, int? timeoutMs = null)
    {
        var beforeState = IsCheckedCore(element);
        EnsureVisible(element, timeoutMs ?? DefaultTimeoutMs);

        element.Toggle();

        if (!WaitForStateChange(element, beforeState, timeoutMs))
        {
            throw new InvalidOperationException(
                $"The element accepted Toggle and its checked state did not change, so nothing "
                + $"the test asked for happened. It was {Describe(beforeState)} before and after. "
                + $"Locator: {Locator}");
        }
    }

    private static string Describe(bool? state)
        => state switch { true => "checked", false => "unchecked", _ => "in an unknown state" };

    private bool WaitForStateChange(IMauiElement element, bool? beforeState, int? timeoutMs = null)
    {
        if (beforeState == null)
            return true;
        return RunWaitWithElement(!beforeState, e => IsCheckedCore(e) != beforeState, timeoutMs);
    }

    /// <summary>
    /// Sets checked state on pre-found element. No-op if already in the target state.
    /// </summary>
    /// <remarks>
    /// Prefers the platform's set-state command over a toggle: asking for the state you want
    /// is idempotent, while toggling depends on the state read beforehand still being true
    /// when the toggle lands. Falls back to <see cref="ToggleCore"/> where no such command
    /// exists — which is every mobile platform, since neither Android nor iOS exposes one.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="checked">The desired checked state. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SetCheckedCore(IMauiElement element, bool? @checked, int? timeoutMs = null)
    {
        if (@checked == null)
            return;

        var current = IsCheckedCore(element);
        if (current == @checked)
            return;

        if (TrySetStateByPattern(element, @checked.Value, timeoutMs))
            return;

        ToggleCore(element, timeoutMs);
    }

    /// <summary>
    /// Sets the state through the platform's Toggle pattern, when it offers one.
    /// </summary>
    /// <remarks>
    /// The state change is confirmed rather than assumed: a pattern that reports success
    /// without moving the control would otherwise leave the caller believing a state was set
    /// that was not.
    /// </remarks>
    private bool TrySetStateByPattern(IMauiElement element, bool @checked, int? timeoutMs)
    {
        if (element is not ITogglePatternElement { SupportsTogglePattern: true } toggle)
            return false;

        if (!toggle.SetToggleStatePattern(@checked))
            return false;

        return RunWaitWithElement(@checked, e => IsCheckedCore(e) == @checked, timeoutMs);
    }

    /// <summary>
    /// Gets checked state from pre-found element.
    /// Reads the toggle pattern, then selection - the two ways a platform publishes it.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if checked, false if unchecked, null if element is null.</returns>
    protected virtual bool? IsCheckedCore(IMauiElement? element)
    {
        if (element == null) return null;

        if (element is ITogglePatternElement { SupportsTogglePattern: true } toggle)
        {
            var checkedViaPattern = toggle.IsTogglePatternChecked();
            if (checkedViaPattern != null)
                return checkedViaPattern;
        }

        // A radio button reports itself through selection rather than through a toggle, and
        // Selected reads the selection pattern on Windows and the selected flag on Android.
        return element.Selected;
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Sets the control to the checked state. Convenience alias for SetChecked(true).
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope Check(int? timeoutMs = null) => SetChecked(true, timeoutMs);

    /// <summary>
    /// Sets the control to the unchecked state. Convenience alias for SetChecked(false).
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope Uncheck(int? timeoutMs = null) => SetChecked(false, timeoutMs);

    /// <summary>
    /// Asserts the element is checked. Throws if it isn't.
    /// </summary>
    /// <param name="message">Optional custom message for the assertion failure.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertChecked(string? message, int? timeoutMs = null)
        => AssertChecked(true, message, timeoutMs);

    #endregion
}

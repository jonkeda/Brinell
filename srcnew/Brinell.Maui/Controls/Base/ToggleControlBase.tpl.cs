using Brinell.Maui.Configuration;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// Base class for MAUI controls with toggle capability.
/// Implements IToggleControlObject with Toggle, Check, Uncheck, SetChecked, and Click.
/// </summary>
/// <remarks>
/// <para>
/// <b>Focusable, not clickable</b> (step 102, option B). This used to derive from
/// <c>ClickableControlBase</c>, which gave every switch, check box and radio button a generated
/// <c>DoubleClick</c>, <c>RightClick</c>, <c>Hover</c>, <c>LongPress</c>, <c>Press</c> and
/// <c>IsClickable</c>/<c>WaitClickable</c>/<c>AssertClickable</c>. None of them means anything
/// for a toggle, the first four exist only as real pointer input on Windows - which the quiet
/// default refuses - and <c>Click</c> had to be overridden to mean something else anyway.
/// </para>
/// <para>
/// <c>Click</c> stays, declared here for what it is on a toggle: flipping it.
/// </para>
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class ToggleControlBase<TScope> : FocusableControlBase<TScope>,
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
    /// Clicking a toggle toggles it.
    /// </summary>
    /// <remarks>
    /// MAUI's <c>Switch</c> and <c>CheckBox</c> expose Toggle and neither Invoke nor
    /// SelectionItem. <c>RadioButton</c> overrides this, because being chosen from a group is not
    /// the same operation as being flipped.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureEnabledCore(element);
        element.Toggle();
    }

    /// <summary>Refuses to act on a disabled toggle.</summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void EnsureEnabledCore(IMauiElement element)
    {
        if (IsEnabledCore(element) != true)
        {
            throw new TimeoutException($"Element was not enabled. Locator: {Locator}");
        }
    }

    /// <inheritdoc />
    protected override void EnsureReadyForActionCore(IMauiElement element)
        => EnsureEnabledCore(element);

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
    /// <para>
    /// <b>Toggles, and knows nothing else.</b> This used to try the platform's set-state command
    /// first and fall through to a toggle on any <c>false</c> - a two-rung ladder in a base that
    /// three different controls share. Which route a control has is that control's knowledge, so
    /// <c>Switch</c> and <c>CheckBox</c> override this to set the state directly where the
    /// platform can, and <c>RadioButton</c> overrides it because a radio button cannot be
    /// unchecked at all (step 108).
    /// </para>
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="checked">The desired checked state. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SetCheckedCore(IMauiElement element, bool? @checked, int? timeoutMs = null)
    {
        if (@checked == null)
            return;

        if (IsCheckedCore(element) == @checked)
            return;

        ToggleCore(element, timeoutMs);
    }

    /// <summary>
    /// Sets the state through the platform's own set-state command, and confirms it took.
    /// </summary>
    /// <remarks>
    /// For the controls that have such a command. A command the platform accepts without moving
    /// the control throws, as <see cref="ToggleCore"/> does: it is never followed by a toggle, which
    /// is what made the old route a ladder.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="checked">The desired checked state.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected void SetCheckedDirectly(IMauiElement element, bool @checked, int? timeoutMs)
    {
        element.SetChecked(@checked);

        if (!RunWaitWithElement(@checked, e => IsCheckedCore(e) == @checked, timeoutMs))
        {
            throw new InvalidOperationException(
                $"The element accepted SetChecked({@checked}) and its checked state did not change. "
                + $"Locator: {Locator}");
        }
    }

    /// <summary>
    /// Gets checked state from pre-found element.
    /// </summary>
    /// <remarks>
    /// <see cref="IMauiElement.Checked"/>, and only that. It used to read the toggle state and fall
    /// back to <c>Selected</c>, which on Windows fell back to the toggle state again - so a control
    /// could not tell which of the two the platform had published (step 105a). A radio button,
    /// which is chosen rather than checked, overrides this.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if checked, false if unchecked, null if unknown or element is null.</returns>
    protected virtual bool? IsCheckedCore(IMauiElement? element) => element?.Checked;

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

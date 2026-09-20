using Brinell.Maui.Configuration;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// Base class for MAUI controls with toggle capability.
/// Implements IToggleControlObject with Toggle, Check, Uncheck, SetChecked, and Click.
/// </summary>
/// <remarks>
/// <c>Click</c> on a toggle flips it.
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
            throw new ElementNotReadyException(Locator, NotReadyReason.Disabled);
        }
    }

    /// <inheritdoc />
    protected override void EnsureReadyForActionCore(IMauiElement element)
        => EnsureEnabledCore(element);

    /// <summary>
    /// Performs toggle on pre-found element, and confirms the state actually changed.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ToggleCore(IMauiElement element, int? timeoutMs = null)
    {
        var beforeState = IsCheckedCore(element);

        element.Toggle();

        if (beforeState == null)
        {
            return;
        }

        var confirmation = Confirm(() => IsCheckedCore(element), state => state != beforeState, timeoutMs);
        if (!confirmation.IsConfirmed)
        {
            throw confirmation.Failure(Locator, "Toggle", lastError => new InvalidOperationException(
                $"The element accepted Toggle and its checked state did not change, so nothing "
                + $"the test asked for happened. It was {Describe(beforeState)} before and after. "
                + $"Locator: {Locator}", lastError));
        }
    }

    private static string Describe(bool? state)
        => state switch { true => "checked", false => "unchecked", _ => "in an unknown state" };

    /// <summary>
    /// Sets checked state on pre-found element. No-op if already in the target state.
    /// </summary>
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
    /// <param name="element">The pre-found element.</param>
    /// <param name="checked">The desired checked state.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected void SetCheckedDirectly(IMauiElement element, bool @checked, int? timeoutMs)
    {
        element.SetChecked(@checked);

        var confirmation = Confirm(() => IsCheckedCore(element), state => state == @checked, timeoutMs);
        if (!confirmation.IsConfirmed)
        {
            throw confirmation.Failure(Locator, "SetChecked", lastError => new InvalidOperationException(
                $"The element accepted SetChecked({@checked}) and its checked state did not change. "
                + $"Locator: {Locator}", lastError));
        }
    }

    /// <summary>
    /// Gets checked state from pre-found element.
    /// </summary>
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

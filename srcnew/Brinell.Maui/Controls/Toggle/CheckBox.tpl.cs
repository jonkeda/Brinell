namespace Brinell.Maui.Controls.Toggle;

/// <summary>
/// MAUI CheckBox control with toggle capability.
/// Inherits Toggle, Check, Uncheck, IsChecked, AssertChecked from ToggleControlBase.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class CheckBox<TScope> : Base.ToggleControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new checkbox control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the checkbox element.</param>
    public CheckBox(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new checkbox control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public CheckBox(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <inheritdoc />
    /// <remarks>
    /// <b>One question, then one route.</b> Where the platform can set the state directly -
    /// Windows, through the Toggle pattern against a fresh read - it is asked for the state
    /// wanted, which is idempotent. Otherwise the control toggles, which is how Android and iOS
    /// change it. Never both (step 108).
    /// </remarks>
    protected override void SetCheckedCore(IMauiElement element, bool? @checked, int? timeoutMs = null)
    {
        if (@checked == null || IsCheckedCore(element) == @checked)
            return;

        if (element.SupportsSetChecked)
        {
            SetCheckedDirectly(element, @checked.Value, timeoutMs);
            return;
        }

        ToggleCore(element, timeoutMs);
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Puts the checkbox in the checked state, clicking it only if it is not already
    /// checked. Safe to call as an arrange step regardless of the current state.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope CheckOn(int? timeoutMs = null) => SetChecked(true, timeoutMs);

    /// <summary>
    /// Puts the checkbox in the unchecked state, clicking it only if it is not already
    /// unchecked. Safe to call as an arrange step regardless of the current state.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope CheckOff(int? timeoutMs = null) => SetChecked(false, timeoutMs);

    #endregion
}

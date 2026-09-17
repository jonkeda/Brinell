namespace Brinell.Maui.Controls.Toggle;

/// <summary>
/// MAUI RadioButton control with Select terminology.
/// Provides IsSelected, Select alias methods. Note: Radio buttons cannot be directly deselected.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class RadioButton<TScope> : Base.ToggleControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new radio button control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the radio button element.</param>
    public RadioButton(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new radio button control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public RadioButton(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// A radio button is chosen from a group, not flipped.
    /// </summary>
    /// <remarks>
    /// Selecting one member deselects the rest; toggling does not.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected override void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureEnabledCore(element);
        element.Select();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Selects the radio button, and throws when the selection does not take.
    /// </remarks>
    protected override void ToggleCore(IMauiElement element, int? timeoutMs = null)
    {
        var before = IsCheckedCore(element);
        EnsureVisible(element, timeoutMs ?? DefaultTimeoutMs);

        element.Select();

        if (before != true && IsCheckedCore(element) != true)
        {
            throw new InvalidOperationException(
                $"The radio button accepted Select and did not become selected. Locator: {Locator}");
        }
    }

    /// <summary>
    /// Reads the selected state from the pre-found element.
    /// RadioButton terminology for the underlying checked state.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if selected, false if not, null if element is null.</returns>
    protected virtual bool? IsSelectedCore(IMauiElement? element) => IsCheckedCore(element);

    /// <inheritdoc />
    /// <remarks>
    /// Checked where the platform publishes it - Android's <c>checked</c>, Windows' Toggle
    /// pattern - and chosen otherwise, because a radio button is fundamentally one of a group.
    /// </remarks>
    protected override bool? IsCheckedCore(IMauiElement? element)
        => element == null ? null : element.Checked ?? element.Selected;

    /// <inheritdoc />
    /// Checking a radio button selects it. Unchecking one throws <see cref="NotSupportedException"/>:
    /// select another member of the group instead.
    /// </remarks>
    protected override void SetCheckedCore(IMauiElement element, bool? @checked, int? timeoutMs = null)
    {
        if (@checked == null || IsCheckedCore(element) == @checked)
            return;

        if (@checked == false)
        {
            throw new NotSupportedException(
                "A radio button cannot be unchecked; select another one in its group instead. "
                + $"Locator: {Locator}");
        }

        ToggleCore(element, timeoutMs);
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Selects this radio button.
    /// Alias for Check(). Selecting a radio button will deselect others in the same group.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope Select(int? timeoutMs = null) => Check(timeoutMs);

    /// <summary>
    /// Asserts the radio button is selected.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertSelected(string? message, int? timeoutMs = null)
        => AssertSelected(true, message ?? "Expected radio button to be selected", timeoutMs);

    /// <summary>
    /// Asserts the radio button is not selected.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertNotSelected(string? message = null, int? timeoutMs = null)
        => AssertSelected(false, message ?? "Expected radio button not to be selected", timeoutMs);

    #endregion
}

namespace Brinell.Maui.Controls.Toggle;

/// <summary>
/// MAUI Switch control with On/Off terminology.
/// Provides IsOn, TurnOn, TurnOff alias methods in addition to inherited toggle methods.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Switch<TScope> : Base.ToggleControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new switch control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the switch element.</param>
    public Switch(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new switch control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public Switch(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Reads the On/Off state from the pre-found element.
    /// Switch terminology for the underlying checked state.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if on, false if off, null if element is null.</returns>
    protected virtual bool? IsOnCore(IMauiElement? element) => IsCheckedCore(element);

    /// <summary>
    /// Sets the On/Off state on the pre-found element.
    /// Switch terminology for the underlying checked state.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="on">The desired state. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SetOnCore(IMauiElement element, bool? on, int? timeoutMs = null)
        => SetCheckedCore(element, on, timeoutMs);

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Turns the switch on. Alias for Check().
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope TurnOn(int? timeoutMs = null) => SetOn(true, timeoutMs);

    /// <summary>
    /// Turns the switch off. Alias for Uncheck().
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope TurnOff(int? timeoutMs = null) => SetOn(false, timeoutMs);

    /// <summary>
    /// Asserts the switch is on.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertOn(string? message, int? timeoutMs = null)
        => AssertOn(true, message ?? "Expected switch to be on", timeoutMs);

    /// <summary>
    /// Asserts the switch is off.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertOff(string? message = null, int? timeoutMs = null)
        => AssertOn(false, message ?? "Expected switch to be off", timeoutMs);

    #endregion
}

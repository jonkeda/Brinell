namespace Brinell.Maui.Controls.Toggle;

/// <summary>
/// MAUI Switch control with On/Off terminology.
/// Provides IsOn, TurnOn, TurnOff alias methods in addition to inherited toggle methods.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class Switch<TScope> : ToggleControlBase<TScope>
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

    #region Switch-Specific Alias Methods

    /// <summary>
    /// Checks if the switch is in the On position.
    /// Alias for IsChecked().
    /// </summary>
    /// <returns>True if on, false if off, null if element not found.</returns>
    public bool? IsOn() => IsChecked();

    /// <summary>
    /// Turns the switch on.
    /// Alias for Check().
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope TurnOn(int? timeoutMs = null) => Check(timeoutMs);

    /// <summary>
    /// Turns the switch off.
    /// Alias for Uncheck().
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope TurnOff(int? timeoutMs = null) => Uncheck(timeoutMs);

    /// <summary>
    /// Waits for the switch to be in the expected On/Off state.
    /// Alias for WaitChecked().
    /// </summary>
    /// <param name="expected">Expected state (true = on, false = off).</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    public bool WaitOn(bool? expected, int? timeoutMs = null) => WaitChecked(expected, timeoutMs);

    /// <summary>
    /// Asserts the switch is on.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertOn(string? message = null, int? timeoutMs = null)
        => AssertChecked(true, message ?? "Expected switch to be on", timeoutMs);

    /// <summary>
    /// Asserts the switch is off.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertOff(string? message = null, int? timeoutMs = null)
        => AssertChecked(false, message ?? "Expected switch to be off", timeoutMs);

    #endregion
}

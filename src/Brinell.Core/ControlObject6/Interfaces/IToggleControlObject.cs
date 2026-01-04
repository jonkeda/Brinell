namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for toggle controls (CheckBox, Switch, RadioButton).
/// Provides checked state operations.
/// </summary>
public interface IToggleControlObject : IInteractiveControlObject
{
    #region Checked State

    /// <summary>
    /// Gets whether the control is checked/on.
    /// </summary>
    bool IsChecked();

    /// <summary>
    /// Waits for the control to reach the expected checked state.
    /// If expected is null, returns true immediately (skip operation).
    /// </summary>
    bool WaitChecked(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Checks that the control is in the expected checked state.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void CheckChecked(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts the control is in the expected checked state.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Toggle Actions

    /// <summary>
    /// Toggles the control state (checked to unchecked or vice versa).
    /// </summary>
    void Toggle(int? timeoutMs = null);

    /// <summary>
    /// Sets the control to checked state.
    /// If the control is already checked, does nothing.
    /// </summary>
    void Check(int? timeoutMs = null);

    /// <summary>
    /// Sets the control to unchecked state.
    /// If the control is already unchecked, does nothing.
    /// </summary>
    void Uncheck(int? timeoutMs = null);

    /// <summary>
    /// Sets the control to the specified checked state.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void SetChecked(bool? expected, int? timeoutMs = null);

    #endregion
}

namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for toggle/boolean controls (checkbox, switch, radio button).
/// </summary>
public interface IToggleControl : IControlObject
{
    /// <summary>
    /// Check if the control is checked/on.
    /// </summary>
    bool IsChecked();
    
    /// <summary>
    /// Toggle the control state.
    /// </summary>
    void Toggle();
    
    /// <summary>
    /// Set the control to checked/on.
    /// </summary>
    void Check();
    
    /// <summary>
    /// Set the control to unchecked/off.
    /// </summary>
    void Uncheck();
    
    /// <summary>
    /// Set checked state to specific value.
    /// </summary>
    void SetChecked(bool value);
    
    /// <summary>
    /// Wait for checked state.
    /// </summary>
    bool WaitChecked(bool expected = true, int? timeoutMs = null);
    
    /// <summary>
    /// Assert control is checked.
    /// </summary>
    void AssertChecked(string? message = null);
    
    /// <summary>
    /// Assert control is unchecked.
    /// </summary>
    void AssertUnchecked(string? message = null);
}

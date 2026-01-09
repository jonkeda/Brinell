namespace Brinell.Core.Interfaces;

/// <summary>
/// Toggle state capability for checkboxes, switches, radio buttons.
/// </summary>
public interface IToggleControlObject : IControlObject
{
    /// <summary>
    /// Check if the control is in checked/on state.
    /// Returns null if element not found.
    /// </summary>
    bool? IsChecked();
    
    /// <summary>
    /// Toggle the control state.
    /// </summary>
    void Toggle(int? timeoutMs = null);
    
    /// <summary>
    /// Set the control to checked/unchecked state.
    /// If checked is null, returns immediately (skip).
    /// </summary>
    void SetChecked(bool? @checked, int? timeoutMs = null);
    
    /// <summary>
    /// Set to checked state (convenience method).
    /// </summary>
    void Check(int? timeoutMs = null);
    
    /// <summary>
    /// Set to unchecked state (convenience method).
    /// </summary>
    void Uncheck(int? timeoutMs = null);
    
    /// <summary>
    /// Assert checked state matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until checked state matches expected value.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitChecked(bool? expected, int? timeoutMs = null);
}

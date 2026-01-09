namespace Brinell.Core.Interfaces;

/// <summary>
/// Numeric range capability for sliders, steppers, progress bars.
/// </summary>
public interface IRangeControlObject : IControlObject
{
    /// <summary>
    /// Get the current value.
    /// Returns null if element not found.
    /// </summary>
    double? GetValue(int? timeoutMs = null);
    
    /// <summary>
    /// Set the value.
    /// If value is null, returns immediately (skip).
    /// </summary>
    void SetValue(double? value, int? timeoutMs = null);
    
    /// <summary>
    /// Get the minimum allowed value.
    /// Returns null if element not found.
    /// </summary>
    double? GetMinimum(int? timeoutMs = null);
    
    /// <summary>
    /// Get the maximum allowed value.
    /// Returns null if element not found.
    /// </summary>
    double? GetMaximum(int? timeoutMs = null);
    
    /// <summary>
    /// Get the step/increment value.
    /// Returns null if element not found.
    /// </summary>
    double? GetStep(int? timeoutMs = null);
    
    /// <summary>
    /// Assert value equals expected (within tolerance).
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertValue(double? expected, double tolerance = 0.001, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until value equals expected (within tolerance).
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitValue(double? expected, double tolerance = 0.001, int? timeoutMs = null);
    
    /// <summary>
    /// Increment value by step amount.
    /// </summary>
    void Increment(int? timeoutMs = null);
    
    /// <summary>
    /// Decrement value by step amount.
    /// </summary>
    void Decrement(int? timeoutMs = null);
}

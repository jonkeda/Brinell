namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for controls with numeric range values (slider, progress bar).
/// </summary>
public interface IRangeControl : IControlObject
{
    /// <summary>
    /// Get the current value.
    /// </summary>
    double GetValue();
    
    /// <summary>
    /// Get the minimum value.
    /// </summary>
    double GetMinimum();
    
    /// <summary>
    /// Get the maximum value.
    /// </summary>
    double GetMaximum();
    
    /// <summary>
    /// Set the value.
    /// </summary>
    void SetValue(double value);
    
    /// <summary>
    /// Increment the value.
    /// </summary>
    void Increment();
    
    /// <summary>
    /// Decrement the value.
    /// </summary>
    void Decrement();
    
    /// <summary>
    /// Assert value equals expected (within tolerance).
    /// </summary>
    void AssertValue(double expected, double tolerance = 0.001, string? message = null);
}

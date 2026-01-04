namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for range controls (Slider, Stepper).
/// Provides value and range operations.
/// </summary>
public interface IRangeControlObject : IInteractiveControlObject
{
    #region Value

    /// <summary>
    /// Gets the current value.
    /// </summary>
    double GetValue(int? timeoutMs = null);

    /// <summary>
    /// Sets the value.
    /// If value is null, does nothing (skip operation).
    /// </summary>
    void SetValue(double? value, int? timeoutMs = null);

    /// <summary>
    /// Waits for the value to reach the expected value (within tolerance).
    /// If expected is null, returns true immediately (skip operation).
    /// </summary>
    bool WaitValue(double? expected, double tolerance = 0.01, int? timeoutMs = null);

    /// <summary>
    /// Asserts the value matches the expected value (within tolerance).
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertValue(double? expected, double tolerance = 0.01, string? message = null, int? timeoutMs = null);

    #endregion

    #region Range

    /// <summary>
    /// Gets the minimum value.
    /// </summary>
    double GetMinimum(int? timeoutMs = null);

    /// <summary>
    /// Gets the maximum value.
    /// </summary>
    double GetMaximum(int? timeoutMs = null);

    /// <summary>
    /// Gets the value range (min, max).
    /// </summary>
    (double minimum, double maximum) GetRange(int? timeoutMs = null);

    /// <summary>
    /// Gets the current value as a percentage (0.0 to 1.0).
    /// </summary>
    double GetValuePercent(int? timeoutMs = null);

    /// <summary>
    /// Sets the value as a percentage (0.0 to 1.0).
    /// If percent is null, does nothing (skip operation).
    /// </summary>
    void SetValuePercent(double? percent, int? timeoutMs = null);

    #endregion

    #region Step Actions

    /// <summary>
    /// Increases the value by one step.
    /// </summary>
    void Increase(int? timeoutMs = null);

    /// <summary>
    /// Decreases the value by one step.
    /// </summary>
    void Decrease(int? timeoutMs = null);

    /// <summary>
    /// Sets the value to the minimum.
    /// </summary>
    void SetToMinimum(int? timeoutMs = null);

    /// <summary>
    /// Sets the value to the maximum.
    /// </summary>
    void SetToMaximum(int? timeoutMs = null);

    #endregion
}

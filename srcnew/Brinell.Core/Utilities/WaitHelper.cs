using System.Diagnostics;

namespace Brinell.Core.Utilities;

/// <summary>
/// Helper class for wait/polling operations.
/// </summary>
public static class WaitHelper
{
    /// <summary>
    /// Wait for a condition to become true.
    /// </summary>
    /// <param name="condition">Condition to check.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <param name="pollingIntervalMs">Interval between checks in milliseconds.</param>
    /// <returns>True if condition became true, false if timeout.</returns>
    public static bool WaitFor(Func<bool> condition, int timeoutMs, int pollingIntervalMs = 100)
    {
        var sw = Stopwatch.StartNew();
        
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                if (condition())
                    return true;
            }
            catch
            {
                // Swallow exceptions and continue polling
            }
            
            Thread.Sleep(pollingIntervalMs);
        }
        
        // Final check
        try
        {
            return condition();
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Wait for a value getter to return a value that matches the predicate.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="getValue">Function to get the current value.</param>
    /// <param name="predicate">Predicate to check the value.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <param name="pollingIntervalMs">Interval between checks in milliseconds.</param>
    /// <returns>True if predicate became true, false if timeout.</returns>
    public static bool WaitFor<T>(Func<T?> getValue, Func<T?, bool> predicate, int timeoutMs, int pollingIntervalMs = 100)
    {
        var sw = Stopwatch.StartNew();
        
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                var value = getValue();
                if (predicate(value))
                    return true;
            }
            catch
            {
                // Swallow exceptions and continue polling
            }
            
            Thread.Sleep(pollingIntervalMs);
        }
        
        // Final check
        try
        {
            var value = getValue();
            return predicate(value);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Wait for a value getter to return a non-null value.
    /// </summary>
    /// <typeparam name="T">Type of value.</typeparam>
    /// <param name="getValue">Function to get the value.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <param name="pollingIntervalMs">Interval between checks in milliseconds.</param>
    /// <returns>The value if found, or default if timeout.</returns>
    public static T? WaitForValue<T>(Func<T?> getValue, int timeoutMs, int pollingIntervalMs = 100) where T : class
    {
        var sw = Stopwatch.StartNew();
        
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                var value = getValue();
                if (value != null)
                    return value;
            }
            catch
            {
                // Swallow exceptions and continue polling
            }
            
            Thread.Sleep(pollingIntervalMs);
        }
        
        // Final check
        try
        {
            return getValue();
        }
        catch
        {
            return default;
        }
    }
    
    /// <summary>
    /// Get elapsed time while waiting for a condition.
    /// </summary>
    /// <param name="condition">Condition to check.</param>
    /// <param name="timeoutMs">Maximum time to wait.</param>
    /// <param name="pollingIntervalMs">Polling interval.</param>
    /// <returns>Tuple of (success, elapsed milliseconds).</returns>
    public static (bool Success, int ElapsedMs) WaitForWithTiming(Func<bool> condition, int timeoutMs, int pollingIntervalMs = 100)
    {
        var sw = Stopwatch.StartNew();
        
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                if (condition())
                    return (true, (int)sw.ElapsedMilliseconds);
            }
            catch
            {
                // Swallow exceptions and continue polling
            }
            
            Thread.Sleep(pollingIntervalMs);
        }
        
        // Final check
        try
        {
            if (condition())
                return (true, (int)sw.ElapsedMilliseconds);
        }
        catch { }
        
        return (false, (int)sw.ElapsedMilliseconds);
    }
}

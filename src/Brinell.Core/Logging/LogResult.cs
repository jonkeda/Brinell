namespace Brinell.Core.Logging;

/// <summary>
/// Standard result values for CSV logging.
/// These provide consistent filtering in Excel.
/// </summary>
public enum LogResult
{
    /// <summary>
    /// Operation succeeded (actions, waits, assertions that pass).
    /// </summary>
    Ok,
    
    /// <summary>
    /// Operation failed (assertions, checks that fail).
    /// </summary>
    Fail,
    
    /// <summary>
    /// Exception occurred.
    /// </summary>
    Error,
    
    /// <summary>
    /// Informational message (status updates, navigation).
    /// </summary>
    Info,
    
    /// <summary>
    /// Operation was skipped (conditional logic).
    /// </summary>
    Skip
}

namespace Brinell.Maui.Testing.MultiDevice;

/// <summary>
/// Results from a multi-device test run.
/// </summary>
public class MultiDeviceTestResults
{
    /// <summary>
    /// Test name.
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// When the test run started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the test run completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Total duration of the test run.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// Individual device results.
    /// </summary>
    public List<DeviceTestResult> DeviceResults { get; set; } = new();

    /// <summary>
    /// Overall success (all devices passed).
    /// </summary>
    public bool AllPassed => DeviceResults.All(r => r.Passed);

    /// <summary>
    /// Number of passed devices.
    /// </summary>
    public int PassedCount => DeviceResults.Count(r => r.Passed);

    /// <summary>
    /// Number of failed devices.
    /// </summary>
    public int FailedCount => DeviceResults.Count(r => !r.Passed);

    /// <summary>
    /// Total device count.
    /// </summary>
    public int TotalCount => DeviceResults.Count;

    /// <summary>
    /// Get a summary string.
    /// </summary>
    public string Summary => $"{TestName}: {PassedCount}/{TotalCount} passed in {Duration.TotalSeconds:F1}s";

    /// <summary>
    /// Get failed device results.
    /// </summary>
    public IEnumerable<DeviceTestResult> FailedResults => DeviceResults.Where(r => !r.Passed);

    /// <summary>
    /// Get passed device results.
    /// </summary>
    public IEnumerable<DeviceTestResult> PassedResults => DeviceResults.Where(r => r.Passed);
}

/// <summary>
/// Result from a single device in a multi-device test run.
/// </summary>
public class DeviceTestResult
{
    /// <summary>
    /// Device configuration used.
    /// </summary>
    public DeviceConfiguration Device { get; set; } = new();

    /// <summary>
    /// Whether the test passed on this device.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Full exception if failed.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Test duration on this device.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Screenshots taken during test.
    /// </summary>
    public List<string> Screenshots { get; set; } = new();

    /// <summary>
    /// Log output from the test.
    /// </summary>
    public string? LogOutput { get; set; }

    /// <summary>
    /// Performance metrics captured.
    /// </summary>
    public Dictionary<string, double> Metrics { get; set; } = new();

    /// <summary>
    /// Create a passed result.
    /// </summary>
    public static DeviceTestResult Success(DeviceConfiguration device, TimeSpan duration) => new()
    {
        Device = device,
        Passed = true,
        Duration = duration
    };

    /// <summary>
    /// Create a failed result.
    /// </summary>
    public static DeviceTestResult Failure(DeviceConfiguration device, Exception ex, TimeSpan duration) => new()
    {
        Device = device,
        Passed = false,
        ErrorMessage = ex.Message,
        Exception = ex,
        Duration = duration
    };
}

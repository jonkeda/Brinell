using System.Diagnostics;

namespace Brinell.Maui.Testing.Performance;

/// <summary>
/// Metrics collected during performance monitoring.
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Name or description of the measured operation.
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// When the measurement started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the measurement ended.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Duration of the operation.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public double DurationMs => Duration.TotalMilliseconds;

    /// <summary>
    /// Memory usage at start (bytes).
    /// </summary>
    public long MemoryAtStart { get; set; }

    /// <summary>
    /// Memory usage at end (bytes).
    /// </summary>
    public long MemoryAtEnd { get; set; }

    /// <summary>
    /// Memory delta during operation (bytes).
    /// </summary>
    public long MemoryDelta => MemoryAtEnd - MemoryAtStart;

    /// <summary>
    /// CPU time used (if available).
    /// </summary>
    public TimeSpan? CpuTime { get; set; }

    /// <summary>
    /// Frame rate during operation (if measured).
    /// </summary>
    public double? FrameRate { get; set; }

    /// <summary>
    /// Number of frames dropped (if measured).
    /// </summary>
    public int? DroppedFrames { get; set; }

    /// <summary>
    /// Custom metrics.
    /// </summary>
    public Dictionary<string, double> CustomMetrics { get; set; } = new();

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Whether the operation met performance thresholds.
    /// </summary>
    public bool? MetThreshold { get; set; }

    /// <summary>
    /// The threshold that was used for comparison.
    /// </summary>
    public double? ThresholdMs { get; set; }

    /// <summary>
    /// Get a summary string.
    /// </summary>
    public override string ToString()
    {
        var threshold = MetThreshold.HasValue
            ? (MetThreshold.Value ? " ✓" : $" ✗ (threshold: {ThresholdMs}ms)")
            : "";
        return $"{OperationName}: {DurationMs:F1}ms{threshold}";
    }
}

/// <summary>
/// Metrics specific to navigation timing.
/// </summary>
public class NavigationMetrics : PerformanceMetrics
{
    /// <summary>
    /// Source page/route.
    /// </summary>
    public string FromPage { get; set; } = string.Empty;

    /// <summary>
    /// Destination page/route.
    /// </summary>
    public string ToPage { get; set; } = string.Empty;

    /// <summary>
    /// Navigation type (Push, Pop, Replace, etc.).
    /// </summary>
    public string NavigationType { get; set; } = "Push";

    /// <summary>
    /// Time until first render.
    /// </summary>
    public TimeSpan? TimeToFirstRender { get; set; }

    /// <summary>
    /// Time until interactive.
    /// </summary>
    public TimeSpan? TimeToInteractive { get; set; }

    /// <summary>
    /// Whether transition animation played.
    /// </summary>
    public bool AnimationPlayed { get; set; }

    /// <summary>
    /// Animation duration.
    /// </summary>
    public TimeSpan? AnimationDuration { get; set; }

    /// <summary>
    /// Get a summary string.
    /// </summary>
    public override string ToString()
    {
        return $"Navigation {FromPage} -> {ToPage}: {DurationMs:F1}ms ({NavigationType})";
    }
}

/// <summary>
/// Aggregated performance statistics.
/// </summary>
public class PerformanceStatistics
{
    /// <summary>
    /// All collected metrics.
    /// </summary>
    public List<PerformanceMetrics> Metrics { get; set; } = new();

    /// <summary>
    /// Total number of measurements.
    /// </summary>
    public int Count => Metrics.Count;

    /// <summary>
    /// Average duration in milliseconds.
    /// </summary>
    public double AverageMs => Metrics.Count > 0 ? Metrics.Average(m => m.DurationMs) : 0;

    /// <summary>
    /// Minimum duration in milliseconds.
    /// </summary>
    public double MinMs => Metrics.Count > 0 ? Metrics.Min(m => m.DurationMs) : 0;

    /// <summary>
    /// Maximum duration in milliseconds.
    /// </summary>
    public double MaxMs => Metrics.Count > 0 ? Metrics.Max(m => m.DurationMs) : 0;

    /// <summary>
    /// Median duration in milliseconds.
    /// </summary>
    public double MedianMs
    {
        get
        {
            if (Metrics.Count == 0) return 0;
            var sorted = Metrics.OrderBy(m => m.DurationMs).ToList();
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 0
                ? (sorted[mid - 1].DurationMs + sorted[mid].DurationMs) / 2
                : sorted[mid].DurationMs;
        }
    }

    /// <summary>
    /// 95th percentile duration.
    /// </summary>
    public double P95Ms
    {
        get
        {
            if (Metrics.Count == 0) return 0;
            var sorted = Metrics.OrderBy(m => m.DurationMs).ToList();
            var index = (int)Math.Ceiling(sorted.Count * 0.95) - 1;
            return sorted[Math.Max(0, index)].DurationMs;
        }
    }

    /// <summary>
    /// Standard deviation.
    /// </summary>
    public double StdDevMs
    {
        get
        {
            if (Metrics.Count < 2) return 0;
            var avg = AverageMs;
            var sumSquares = Metrics.Sum(m => Math.Pow(m.DurationMs - avg, 2));
            return Math.Sqrt(sumSquares / (Metrics.Count - 1));
        }
    }

    /// <summary>
    /// Percentage of measurements that met threshold.
    /// </summary>
    public double PassRate => Metrics.Count > 0
        ? (double)Metrics.Count(m => m.MetThreshold == true) / Metrics.Count * 100
        : 0;

    /// <summary>
    /// Get a summary string.
    /// </summary>
    public override string ToString()
    {
        return $"Count: {Count}, Avg: {AverageMs:F1}ms, Min: {MinMs:F1}ms, Max: {MaxMs:F1}ms, P95: {P95Ms:F1}ms";
    }
}

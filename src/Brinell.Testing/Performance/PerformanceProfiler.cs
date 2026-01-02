using System.Diagnostics;

namespace Brinell.Testing.Performance;

/// <summary>
/// Performance profiling for UI testing.
/// Measures page load times, interaction timing, memory usage, and network metrics.
/// </summary>
public class PerformanceProfiler
{
    private readonly Stopwatch _timer = new();
    private readonly List<PerformanceMetric> _metrics = new();
    private long _initialMemory;

    /// <summary>
    /// Start measuring a performance operation.
    /// </summary>
    public void StartMeasure(string operationName)
    {
        _initialMemory = GC.GetTotalMemory(true);
        _timer.Restart();
    }

    /// <summary>
    /// End measurement and record metric.
    /// </summary>
    public PerformanceMetric EndMeasure(string operationName)
    {
        _timer.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        var memoryDelta = finalMemory - _initialMemory;

        var metric = new PerformanceMetric
        {
            OperationName = operationName,
            ElapsedMilliseconds = _timer.ElapsedMilliseconds,
            MemoryAllocatedBytes = memoryDelta,
            Timestamp = DateTime.UtcNow
        };

        _metrics.Add(metric);
        return metric;
    }

    /// <summary>
    /// Measure operation with action.
    /// </summary>
    public PerformanceMetric Measure(string operationName, Action operation)
    {
        StartMeasure(operationName);
        operation();
        return EndMeasure(operationName);
    }

    /// <summary>
    /// Measure async operation.
    /// </summary>
    public async Task<PerformanceMetric> MeasureAsync(string operationName, Func<Task> operation)
    {
        StartMeasure(operationName);
        await operation();
        return EndMeasure(operationName);
    }

    /// <summary>
    /// Assert operation completes within time budget.
    /// </summary>
    public void AssertCompletedWithin(string operationName, long maxMilliseconds)
    {
        var metric = _metrics.FirstOrDefault(m => m.OperationName == operationName);
        if (metric == null)
        {
            throw new PerformanceException($"No measurement found for {operationName}");
        }

        if (metric.ElapsedMilliseconds > maxMilliseconds)
        {
            throw new PerformanceException(
                $"Operation {operationName} took {metric.ElapsedMilliseconds}ms but should complete within {maxMilliseconds}ms");
        }
    }

    /// <summary>
    /// Assert memory usage is reasonable.
    /// </summary>
    public void AssertMemoryUsageUnder(string operationName, long maxBytes)
    {
        var metric = _metrics.FirstOrDefault(m => m.OperationName == operationName);
        if (metric == null)
        {
            throw new PerformanceException($"No measurement found for {operationName}");
        }

        if (metric.MemoryAllocatedBytes > maxBytes)
        {
            throw new PerformanceException(
                $"Operation {operationName} allocated {metric.MemoryAllocatedBytes} bytes but should use less than {maxBytes} bytes");
        }
    }

    /// <summary>
    /// Assert average performance across multiple runs.
    /// </summary>
    public void AssertAveragePerformance(string operationName, long maxAverageMs, int minSamples = 3)
    {
        var metrics = _metrics.Where(m => m.OperationName == operationName).ToList();
        if (metrics.Count < minSamples)
        {
            throw new PerformanceException(
                $"Not enough samples for {operationName}: {metrics.Count} < {minSamples}");
        }

        var average = (long)metrics.Average(m => m.ElapsedMilliseconds);
        if (average > maxAverageMs)
        {
            throw new PerformanceException(
                $"Operation {operationName} average {average}ms exceeds {maxAverageMs}ms budget");
        }
    }

    /// <summary>
    /// Measure page load timing metrics.
    /// </summary>
    public PageLoadMetrics MeasurePageLoad(
        long navigationStartMs,
        long domContentLoadedMs,
        long loadCompleteMs)
    {
        var metrics = new PageLoadMetrics
        {
            NavigationStart = navigationStartMs,
            DomContentLoaded = domContentLoadedMs,
            LoadComplete = loadCompleteMs,
            DomInteractiveTime = domContentLoadedMs - navigationStartMs,
            PageLoadTime = loadCompleteMs - navigationStartMs
        };

        return metrics;
    }

    /// <summary>
    /// Get all recorded metrics.
    /// </summary>
    public List<PerformanceMetric> GetMetrics() => _metrics.ToList();

    /// <summary>
    /// Get performance summary.
    /// </summary>
    public PerformanceSummary GetSummary()
    {
        var groupedMetrics = _metrics
            .GroupBy(m => m.OperationName)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Count = g.Count(),
                    AvgTime = g.Average(m => m.ElapsedMilliseconds),
                    MinTime = g.Min(m => m.ElapsedMilliseconds),
                    MaxTime = g.Max(m => m.ElapsedMilliseconds),
                    TotalMemory = g.Sum(m => m.MemoryAllocatedBytes)
                });

        return new PerformanceSummary
        {
            TotalMeasurements = _metrics.Count,
            UniqueOperations = groupedMetrics.Count,
            Metrics = groupedMetrics.ToDictionary(
                kvp => kvp.Key,
                kvp => new OperationMetrics
                {
                    Count = kvp.Value.Count,
                    AverageMs = (long)kvp.Value.AvgTime,
                    MinMs = kvp.Value.MinTime,
                    MaxMs = kvp.Value.MaxTime,
                    TotalMemoryBytes = kvp.Value.TotalMemory
                })
        };
    }

    /// <summary>
    /// Generate performance report.
    /// </summary>
    public string GenerateReport()
    {
        var summary = GetSummary();
        var report = new System.Text.StringBuilder();

        report.AppendLine("Performance Report");
        report.AppendLine("==================");
        report.AppendLine($"Total Measurements: {summary.TotalMeasurements}");
        report.AppendLine($"Unique Operations: {summary.UniqueOperations}");
        report.AppendLine();

        foreach (var op in summary.Metrics)
        {
            report.AppendLine($"{op.Key}:");
            report.AppendLine($"  Runs: {op.Value.Count}");
            report.AppendLine($"  Average: {op.Value.AverageMs}ms");
            report.AppendLine($"  Min: {op.Value.MinMs}ms");
            report.AppendLine($"  Max: {op.Value.MaxMs}ms");
            report.AppendLine($"  Memory: {FormatBytes(op.Value.TotalMemoryBytes)}");
            report.AppendLine();
        }

        return report.ToString();
    }

    /// <summary>
    /// Clear all metrics.
    /// </summary>
    public void Reset() => _metrics.Clear();

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Single performance measurement.
/// </summary>
public class PerformanceMetric
{
    public required string OperationName { get; set; }
    public required long ElapsedMilliseconds { get; set; }
    public required long MemoryAllocatedBytes { get; set; }
    public required DateTime Timestamp { get; set; }
}

/// <summary>
/// Page load timing metrics.
/// </summary>
public class PageLoadMetrics
{
    public required long NavigationStart { get; set; }
    public required long DomContentLoaded { get; set; }
    public required long LoadComplete { get; set; }
    public long DomInteractiveTime { get; set; }
    public long PageLoadTime { get; set; }
}

/// <summary>
/// Performance summary across multiple runs.
/// </summary>
public class PerformanceSummary
{
    public required int TotalMeasurements { get; set; }
    public required int UniqueOperations { get; set; }
    public required Dictionary<string, OperationMetrics> Metrics { get; set; }
}

/// <summary>
/// Metrics for a single operation.
/// </summary>
public class OperationMetrics
{
    public required int Count { get; set; }
    public required long AverageMs { get; set; }
    public required long MinMs { get; set; }
    public required long MaxMs { get; set; }
    public required long TotalMemoryBytes { get; set; }
}

/// <summary>
/// Exception for performance violations.
/// </summary>
public class PerformanceException : Exception
{
    public PerformanceException(string message) : base(message) { }
}

/// <summary>
/// Extension methods for performance profiling.
/// </summary>
public static class PerformanceExtensions
{
    /// <summary>
    /// Create performance profiler.
    /// </summary>
    public static PerformanceProfiler CreateProfiler() => new();

    /// <summary>
    /// Assert operation meets performance budget.
    /// </summary>
    public static void AssertFast(this PerformanceProfiler profiler, string operation, long maxMs = 1000)
    {
        profiler.AssertCompletedWithin(operation, maxMs);
    }

    /// <summary>
    /// Get formatted performance report.
    /// </summary>
    public static string Report(this PerformanceProfiler profiler) => profiler.GenerateReport();
}

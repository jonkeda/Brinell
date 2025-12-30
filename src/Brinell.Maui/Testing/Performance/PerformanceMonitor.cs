using System.Diagnostics;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Testing.Performance;

/// <summary>
/// Monitor for tracking performance metrics during tests.
/// </summary>
public class PerformanceMonitor : IDisposable
{
    private readonly AppiumTestContext? _context;
    private readonly List<PerformanceMetrics> _metrics = new();
    private readonly object _lock = new();
    private Stopwatch? _activeStopwatch;
    private string? _activeOperation;
    private long _activeMemoryStart;

    public PerformanceMonitor(AppiumTestContext? context = null)
    {
        _context = context;
    }

    /// <summary>
    /// All collected metrics.
    /// </summary>
    public IReadOnlyList<PerformanceMetrics> Metrics => _metrics.AsReadOnly();

    /// <summary>
    /// Start measuring an operation.
    /// </summary>
    /// <param name="operationName">Name of the operation.</param>
    public void Start(string operationName)
    {
        if (_activeStopwatch != null)
            throw new InvalidOperationException($"Already measuring '{_activeOperation}'. Call Stop() first.");

        _activeOperation = operationName;
        _activeMemoryStart = GC.GetTotalMemory(false);
        _activeStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Stop measuring the current operation.
    /// </summary>
    /// <param name="thresholdMs">Optional threshold to compare against.</param>
    /// <returns>The recorded metrics.</returns>
    public PerformanceMetrics Stop(double? thresholdMs = null)
    {
        if (_activeStopwatch == null)
            throw new InvalidOperationException("No active measurement. Call Start() first.");

        _activeStopwatch.Stop();

        var metrics = new PerformanceMetrics
        {
            OperationName = _activeOperation!,
            StartedAt = DateTime.UtcNow - _activeStopwatch.Elapsed,
            CompletedAt = DateTime.UtcNow,
            MemoryAtStart = _activeMemoryStart,
            MemoryAtEnd = GC.GetTotalMemory(false),
            ThresholdMs = thresholdMs,
            MetThreshold = thresholdMs.HasValue ? _activeStopwatch.ElapsedMilliseconds <= thresholdMs : null
        };

        lock (_lock)
        {
            _metrics.Add(metrics);
        }

        _activeStopwatch = null;
        _activeOperation = null;

        return metrics;
    }

    /// <summary>
    /// Measure an action.
    /// </summary>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="action">Action to measure.</param>
    /// <param name="thresholdMs">Optional threshold.</param>
    /// <returns>The recorded metrics.</returns>
    public PerformanceMetrics Measure(string operationName, Action action, double? thresholdMs = null)
    {
        Start(operationName);
        try
        {
            action();
            return Stop(thresholdMs);
        }
        catch
        {
            Stop(thresholdMs);
            throw;
        }
    }

    /// <summary>
    /// Measure an async action.
    /// </summary>
    public async Task<PerformanceMetrics> MeasureAsync(string operationName, Func<Task> action, double? thresholdMs = null)
    {
        Start(operationName);
        try
        {
            await action();
            return Stop(thresholdMs);
        }
        catch
        {
            Stop(thresholdMs);
            throw;
        }
    }

    /// <summary>
    /// Measure a function and return its result.
    /// </summary>
    public (T Result, PerformanceMetrics Metrics) Measure<T>(string operationName, Func<T> func, double? thresholdMs = null)
    {
        Start(operationName);
        try
        {
            var result = func();
            var metrics = Stop(thresholdMs);
            return (result, metrics);
        }
        catch
        {
            Stop(thresholdMs);
            throw;
        }
    }

    /// <summary>
    /// Record a navigation timing.
    /// </summary>
    public NavigationMetrics RecordNavigation(
        string fromPage,
        string toPage,
        TimeSpan duration,
        string navigationType = "Push",
        double? thresholdMs = null)
    {
        var metrics = new NavigationMetrics
        {
            OperationName = $"Navigation: {fromPage} -> {toPage}",
            FromPage = fromPage,
            ToPage = toPage,
            NavigationType = navigationType,
            StartedAt = DateTime.UtcNow - duration,
            CompletedAt = DateTime.UtcNow,
            ThresholdMs = thresholdMs,
            MetThreshold = thresholdMs.HasValue ? duration.TotalMilliseconds <= thresholdMs : null
        };

        lock (_lock)
        {
            _metrics.Add(metrics);
        }

        return metrics;
    }

    /// <summary>
    /// Add a custom metric value.
    /// </summary>
    public void AddCustomMetric(string name, double value, params string[] tags)
    {
        var metrics = new PerformanceMetrics
        {
            OperationName = name,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Tags = tags.ToList(),
            CustomMetrics = { [name] = value }
        };

        lock (_lock)
        {
            _metrics.Add(metrics);
        }
    }

    /// <summary>
    /// Get aggregated statistics for all metrics.
    /// </summary>
    public PerformanceStatistics GetStatistics()
    {
        lock (_lock)
        {
            return new PerformanceStatistics { Metrics = new List<PerformanceMetrics>(_metrics) };
        }
    }

    /// <summary>
    /// Get statistics for a specific operation.
    /// </summary>
    public PerformanceStatistics GetStatistics(string operationName)
    {
        lock (_lock)
        {
            var filtered = _metrics.Where(m =>
                m.OperationName.Equals(operationName, StringComparison.OrdinalIgnoreCase)).ToList();
            return new PerformanceStatistics { Metrics = filtered };
        }
    }

    /// <summary>
    /// Get navigation-specific statistics.
    /// </summary>
    public PerformanceStatistics GetNavigationStatistics()
    {
        lock (_lock)
        {
            var navigations = _metrics.OfType<NavigationMetrics>().Cast<PerformanceMetrics>().ToList();
            return new PerformanceStatistics { Metrics = navigations };
        }
    }

    /// <summary>
    /// Assert that an operation meets a threshold.
    /// </summary>
    public void AssertThreshold(string operationName, double maxMs, string? message = null)
    {
        lock (_lock)
        {
            var matching = _metrics.Where(m =>
                m.OperationName.Equals(operationName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (matching.Count == 0)
                throw new InvalidOperationException($"No metrics found for operation '{operationName}'.");

            var violations = matching.Where(m => m.DurationMs > maxMs).ToList();
            if (violations.Any())
            {
                var avg = violations.Average(m => m.DurationMs);
                throw new PerformanceThresholdException(
                    message ?? $"Performance threshold exceeded for '{operationName}'. " +
                    $"Threshold: {maxMs}ms, Violations: {violations.Count}, Avg: {avg:F1}ms");
            }
        }
    }

    /// <summary>
    /// Assert average performance meets threshold.
    /// </summary>
    public void AssertAverageThreshold(string operationName, double maxAverageMs, string? message = null)
    {
        var stats = GetStatistics(operationName);
        if (stats.Count == 0)
            throw new InvalidOperationException($"No metrics found for operation '{operationName}'.");

        if (stats.AverageMs > maxAverageMs)
        {
            throw new PerformanceThresholdException(
                message ?? $"Average performance threshold exceeded for '{operationName}'. " +
                $"Threshold: {maxAverageMs}ms, Average: {stats.AverageMs:F1}ms");
        }
    }

    /// <summary>
    /// Clear all collected metrics.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _metrics.Clear();
        }
    }

    /// <summary>
    /// Export metrics to JSON.
    /// </summary>
    public string ExportToJson()
    {
        lock (_lock)
        {
            return System.Text.Json.JsonSerializer.Serialize(_metrics, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }

    /// <summary>
    /// Export metrics to a file.
    /// </summary>
    public void ExportToFile(string path)
    {
        var json = ExportToJson();
        File.WriteAllText(path, json);
    }

    public void Dispose()
    {
        _activeStopwatch = null;
    }
}

/// <summary>
/// Exception thrown when performance threshold is exceeded.
/// </summary>
public class PerformanceThresholdException : Exception
{
    public PerformanceThresholdException(string message) : base(message) { }
}

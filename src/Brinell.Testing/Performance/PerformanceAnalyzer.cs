using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Brinell.Testing.Performance;

/// <summary>
/// Detailed performance profiling with hotspot detection and statistical analysis.
/// </summary>
public class PerformanceAnalyzer
{
    private readonly Dictionary<string, List<PerformanceSnapshot>> _operationSnapshots = new();

    /// <summary>
    /// Begin capturing performance data for an operation.
    /// </summary>
    public PerformanceCapture BeginCapture(string operationName)
    {
        return new PerformanceCapture(operationName, this);
    }

    /// <summary>
    /// Record a performance snapshot for an operation.
    /// </summary>
    internal void RecordSnapshot(PerformanceSnapshot snapshot)
    {
        if (!_operationSnapshots.ContainsKey(snapshot.OperationName))
        {
            _operationSnapshots[snapshot.OperationName] = new List<PerformanceSnapshot>();
        }

        _operationSnapshots[snapshot.OperationName].Add(snapshot);
    }

    /// <summary>
    /// Capture performance of an async operation.
    /// </summary>
    public async Task<PerformanceSnapshot> CaptureAsync(string operationName, Func<Task> operation)
    {
        var watch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);
        var stopwatch = Stopwatch.StartNew();
        var initialCount = GC.GetTotalAllocatedBytes();

        try
        {
            await operation();
        }
        finally
        {
            watch.Stop();
            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            var finalCount = GC.GetTotalAllocatedBytes();

            var snapshot = new PerformanceSnapshot
            {
                OperationName = operationName,
                ElapsedMilliseconds = watch.ElapsedMilliseconds,
                MemoryAllocatedBytes = finalCount - initialCount,
                CaptureTime = DateTime.UtcNow,
                AllocationCount = (int)((finalCount - initialCount) / 100)
            };

            RecordSnapshot(snapshot);
            return snapshot;
        }
    }

    /// <summary>
    /// Capture performance of a synchronous operation.
    /// </summary>
    public PerformanceSnapshot Capture(string operationName, Action operation)
    {
        var watch = Stopwatch.StartNew();
        var initialCount = GC.GetTotalAllocatedBytes();

        try
        {
            operation();
        }
        finally
        {
            watch.Stop();
            var finalCount = GC.GetTotalAllocatedBytes();

            var snapshot = new PerformanceSnapshot
            {
                OperationName = operationName,
                ElapsedMilliseconds = watch.ElapsedMilliseconds,
                MemoryAllocatedBytes = finalCount - initialCount,
                CaptureTime = DateTime.UtcNow,
                AllocationCount = (int)((finalCount - initialCount) / 100)
            };

            RecordSnapshot(snapshot);
            return snapshot;
        }
    }

    /// <summary>
    /// Analyze performance data for a specific operation.
    /// </summary>
    public PerformanceAnalysis? Analyze(string operationName)
    {
        if (!_operationSnapshots.TryGetValue(operationName, out var snapshots) || snapshots.Count == 0)
        {
            return null;
        }

        var stats = CalculateStatistics(snapshots);
        var slowest = snapshots.OrderByDescending(s => s.ElapsedMilliseconds).First();
        var fastest = snapshots.OrderBy(s => s.ElapsedMilliseconds).First();

        return new PerformanceAnalysis
        {
            OperationName = operationName,
            Statistics = stats,
            Samples = snapshots.ToArray(),
            Slowest = slowest,
            Fastest = fastest
        };
    }

    /// <summary>
    /// Get the top hotspots (slowest operations).
    /// </summary>
    public PerformanceHotspot[] GetHotspots(int topCount = 5)
    {
        var totalTime = _operationSnapshots
            .SelectMany(kvp => kvp.Value)
            .Sum(s => s.ElapsedMilliseconds);

        if (totalTime == 0)
        {
            return Array.Empty<PerformanceHotspot>();
        }

        return _operationSnapshots
            .Select(kvp => new PerformanceHotspot
            {
                OperationName = kvp.Key,
                TotalTimeMs = kvp.Value.Sum(s => s.ElapsedMilliseconds),
                SampleCount = kvp.Value.Count,
                PercentageOfTotal = (decimal)kvp.Value.Sum(s => s.ElapsedMilliseconds) / totalTime * 100,
                Statistics = CalculateStatistics(kvp.Value)
            })
            .OrderByDescending(h => h.TotalTimeMs)
            .Take(topCount)
            .ToArray();
    }

    /// <summary>
    /// Check if an operation has regressed in performance.
    /// </summary>
    public bool IsRegression(string operationName, double regressionThreshold = 1.1)
    {
        if (!_operationSnapshots.TryGetValue(operationName, out var snapshots) || snapshots.Count < 2)
        {
            return false;
        }

        var recentMean = snapshots
            .TakeLast(5)
            .Average(s => s.ElapsedMilliseconds);

        var historicalMean = snapshots
            .SkipLast(5)
            .Take(Math.Max(1, snapshots.Count - 5))
            .Average(s => s.ElapsedMilliseconds);

        return recentMean > historicalMean * regressionThreshold;
    }

    /// <summary>
    /// Identify optimization opportunities.
    /// </summary>
    public OptimizationOpportunity[] IdentifyOpportunities()
    {
        var opportunities = new List<OptimizationOpportunity>();
        var hotspots = GetHotspots(10);

        foreach (var hotspot in hotspots)
        {
            if (hotspot.PercentageOfTotal > 30)
            {
                opportunities.Add(new OptimizationOpportunity
                {
                    OperationName = hotspot.OperationName,
                    Issue = "High time consumption",
                    Recommendation = $"Operation consumes {hotspot.PercentageOfTotal:F1}% of total time. Consider optimization.",
                    EstimatedImprovement = 0.2  // 20% potential improvement
                });
            }

            if (hotspot.Statistics.StandardDeviation > hotspot.Statistics.MeanMilliseconds)
            {
                opportunities.Add(new OptimizationOpportunity
                {
                    OperationName = hotspot.OperationName,
                    Issue = "High variance in performance",
                    Recommendation = "Performance is inconsistent. Look for caching opportunities or optimize resource allocation.",
                    EstimatedImprovement = 0.15
                });
            }
        }

        return opportunities.ToArray();
    }

    /// <summary>
    /// Generate a performance report.
    /// </summary>
    public PerformanceReport GenerateReport()
    {
        return new PerformanceReport
        {
            TotalOperations = _operationSnapshots.Count,
            TotalSnapshots = _operationSnapshots.Values.Sum(v => v.Count),
            Hotspots = GetHotspots(10),
            OpportunityCount = IdentifyOpportunities().Length
        };
    }

    /// <summary>
    /// Clear all recorded snapshots.
    /// </summary>
    public void Clear()
    {
        _operationSnapshots.Clear();
    }

    private PerformanceStatistics CalculateStatistics(List<PerformanceSnapshot> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return new PerformanceStatistics();
        }

        var times = snapshots.Select(s => s.ElapsedMilliseconds).OrderBy(t => t).ToArray();
        var mean = times.Average();
        var variance = times.Average(t => Math.Pow(t - mean, 2));
        var stdDev = Math.Sqrt(variance);

        return new PerformanceStatistics
        {
            MeanMilliseconds = mean,
            MedianMilliseconds = times.Length % 2 == 0
                ? (times[times.Length / 2 - 1] + times[times.Length / 2]) / 2.0
                : times[times.Length / 2],
            StandardDeviation = stdDev,
            MinMilliseconds = times[0],
            MaxMilliseconds = times[times.Length - 1],
            P95Milliseconds = times[(int)(times.Length * 0.95)],
            P99Milliseconds = times[(int)(times.Length * 0.99)]
        };
    }
}

/// <summary>
/// Represents a single performance measurement.
/// </summary>
public class PerformanceSnapshot
{
    /// <summary>
    /// Name of the operation being measured.
    /// </summary>
    public required string OperationName { get; set; }

    /// <summary>
    /// Time elapsed in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Memory allocated in bytes.
    /// </summary>
    public long MemoryAllocatedBytes { get; set; }

    /// <summary>
    /// When this snapshot was captured.
    /// </summary>
    public DateTime CaptureTime { get; set; }

    /// <summary>
    /// Number of allocations.
    /// </summary>
    public int AllocationCount { get; set; }
}

/// <summary>
/// Statistical analysis of performance measurements.
/// </summary>
public class PerformanceAnalysis
{
    /// <summary>
    /// Name of the operation.
    /// </summary>
    public required string OperationName { get; set; }

    /// <summary>
    /// Statistical metrics.
    /// </summary>
    public PerformanceStatistics Statistics { get; set; } = new();

    /// <summary>
    /// All captured snapshots.
    /// </summary>
    public PerformanceSnapshot[] Samples { get; set; } = Array.Empty<PerformanceSnapshot>();

    /// <summary>
    /// Slowest measurement.
    /// </summary>
    public PerformanceSnapshot? Slowest { get; set; }

    /// <summary>
    /// Fastest measurement.
    /// </summary>
    public PerformanceSnapshot? Fastest { get; set; }
}

/// <summary>
/// Statistical measures for performance.
/// </summary>
public class PerformanceStatistics
{
    /// <summary>
    /// Mean execution time in milliseconds.
    /// </summary>
    public double MeanMilliseconds { get; set; }

    /// <summary>
    /// Median execution time in milliseconds.
    /// </summary>
    public double MedianMilliseconds { get; set; }

    /// <summary>
    /// Standard deviation of execution time.
    /// </summary>
    public double StandardDeviation { get; set; }

    /// <summary>
    /// Minimum execution time in milliseconds.
    /// </summary>
    public long MinMilliseconds { get; set; }

    /// <summary>
    /// Maximum execution time in milliseconds.
    /// </summary>
    public long MaxMilliseconds { get; set; }

    /// <summary>
    /// 95th percentile execution time.
    /// </summary>
    public double P95Milliseconds { get; set; }

    /// <summary>
    /// 99th percentile execution time.
    /// </summary>
    public double P99Milliseconds { get; set; }
}

/// <summary>
/// High-consumption operation summary.
/// </summary>
public class PerformanceHotspot
{
    /// <summary>
    /// Operation name.
    /// </summary>
    public required string OperationName { get; set; }

    /// <summary>
    /// Total cumulative time in milliseconds.
    /// </summary>
    public long TotalTimeMs { get; set; }

    /// <summary>
    /// Number of samples collected.
    /// </summary>
    public int SampleCount { get; set; }

    /// <summary>
    /// Percentage of total measured time.
    /// </summary>
    public decimal PercentageOfTotal { get; set; }

    /// <summary>
    /// Statistical metrics.
    /// </summary>
    public PerformanceStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Identified optimization opportunity.
/// </summary>
public class OptimizationOpportunity
{
    /// <summary>
    /// Operation name.
    /// </summary>
    public required string OperationName { get; set; }

    /// <summary>
    /// Issue description.
    /// </summary>
    public required string Issue { get; set; }

    /// <summary>
    /// Optimization recommendation.
    /// </summary>
    public required string Recommendation { get; set; }

    /// <summary>
    /// Estimated improvement (0.1 = 10% faster).
    /// </summary>
    public double EstimatedImprovement { get; set; }
}

/// <summary>
/// Overall performance report.
/// </summary>
public class PerformanceReport
{
    /// <summary>
    /// Number of unique operations measured.
    /// </summary>
    public int TotalOperations { get; set; }

    /// <summary>
    /// Total number of measurements.
    /// </summary>
    public int TotalSnapshots { get; set; }

    /// <summary>
    /// Top hotspots.
    /// </summary>
    public PerformanceHotspot[] Hotspots { get; set; } = Array.Empty<PerformanceHotspot>();

    /// <summary>
    /// Number of identified optimization opportunities.
    /// </summary>
    public int OpportunityCount { get; set; }
}

/// <summary>
/// Context manager for capturing performance data.
/// </summary>
public class PerformanceCapture : IDisposable
{
    private readonly PerformanceAnalyzer _analyzer;
    private readonly Stopwatch _watch;
    private readonly long _initialMemory;
    private readonly string _operationName;

    internal PerformanceCapture(string operationName, PerformanceAnalyzer analyzer)
    {
        _operationName = operationName;
        _analyzer = analyzer;
        _watch = Stopwatch.StartNew();
        _initialMemory = GC.GetTotalAllocatedBytes();
    }

    /// <summary>
    /// Complete the capture and record the snapshot.
    /// </summary>
    public void Dispose()
    {
        _watch.Stop();
        var finalMemory = GC.GetTotalAllocatedBytes();

        var snapshot = new PerformanceSnapshot
        {
            OperationName = _operationName,
            ElapsedMilliseconds = _watch.ElapsedMilliseconds,
            MemoryAllocatedBytes = finalMemory - _initialMemory,
            CaptureTime = DateTime.UtcNow,
            AllocationCount = 0
        };

        _analyzer.RecordSnapshot(snapshot);
    }
}

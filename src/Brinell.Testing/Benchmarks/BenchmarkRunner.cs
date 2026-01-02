using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Brinell.Testing.Benchmarks;

/// <summary>
/// Runs and manages performance benchmarks with regression detection.
/// </summary>
public class BenchmarkRunner
{
    private readonly Dictionary<string, List<BenchmarkResult>> _results = new();
    private readonly Dictionary<string, BenchmarkBaseline> _baselines = new();

    /// <summary>
    /// Run a benchmark operation multiple times.
    /// </summary>
    public async Task<BenchmarkResult> RunBenchmarkAsync(
        string name,
        Func<Task> operation,
        int iterations = 100)
    {
        var measurements = new List<long>();

        for (int i = 0; i < iterations; i++)
        {
            var watch = Stopwatch.StartNew();
            await operation();
            watch.Stop();
            measurements.Add(watch.ElapsedMilliseconds);
        }

        return CreateResult(name, measurements, iterations);
    }

    /// <summary>
    /// Run multiple benchmarks.
    /// </summary>
    public async Task<BenchmarkResult[]> RunBenchmarksAsync(
        PerformanceBenchmark[] benchmarks)
    {
        var results = new List<BenchmarkResult>();

        foreach (var benchmark in benchmarks)
        {
            var result = await RunBenchmarkAsync(
                benchmark.Name,
                benchmark.ExecuteAsync,
                benchmark.Iterations);

            results.Add(result);
        }

        return results.ToArray();
    }

    /// <summary>
    /// Save a baseline for comparison.
    /// </summary>
    public void SaveBaseline(string name, BenchmarkResult result)
    {
        _baselines[name] = new BenchmarkBaseline
        {
            Name = name,
            AverageMilliseconds = result.AverageMilliseconds,
            StandardDeviation = result.StandardDeviation,
            EstablishedAt = DateTime.UtcNow,
            SampleCount = result.Iterations
        };
    }

    /// <summary>
    /// Load a baseline for comparison.
    /// </summary>
    public BenchmarkBaseline? LoadBaseline(string name)
    {
        return _baselines.TryGetValue(name, out var baseline) ? baseline : null;
    }

    /// <summary>
    /// Check if a result is a regression.
    /// </summary>
    public bool IsRegression(string name, BenchmarkResult result, double threshold = 0.1)
    {
        var baseline = LoadBaseline(name);
        if (baseline == null)
        {
            return false;
        }

        var percentageChange = (result.AverageMilliseconds - baseline.AverageMilliseconds) / baseline.AverageMilliseconds;
        return percentageChange > threshold;
    }

    /// <summary>
    /// Compare current result with baseline.
    /// </summary>
    public BenchmarkComparison Compare(string name, BenchmarkResult current)
    {
        var baseline = LoadBaseline(name);

        if (baseline == null)
        {
            return new BenchmarkComparison
            {
                BenchmarkName = name,
                Current = current,
                Baseline = null,
                PercentageChange = 0,
                IsRegression = false,
                Verdict = "NoBaseline"
            };
        }

        var percentageChange = (current.AverageMilliseconds - baseline.AverageMilliseconds) / baseline.AverageMilliseconds;
        var isRegression = percentageChange > 0.1;

        return new BenchmarkComparison
        {
            BenchmarkName = name,
            Current = current,
            Baseline = baseline,
            PercentageChange = percentageChange,
            IsRegression = isRegression,
            Verdict = percentageChange < -0.05 ? "Improved" : isRegression ? "Regressed" : "Passed"
        };
    }

    /// <summary>
    /// Generate a report of benchmark results.
    /// </summary>
    public BenchmarkReport GenerateReport(BenchmarkResult[] results)
    {
        return new BenchmarkReport
        {
            ExecutedAt = DateTime.UtcNow,
            TotalBenchmarks = results.Length,
            Results = results,
            TotalTimeMs = results.Sum(r => r.TotalMilliseconds),
            AverageTimeMs = results.Average(r => r.AverageMilliseconds)
        };
    }

    /// <summary>
    /// Get all stored results for a benchmark.
    /// </summary>
    public IReadOnlyList<BenchmarkResult> GetResults(string name)
    {
        return _results.TryGetValue(name, out var results)
            ? results.AsReadOnly()
            : new List<BenchmarkResult>().AsReadOnly();
    }

    private BenchmarkResult CreateResult(string name, List<long> measurements, int iterations)
    {
        var totalMs = measurements.Sum();
        var sortedMeasurements = measurements.OrderBy(m => m).ToArray();
        var mean = measurements.Average();
        var variance = measurements.Average(m => Math.Pow(m - mean, 2));
        var stdDev = Math.Sqrt(variance);

        var result = new BenchmarkResult
        {
            Name = name,
            Iterations = iterations,
            TotalMilliseconds = totalMs,
            AverageMilliseconds = mean,
            MinMilliseconds = sortedMeasurements[0],
            MaxMilliseconds = sortedMeasurements[sortedMeasurements.Length - 1],
            StandardDeviation = stdDev,
            ExecutedAt = DateTime.UtcNow
        };

        if (!_results.ContainsKey(name))
        {
            _results[name] = new List<BenchmarkResult>();
        }

        _results[name].Add(result);

        return result;
    }
}

/// <summary>
/// Base class for performance benchmarks.
/// </summary>
public abstract class PerformanceBenchmark
{
    /// <summary>
    /// Name of the benchmark.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Number of iterations to run.
    /// </summary>
    public int Iterations { get; set; } = 100;

    /// <summary>
    /// Execute the benchmark operation.
    /// </summary>
    public abstract Task ExecuteAsync();
}

/// <summary>
/// Single benchmark measurement.
/// </summary>
public class BenchmarkResult
{
    /// <summary>
    /// Name of the benchmark.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Number of iterations executed.
    /// </summary>
    public int Iterations { get; set; }

    /// <summary>
    /// Total time for all iterations in milliseconds.
    /// </summary>
    public long TotalMilliseconds { get; set; }

    /// <summary>
    /// Average time per iteration in milliseconds.
    /// </summary>
    public double AverageMilliseconds { get; set; }

    /// <summary>
    /// Minimum iteration time in milliseconds.
    /// </summary>
    public double MinMilliseconds { get; set; }

    /// <summary>
    /// Maximum iteration time in milliseconds.
    /// </summary>
    public double MaxMilliseconds { get; set; }

    /// <summary>
    /// Standard deviation of iteration times.
    /// </summary>
    public double StandardDeviation { get; set; }

    /// <summary>
    /// When this benchmark was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; }
}

/// <summary>
/// Established performance baseline.
/// </summary>
public class BenchmarkBaseline
{
    /// <summary>
    /// Name of the benchmark.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Average execution time in milliseconds.
    /// </summary>
    public double AverageMilliseconds { get; set; }

    /// <summary>
    /// Standard deviation.
    /// </summary>
    public double StandardDeviation { get; set; }

    /// <summary>
    /// When this baseline was established.
    /// </summary>
    public DateTime EstablishedAt { get; set; }

    /// <summary>
    /// Number of samples used to establish the baseline.
    /// </summary>
    public int SampleCount { get; set; }
}

/// <summary>
/// Comparison of benchmark result with baseline.
/// </summary>
public class BenchmarkComparison
{
    /// <summary>
    /// Name of the benchmark.
    /// </summary>
    public required string BenchmarkName { get; set; }

    /// <summary>
    /// Current result.
    /// </summary>
    public BenchmarkResult? Current { get; set; }

    /// <summary>
    /// Baseline for comparison.
    /// </summary>
    public BenchmarkBaseline? Baseline { get; set; }

    /// <summary>
    /// Percentage change from baseline (positive = slower, negative = faster).
    /// </summary>
    public double PercentageChange { get; set; }

    /// <summary>
    /// Whether this is a regression.
    /// </summary>
    public bool IsRegression { get; set; }

    /// <summary>
    /// Verdict: Passed, Regressed, Improved, NoBaseline.
    /// </summary>
    public required string Verdict { get; set; }
}

/// <summary>
/// Report of benchmark execution.
/// </summary>
public class BenchmarkReport
{
    /// <summary>
    /// When the report was generated.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Number of benchmarks executed.
    /// </summary>
    public int TotalBenchmarks { get; set; }

    /// <summary>
    /// Individual benchmark results.
    /// </summary>
    public BenchmarkResult[] Results { get; set; } = Array.Empty<BenchmarkResult>();

    /// <summary>
    /// Total time for all benchmarks in milliseconds.
    /// </summary>
    public long TotalTimeMs { get; set; }

    /// <summary>
    /// Average time per benchmark in milliseconds.
    /// </summary>
    public double AverageTimeMs { get; set; }
}

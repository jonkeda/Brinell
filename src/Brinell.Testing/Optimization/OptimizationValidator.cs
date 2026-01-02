using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Brinell.Testing.Optimization;

/// <summary>
/// Validates that optimizations actually improve performance.
/// </summary>
public class OptimizationValidator
{
    private OptimizationMeasurement? _beforeMeasurement;
    private OptimizationMeasurement? _afterMeasurement;

    /// <summary>
    /// Measure performance before optimization.
    /// </summary>
    public OptimizationMeasurement MeasureBefore(
        string name,
        Func<Task> operation,
        int iterations = 100)
    {
        return MeasureOperation(name, operation, iterations);
    }

    /// <summary>
    /// Measure performance after optimization.
    /// </summary>
    public OptimizationMeasurement MeasureAfter(
        string name,
        Func<Task> operation,
        int iterations = 100)
    {
        var measurement = MeasureOperation(name, operation, iterations);
        _afterMeasurement = measurement;
        return measurement;
    }

    /// <summary>
    /// Validate that optimization improved performance.
    /// </summary>
    public OptimizationValidation Validate(double minimumImprovement = 0.1)
    {
        if (_beforeMeasurement == null || _afterMeasurement == null)
        {
            return new OptimizationValidation
            {
                IsValid = false,
                ImprovementPercentage = 0,
                MemoryReductionPercentage = 0,
                Verdict = "Inconclusive",
                Issues = new[] { "Missing before or after measurements" }
            };
        }

        var timingImprovement = 1 - (_afterMeasurement.AverageMilliseconds / _beforeMeasurement.AverageMilliseconds);
        var memoryImprovement = _beforeMeasurement.AverageMemoryBytes > 0
            ? 1 - (_afterMeasurement.AverageMemoryBytes / (double)_beforeMeasurement.AverageMemoryBytes)
            : 0;

        if (timingImprovement < 0 || memoryImprovement < -0.1)
        {
            return new OptimizationValidation
            {
                IsValid = false,
                ImprovementPercentage = timingImprovement,
                MemoryReductionPercentage = memoryImprovement,
                Verdict = "Regression",
                Issues = new[] { "Performance got worse, not better" }
            };
        }

        if (timingImprovement < minimumImprovement && memoryImprovement < minimumImprovement)
        {
            return new OptimizationValidation
            {
                IsValid = false,
                ImprovementPercentage = timingImprovement,
                MemoryReductionPercentage = memoryImprovement,
                Verdict = "NoImprovement",
                Issues = new[] { $"Improvement below {minimumImprovement:P} threshold" }
            };
        }

        return new OptimizationValidation
        {
            IsValid = true,
            ImprovementPercentage = timingImprovement,
            MemoryReductionPercentage = memoryImprovement,
            Verdict = "Success",
            Issues = Array.Empty<string>()
        };
    }

    /// <summary>
    /// Check if improvement is significant.
    /// </summary>
    public bool IsSignificantImprovement(double threshold = 0.1)
    {
        if (_beforeMeasurement == null || _afterMeasurement == null)
        {
            return false;
        }

        var timingImprovement = 1 - (_afterMeasurement.AverageMilliseconds / _beforeMeasurement.AverageMilliseconds);
        var memoryImprovement = _beforeMeasurement.AverageMemoryBytes > 0
            ? 1 - (_afterMeasurement.AverageMemoryBytes / (double)_beforeMeasurement.AverageMemoryBytes)
            : 0;

        return timingImprovement > threshold || memoryImprovement > threshold;
    }

    /// <summary>
    /// Check if within regression threshold.
    /// </summary>
    public bool IsWithinRegressionThreshold(double threshold = 0.05)
    {
        if (_beforeMeasurement == null || _afterMeasurement == null)
        {
            return true;
        }

        var timingChange = (_afterMeasurement.AverageMilliseconds - _beforeMeasurement.AverageMilliseconds)
            / _beforeMeasurement.AverageMilliseconds;

        return Math.Abs(timingChange) <= threshold;
    }

    /// <summary>
    /// Generate a detailed report.
    /// </summary>
    public OptimizationReport GenerateReport()
    {
        if (_beforeMeasurement == null || _afterMeasurement == null)
        {
            return new OptimizationReport
            {
                Before = new OptimizationMeasurement { OperationName = "Unknown" },
                After = new OptimizationMeasurement { OperationName = "Unknown" },
                TimingImprovement = 0,
                MemoryImprovement = 0,
                Validation = Validate()
            };
        }

        var timingImprovement = 1 - (_afterMeasurement.AverageMilliseconds / _beforeMeasurement.AverageMilliseconds);
        var memoryImprovement = _beforeMeasurement.AverageMemoryBytes > 0
            ? 1 - (_afterMeasurement.AverageMemoryBytes / (double)_beforeMeasurement.AverageMemoryBytes)
            : 0;

        return new OptimizationReport
        {
            Before = _beforeMeasurement,
            After = _afterMeasurement,
            TimingImprovement = timingImprovement,
            MemoryImprovement = memoryImprovement,
            Validation = Validate()
        };
    }

    private OptimizationMeasurement MeasureOperation(string name, Func<Task> operation, int iterations)
    {
        var measurements = new long[iterations];
        var initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < iterations; i++)
        {
            var watch = Stopwatch.StartNew();
            operation().Wait();
            watch.Stop();
            measurements[i] = watch.ElapsedMilliseconds;
        }

        var finalMemory = GC.GetTotalMemory(false);
        var totalTime = measurements.Sum(m => m);
        var avgTime = totalTime / (double)iterations;
        var memory = finalMemory - initialMemory;

        return new OptimizationMeasurement
        {
            OperationName = name,
            Iterations = iterations,
            TotalMilliseconds = totalTime,
            AverageMilliseconds = avgTime,
            TotalMemoryBytes = memory,
            AverageMemoryBytes = Math.Max(0, memory / iterations),
            MeasuredAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Measurement of operation performance.
/// </summary>
public class OptimizationMeasurement
{
    /// <summary>
    /// Name of the measured operation.
    /// </summary>
    public required string OperationName { get; set; }

    /// <summary>
    /// Number of iterations.
    /// </summary>
    public int Iterations { get; set; }

    /// <summary>
    /// Total time in milliseconds.
    /// </summary>
    public long TotalMilliseconds { get; set; }

    /// <summary>
    /// Average time per iteration in milliseconds.
    /// </summary>
    public double AverageMilliseconds { get; set; }

    /// <summary>
    /// Total memory allocated in bytes.
    /// </summary>
    public long TotalMemoryBytes { get; set; }

    /// <summary>
    /// Average memory per iteration in bytes.
    /// </summary>
    public long AverageMemoryBytes { get; set; }

    /// <summary>
    /// When this measurement was taken.
    /// </summary>
    public DateTime MeasuredAt { get; set; }
}

/// <summary>
/// Validation result of an optimization.
/// </summary>
public class OptimizationValidation
{
    /// <summary>
    /// Whether the optimization is valid (improved performance).
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Percentage improvement in timing (0.1 = 10% faster, -0.1 = 10% slower).
    /// </summary>
    public double ImprovementPercentage { get; set; }

    /// <summary>
    /// Percentage reduction in memory usage.
    /// </summary>
    public double MemoryReductionPercentage { get; set; }

    /// <summary>
    /// Verdict: Success, Regression, NoImprovement, Inconclusive.
    /// </summary>
    public required string Verdict { get; set; }

    /// <summary>
    /// Any issues found.
    /// </summary>
    public string[] Issues { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Report of optimization validation.
/// </summary>
public class OptimizationReport
{
    /// <summary>
    /// Measurement before optimization.
    /// </summary>
    public OptimizationMeasurement Before { get; set; } = new() { OperationName = "Before" };

    /// <summary>
    /// Measurement after optimization.
    /// </summary>
    public OptimizationMeasurement After { get; set; } = new() { OperationName = "After" };

    /// <summary>
    /// Percentage timing improvement.
    /// </summary>
    public double TimingImprovement { get; set; }

    /// <summary>
    /// Percentage memory improvement.
    /// </summary>
    public double MemoryImprovement { get; set; }

    /// <summary>
    /// Validation result.
    /// </summary>
    public OptimizationValidation Validation { get; set; } = new() { Verdict = "Pending" };
}

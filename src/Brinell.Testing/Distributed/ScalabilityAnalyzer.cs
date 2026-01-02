namespace Brinell.Testing.Distributed;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// Analyzes test performance at scale and provides scaling recommendations.
/// Uses Amdahl's Law and other metrics to project performance as systems scale.
/// </summary>
public class ScalabilityAnalyzer
{
    private readonly DistributedTestCoordinator _coordinator;
    private readonly List<ScaleDataPoint> _scaleDataPoints = new();
    private readonly object _dataLock = new();

    /// <summary>
    /// Initializes scalability analyzer with a distributed coordinator.
    /// </summary>
    public ScalabilityAnalyzer(DistributedTestCoordinator coordinator)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    /// <summary>
    /// Records a data point about system performance at a specific scale.
    /// </summary>
    public void RecordScaleMetrics(int nodeCount, int testCount, long executionTimeMs, double cpuUtilization, double memoryUtilization)
    {
        if (nodeCount <= 0)
        {
            throw new ArgumentException("Node count must be positive", nameof(nodeCount));
        }

        lock (_dataLock)
        {
            _scaleDataPoints.Add(new ScaleDataPoint
            {
                NodeCount = nodeCount,
                TestCount = testCount,
                ExecutionTimeMs = executionTimeMs,
                CpuUtilization = cpuUtilization,
                MemoryUtilization = memoryUtilization,
                RecordedAt = DateTime.UtcNow,
                ThroughputTestsPerSecond = (testCount * 1000.0) / executionTimeMs
            });
        }
    }

    /// <summary>
    /// Analyzes current system performance at present scale.
    /// </summary>
    public ScalabilityReport AnalyzeCurrentScale()
    {
        var nodes = _coordinator.GetAllNodes();
        var stats = _coordinator.GetCoordinationStats();

        var healthyNodes = _coordinator.GetHealthyNodes();
        var totalCapacity = healthyNodes.Sum(n => n.AvailableSlots);

        var report = new ScalabilityReport
        {
            CurrentNodeCount = nodes.Count,
            HealthyNodeCount = healthyNodes.Count,
            TotalTestsCompleted = stats.TotalTestsCompleted,
            TotalExecutionMs = 0,
            ThroughputTestsPerSecond = stats.TotalTestsCompleted > 0 ? stats.TotalTestsCompleted / (stats.AvgTestDuration / 1000.0) : 0,
            AvgNodeCapacityUtilization = nodes.Count > 0 ? nodes.Average(n => n.CapacityUtilization) : 0,
            TotalAvailableCapacity = totalCapacity,
            Bottlenecks = new List<Bottleneck>(),
            Recommendations = new List<string>()
        };

        // Identify bottlenecks
        IdentifyBottlenecks(report);

        // Generate recommendations
        GenerateRecommendations(report);

        return report;
    }

    /// <summary>
    /// Projects performance if the system is scaled to a different number of nodes.
    /// </summary>
    public ScaleProjection ProjectPerformance(int targetNodeCount)
    {
        if (targetNodeCount <= 0)
        {
            throw new ArgumentException("Target node count must be positive", nameof(targetNodeCount));
        }

        var currentMetrics = AnalyzeCurrentScale();
        var currentNodeCount = currentMetrics.CurrentNodeCount;

        // Calculate speedup using Amdahl's Law
        // Estimate parallel fraction based on current bottlenecks
        var parallelFraction = EstimateParallelFraction(currentMetrics);
        var speedup = CalculateAmdahlLaw(targetNodeCount, parallelFraction);

        // Project execution time
        var currentExecutionTime = currentMetrics.AvgTestDuration;
        var projectedExecutionTime = currentNodeCount > 0 
            ? currentExecutionTime / speedup 
            : currentExecutionTime;

        var projection = new ScaleProjection
        {
            TargetNodeCount = targetNodeCount,
            CurrentNodeCount = currentNodeCount,
            CurrentExecutionMs = (long)currentExecutionTime,
            ProjectedExecutionMs = (long)projectedExecutionTime,
            ProjectedThroughput = currentMetrics.ThroughputTestsPerSecond * speedup,
            ScalingEfficiency = CalculateScalingEfficiency(currentNodeCount, targetNodeCount, speedup),
            AmdahlSpeedup = speedup,
            ParallelFraction = parallelFraction,
            Feasibility = DetermineFeasibility(speedup, parallelFraction)
        };

        return projection;
    }

    /// <summary>
    /// Identifies bottlenecks limiting system performance.
    /// </summary>
    public List<Bottleneck> IdentifyBottlenecks()
    {
        var report = AnalyzeCurrentScale();
        return report.Bottlenecks;
    }

    /// <summary>
    /// Gets scaling recommendations based on current metrics.
    /// </summary>
    public List<string> GetScalingRecommendations()
    {
        var report = AnalyzeCurrentScale();
        return report.Recommendations;
    }

    /// <summary>
    /// Calculates scaling efficiency (0-1, higher is better).
    /// Efficiency = speedup / N where N is node count ratio.
    /// </summary>
    public double CalculateScalingEfficiency(int fromNodeCount, int toNodeCount, double speedup)
    {
        if (fromNodeCount <= 0 || toNodeCount <= 0)
        {
            return 0;
        }

        var nodeRatio = (double)toNodeCount / fromNodeCount;
        return speedup / nodeRatio;
    }

    /// <summary>
    /// Calculates speedup using Amdahl's Law.
    /// S = 1 / ((1 - P) + (P / N))
    /// Where P is parallel fraction and N is processor count.
    /// </summary>
    public double CalculateAmdahlLaw(int processorCount, double parallelFraction)
    {
        if (processorCount <= 0)
        {
            throw new ArgumentException("Processor count must be positive", nameof(processorCount));
        }

        if (parallelFraction < 0 || parallelFraction > 1)
        {
            throw new ArgumentException("Parallel fraction must be between 0 and 1", nameof(parallelFraction));
        }

        if (processorCount == 1)
        {
            return 1.0;
        }

        var serialFraction = 1 - parallelFraction;
        var speedup = 1 / (serialFraction + (parallelFraction / processorCount));

        return speedup;
    }

    /// <summary>
    /// Gets a historical view of performance across different scales.
    /// </summary>
    public ScaleAnalysisReport GetScaleAnalysis()
    {
        lock (_dataLock)
        {
            var report = new ScaleAnalysisReport
            {
                DataPoints = _scaleDataPoints.Select(p => new ScaleDataPointResult
                {
                    NodeCount = p.NodeCount,
                    TestCount = p.TestCount,
                    ExecutionTimeMs = p.ExecutionTimeMs,
                    CpuUtilization = p.CpuUtilization,
                    MemoryUtilization = p.MemoryUtilization,
                    RecordedAt = p.RecordedAt,
                    ThroughputTestsPerSecond = p.ThroughputTestsPerSecond
                }).ToList()
            };

            if (_scaleDataPoints.Count > 0)
            {
                report.MinNodeCount = _scaleDataPoints.Min(p => p.NodeCount);
                report.MaxNodeCount = _scaleDataPoints.Max(p => p.NodeCount);
                report.AvgThroughput = _scaleDataPoints.Average(p => p.ThroughputTestsPerSecond);

                // Calculate scaling factor
                var minScale = _scaleDataPoints.OrderBy(p => p.NodeCount).First();
                var maxScale = _scaleDataPoints.OrderByDescending(p => p.NodeCount).First();

                if (minScale.NodeCount > 0)
                {
                    var speedup = minScale.ExecutionTimeMs / (double)maxScale.ExecutionTimeMs;
                    var nodeIncrease = (double)maxScale.NodeCount / minScale.NodeCount;
                    report.ObservedScalingFactor = speedup / nodeIncrease;
                }
            }

            return report;
        }
    }

    /// <summary>
    /// Estimates the fraction of work that can be parallelized.
    /// </summary>
    private double EstimateParallelFraction(ScalabilityReport report)
    {
        // If CPU is the bottleneck, estimate lower parallel fraction
        var cpuBottleneck = report.Bottlenecks.FirstOrDefault(b => b.Component == "CPU");
        if (cpuBottleneck != null)
        {
            return 0.7 - (cpuBottleneck.UtilizationPercent / 100.0) * 0.2;
        }

        // Otherwise, assume most work is parallelizable
        return 0.85;
    }

    /// <summary>
    /// Identifies bottlenecks and populates the report.
    /// </summary>
    private void IdentifyBottlenecks(ScalabilityReport report)
    {
        // Check CPU utilization (target: 60-80%)
        if (report.AvgNodeCapacityUtilization > 85)
        {
            report.Bottlenecks.Add(new Bottleneck
            {
                Component = "CPU",
                UtilizationPercent = report.AvgNodeCapacityUtilization,
                Recommendation = "CPU is overutilized. Consider scaling up nodes or optimizing test parallelism.",
                EstimatedImpact = 0.25
            });
        }

        // Check memory (target: 50-70%)
        if (report.AvgNodeCapacityUtilization > 80)
        {
            report.Bottlenecks.Add(new Bottleneck
            {
                Component = "Memory",
                UtilizationPercent = 75,
                Recommendation = "Memory usage is high. Consider optimizing test memory or increasing node resources.",
                EstimatedImpact = 0.15
            });
        }

        // Check network (estimated at 40% baseline)
        report.Bottlenecks.Add(new Bottleneck
        {
            Component = "Network",
            UtilizationPercent = 40,
            Recommendation = "Network bandwidth may become limiting at higher scales. Consider optimizing coordination.",
            EstimatedImpact = 0.10
        });

        // Check coordination overhead
        if (report.HealthyNodeCount > 10)
        {
            report.Bottlenecks.Add(new Bottleneck
            {
                Component = "Coordination",
                UtilizationPercent = Math.Min(100, report.HealthyNodeCount * 5),
                Recommendation = "Coordination overhead increases with node count. Consider hierarchical coordination.",
                EstimatedImpact = 0.05
            });
        }
    }

    /// <summary>
    /// Generates scaling recommendations based on metrics.
    /// </summary>
    private void GenerateRecommendations(ScalabilityReport report)
    {
        // Recommendation based on capacity utilization
        if (report.TotalAvailableCapacity < 10)
        {
            report.Recommendations.Add("Current available capacity is low. Scale up to 20+ nodes for better throughput.");
        }

        if (report.CurrentNodeCount <= 5 && report.TotalTestsCompleted > 100)
        {
            report.Recommendations.Add("Small cluster detected with moderate test load. Scale to 10-15 nodes for optimal performance.");
        }

        // Recommendation based on bottlenecks
        var cpuBottleneck = report.Bottlenecks.FirstOrDefault(b => b.Component == "CPU");
        if (cpuBottleneck != null)
        {
            report.Recommendations.Add("Optimize test execution time to reduce CPU pressure.");
            report.Recommendations.Add("Consider using higher-spec nodes with more cores.");
        }

        var coordBottleneck = report.Bottlenecks.FirstOrDefault(b => b.Component == "Coordination");
        if (coordBottleneck != null)
        {
            report.Recommendations.Add("Implement hierarchical coordination to reduce overhead at scale.");
        }

        // Add generic recommendations
        if (report.Recommendations.Count == 0)
        {
            report.Recommendations.Add("System is well-balanced. Current scale appears optimal for test load.");
        }
    }

    /// <summary>
    /// Determines feasibility of scaling to target node count.
    /// </summary>
    private string DetermineFeasibility(double speedup, double parallelFraction)
    {
        if (speedup < 1.1)
        {
            return "Diminishing - little performance gain expected";
        }

        if (parallelFraction < 0.5)
        {
            return "Limited - serial bottleneck prevents efficient scaling";
        }

        return "Feasible - good scaling potential";
    }

    /// <summary>
    /// Clears all recorded scale data points.
    /// </summary>
    public void ClearScaleData()
    {
        lock (_dataLock)
        {
            _scaleDataPoints.Clear();
        }
    }
}

/// <summary>
/// Represents a performance measurement at a specific scale.
/// </summary>
internal class ScaleDataPoint
{
    public int NodeCount { get; set; }
    public int TestCount { get; set; }
    public long ExecutionTimeMs { get; set; }
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public DateTime RecordedAt { get; set; }
    public double ThroughputTestsPerSecond { get; set; }
}

/// <summary>
/// Report on system scalability at current scale.
/// </summary>
public class ScalabilityReport
{
    /// <summary>Current number of nodes.</summary>
    public int CurrentNodeCount { get; set; }

    /// <summary>Number of healthy nodes.</summary>
    public int HealthyNodeCount { get; set; }

    /// <summary>Total tests completed.</summary>
    public int TotalTestsCompleted { get; set; }

    /// <summary>Total execution time in milliseconds.</summary>
    public long TotalExecutionMs { get; set; }

    /// <summary>Average test duration in milliseconds.</summary>
    public double AvgTestDuration { get; set; }

    /// <summary>Tests per second throughput.</summary>
    public double ThroughputTestsPerSecond { get; set; }

    /// <summary>Average capacity utilization percentage.</summary>
    public double AvgNodeCapacityUtilization { get; set; }

    /// <summary>Total available test slots across all nodes.</summary>
    public int TotalAvailableCapacity { get; set; }

    /// <summary>Identified performance bottlenecks.</summary>
    public List<Bottleneck> Bottlenecks { get; set; } = new();

    /// <summary>Scaling recommendations.</summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Represents a performance bottleneck.
/// </summary>
public class Bottleneck
{
    /// <summary>Component that is bottlenecked (CPU, Memory, Network, Disk, Coordination).</summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>Current utilization percentage (0-100).</summary>
    public double UtilizationPercent { get; set; }

    /// <summary>Recommendation to address the bottleneck.</summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>Estimated performance impact if resolved (0-1).</summary>
    public double EstimatedImpact { get; set; }
}

/// <summary>
/// Projection of system performance at a target scale.
/// </summary>
public class ScaleProjection
{
    /// <summary>Target number of nodes.</summary>
    public int TargetNodeCount { get; set; }

    /// <summary>Current number of nodes.</summary>
    public int CurrentNodeCount { get; set; }

    /// <summary>Current execution time in milliseconds.</summary>
    public long CurrentExecutionMs { get; set; }

    /// <summary>Projected execution time in milliseconds.</summary>
    public long ProjectedExecutionMs { get; set; }

    /// <summary>Projected throughput (tests per second).</summary>
    public double ProjectedThroughput { get; set; }

    /// <summary>Scaling efficiency (0-1, where 1 is perfect linear scaling).</summary>
    public double ScalingEfficiency { get; set; }

    /// <summary>Speedup calculated using Amdahl's Law.</summary>
    public double AmdahlSpeedup { get; set; }

    /// <summary>Estimated parallel fraction of work.</summary>
    public double ParallelFraction { get; set; }

    /// <summary>Feasibility assessment of the scaling.</summary>
    public string Feasibility { get; set; } = string.Empty;

    /// <summary>Gets time savings if projection is accurate.</summary>
    public long TimeSavingsMs => Math.Max(0, CurrentExecutionMs - ProjectedExecutionMs);

    /// <summary>Gets efficiency as percentage.</summary>
    public double EfficiencyPercent => ScalingEfficiency * 100;
}

/// <summary>
/// Analysis of system performance across multiple scales.
/// </summary>
public class ScaleAnalysisReport
{
    /// <summary>All recorded data points.</summary>
    public List<ScaleDataPointResult> DataPoints { get; set; } = new();

    /// <summary>Minimum node count in data.</summary>
    public int MinNodeCount { get; set; }

    /// <summary>Maximum node count in data.</summary>
    public int MaxNodeCount { get; set; }

    /// <summary>Average throughput across all scales.</summary>
    public double AvgThroughput { get; set; }

    /// <summary>Observed scaling factor from data.</summary>
    public double ObservedScalingFactor { get; set; }
}

/// <summary>
/// Public version of scale data point for reporting.
/// </summary>
public class ScaleDataPointResult
{
    /// <summary>Number of nodes.</summary>
    public int NodeCount { get; set; }

    /// <summary>Number of tests.</summary>
    public int TestCount { get; set; }

    /// <summary>Execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>CPU utilization percentage.</summary>
    public double CpuUtilization { get; set; }

    /// <summary>Memory utilization percentage.</summary>
    public double MemoryUtilization { get; set; }

    /// <summary>When recorded.</summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>Throughput in tests per second.</summary>
    public double ThroughputTestsPerSecond { get; set; }
}

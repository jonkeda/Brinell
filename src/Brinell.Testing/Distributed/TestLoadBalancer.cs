namespace Brinell.Testing.Distributed;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Intelligently distributes tests across nodes based on performance metrics and capacity.
/// Uses adaptive algorithms to minimize overall execution time.
/// </summary>
public class TestLoadBalancer
{
    private readonly DistributedTestCoordinator _coordinator;
    private readonly Dictionary<string, NodePerformanceHistory> _performanceHistory = new();
    private readonly object _historyLock = new();

    /// <summary>
    /// Initializes the load balancer with a distributed test coordinator.
    /// </summary>
    public TestLoadBalancer(DistributedTestCoordinator coordinator)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    /// <summary>
    /// Balances test load across nodes using round-robin with capacity adjustment.
    /// </summary>
    public TestAssignment[] BalanceLoad(TestCase[] tests, TestNode[] nodes)
    {
        if (tests == null || tests.Length == 0)
        {
            return Array.Empty<TestAssignment>();
        }

        if (nodes == null || nodes.Length == 0)
        {
            throw new InvalidOperationException("No nodes available for load balancing");
        }

        var healthyNodes = nodes.Where(n => n.Status == NodeStatus.Healthy && n.AvailableSlots > 0).ToList();
        if (healthyNodes.Count == 0)
        {
            throw new InvalidOperationException("No healthy nodes available");
        }

        var assignments = new List<TestAssignment>();
        var nodeIndex = 0;

        foreach (var test in tests)
        {
            var node = healthyNodes[nodeIndex % healthyNodes.Count];
            var estimatedDuration = EstimateTestDuration(test.Name, node.NodeId);
            var score = CalculateNodeScore(node, test);

            assignments.Add(new TestAssignment
            {
                Test = test,
                TargetNode = node,
                EstimatedDuration = estimatedDuration,
                LoadScore = score
            });

            nodeIndex++;
        }

        return assignments.ToArray();
    }

    /// <summary>
    /// Uses adaptive balancing based on historical performance data.
    /// Assigns tests to nodes that have historically performed best.
    /// </summary>
    public TestAssignment[] AdaptiveBalance(TestCase[] tests, TestNode[] nodes)
    {
        if (tests == null || tests.Length == 0)
        {
            return Array.Empty<TestAssignment>();
        }

        if (nodes == null || nodes.Length == 0)
        {
            throw new InvalidOperationException("No nodes available for load balancing");
        }

        var healthyNodes = nodes.Where(n => n.Status == NodeStatus.Healthy && n.AvailableSlots > 0).ToList();
        if (healthyNodes.Count == 0)
        {
            throw new InvalidOperationException("No healthy nodes available");
        }

        var assignments = new List<TestAssignment>();

        // Sort tests by estimated duration (descending)
        var sortedTests = tests.OrderByDescending(t => EstimateTestDuration(t.Name, null)).ToList();

        foreach (var test in sortedTests)
        {
            // Find best node for this specific test
            var bestNode = healthyNodes
                .OrderBy(n => CalculateNodeScore(n, test))
                .First();

            var estimatedDuration = EstimateTestDuration(test.Name, bestNode.NodeId);
            var score = CalculateNodeScore(bestNode, test);

            assignments.Add(new TestAssignment
            {
                Test = test,
                TargetNode = bestNode,
                EstimatedDuration = estimatedDuration,
                LoadScore = score
            });
        }

        return assignments.ToArray();
    }

    /// <summary>
    /// Selects the best node for a specific test based on performance metrics.
    /// </summary>
    public TestNode SelectBestNode(TestCase test, TestNode[] nodes)
    {
        if (nodes == null || nodes.Length == 0)
        {
            throw new ArgumentException("No nodes provided", nameof(nodes));
        }

        var healthyNodes = nodes.Where(n => n.Status == NodeStatus.Healthy && n.AvailableSlots > 0).ToList();
        if (healthyNodes.Count == 0)
        {
            throw new InvalidOperationException("No healthy nodes available");
        }

        return healthyNodes
            .OrderBy(n => CalculateNodeScore(n, test))
            .First();
    }

    /// <summary>
    /// Calculates a score for a node assignment (lower is better).
    /// Considers capacity utilization, historical performance, and network latency.
    /// </summary>
    public double CalculateNodeScore(TestNode node, TestCase test)
    {
        if (node == null)
        {
            return double.MaxValue;
        }

        // Capacity score (0-1): higher utilization = higher score
        var capacityScore = Math.Min(1.0, node.CapacityUtilization / 100.0) * 0.4;

        // Performance score (0-1): based on historical data
        var performanceScore = GetNodePerformanceScore(node.NodeId) * 0.3;

        // Availability score (0-1): available slots
        var availabilityScore = (node.AvailableSlots > 0 ? 0.5 : 1.0) * 0.2;

        // Latency score (0-1): simulated network latency
        var latencyScore = (CalculateNetworkLatency(node) / 1000.0) * 0.1;

        return capacityScore + performanceScore + availabilityScore + latencyScore;
    }

    /// <summary>
    /// Records test execution result for historical performance tracking.
    /// </summary>
    public void RecordTestExecution(string nodeId, string testName, double durationMs)
    {
        lock (_historyLock)
        {
            if (!_performanceHistory.TryGetValue(nodeId, out var history))
            {
                history = new NodePerformanceHistory { NodeId = nodeId };
                _performanceHistory[nodeId] = history;
            }

            history.AddExecution(testName, durationMs);
        }
    }

    /// <summary>
    /// Monitors node health and adjusts assignments as needed.
    /// </summary>
    public async Task MonitorAndAdjustAsync()
    {
        var nodes = _coordinator.GetAllNodes();
        foreach (var node in nodes)
        {
            var health = _coordinator.GetNodeHealth(node.NodeId);
            if (!health.IsHealthy && node.Status != NodeStatus.Unhealthy)
            {
                // Node health degraded, mark for rebalancing
                node.Status = NodeStatus.Degraded;
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Rebalances the load across nodes when conditions change.
    /// </summary>
    public async Task RebalanceAsync()
    {
        var nodes = _coordinator.GetHealthyNodes();
        if (nodes.Count == 0)
        {
            return;
        }

        // Analyze current load distribution
        var avgLoad = nodes.Average(n => n.CapacityUtilization);

        // If any node is significantly overloaded, mark for rebalancing
        var overloadedNodes = nodes
            .Where(n => n.CapacityUtilization > avgLoad * 1.5)
            .ToList();

        if (overloadedNodes.Count > 0)
        {
            // In a real system, this would redistribute tests
            // For now, we just adjust slots based on utilization
            foreach (var node in nodes)
            {
                var underutilized = node.CapacityUtilization < avgLoad * 0.5;
                if (underutilized && node.AvailableSlots < 10)
                {
                    node.AvailableSlots += 1;
                }
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets statistics about node load distribution.
    /// </summary>
    public LoadBalancingStats GetLoadStats()
    {
        var nodes = _coordinator.GetAllNodes();
        var healthyNodes = nodes.Where(n => n.Status == NodeStatus.Healthy).ToList();

        var avgCapacity = healthyNodes.Count > 0 ? healthyNodes.Average(n => n.CapacityUtilization) : 0;
        var avgSlots = healthyNodes.Count > 0 ? healthyNodes.Average(n => n.AvailableSlots) : 0;

        var overloadedNodes = healthyNodes.Count(n => n.CapacityUtilization > avgCapacity * 1.5);
        var underutilizedNodes = healthyNodes.Count(n => n.CapacityUtilization < avgCapacity * 0.5);

        return new LoadBalancingStats
        {
            TotalNodes = nodes.Count,
            HealthyNodes = healthyNodes.Count,
            AverageCapacityUtilization = avgCapacity,
            AverageAvailableSlots = avgSlots,
            OverloadedNodes = overloadedNodes,
            UnderutilizedNodes = underutilizedNodes,
            LoadImbalanceRatio = avgCapacity > 0 ? GetLoadImbalanceRatio(healthyNodes) : 0
        };
    }

    /// <summary>
    /// Calculates the load imbalance ratio (0 = perfectly balanced, 1 = completely imbalanced).
    /// </summary>
    private double GetLoadImbalanceRatio(List<TestNode> nodes)
    {
        if (nodes.Count <= 1)
        {
            return 0;
        }

        var avgLoad = nodes.Average(n => n.CapacityUtilization);
        var stdDev = Math.Sqrt(nodes.Average(n => Math.Pow(n.CapacityUtilization - avgLoad, 2)));

        // Normalize by max possible standard deviation
        var maxStdDev = Math.Max(avgLoad, 100 - avgLoad);
        return maxStdDev > 0 ? Math.Min(1.0, stdDev / maxStdDev) : 0;
    }

    /// <summary>
    /// Estimates test duration based on historical data.
    /// </summary>
    private double EstimateTestDuration(string testName, string? nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            // Return average across all nodes
            lock (_historyLock)
            {
                var allDurations = _performanceHistory.Values
                    .SelectMany(h => h.GetExecutionDurations(testName))
                    .ToList();

                return allDurations.Count > 0 ? allDurations.Average() : 100;
            }
        }

        lock (_historyLock)
        {
            if (_performanceHistory.TryGetValue(nodeId, out var history))
            {
                var durations = history.GetExecutionDurations(testName).ToList();
                if (durations.Count > 0)
                {
                    return durations.Average();
                }
            }
        }

        return 100; // Default estimate
    }

    /// <summary>
    /// Gets performance score for a node (0-1, higher is better).
    /// </summary>
    private double GetNodePerformanceScore(string nodeId)
    {
        lock (_historyLock)
        {
            if (!_performanceHistory.TryGetValue(nodeId, out var history))
            {
                return 0.5; // Neutral score for unknown nodes
            }

            var avgDuration = history.GetAverageDuration();
            var maxExpectedDuration = 500.0;

            // Lower duration = higher score
            return Math.Max(0, 1.0 - (avgDuration / maxExpectedDuration));
        }
    }

    /// <summary>
    /// Calculates network latency to a node (in milliseconds).
    /// </summary>
    private double CalculateNetworkLatency(TestNode node)
    {
        // In a real system, this would measure actual latency
        // For now, return a simulated value based on node ID hash
        var hash = Math.Abs(node.NodeId.GetHashCode() % 50);
        return hash + Random.Shared.Next(0, 20);
    }

    /// <summary>
    /// Clears all performance history.
    /// </summary>
    public void ClearPerformanceHistory()
    {
        lock (_historyLock)
        {
            _performanceHistory.Clear();
        }
    }
}

/// <summary>
/// Represents an assignment of a test to a node.
/// </summary>
public class TestAssignment
{
    /// <summary>The test to execute.</summary>
    public TestCase Test { get; set; } = new();

    /// <summary>The target node for execution.</summary>
    public TestNode TargetNode { get; set; } = new();

    /// <summary>Estimated execution duration in milliseconds.</summary>
    public double EstimatedDuration { get; set; }

    /// <summary>Load balance score (lower is better).</summary>
    public double LoadScore { get; set; }
}

/// <summary>
/// Tracks performance history for a node.
/// </summary>
internal class NodePerformanceHistory
{
    private readonly Dictionary<string, List<double>> _testDurations = new();
    private readonly object _lock = new();

    public string NodeId { get; set; } = string.Empty;

    public void AddExecution(string testName, double durationMs)
    {
        lock (_lock)
        {
            if (!_testDurations.TryGetValue(testName, out var durations))
            {
                durations = new List<double>();
                _testDurations[testName] = durations;
            }

            durations.Add(durationMs);

            // Keep only last 100 executions per test
            if (durations.Count > 100)
            {
                durations.RemoveAt(0);
            }
        }
    }

    public IEnumerable<double> GetExecutionDurations(string testName)
    {
        lock (_lock)
        {
            return _testDurations.TryGetValue(testName, out var durations)
                ? durations.ToList()
                : Enumerable.Empty<double>();
        }
    }

    public double GetAverageDuration()
    {
        lock (_lock)
        {
            var allDurations = _testDurations.Values.SelectMany(d => d).ToList();
            return allDurations.Count > 0 ? allDurations.Average() : 0;
        }
    }
}

/// <summary>
/// Statistics about load balancing across nodes.
/// </summary>
public class LoadBalancingStats
{
    /// <summary>Total number of nodes.</summary>
    public int TotalNodes { get; set; }

    /// <summary>Number of healthy nodes.</summary>
    public int HealthyNodes { get; set; }

    /// <summary>Average capacity utilization percentage.</summary>
    public double AverageCapacityUtilization { get; set; }

    /// <summary>Average available slots per node.</summary>
    public double AverageAvailableSlots { get; set; }

    /// <summary>Number of overloaded nodes.</summary>
    public int OverloadedNodes { get; set; }

    /// <summary>Number of underutilized nodes.</summary>
    public int UnderutilizedNodes { get; set; }

    /// <summary>Load imbalance ratio (0 = balanced, 1 = imbalanced).</summary>
    public double LoadImbalanceRatio { get; set; }
}

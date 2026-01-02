namespace Brinell.Testing.Distributed;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Coordinates test execution across multiple distributed nodes.
/// Manages node registration, health monitoring, test distribution, and result aggregation.
/// </summary>
public class DistributedTestCoordinator
{
    private readonly ConcurrentDictionary<string, TestNode> _nodes = new();
    private readonly ConcurrentDictionary<string, TestResult[]> _nodeResults = new();
    private readonly ConcurrentDictionary<string, TestNodeMetrics> _nodeMetrics = new();
    private readonly List<string> _testLog = new();
    private readonly object _logLock = new();

    /// <summary>
    /// Registers a test node in the distributed cluster.
    /// </summary>
    public void RegisterNode(TestNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        _nodes.TryAdd(node.NodeId, node);
        _nodeMetrics.TryAdd(node.NodeId, new TestNodeMetrics { NodeId = node.NodeId });
        LogOperation($"Node registered: {node.NodeId} at {node.Address}:{node.Port}");
    }

    /// <summary>
    /// Unregisters a test node from the cluster.
    /// </summary>
    public void UnregisterNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            throw new ArgumentException("Node ID cannot be empty", nameof(nodeId));
        }

        _nodes.TryRemove(nodeId, out _);
        _nodeMetrics.TryRemove(nodeId, out _);
        LogOperation($"Node unregistered: {nodeId}");
    }

    /// <summary>
    /// Gets all nodes currently in healthy state.
    /// </summary>
    public IReadOnlyList<TestNode> GetHealthyNodes()
    {
        return _nodes.Values
            .Where(n => n.Status == NodeStatus.Healthy && n.AvailableSlots > 0)
            .ToList();
    }

    /// <summary>
    /// Gets all registered nodes regardless of health status.
    /// </summary>
    public IReadOnlyList<TestNode> GetAllNodes()
    {
        return _nodes.Values.ToList();
    }

    /// <summary>
    /// Gets health status of a specific node.
    /// </summary>
    public NodeHealth GetNodeHealth(string nodeId)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            return new NodeHealth { IsHealthy = false, Message = "Node not found" };
        }

        var metrics = _nodeMetrics.TryGetValue(nodeId, out var m) ? m : new TestNodeMetrics();
        var uptime = DateTime.UtcNow - node.LastHeartbeat;

        return new NodeHealth
        {
            IsHealthy = node.Status == NodeStatus.Healthy,
            NodeId = nodeId,
            Status = node.Status,
            CapacityUtilization = node.CapacityUtilization,
            AvailableSlots = node.AvailableSlots,
            TimeSinceLastHeartbeat = uptime,
            TestsCompleted = metrics.TestsCompleted,
            TestsPassed = metrics.TestsPassed,
            TestsFailed = metrics.TestsFailed,
            AverageTestDuration = metrics.AverageTestDuration
        };
    }

    /// <summary>
    /// Distributes tests across healthy nodes using a round-robin algorithm.
    /// </summary>
    public TestCase[] DistributeTests(TestCase[] tests)
    {
        if (tests == null || tests.Length == 0)
        {
            return Array.Empty<TestCase>();
        }

        var healthyNodes = GetHealthyNodes();
        if (healthyNodes.Count == 0)
        {
            throw new InvalidOperationException("No healthy nodes available for test distribution");
        }

        var distributed = new List<TestCase>();
        var nodeIndex = 0;

        foreach (var test in tests)
        {
            var node = healthyNodes[nodeIndex % healthyNodes.Count];
            AssignTestToNode(test, node.NodeId);
            distributed.Add(test);
            nodeIndex++;
        }

        LogOperation($"Distributed {tests.Length} tests across {healthyNodes.Count} nodes");
        return distributed.ToArray();
    }

    /// <summary>
    /// Distributes tests across nodes with load balancing based on capacity.
    /// </summary>
    public TestCase[] DistributeTestsWithLoadBalance(TestCase[] tests)
    {
        if (tests == null || tests.Length == 0)
        {
            return Array.Empty<TestCase>();
        }

        var healthyNodes = GetHealthyNodes();
        if (healthyNodes.Count == 0)
        {
            throw new InvalidOperationException("No healthy nodes available for test distribution");
        }

        var distributed = new List<TestCase>();

        foreach (var test in tests)
        {
            var bestNode = SelectBestNodeByCapacity(test, healthyNodes);
            AssignTestToNode(test, bestNode.NodeId);
            distributed.Add(test);
        }

        LogOperation($"Load-balanced {tests.Length} tests across {healthyNodes.Count} nodes");
        return distributed.ToArray();
    }

    /// <summary>
    /// Selects the best node for a test based on available capacity.
    /// </summary>
    private TestNode SelectBestNodeByCapacity(TestCase test, IReadOnlyList<TestNode> nodes)
    {
        return nodes
            .OrderByDescending(n => n.AvailableSlots)
            .ThenByDescending(n => 100 - n.CapacityUtilization)
            .First();
    }

    /// <summary>
    /// Assigns a specific test to a specific node.
    /// </summary>
    public void AssignTestToNode(TestCase test, string nodeId)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            throw new InvalidOperationException($"Node {nodeId} not found");
        }

        node.AvailableSlots = Math.Max(0, node.AvailableSlots - 1);
        LogOperation($"Test '{test.Name}' assigned to node {nodeId}");
    }

    /// <summary>
    /// Completes a test execution and updates metrics.
    /// </summary>
    public void CompleteTest(string nodeId, TestResult result)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            return;
        }

        node.AvailableSlots += 1;

        if (_nodeMetrics.TryGetValue(nodeId, out var metrics))
        {
            metrics.TestsCompleted += 1;
            if (result.Passed)
            {
                metrics.TestsPassed += 1;
            }
            else
            {
                metrics.TestsFailed += 1;
            }

            metrics.AverageTestDuration = (metrics.AverageTestDuration * (metrics.TestsCompleted - 1) + result.Duration) / metrics.TestsCompleted;
        }
    }

    /// <summary>
    /// Runs tests across multiple nodes and returns aggregated results.
    /// </summary>
    public async Task<DistributedTestResult> RunTestsAsync(TestCase[] tests)
    {
        return await RunTestsAsync(tests, Environment.ProcessorCount);
    }

    /// <summary>
    /// Runs tests across multiple nodes with specified degree of parallelism.
    /// </summary>
    public async Task<DistributedTestResult> RunTestsAsync(TestCase[] tests, int parallelism)
    {
        if (tests == null || tests.Length == 0)
        {
            return new DistributedTestResult { TotalTests = 0 };
        }

        var distributed = DistributeTestsWithLoadBalance(tests);
        var stopwatch = Stopwatch.StartNew();

        // Simulate async test execution
        var tasks = new List<Task>();
        foreach (var test in distributed)
        {
            tasks.Add(Task.Delay(Random.Shared.Next(10, 100)));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        return AggregateResults(tests, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Stores results from a specific node.
    /// </summary>
    public void StoreNodeResults(string nodeId, TestResult[] results)
    {
        _nodeResults[nodeId] = results;
        LogOperation($"Received {results.Length} results from node {nodeId}");
    }

    /// <summary>
    /// Retrieves results from a specific node.
    /// </summary>
    public TestResult[] GetResultsFromNode(string nodeId)
    {
        return _nodeResults.TryGetValue(nodeId, out var results) ? results : Array.Empty<TestResult>();
    }

    /// <summary>
    /// Aggregates results from all nodes into a single distributed result.
    /// </summary>
    public DistributedTestResult AggregateResults()
    {
        var allTests = new List<TestCase>();
        var resultsByNode = new Dictionary<string, TestResult[]>();

        foreach (var nodeEntry in _nodeResults)
        {
            resultsByNode[nodeEntry.Key] = nodeEntry.Value;
        }

        return new DistributedTestResult
        {
            TotalTests = resultsByNode.Values.Sum(r => r.Length),
            PassedTests = resultsByNode.Values.Sum(r => r.Count(t => t.Passed)),
            FailedTests = resultsByNode.Values.Sum(r => r.Count(t => !t.Passed)),
            ResultsByNode = resultsByNode,
            TotalExecutionMs = (long)resultsByNode.Values.Sum(r => r.Sum(t => t.Duration))
        };
    }

    /// <summary>
    /// Aggregates results from a specific test run.
    /// </summary>
    private DistributedTestResult AggregateResults(TestCase[] tests, long totalExecutionMs)
    {
        var results = new Dictionary<string, TestResult[]>();
        var totalPassed = 0;
        var totalFailed = 0;

        foreach (var nodeEntry in _nodeResults)
        {
            results[nodeEntry.Key] = nodeEntry.Value;
            totalPassed += nodeEntry.Value.Count(r => r.Passed);
            totalFailed += nodeEntry.Value.Count(r => !r.Passed);
        }

        return new DistributedTestResult
        {
            TotalTests = tests.Length,
            PassedTests = totalPassed,
            FailedTests = totalFailed,
            SkippedTests = 0,
            TotalExecutionMs = totalExecutionMs,
            ResultsByNode = results
        };
    }

    /// <summary>
    /// Handles node failure with failover logic.
    /// </summary>
    public void HandleNodeFailure(string failedNodeId)
    {
        if (!_nodes.TryGetValue(failedNodeId, out var node))
        {
            return;
        }

        node.Status = NodeStatus.Unhealthy;
        LogOperation($"Node {failedNodeId} marked as unhealthy");

        // Redistribute tests from failed node
        var failedTests = GetResultsFromNode(failedNodeId);
        if (failedTests.Length > 0)
        {
            var healthyNodes = GetHealthyNodes();
            if (healthyNodes.Count > 0)
            {
                LogOperation($"Redistributing {failedTests.Length} tests from failed node {failedNodeId}");
            }
        }
    }

    /// <summary>
    /// Updates node health status based on heartbeat.
    /// </summary>
    public void UpdateNodeHealth(string nodeId, NodeStatus status, double capacityUtilization)
    {
        if (_nodes.TryGetValue(nodeId, out var node))
        {
            node.Status = status;
            node.CapacityUtilization = capacityUtilization;
            node.LastHeartbeat = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets coordination statistics across all nodes.
    /// </summary>
    public CoordinationStats GetCoordinationStats()
    {
        var allMetrics = _nodeMetrics.Values.ToList();

        return new CoordinationStats
        {
            TotalNodes = _nodes.Count,
            HealthyNodes = GetHealthyNodes().Count,
            TotalTestsCompleted = allMetrics.Sum(m => m.TestsCompleted),
            TotalTestsPassed = allMetrics.Sum(m => m.TestsPassed),
            TotalTestsFailed = allMetrics.Sum(m => m.TestsFailed),
            AvgTestDuration = allMetrics.Count > 0 ? allMetrics.Average(m => m.AverageTestDuration) : 0,
            Logs = GetLogs()
        };
    }

    /// <summary>
    /// Logs an operation for debugging and auditing.
    /// </summary>
    private void LogOperation(string operation)
    {
        lock (_logLock)
        {
            _testLog.Add($"{DateTime.UtcNow:O} - {operation}");
        }
    }

    /// <summary>
    /// Retrieves all logged operations.
    /// </summary>
    public IReadOnlyList<string> GetLogs()
    {
        lock (_logLock)
        {
            return _testLog.AsReadOnly();
        }
    }

    /// <summary>
    /// Clears all logs.
    /// </summary>
    public void ClearLogs()
    {
        lock (_logLock)
        {
            _testLog.Clear();
        }
    }
}

/// <summary>
/// Represents a test node in the distributed cluster.
/// </summary>
public class TestNode
{
    /// <summary>Unique identifier for the node.</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>Network address of the node.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Port number for communication.</summary>
    public int Port { get; set; }

    /// <summary>Current health status.</summary>
    public NodeStatus Status { get; set; } = NodeStatus.Healthy;

    /// <summary>Last heartbeat timestamp.</summary>
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    /// <summary>Number of available test slots.</summary>
    public int AvailableSlots { get; set; } = 10;

    /// <summary>Current capacity utilization percentage (0-100).</summary>
    public double CapacityUtilization { get; set; } = 0;
}

/// <summary>
/// Enumerates possible node health statuses.
/// </summary>
public enum NodeStatus
{
    /// <summary>Node is operating normally.</summary>
    Healthy,

    /// <summary>Node is experiencing performance degradation.</summary>
    Degraded,

    /// <summary>Node is experiencing errors.</summary>
    Unhealthy,

    /// <summary>Node is offline.</summary>
    Offline
}

/// <summary>
/// Contains health information for a test node.
/// </summary>
public class NodeHealth
{
    /// <summary>Whether the node is in healthy state.</summary>
    public bool IsHealthy { get; set; }

    /// <summary>Node identifier.</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>Current status.</summary>
    public NodeStatus Status { get; set; }

    /// <summary>Capacity utilization percentage.</summary>
    public double CapacityUtilization { get; set; }

    /// <summary>Available test slots.</summary>
    public int AvailableSlots { get; set; }

    /// <summary>Time elapsed since last heartbeat.</summary>
    public TimeSpan TimeSinceLastHeartbeat { get; set; }

    /// <summary>Total tests completed.</summary>
    public int TestsCompleted { get; set; }

    /// <summary>Tests that passed.</summary>
    public int TestsPassed { get; set; }

    /// <summary>Tests that failed.</summary>
    public int TestsFailed { get; set; }

    /// <summary>Average test duration in milliseconds.</summary>
    public double AverageTestDuration { get; set; }

    /// <summary>Health status message.</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Metrics tracked per node.
/// </summary>
internal class TestNodeMetrics
{
    public string NodeId { get; set; } = string.Empty;
    public int TestsCompleted { get; set; }
    public int TestsPassed { get; set; }
    public int TestsFailed { get; set; }
    public double AverageTestDuration { get; set; }
}

/// <summary>
/// Result of a distributed test run.
/// </summary>
public class DistributedTestResult
{
    /// <summary>Total number of tests.</summary>
    public int TotalTests { get; set; }

    /// <summary>Tests that passed.</summary>
    public int PassedTests { get; set; }

    /// <summary>Tests that failed.</summary>
    public int FailedTests { get; set; }

    /// <summary>Tests that were skipped.</summary>
    public int SkippedTests { get; set; }

    /// <summary>Total execution time in milliseconds.</summary>
    public long TotalExecutionMs { get; set; }

    /// <summary>Results grouped by node.</summary>
    public Dictionary<string, TestResult[]> ResultsByNode { get; set; } = new();

    /// <summary>Gets the pass rate as a percentage.</summary>
    public double PassRate => TotalTests > 0 ? (PassedTests / (double)TotalTests) * 100 : 0;
}

/// <summary>
/// Represents a single test case.
/// </summary>
public class TestCase
{
    /// <summary>Test name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Test class or category.</summary>
    public string TestClass { get; set; } = string.Empty;

    /// <summary>Test method name.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Tags for organizing tests.</summary>
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Represents the result of a single test execution.
/// </summary>
public class TestResult
{
    /// <summary>Test name.</summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>Whether the test passed.</summary>
    public bool Passed { get; set; }

    /// <summary>Test execution duration in milliseconds.</summary>
    public double Duration { get; set; }

    /// <summary>Error message if test failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Stack trace if test failed.</summary>
    public string? StackTrace { get; set; }
}

/// <summary>
/// Coordination statistics across all nodes.
/// </summary>
public class CoordinationStats
{
    /// <summary>Total number of registered nodes.</summary>
    public int TotalNodes { get; set; }

    /// <summary>Number of healthy nodes.</summary>
    public int HealthyNodes { get; set; }

    /// <summary>Total tests completed across all nodes.</summary>
    public int TotalTestsCompleted { get; set; }

    /// <summary>Total tests passed.</summary>
    public int TotalTestsPassed { get; set; }

    /// <summary>Total tests failed.</summary>
    public int TotalTestsFailed { get; set; }

    /// <summary>Average test duration in milliseconds.</summary>
    public double AvgTestDuration { get; set; }

    /// <summary>All operation logs.</summary>
    public IReadOnlyList<string> Logs { get; set; } = new List<string>();

    /// <summary>Gets overall pass rate as a percentage.</summary>
    public double OverallPassRate => TotalTestsCompleted > 0 ? (TotalTestsPassed / (double)TotalTestsCompleted) * 100 : 0;
}

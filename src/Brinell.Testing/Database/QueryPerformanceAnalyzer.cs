using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Brinell.Testing.Database;

/// <summary>
/// Analyzes database query performance and identifies optimization opportunities.
/// </summary>
public class QueryPerformanceAnalyzer
{
    private readonly List<DatabaseQuery> _capturedQueries = new();

    /// <summary>
    /// Record a query execution.
    /// </summary>
    public void RecordQuery(DatabaseQuery query)
    {
        _capturedQueries.Add(query);
    }

    /// <summary>
    /// Get all captured queries.
    /// </summary>
    public IReadOnlyList<DatabaseQuery> CapturedQueries => _capturedQueries.AsReadOnly();

    /// <summary>
    /// Analyze all captured queries.
    /// </summary>
    public QueryAnalysis AnalyzeQueries()
    {
        if (_capturedQueries.Count == 0)
        {
            return new QueryAnalysis
            {
                TotalQueries = 0,
                TotalExecutionMs = 0,
                SelectCount = 0,
                UpdateCount = 0,
                DeleteCount = 0,
                AverageExecutionMs = 0,
                SlowestQuery = null,
                PotentialOptimizations = Array.Empty<QueryOptimization>()
            };
        }

        var analysis = new QueryAnalysis
        {
            TotalQueries = _capturedQueries.Count,
            TotalExecutionMs = _capturedQueries.Sum(q => q.ElapsedMilliseconds),
            SelectCount = _capturedQueries.Count(q => q.IsSelect),
            UpdateCount = _capturedQueries.Count(q => q.IsUpdate),
            DeleteCount = _capturedQueries.Count(q => q.IsDelete),
            AverageExecutionMs = _capturedQueries.Average(q => q.ElapsedMilliseconds),
            SlowestQuery = _capturedQueries.OrderByDescending(q => q.ElapsedMilliseconds).First(),
            PotentialOptimizations = DetectOptimizations()
        };

        return analysis;
    }

    /// <summary>
    /// Detect if there's an N+1 query pattern.
    /// </summary>
    public bool HasNPlusOnePattern(string entityType)
    {
        var tableQueries = _capturedQueries
            .Where(q => q.Tables.Contains(entityType))
            .ToList();

        if (tableQueries.Count <= 2)
        {
            return false;
        }

        var singleRecordQueries = tableQueries
            .Where(q => !q.JoinedTables.Any())
            .Count();

        return singleRecordQueries >= tableQueries.Count - 1;
    }

    /// <summary>
    /// Find tables that might need eager loading.
    /// </summary>
    public string[] FindMissingIncludes()
    {
        var tableFrequency = new Dictionary<string, int>();

        foreach (var query in _capturedQueries)
        {
            foreach (var table in query.Tables)
            {
                if (!tableFrequency.ContainsKey(table))
                {
                    tableFrequency[table] = 0;
                }

                tableFrequency[table]++;
            }
        }

        var frequentTables = tableFrequency
            .Where(kvp => kvp.Value > 2)
            .Select(kvp => kvp.Key)
            .ToArray();

        return frequentTables;
    }

    /// <summary>
    /// Assert that all expected includes are present.
    /// </summary>
    public void AssertAllIncluded(params string[] expectedIncludes)
    {
        var missingIncludes = FindMissingIncludes();
        var actualIncludes = _capturedQueries
            .SelectMany(q => q.JoinedTables)
            .Distinct()
            .ToArray();

        foreach (var include in expectedIncludes)
        {
            if (!actualIncludes.Contains(include) && missingIncludes.Contains(include))
            {
                throw new InvalidOperationException($"Expected include '{include}' was not present in query execution.");
            }
        }
    }

    /// <summary>
    /// Assert that there are no N+1 patterns.
    /// </summary>
    public void AssertNoNPlusOne()
    {
        var tableFrequency = new Dictionary<string, int>();

        foreach (var query in _capturedQueries)
        {
            foreach (var table in query.Tables)
            {
                if (!tableFrequency.ContainsKey(table))
                {
                    tableFrequency[table] = 0;
                }

                tableFrequency[table]++;
            }
        }

        foreach (var kvp in tableFrequency.Where(kvp => kvp.Value > 5))
        {
            if (HasNPlusOnePattern(kvp.Key))
            {
                throw new InvalidOperationException(
                    $"N+1 query pattern detected for entity type '{kvp.Key}'. " +
                    $"Table appears in {kvp.Value} separate queries.");
            }
        }
    }

    /// <summary>
    /// Assert that total query execution time is within budget.
    /// </summary>
    public void AssertExecutionTime(long maxMilliseconds)
    {
        var totalTime = _capturedQueries.Sum(q => q.ElapsedMilliseconds);

        if (totalTime > maxMilliseconds)
        {
            throw new InvalidOperationException(
                $"Query execution time {totalTime}ms exceeds budget of {maxMilliseconds}ms.");
        }
    }

    /// <summary>
    /// Assert the number of queries executed.
    /// </summary>
    public void AssertQueryCount(int expected)
    {
        if (_capturedQueries.Count != expected)
        {
            throw new InvalidOperationException(
                $"Expected {expected} queries but executed {_capturedQueries.Count}.");
        }
    }

    /// <summary>
    /// Clear all recorded queries.
    /// </summary>
    public void Clear()
    {
        _capturedQueries.Clear();
    }

    private QueryOptimization[] DetectOptimizations()
    {
        var optimizations = new List<QueryOptimization>();

        // Detect N+1 patterns
        var tableFrequency = new Dictionary<string, int>();
        foreach (var query in _capturedQueries)
        {
            foreach (var table in query.Tables)
            {
                if (!tableFrequency.ContainsKey(table))
                {
                    tableFrequency[table] = 0;
                }

                tableFrequency[table]++;
            }
        }

        foreach (var kvp in tableFrequency.Where(kvp => kvp.Value > 2))
        {
            if (HasNPlusOnePattern(kvp.Key))
            {
                optimizations.Add(new QueryOptimization
                {
                    Type = "N+1",
                    Description = $"N+1 pattern detected for '{kvp.Key}'",
                    Recommendation = $"Use eager loading (.Include()) to load '{kvp.Key}' with parent queries",
                    EstimatedImprovement = 0.3,  // 30% improvement
                    AffectedQueries = _capturedQueries
                        .Where(q => q.Tables.Contains(kvp.Key))
                        .Select(q => q.Sql)
                        .ToArray()
                });
            }
        }

        // Detect missing includes
        var missingIncludes = FindMissingIncludes();
        if (missingIncludes.Length > 0)
        {
            optimizations.Add(new QueryOptimization
            {
                Type = "MissingInclude",
                Description = $"{missingIncludes.Length} tables appear frequently without joins",
                Recommendation = $"Add .Include() for tables: {string.Join(", ", missingIncludes)}",
                EstimatedImprovement = 0.2,  // 20% improvement
                AffectedQueries = _capturedQueries
                    .Where(q => missingIncludes.Any(mi => q.Tables.Contains(mi)))
                    .Select(q => q.Sql)
                    .ToArray()
            });
        }

        // Detect slow queries
        var slowQueries = _capturedQueries
            .Where(q => q.ElapsedMilliseconds > 100)
            .ToArray();

        if (slowQueries.Length > 0)
        {
            optimizations.Add(new QueryOptimization
            {
                Type = "SlowQuery",
                Description = $"{slowQueries.Length} queries exceed 100ms",
                Recommendation = "Consider adding indexes or restructuring these queries",
                EstimatedImprovement = 0.25,  // 25% improvement
                AffectedQueries = slowQueries.Select(q => q.Sql).ToArray()
            });
        }

        return optimizations.ToArray();
    }
}

/// <summary>
/// Records information about a database query.
/// </summary>
public class DatabaseQuery
{
    /// <summary>
    /// The SQL query text.
    /// </summary>
    public required string Sql { get; set; }

    /// <summary>
    /// Execution time in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Number of parameters.
    /// </summary>
    public int ParameterCount { get; set; }

    /// <summary>
    /// Tables accessed in FROM clause.
    /// </summary>
    public string[] Tables { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Joined tables.
    /// </summary>
    public List<string> JoinedTables { get; set; } = new();

    /// <summary>
    /// When the query was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Whether this is a SELECT query.
    /// </summary>
    public bool IsSelect { get; set; }

    /// <summary>
    /// Whether this is an UPDATE query.
    /// </summary>
    public bool IsUpdate { get; set; }

    /// <summary>
    /// Whether this is a DELETE query.
    /// </summary>
    public bool IsDelete { get; set; }
}

/// <summary>
/// Analysis results of database queries.
/// </summary>
public class QueryAnalysis
{
    /// <summary>
    /// Total queries executed.
    /// </summary>
    public int TotalQueries { get; set; }

    /// <summary>
    /// Total execution time in milliseconds.
    /// </summary>
    public long TotalExecutionMs { get; set; }

    /// <summary>
    /// Number of SELECT queries.
    /// </summary>
    public int SelectCount { get; set; }

    /// <summary>
    /// Number of UPDATE queries.
    /// </summary>
    public int UpdateCount { get; set; }

    /// <summary>
    /// Number of DELETE queries.
    /// </summary>
    public int DeleteCount { get; set; }

    /// <summary>
    /// Average execution time per query.
    /// </summary>
    public double AverageExecutionMs { get; set; }

    /// <summary>
    /// Slowest query executed.
    /// </summary>
    public DatabaseQuery? SlowestQuery { get; set; }

    /// <summary>
    /// Identified optimizations.
    /// </summary>
    public QueryOptimization[] PotentialOptimizations { get; set; } = Array.Empty<QueryOptimization>();
}

/// <summary>
/// Identified query optimization opportunity.
/// </summary>
public class QueryOptimization
{
    /// <summary>
    /// Type of optimization (N+1, MissingInclude, SlowQuery, etc).
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Description of the issue.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Recommendation to fix the issue.
    /// </summary>
    public required string Recommendation { get; set; }

    /// <summary>
    /// Estimated performance improvement (0.3 = 30% faster).
    /// </summary>
    public double EstimatedImprovement { get; set; }

    /// <summary>
    /// Queries affected by this optimization.
    /// </summary>
    public string[] AffectedQueries { get; set; } = Array.Empty<string>();
}

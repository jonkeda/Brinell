using System.Security.Cryptography;
using System.Text.Json;

namespace Brinell.Testing.VisualRegression;

/// <summary>
/// Visual regression testing for comparing screenshots across test runs.
/// Supports baseline generation, comparison, and diff reporting.
/// </summary>
public class VisualRegressionTester
{
    private readonly string _baselineDir;
    private readonly string _actualDir;
    private readonly string _diffDir;
    private readonly decimal _threshold;

    /// <summary>
    /// Create visual regression tester with baseline and diff directories.
    /// </summary>
    public VisualRegressionTester(string baselineDir, decimal threshold = 0.01m)
    {
        _baselineDir = Path.Combine(baselineDir, "baselines");
        _actualDir = Path.Combine(baselineDir, "actual");
        _diffDir = Path.Combine(baselineDir, "diffs");
        _threshold = threshold;

        CreateDirectoriesIfNeeded();
    }

    private void CreateDirectoriesIfNeeded()
    {
        Directory.CreateDirectory(_baselineDir);
        Directory.CreateDirectory(_actualDir);
        Directory.CreateDirectory(_diffDir);
    }

    /// <summary>
    /// Capture screenshot and save as actual.
    /// </summary>
    public async Task CaptureAsync(byte[] screenshotData, string testName)
    {
        var path = Path.Combine(_actualDir, $"{testName}.png");
        await File.WriteAllBytesAsync(path, screenshotData);
    }

    /// <summary>
    /// Compare actual screenshot against baseline.
    /// Returns null if identical, or diff metrics if different.
    /// </summary>
    public VisualDiffResult? Compare(string testName)
    {
        var baselinePath = Path.Combine(_baselineDir, $"{testName}.png");
        var actualPath = Path.Combine(_actualDir, $"{testName}.png");

        if (!File.Exists(baselinePath))
        {
            return new VisualDiffResult
            {
                TestName = testName,
                Status = DiffStatus.NoBaseline,
                Message = "No baseline found - run with --update-baseline to create one"
            };
        }

        if (!File.Exists(actualPath))
        {
            return new VisualDiffResult
            {
                TestName = testName,
                Status = DiffStatus.MissingActual,
                Message = "Actual screenshot not captured"
            };
        }

        var baselineData = File.ReadAllBytes(baselinePath);
        var actualData = File.ReadAllBytes(actualPath);

        if (baselineData.SequenceEqual(actualData))
        {
            return null; // Identical
        }

        var diffPercentage = CalculateDifference(baselineData, actualData);
        var passed = diffPercentage <= _threshold;

        return new VisualDiffResult
        {
            TestName = testName,
            Status = passed ? DiffStatus.Accepted : DiffStatus.Failed,
            DifferencePercentage = diffPercentage,
            Threshold = _threshold,
            BaselinePath = baselinePath,
            ActualPath = actualPath,
            DiffPath = Path.Combine(_diffDir, $"{testName}-diff.json"),
            Message = passed
                ? $"Visual difference {diffPercentage:P2} is within threshold {_threshold:P2}"
                : $"Visual difference {diffPercentage:P2} exceeds threshold {_threshold:P2}"
        };
    }

    /// <summary>
    /// Update baseline with actual screenshot.
    /// </summary>
    public bool UpdateBaseline(string testName)
    {
        var actualPath = Path.Combine(_actualDir, $"{testName}.png");
        var baselinePath = Path.Combine(_baselineDir, $"{testName}.png");

        if (!File.Exists(actualPath))
        {
            return false;
        }

        File.Copy(actualPath, baselinePath, overwrite: true);
        return true;
    }

    /// <summary>
    /// Calculate binary difference percentage between two images.
    /// </summary>
    private decimal CalculateDifference(byte[] baseline, byte[] actual)
    {
        var minLength = Math.Min(baseline.Length, actual.Length);
        if (minLength == 0) return 1m;

        var differences = 0;
        for (int i = 0; i < minLength; i++)
        {
            if (baseline[i] != actual[i])
                differences++;
        }

        // Account for size difference
        var sizeDiff = Math.Abs(baseline.Length - actual.Length);
        var totalDiff = differences + sizeDiff;
        var totalBytes = Math.Max(baseline.Length, actual.Length);

        return (decimal)totalDiff / totalBytes;
    }

    /// <summary>
    /// Get all comparison results for a test suite.
    /// </summary>
    public List<VisualDiffResult> CompareAll(string[] testNames)
    {
        return testNames
            .Select(name => Compare(name))
            .Where(r => r != null)
            .OfType<VisualDiffResult>()
            .ToList();
    }

    /// <summary>
    /// Generate HTML report of visual diffs.
    /// </summary>
    public async Task GenerateReportAsync(List<VisualDiffResult> results, string outputPath)
    {
        var html = GenerateHtmlReport(results);
        await File.WriteAllTextAsync(outputPath, html);
    }

    private string GenerateHtmlReport(List<VisualDiffResult> results)
    {
        var passed = results.Count(r => r.Status == DiffStatus.Accepted);
        var failed = results.Count(r => r.Status == DiffStatus.Failed);
        var missing = results.Count(r => r.Status == DiffStatus.NoBaseline || r.Status == DiffStatus.MissingActual);

        var html = $$"""
<!DOCTYPE html>
<html>
<head>
    <title>Visual Regression Report</title>
    <style>
        body { font-family: Arial; margin: 20px; }
        .summary { background: #f0f0f0; padding: 10px; margin-bottom: 20px; border-radius: 5px; }
        .passed { background: #d4edda; }
        .failed { background: #f8d7da; }
        .missing { background: #fff3cd; }
        .test { margin: 20px 0; padding: 10px; border: 1px solid #ddd; }
        .diff-image { max-width: 800px; margin: 10px 0; }
        img { max-width: 100%; border: 1px solid #ccc; }
    </style>
</head>
<body>
    <h1>Visual Regression Test Report</h1>
    <div class="summary">
        <p><strong>Total:</strong> {results.Count} | 
           <span class="passed"><strong>Passed:</strong> {passed}</span> | 
           <span class="failed"><strong>Failed:</strong> {failed}</span> | 
           <span class="missing"><strong>Missing:</strong> {missing}</span></p>
        <p>Generated: {DateTime.UtcNow:O}</p>
    </div>
    
    <h2>Results</h2>
""";

        foreach (var result in results)
        {
            var className = result.Status switch
            {
                DiffStatus.Accepted => "passed",
                DiffStatus.Failed => "failed",
                _ => "missing"
            };

            html += $"""
<div class="test {className}">
    <h3>{result.TestName}</h3>
    <p><strong>Status:</strong> {result.Status}</p>
    <p><strong>Message:</strong> {result.Message}</p>
""";

            if (result.DifferencePercentage.HasValue)
            {
                html += $"<p><strong>Difference:</strong> {result.DifferencePercentage:P2} (Threshold: {result.Threshold:P2})</p>";
            }

            if (File.Exists(result.BaselinePath))
            {
                html += $"""
<div class="diff-image">
    <h4>Baseline</h4>
    <img src="{result.BaselinePath}" alt="Baseline for {result.TestName}">
</div>
""";
            }

            if (File.Exists(result.ActualPath))
            {
                html += $"""
<div class="diff-image">
    <h4>Actual</h4>
    <img src="{result.ActualPath}" alt="Actual for {result.TestName}">
</div>
""";
            }

            html += "</div>\n";
        }

        html += """
</body>
</html>
""";

        return html;
    }
}

/// <summary>
/// Result of visual comparison.
/// </summary>
public class VisualDiffResult
{
    public required string TestName { get; set; }
    public required DiffStatus Status { get; set; }
    public required string Message { get; set; }
    public decimal? DifferencePercentage { get; set; }
    public decimal Threshold { get; set; }
    public string? BaselinePath { get; set; }
    public string? ActualPath { get; set; }
    public string? DiffPath { get; set; }
}

/// <summary>
/// Status of visual regression comparison.
/// </summary>
public enum DiffStatus
{
    Accepted,           // Within threshold
    Failed,             // Exceeds threshold
    NoBaseline,         // No baseline exists
    MissingActual       // Actual screenshot not captured
}

/// <summary>
/// Extension methods for visual regression testing in test base classes.
/// </summary>
public static class VisualRegressionExtensions
{
    /// <summary>
    /// Snapshot test - capture and compare screenshot.
    /// </summary>
    public static async Task SnapshotAsync(
        this byte[] screenshotData,
        string testName,
        VisualRegressionTester tester)
    {
        await tester.CaptureAsync(screenshotData, testName);
        var result = tester.Compare(testName);

        if (result?.Status == DiffStatus.Failed)
        {
            throw new SnapshotMismatchException(
                $"Visual regression failed for {testName}: {result.Message}");
        }
    }

    /// <summary>
    /// Update visual baseline for a test.
    /// </summary>
    public static void UpdateSnapshot(
        this byte[] screenshotData,
        string testName,
        VisualRegressionTester tester)
    {
        tester.UpdateBaseline(testName);
    }

    /// <summary>
    /// Assert no visual difference from baseline.
    /// </summary>
    public static void AssertVisualMatch(
        this VisualDiffResult? result,
        string testName)
    {
        if (result == null) return; // Identical

        if (result.Status != DiffStatus.Accepted)
        {
            throw new VisualRegressionException(
                $"{testName}: {result.Message}\n" +
                $"Baseline: {result.BaselinePath}\n" +
                $"Actual: {result.ActualPath}");
        }
    }
}

/// <summary>
/// Exception for visual regression failures.
/// </summary>
public class VisualRegressionException : Exception
{
    public VisualRegressionException(string message) : base(message) { }
}

/// <summary>
/// Exception for snapshot mismatches.
/// </summary>
public class SnapshotMismatchException : Exception
{
    public SnapshotMismatchException(string message) : base(message) { }
}

using Brinell.Core.Artifacts;
using Brinell.Core.Configuration;
using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Logging;

namespace Brinell.Core.Services;

/// <summary>
/// Service for capturing and saving screenshots during test execution.
/// Screenshot failures are handled gracefully and do not throw exceptions.
/// </summary>
public class ScreenshotService : IScreenshotService
{
    private readonly ITestContext _context;
    private readonly ITestLogger _logger;
    private readonly ScreenshotSettings _settings;
    
    /// <summary>
    /// Creates a new screenshot service.
    /// </summary>
    /// <param name="context">Test context for capturing screenshots.</param>
    /// <param name="logger">Logger for screenshot events.</param>
    /// <param name="settings">Screenshot configuration settings.</param>
    public ScreenshotService(
        ITestContext context,
        ITestLogger logger,
        ScreenshotSettings? settings = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? ScreenshotSettings.Default;
    }
    
    /// <inheritdoc />
    public ScreenshotSettings Settings => _settings;
    
    /// <inheritdoc />
    public string Capture(string? description = null)
    {
        return Capture("Manual", "Capture", description ?? "screenshot");
    }
    
    /// <inheritdoc />
    public string Capture(string testClass, string testMethod, string description)
    {
        var filename = GenerateFilename(testClass, testMethod, description);
        return CaptureAndSave(filename, testClass, testMethod, ScreenshotReason.Manual);
    }
    
    /// <inheritdoc />
    public string CaptureOnFailure(string testClass, string testMethod, Exception exception)
    {
        if (!_settings.CaptureOnFailure)
            return string.Empty;
        
        var (description, reason) = GetFailureInfo(exception);
        var filename = GenerateFilename(testClass, testMethod, description);
        return CaptureAndSave(filename, testClass, testMethod, reason);
    }
    
    /// <summary>
    /// Generates a filename following the pattern: {TestClass}_{TestMethod}_{Timestamp}_{Description}.{ext}
    /// </summary>
    private string GenerateFilename(string testClass, string testMethod, string description)
    {
        var timestamp = _settings.IncludeTimestamp
            ? $"_{DateTime.Now:yyyyMMdd_HHmmss}"
            : "";
        var ext = _settings.Format == ScreenshotFormat.Png ? "png" : "jpg";
        var filename = $"{testClass}_{testMethod}{timestamp}_{description}.{ext}";
        return SanitizeFilename(filename);
    }
    
    /// <summary>
    /// Captures and saves the screenshot, returning the file path.
    /// </summary>
    private string CaptureAndSave(string filename, string testClass, string testMethod, ScreenshotReason reason)
    {
        try
        {
            EnsureDirectoryExists();
            var path = Path.Combine(_settings.OutputDirectory, filename);
            _context.SaveScreenshot(path);
            _logger.LogScreenshot(testMethod, testClass, path, reason);
            TestArtifactManifestWriter.RecordArtifact(
                path,
                "screenshot",
                $"{testClass}.{testMethod}",
                reason.ToString(),
                new Dictionary<string, string?>
                {
                    ["testClass"] = testClass,
                    ["testMethod"] = testMethod,
                    ["reason"] = reason.ToString()
                });
            return path;
        }
        catch (Exception ex)
        {
            // Screenshot failures should not fail the test
            _logger.LogError(testMethod, testClass, "", "CaptureScreenshot", ex);
            return string.Empty;
        }
    }
    
    /// <summary>
    /// Ensures the output directory exists.
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_settings.OutputDirectory))
        {
            Directory.CreateDirectory(_settings.OutputDirectory);
        }
    }
    
    /// <summary>
    /// Maps exception types to failure descriptions and reasons.
    /// </summary>
    private static (string description, ScreenshotReason reason) GetFailureInfo(Exception ex)
    {
        return ex switch
        {
            AssertionException => ("assertion_failure", ScreenshotReason.AssertionFailure),
            ElementNotFoundException => ("element_not_found", ScreenshotReason.ElementNotFound),
            WaitTimeoutException => ("timeout", ScreenshotReason.Timeout),
            TimeoutException => ("timeout", ScreenshotReason.Timeout),
            _ => ("exception", ScreenshotReason.Exception)
        };
    }
    
    /// <summary>
    /// Removes invalid filename characters.
    /// </summary>
    private static string SanitizeFilename(string filename)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(filename.Select(c => invalidChars.Contains(c) ? '_' : c));
    }
}

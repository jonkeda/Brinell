namespace Brinell.Core.Screenshots;

/// <summary>
/// Base implementation with common file operations.
/// Technology-specific services inherit from this.
/// </summary>
public abstract class ScreenshotServiceBase : IScreenshotService
{
    private readonly string _outputDirectory;
    
    /// <summary>
    /// Create a screenshot service with the specified output directory.
    /// </summary>
    /// <param name="outputDirectory">
    /// Override for output directory. If null, uses UITEST_SCREENSHOT_DIR environment variable,
    /// or falls back to TestResults/Screenshots/{date} under current directory.
    /// </param>
    protected ScreenshotServiceBase(string? outputDirectory = null)
    {
        _outputDirectory = outputDirectory 
            ?? Environment.GetEnvironmentVariable("UITEST_SCREENSHOT_DIR")
            ?? Path.Combine(Environment.CurrentDirectory, "TestResults", "Screenshots", 
                DateTime.Now.ToString("yyyy-MM-dd"));
    }
    
    /// <inheritdoc />
    public string ScreenshotDirectory => _outputDirectory;
    
    /// <inheritdoc />
    public abstract byte[] CaptureWindow();
    
    /// <inheritdoc />
    public string SaveScreenshot(byte[] imageData, string testName, string suffix)
    {
        if (imageData.Length == 0)
            return string.Empty;
            
        try
        {
            Directory.CreateDirectory(_outputDirectory);
            
            var sanitizedName = SanitizeFileName(testName);
            var sanitizedSuffix = SanitizeSuffix(suffix);
            var timestamp = DateTime.Now.ToString("HHmmss");
            var fileName = $"{sanitizedName}_{timestamp}_{sanitizedSuffix}.png";
            var filePath = Path.Combine(_outputDirectory, fileName);
            
            File.WriteAllBytes(filePath, imageData);
            return filePath;
        }
        catch
        {
            return string.Empty;
        }
    }
    
    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
    
    private static string SanitizeSuffix(string suffix)
    {
        return suffix
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(":", "-");
    }
}

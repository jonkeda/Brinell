using System.Drawing;
using System.Drawing.Imaging;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;

namespace Brinell.Wpf.VisualValidation;

/// <summary>
/// Captures screenshots for visual validation testing.
/// Provides methods to capture full windows, specific elements, and regions.
/// </summary>
public class ScreenshotCapture
{
    private readonly string _outputDirectory;
    private readonly string _sessionId;

    /// <summary>
    /// Creates a new ScreenshotCapture instance.
    /// </summary>
    /// <param name="outputDirectory">Base directory for storing screenshots.</param>
    /// <param name="sessionId">Unique session identifier for organizing captures.</param>
    public ScreenshotCapture(string outputDirectory, string? sessionId = null)
    {
        _outputDirectory = outputDirectory;
        _sessionId = sessionId ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
        // Create output directory structure
        Directory.CreateDirectory(GetSessionDirectory());
    }

    /// <summary>
    /// Gets the directory for the current session.
    /// </summary>
    public string GetSessionDirectory()
    {
        return Path.Combine(_outputDirectory, _sessionId);
    }

    /// <summary>
    /// Captures a screenshot of the entire screen.
    /// </summary>
    public ScreenshotResult CaptureScreen(string name)
    {
        try
        {
            var screenshot = Capture.Screen();
            return SaveScreenshot(screenshot, name, "screen");
        }
        catch (Exception ex)
        {
            return ScreenshotResult.Failed(name, ex);
        }
    }

    /// <summary>
    /// Captures a screenshot of a specific window.
    /// </summary>
    public ScreenshotResult CaptureWindow(Window window, string name)
    {
        if (window == null)
            return ScreenshotResult.Failed(name, new ArgumentNullException(nameof(window)));

        try
        {
            var screenshot = Capture.Element(window);
            return SaveScreenshot(screenshot, name, "window");
        }
        catch (Exception ex)
        {
            return ScreenshotResult.Failed(name, ex);
        }
    }

    /// <summary>
    /// Captures a screenshot of a specific UI element.
    /// </summary>
    public ScreenshotResult CaptureElement(AutomationElement element, string name)
    {
        if (element == null)
            return ScreenshotResult.Failed(name, new ArgumentNullException(nameof(element)));

        try
        {
            var screenshot = Capture.Element(element);
            return SaveScreenshot(screenshot, name, "element");
        }
        catch (Exception ex)
        {
            return ScreenshotResult.Failed(name, ex);
        }
    }

    /// <summary>
    /// Captures a screenshot of a specific region.
    /// </summary>
    public ScreenshotResult CaptureRegion(Rectangle region, string name)
    {
        try
        {
            var screenshot = Capture.Rectangle(region);
            return SaveScreenshot(screenshot, name, "region");
        }
        catch (Exception ex)
        {
            return ScreenshotResult.Failed(name, ex);
        }
    }

    /// <summary>
    /// Captures the main content area of a window (excluding title bar and borders).
    /// </summary>
    public ScreenshotResult CaptureClientArea(Window window, string name)
    {
        if (window == null)
            return ScreenshotResult.Failed(name, new ArgumentNullException(nameof(window)));

        try
        {
            // Get the window's bounding rectangle
            var bounds = window.BoundingRectangle;
            
            // Estimate client area (approximate - removes ~32px title bar and ~8px borders)
            var clientArea = new Rectangle(
                bounds.X + 8,
                bounds.Y + 32,
                bounds.Width - 16,
                bounds.Height - 40);

            var screenshot = Capture.Rectangle(clientArea);
            return SaveScreenshot(screenshot, name, "client");
        }
        catch (Exception ex)
        {
            return ScreenshotResult.Failed(name, ex);
        }
    }

    /// <summary>
    /// Captures multiple elements with a common prefix.
    /// </summary>
    public IEnumerable<ScreenshotResult> CaptureElements(
        IEnumerable<(AutomationElement Element, string Name)> elements,
        string prefix = "")
    {
        var results = new List<ScreenshotResult>();
        foreach (var (element, name) in elements)
        {
            var fullName = string.IsNullOrEmpty(prefix) ? name : $"{prefix}_{name}";
            results.Add(CaptureElement(element, fullName));
        }
        return results;
    }

    /// <summary>
    /// Captures a view with metadata for AI validation.
    /// </summary>
    public ViewCapture CaptureView(Window window, string viewName, string? description = null)
    {
        var result = CaptureWindow(window, viewName);
        return new ViewCapture(viewName, result, description);
    }

    private ScreenshotResult SaveScreenshot(CaptureImage capture, string name, string type)
    {
        var sanitizedName = SanitizeFileName(name);
        var fileName = $"{sanitizedName}_{type}.png";
        var filePath = Path.Combine(GetSessionDirectory(), fileName);

        try
        {
            capture.ToFile(filePath);
            
            return new ScreenshotResult
            {
                Name = name,
                FilePath = filePath,
                Type = type,
                CapturedAt = DateTime.Now,
                Width = capture.OriginalBounds.Width,
                Height = capture.OriginalBounds.Height,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return ScreenshotResult.Failed(name, ex);
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}

/// <summary>
/// Result of a screenshot capture operation.
/// </summary>
public class ScreenshotResult
{
    public required string Name { get; init; }
    public string? FilePath { get; init; }
    public string? Type { get; init; }
    public DateTime CapturedAt { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }

    public static ScreenshotResult Failed(string name, Exception ex)
    {
        return new ScreenshotResult
        {
            Name = name,
            Success = false,
            ErrorMessage = ex.Message,
            Exception = ex,
            CapturedAt = DateTime.Now
        };
    }
}

/// <summary>
/// A captured view with metadata for AI validation.
/// </summary>
public class ViewCapture
{
    public string ViewName { get; }
    public ScreenshotResult Screenshot { get; }
    public string? Description { get; }
    public Dictionary<string, string> Metadata { get; } = new();

    public ViewCapture(string viewName, ScreenshotResult screenshot, string? description = null)
    {
        ViewName = viewName;
        Screenshot = screenshot;
        Description = description;
    }

    /// <summary>
    /// Adds metadata for AI validation context.
    /// </summary>
    public ViewCapture WithMetadata(string key, string value)
    {
        Metadata[key] = value;
        return this;
    }
}

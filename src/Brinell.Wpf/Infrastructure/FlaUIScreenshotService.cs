using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using Brinell.Core.Screenshots;

namespace Brinell.Wpf.Infrastructure;

/// <summary>
/// WPF/FlaUI-specific screenshot capture service.
/// Captures only the test window, not the entire desktop.
/// </summary>
public class FlaUIScreenshotService : ScreenshotServiceBase
{
    private readonly Func<Window?> _windowProvider;
    
    /// <summary>
    /// Create a FlaUI screenshot service.
    /// </summary>
    /// <param name="windowProvider">Function that returns the current main window.</param>
    /// <param name="outputDirectory">Optional output directory override.</param>
    public FlaUIScreenshotService(Func<Window?> windowProvider, string? outputDirectory = null)
        : base(outputDirectory)
    {
        _windowProvider = windowProvider ?? throw new ArgumentNullException(nameof(windowProvider));
    }
    
    /// <inheritdoc />
    public override byte[] CaptureWindow()
    {
        try
        {
            var window = _windowProvider();
            if (window == null)
                return [];
            
            // Use FlaUI's built-in capture which handles DPI scaling correctly
            var capture = Capture.Element(window);
            using var stream = new MemoryStream();
            capture.Bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }
        catch
        {
            return [];
        }
    }
}

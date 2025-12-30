using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Image control wrapper.
/// Provides image display functionality.
/// </summary>
public class ImageControl : ControlBase
{
    public ImageControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ImageControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the image source.
    /// </summary>
    public string? GetSource()
    {
        var element = FindElement();
        return element?.GetAttribute("source") ?? element?.GetAttribute("src");
    }

    /// <summary>
    /// Check if the image is loaded (not loading and has dimensions).
    /// </summary>
    public bool IsLoaded()
    {
        var element = FindElement();
        if (element == null) return false;
        
        var isLoading = element.GetAttribute("isLoading");
        if (isLoading?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false)
            return false;
        
        var size = element.Size;
        return size.Width > 0 && size.Height > 0;
    }

    /// <summary>
    /// Get the image aspect ratio.
    /// </summary>
    public string? GetAspect()
    {
        var element = FindElement();
        return element?.GetAttribute("aspect");
    }

    /// <summary>
    /// Wait for image to finish loading.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    public bool WaitForImageLoad(int? timeoutMs = null)
    {
        Log("WaitForImageLoad()");
        return _context.WaitFor(IsLoaded, timeoutMs, "image load complete");
    }

    #region Assert Methods

    /// <summary>
    /// Assert the image source.
    /// </summary>
    public void AssertSource(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSource();
        if (actual != expected)
        {
            ThrowAssertionFailed("Source", actual ?? "(null)", expected,
                message ?? $"Expected source '{expected}' but got '{actual}'.");
        }
        LogAssertPass("Source", actual ?? "(null)", expected);
    }

    /// <summary>
    /// Assert the image source contains expected text.
    /// </summary>
    public void AssertSourceContains(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSource() ?? string.Empty;
        if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            ThrowAssertionFailed("SourceContains", actual, $"contains '{expected}'",
                message ?? $"Expected source to contain '{expected}' but got '{actual}'.");
        }
        LogAssertPass("SourceContains", actual, expected);
    }

    /// <summary>
    /// Assert the image is loaded.
    /// </summary>
    public void AssertLoaded(string? message = null)
    {
        CheckVisible(expected: true);
        if (!IsLoaded())
        {
            ThrowAssertionFailed("Loaded", "not loaded", "loaded",
                message ?? "Expected image to be loaded.");
        }
        LogAssertPass("Loaded", "loaded", "loaded");
    }

    /// <summary>
    /// Assert the image aspect.
    /// </summary>
    public void AssertAspect(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetAspect();
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            ThrowAssertionFailed("Aspect", actual ?? "(null)", expected,
                message ?? $"Expected aspect '{expected}' but got '{actual}'.");
        }
        LogAssertPass("Aspect", actual ?? "(null)", expected);
    }

    #endregion
}

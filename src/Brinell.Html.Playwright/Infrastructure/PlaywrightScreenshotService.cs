using Microsoft.Playwright;
using Brinell.Core.Screenshots;

namespace Brinell.Html.Playwright.Infrastructure;

/// <summary>
/// Playwright/Web-specific screenshot capture service.
/// Captures the browser window or full page.
/// </summary>
public class PlaywrightScreenshotService : ScreenshotServiceBase
{
    private readonly Func<IPage?> _pageProvider;

    /// <summary>
    /// Create a Playwright screenshot service.
    /// </summary>
    /// <param name="pageProvider">Function that returns the current Playwright page.</param>
    /// <param name="outputDirectory">Optional output directory override.</param>
    public PlaywrightScreenshotService(Func<IPage?> pageProvider, string? outputDirectory = null)
        : base(outputDirectory)
    {
        _pageProvider = pageProvider ?? throw new ArgumentNullException(nameof(pageProvider));
    }

    /// <inheritdoc />
    public override byte[] CaptureWindow()
    {
        try
        {
            var page = _pageProvider();
            if (page == null)
                return [];

            // Playwright's ScreenshotAsync captures the browser viewport
            return page.ScreenshotAsync().GetAwaiter().GetResult();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Capture a full page screenshot (scrolling content included).
    /// </summary>
    public byte[] CaptureFullPage()
    {
        try
        {
            var page = _pageProvider();
            if (page == null)
                return [];

            return page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true })
                .GetAwaiter().GetResult();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Capture a screenshot asynchronously.
    /// </summary>
    public async Task<byte[]> CaptureWindowAsync()
    {
        try
        {
            var page = _pageProvider();
            if (page == null)
                return [];

            return await page.ScreenshotAsync();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Capture a full page screenshot asynchronously.
    /// </summary>
    public async Task<byte[]> CaptureFullPageAsync()
    {
        try
        {
            var page = _pageProvider();
            if (page == null)
                return [];

            return await page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
        }
        catch
        {
            return [];
        }
    }
}

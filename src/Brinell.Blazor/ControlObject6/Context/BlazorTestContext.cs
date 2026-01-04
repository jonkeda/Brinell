using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Microsoft.Playwright;

namespace Brinell.Blazor.ControlObject6.Context;

/// <summary>
/// Test context for Blazor/Playwright-based UI tests.
/// Manages the Playwright page and provides configuration.
/// </summary>
/// <remarks>
/// Control creation uses the 'new' pattern directly in PageObjects:
/// <code>
/// var button = new ButtonControl(context, "submit-btn", page);
/// var input = new InputControl(context, "username", page);
/// </code>
/// </remarks>
public class BlazorTestContext : IAsyncTestContext
{
    private readonly IPage _page;

    /// <inheritdoc />
    public int DefaultTimeoutMs { get; set; } = 30000;

    /// <inheritdoc />
    public int DefaultPollingIntervalMs { get; set; } = 100;

    /// <inheritdoc />
    public IAsyncPageObject? CurrentPage { get; private set; }

    /// <summary>
    /// Gets the underlying Playwright page.
    /// </summary>
    public IPage Page => _page;

    /// <summary>
    /// Creates a new Blazor test context with the specified page.
    /// </summary>
    /// <param name="page">The Playwright page to use.</param>
    public BlazorTestContext(IPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
    }

    /// <inheritdoc />
    public async Task NavigateToAsync(string? route, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (route is null) return;

        await _page.GotoAsync(route, new PageGotoOptions
        {
            Timeout = timeoutMs ?? DefaultTimeoutMs
        });
    }

    /// <inheritdoc />
    public async Task<TPage> NavigateToAsync<TPage>(int? timeoutMs = null, CancellationToken ct = default) 
        where TPage : IAsyncPageObject
    {
        var pageObject = CreatePage<TPage>();
        CurrentPage = pageObject;
        await pageObject.WaitLoadedAsync(true, timeoutMs ?? DefaultTimeoutMs, ct);
        return pageObject;
    }

    /// <inheritdoc />
    public async Task TakeScreenshotAsync(string? filename, CancellationToken ct = default)
    {
        if (filename is null) return;

        var path = Path.Combine(
            Directory.GetCurrentDirectory(),
            $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        
        await _page.ScreenshotAsync(new PageScreenshotOptions { Path = path });
    }

    /// <inheritdoc />
    public void Log(string? message)
    {
        if (message is null) return;
        Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss.fff} - {message}");
    }

    /// <inheritdoc />
    public void LogError(string? message)
    {
        if (message is null) return;
        Console.Error.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} - {message}");
    }

    /// <summary>
    /// Creates a page object of the specified type.
    /// </summary>
    public TPage CreatePage<TPage>() where TPage : IAsyncPageObject
    {
        return (TPage)Activator.CreateInstance(typeof(TPage), this)!;
    }
}

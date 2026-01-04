using Brinell.Blazor.ControlObject6.Context;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.ControlObject6.TestBase;

/// <summary>
/// Base class for Blazor UI tests using ControlObject6 API.
/// Manages Playwright browser lifecycle and provides BlazorTestContext.
/// </summary>
public abstract class BlazorTestBase6 : IAsyncLifetime
{
    protected BlazorTestContext Context = null!;
    protected readonly ITestOutputHelper Output;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
    private IPage? _page;

    /// <summary>
    /// Environment variable name for the Blazor app URL.
    /// </summary>
    private const string BlazorAppUrlEnvVar = "BLAZOR_APP_URL";

    /// <summary>
    /// Default URL for the Blazor application.
    /// </summary>
    private const string DefaultBlazorAppUrl = "http://localhost:5180";

    protected BlazorTestBase6(ITestOutputHelper output)
    {
        Output = output;
    }

    /// <summary>
    /// Gets the base URL for the Blazor application.
    /// </summary>
    protected virtual string BaseUrl =>
        Environment.GetEnvironmentVariable(BlazorAppUrlEnvVar) ?? DefaultBlazorAppUrl;

    /// <summary>
    /// Whether to run browser in headless mode.
    /// </summary>
    protected virtual bool Headless =>
        Environment.GetEnvironmentVariable("HEADLESS")?.ToLowerInvariant() == "true";

    public async Task InitializeAsync()
    {
        Log($"Initializing BlazorTestBase6 for {GetType().Name}");

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless
        });

        _browserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        _page = await _browserContext.NewPageAsync();
        Context = new BlazorTestContext(_page);
        Context.DefaultTimeoutMs = 10000;

        // Navigate to base URL
        await _page.GotoAsync(BaseUrl);

        Log("Playwright browser initialized");
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_page != null)
            {
                await _page.CloseAsync();
            }

            if (_browserContext != null)
            {
                await _browserContext.CloseAsync();
            }

            if (_browser != null)
            {
                await _browser.CloseAsync();
            }

            _playwright?.Dispose();
        }
        catch (Exception ex)
        {
            Log($"Error during cleanup: {ex.Message}");
        }
    }

    protected void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Output.WriteLine($"[{timestamp}] {message}");
    }

    /// <summary>
    /// Navigate to a relative path.
    /// </summary>
    protected async Task NavigateToAsync(string relativePath)
    {
        var url = $"{BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        Log($"Navigating to: {url}");
        await _page!.GotoAsync(url);
        await WaitForBlazorReadyAsync();
    }

    /// <summary>
    /// Wait for Blazor to be fully loaded and interactive.
    /// </summary>
    protected async Task WaitForBlazorReadyAsync(int timeoutMs = 10000)
    {
        Log($"Waiting for Blazor to be ready (timeout: {timeoutMs}ms)");

        // Wait for document ready state
        await _page!.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Small delay for Blazor SignalR connection
        await Task.Delay(500);
    }
}

using Brinell.Html.Playwright.Testing;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.PlaywrightTests.TestBase;

/// <summary>
/// Base class for Brinell Blazor Sample application UI tests using Playwright.
/// Configures the base URL and provides common test infrastructure.
/// </summary>
public abstract class BlazorPlaywrightTestBase : PlaywrightUITestBase
{
    /// <summary>
    /// Environment variable name for the Blazor app URL.
    /// </summary>
    private const string BlazorAppUrlEnvVar = "BLAZOR_APP_URL";

    /// <summary>
    /// Default URL for the Blazor application.
    /// </summary>
    private const string DefaultBlazorAppUrl = "http://localhost:5180";

    protected BlazorPlaywrightTestBase(ITestOutputHelper output)
        : base(output.WriteLine)
    {
    }

    /// <summary>
    /// Gets the base URL for the Blazor application.
    /// Uses BLAZOR_APP_URL environment variable or defaults to http://localhost:5180.
    /// </summary>
    protected override string BaseUrl =>
        Environment.GetEnvironmentVariable(BlazorAppUrlEnvVar) ?? DefaultBlazorAppUrl;

    /// <summary>
    /// Whether to run browser in headless mode.
    /// Uses HEADLESS environment variable. Default is true for Playwright.
    /// </summary>
    protected override bool Headless =>
        Environment.GetEnvironmentVariable("HEADLESS")?.ToLowerInvariant() != "false";

    /// <summary>
    /// Wait for Blazor to be fully loaded and interactive.
    /// </summary>
    protected async Task WaitForBlazorReadyAsync(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? 10000;
        Log($"Waiting for Blazor to be ready (timeout: {timeout}ms)");

        // Wait for document ready state
        await WaitForDocumentReadyAsync(timeout);

        // Wait for Blazor SignalR connection to be established
        await WaitForBlazorConnectionAsync(timeout);
    }

    /// <summary>
    /// Wait for the document to be in ready state.
    /// </summary>
    protected async Task WaitForDocumentReadyAsync(int timeoutMs = 10000)
    {
        var ready = await Context.WaitForAsync(async () =>
        {
            var readyState = await ExecuteScriptAsync<string>("document.readyState");
            return readyState == "complete";
        }, timeoutMs, "document ready");

        if (!ready)
        {
            Log("WARNING: Document did not reach ready state within timeout");
        }
    }

    /// <summary>
    /// Wait for Blazor SignalR connection to be established.
    /// </summary>
    protected async Task WaitForBlazorConnectionAsync(int timeoutMs = 10000)
    {
        // Blazor Server uses SignalR - wait for the connection to be established
        var connected = await Context.WaitForAsync(async () =>
        {
            try
            {
                // Check if Blazor has initialized by looking for the connection state
                var result = await ExecuteScriptAsync<bool>(@"
                    if (typeof Blazor !== 'undefined' && Blazor._internal) {
                        return true;
                    }
                    // Alternative: check if any interactive elements have handlers
                    return document.querySelector('[blazor\\:elementReference]') !== null ||
                           document.readyState === 'complete';
                ");
                return result;
            }
            catch
            {
                return false;
            }
        }, timeoutMs, "Blazor connection");

        if (!connected)
        {
            Log("WARNING: Blazor connection check timed out - continuing anyway");
        }
    }

    /// <summary>
    /// Wait for any loading spinners to disappear.
    /// </summary>
    protected async Task WaitForLoadingCompleteAsync(int timeoutMs = 10000)
    {
        await Context.WaitForAsync(async () =>
        {
            var spinner = await ExecuteScriptAsync<object?>("document.querySelector('.spinner-border')");
            return spinner == null;
        }, timeoutMs, "loading complete");
    }

    /// <summary>
    /// Navigate to a page and wait for Blazor to be ready.
    /// </summary>
    protected async Task NavigateToPageAsync(string relativePath)
    {
        await NavigateToAsync(relativePath);
        await WaitForBlazorReadyAsync();
    }
}

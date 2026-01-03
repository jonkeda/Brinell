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
    /// Uses Playwright's built-in load state waiting for reliability.
    /// </summary>
    protected async Task WaitForBlazorReadyAsync(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? 5000;
        Log($"Waiting for Blazor to be ready (timeout: {timeout}ms)");

        // Use Playwright's built-in load state waiting - more reliable than polling
        await WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
    }

    /// <summary>
    /// Wait for the document to be in ready state.
    /// </summary>
    protected async Task WaitForDocumentReadyAsync(int timeoutMs = 5000)
    {
        // Use Playwright's built-in waiting
        await WaitForLoadStateAsync(Microsoft.Playwright.LoadState.DOMContentLoaded);
    }

    /// <summary>
    /// Wait for Blazor SignalR connection to be established.
    /// This is a lightweight check that succeeds immediately if document is ready.
    /// </summary>
    protected async Task WaitForBlazorConnectionAsync(int timeoutMs = 2000)
    {
        // For Blazor Server, once NetworkIdle is reached, the SignalR connection is established
        // Just do a quick check that the document is interactive
        var connected = await Context.WaitForAsync(async () =>
        {
            try
            {
                var readyState = await ExecuteScriptAsync<string>("document.readyState");
                return readyState == "complete" || readyState == "interactive";
            }
            catch
            {
                return false;
            }
        }, timeoutMs, "document interactive");

        if (!connected)
        {
            Log("WARNING: Document not ready - continuing anyway");
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

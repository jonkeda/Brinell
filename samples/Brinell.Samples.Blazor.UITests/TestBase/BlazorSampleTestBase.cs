using Brinell.Html.Testing;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.TestBase;

/// <summary>
/// Base class for Brinell Blazor Sample application UI tests.
/// Configures the base URL and provides common test infrastructure.
/// </summary>
public abstract class BlazorSampleTestBase : HtmlUITestBase
{
    /// <summary>
    /// Environment variable name for the Blazor app URL.
    /// </summary>
    private const string BlazorAppUrlEnvVar = "BLAZOR_APP_URL";

    /// <summary>
    /// Default URL for the Blazor application.
    /// </summary>
    private const string DefaultBlazorAppUrl = "http://localhost:5180";

    protected BlazorSampleTestBase(ITestOutputHelper output)
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
    /// Override to true for CI/CD environments.
    /// </summary>
    protected override bool Headless =>
        Environment.GetEnvironmentVariable("HEADLESS")?.ToLowerInvariant() == "true";

    /// <summary>
    /// Wait for Blazor to be fully loaded and interactive.
    /// </summary>
    protected void WaitForBlazorReady(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? 10000;
        Log($"Waiting for Blazor to be ready (timeout: {timeout}ms)");

        // Wait for document ready state
        WaitForDocumentReady(timeout);

        // Wait for Blazor SignalR connection to be established
        WaitForBlazorConnection(timeout);
    }

    /// <summary>
    /// Wait for the document to be in ready state.
    /// </summary>
    protected void WaitForDocumentReady(int timeoutMs = 10000)
    {
        var ready = Context?.WaitFor(() =>
        {
            var readyState = ExecuteScript("return document.readyState;") as string;
            return readyState == "complete";
        }, timeoutMs, "document ready");

        if (ready != true)
        {
            Log("WARNING: Document did not reach ready state within timeout");
        }
    }

    /// <summary>
    /// Wait for Blazor SignalR connection to be established.
    /// </summary>
    protected void WaitForBlazorConnection(int timeoutMs = 10000)
    {
        // Blazor Server uses SignalR - wait for the connection to be established
        // The _blazorServerConnection object exists when connected
        var connected = Context?.WaitFor(() =>
        {
            try
            {
                // Check if Blazor has initialized by looking for the connection state
                var result = ExecuteScript(@"
                    if (typeof Blazor !== 'undefined' && Blazor._internal) {
                        return true;
                    }
                    // Alternative: check if any interactive elements have handlers
                    return document.querySelector('[blazor\\:elementReference]') !== null ||
                           document.readyState === 'complete';
                ");
                return result is true;
            }
            catch
            {
                return false;
            }
        }, timeoutMs, "Blazor connection");

        if (connected != true)
        {
            Log("WARNING: Blazor connection check timed out - continuing anyway");
        }
    }

    /// <summary>
    /// Wait for any loading spinners to disappear.
    /// </summary>
    protected void WaitForLoadingComplete(int timeoutMs = 10000)
    {
        Context?.WaitFor(() =>
        {
            var spinner = ExecuteScript("return document.querySelector('.spinner-border');");
            return spinner == null;
        }, timeoutMs, "loading complete");
    }

    /// <summary>
    /// Navigate to a page and wait for Blazor to be ready.
    /// </summary>
    protected void NavigateToPage(string relativePath)
    {
        NavigateTo(relativePath);
        WaitForBlazorReady();
    }
}

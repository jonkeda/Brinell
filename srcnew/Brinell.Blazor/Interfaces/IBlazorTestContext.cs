using Brinell.Core.Interfaces;
using OpenQA.Selenium;

namespace Brinell.Blazor.Interfaces;

/// <summary>
/// Blazor-specific test context that narrows the generic TElement to IWebElement.
/// Provides access to Selenium WebDriver and Blazor-specific functionality.
/// </summary>
public interface IBlazorTestContext : ITestContext<IWebElement>
{
    /// <summary>
    /// Gets the Selenium WebDriver instance for direct driver access when needed.
    /// </summary>
    IWebDriver Driver { get; }

    /// <summary>
    /// Gets the base URL for the Blazor application being tested.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Waits for Blazor to complete all pending renders and async operations.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if Blazor became idle within timeout, false otherwise.</returns>
    bool WaitForBlazorIdle(int? timeoutMs = null);

    /// <summary>
    /// Executes JavaScript in the browser context.
    /// </summary>
    /// <param name="script">The JavaScript code to execute.</param>
    /// <param name="args">Arguments to pass to the script.</param>
    /// <returns>The result of the script execution.</returns>
    object? ExecuteScript(string script, params object[] args);
}

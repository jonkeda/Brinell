using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Blazor.ControlObject6.Interfaces;

/// <summary>
/// Async version of ITestContext for Blazor/Playwright.
/// </summary>
/// <remarks>
/// Control creation uses the 'new' pattern directly in PageObjects:
/// <code>
/// var button = new ButtonControl(context, "submit-btn", this);
/// </code>
/// </remarks>
public interface IAsyncTestContext
{
    int DefaultTimeoutMs { get; set; }
    int DefaultPollingIntervalMs { get; set; }
    IAsyncPageObject? CurrentPage { get; }

    Task NavigateToAsync(string? route, int? timeoutMs = null, CancellationToken ct = default);
    Task<TPage> NavigateToAsync<TPage>(int? timeoutMs = null, CancellationToken ct = default) where TPage : IAsyncPageObject;

    Task TakeScreenshotAsync(string? filename, CancellationToken ct = default);
    void Log(string? message);
    void LogError(string? message);
}

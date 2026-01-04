using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Blazor.ControlObject6.Interfaces;

/// <summary>
/// Async version of IPageObject for Blazor/Playwright.
/// </summary>
/// <remarks>
/// Controls should be created using the 'new' pattern in derived page classes:
/// <code>
/// public ButtonControl SubmitButton => new(Context, "submitBtn", this);
/// public InputControl UsernameInput => new(Context, "username", this);
/// </code>
/// </remarks>
public interface IAsyncPageObject
{
    /// <summary>
    /// The name of this page.
    /// </summary>
    string Name { get; }

    Task<bool> IsLoadedAsync(int? timeoutMs = null, CancellationToken ct = default);
    Task<bool> WaitLoadedAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertLoadedAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    Task<string> GetTitleAsync(int? timeoutMs = null, CancellationToken ct = default);
    Task AssertTitleAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    Task<bool> ControlExistsAsync(ControlLocator locator, int? timeoutMs = null, CancellationToken ct = default);
    Task<bool> WaitControlExistsAsync(ControlLocator locator, bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertControlExistsAsync(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    Task TakeScreenshotAsync(string? filename, int? timeoutMs = null, CancellationToken ct = default);
    Task ScrollToControlAsync(ControlLocator? locator, int? timeoutMs = null, CancellationToken ct = default);
}

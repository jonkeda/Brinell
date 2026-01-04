using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Blazor.ControlObject6.Interfaces;

/// <summary>
/// Async version of IControlObject for Blazor/Playwright.
/// All operations are async due to Playwright's async nature.
/// </summary>
public interface IAsyncControlObject
{
    /// <summary>
    /// The locator used to find this control.
    /// </summary>
    ControlLocator Locator { get; }

    /// <summary>
    /// The page that contains this control.
    /// </summary>
    IAsyncPageObject? Page { get; }

    #region Existence

    Task<bool> IsExistsAsync(CancellationToken ct = default);
    Task<bool> WaitExistsAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    Task CheckExistsAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertExistsAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Visibility

    Task<bool> IsVisibleAsync(CancellationToken ct = default);
    Task<bool> WaitVisibleAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    Task CheckVisibleAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertVisibleAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Text

    Task<string> GetTextAsync(int? timeoutMs = null, CancellationToken ct = default);
    Task AssertTextAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertTextContainsAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertTextStartsWithAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertTextEndsWithAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertTextMatchesAsync(string? pattern, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertTextEmptyAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}

/// <summary>
/// Async version of IInteractiveControlObject.
/// </summary>
public interface IAsyncInteractiveControlObject : IAsyncControlObject
{
    Task<bool> IsEnabledAsync(CancellationToken ct = default);
    Task<bool> WaitEnabledAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    Task CheckEnabledAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertEnabledAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
}

/// <summary>
/// Async version of IFocusableControlObject.
/// </summary>
public interface IAsyncFocusableControlObject : IAsyncInteractiveControlObject
{
    Task<bool> IsFocusedAsync(CancellationToken ct = default);
    Task<bool> WaitFocusedAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    Task CheckFocusedAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertFocusedAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    Task FocusAsync(int? timeoutMs = null, CancellationToken ct = default);
    Task BlurAsync(int? timeoutMs = null, CancellationToken ct = default);
}

/// <summary>
/// Async version of IClickableControlObject.
/// </summary>
public interface IAsyncClickableControlObject : IAsyncInteractiveControlObject
{
    Task ClickAsync(int? timeoutMs = null, CancellationToken ct = default);
    Task DoubleClickAsync(int? timeoutMs = null, CancellationToken ct = default);
    Task RightClickAsync(int? timeoutMs = null, CancellationToken ct = default);
    Task HoverAsync(int? timeoutMs = null, CancellationToken ct = default);
}

/// <summary>
/// Async version of ITextControlObject.
/// </summary>
public interface IAsyncTextControlObject : IAsyncFocusableControlObject
{
    Task EnterAsync(string? text, int? timeoutMs = null, CancellationToken ct = default);
    Task ClearAsync(int? timeoutMs = null, CancellationToken ct = default);
    Task ClearAndEnterAsync(string? text, int? timeoutMs = null, CancellationToken ct = default);
    Task AppendAsync(string? text, int? timeoutMs = null, CancellationToken ct = default);
    Task<bool> IsReadOnlyAsync(CancellationToken ct = default);
    Task<bool> WaitReadOnlyAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    Task AssertReadOnlyAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    Task<int> GetTextLengthAsync(int? timeoutMs = null, CancellationToken ct = default);
    Task AssertTextLengthAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
}

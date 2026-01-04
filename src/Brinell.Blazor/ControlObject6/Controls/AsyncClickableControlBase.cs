using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Base class for clickable controls like buttons and links (async).
/// Provides virtual async click methods that can be overridden.
/// </summary>
public abstract class AsyncClickableControlBase : AsyncControlObjectBase, IAsyncClickableControlObject
{
    /// <summary>
    /// Creates a new clickable control.
    /// </summary>
    protected AsyncClickableControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new clickable control using TestId.
    /// </summary>
    protected AsyncClickableControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page)
    {
    }

    /// <inheritdoc />
    public virtual async Task ClickAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("ClickAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().ClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    /// <inheritdoc />
    public virtual async Task DoubleClickAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("DoubleClickAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().DblClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    /// <inheritdoc />
    public virtual async Task RightClickAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("RightClickAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().ClickAsync(new()
        {
            Button = Microsoft.Playwright.MouseButton.Right,
            Timeout = timeoutMs ?? DefaultTimeoutMs
        });
    }

    /// <inheritdoc />
    public virtual async Task HoverAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("HoverAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);

        await GetLocator().HoverAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }
}

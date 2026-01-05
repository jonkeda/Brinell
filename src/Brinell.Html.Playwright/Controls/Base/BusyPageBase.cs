using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls.Base;

/// <summary>
/// Alias for LoadingPageBase for naming consistency across platforms.
/// Some platforms use "Busy" terminology instead of "Loading".
/// Implements IBusyPageObject for cross-platform busy state tracking.
/// </summary>
public abstract class BusyPageBase : LoadingPageBase, IBusyPageObject
{
    /// <summary>
    /// CSS selector for the busy indicator.
    /// Alias for LoadingIndicatorSelector.
    /// </summary>
    protected virtual string? BusyIndicatorSelector => null;
    
    /// <summary>
    /// Override to use BusyIndicatorSelector if LoadingIndicatorSelector not set.
    /// </summary>
    protected override string? LoadingIndicatorSelector => BusyIndicatorSelector;

    protected BusyPageBase(PlaywrightTestContext context) : base(context)
    {
    }

    /// <summary>
    /// Check if the page is busy.
    /// Alias for IsLoading.
    /// </summary>
    public virtual bool IsBusy() => IsLoading();

    /// <summary>
    /// Check if the page is busy asynchronously.
    /// Alias for IsLoadingAsync.
    /// </summary>
    public virtual Task<bool> IsBusyAsync() => IsLoadingAsync();

    /// <summary>
    /// Wait for the page to not be busy.
    /// Alias for WaitForLoaded.
    /// </summary>
    public virtual bool WaitForNotBusy(int? timeoutMs = null) => WaitForLoaded(timeoutMs);

    /// <summary>
    /// Wait for the page to not be busy asynchronously.
    /// Alias for WaitForLoadedAsync.
    /// </summary>
    public virtual Task<bool> WaitForNotBusyAsync(int? timeoutMs = null) => WaitForLoadedAsync(timeoutMs);

    /// <summary>
    /// Assert the page is not busy.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertNotBusy(string? message = null)
    {
        if (IsBusy())
        {
            ThrowPageNotReady("AssertNotBusy", 
                message ?? $"Expected page '{Name}' to not be busy but it is currently busy.");
        }
    }

    /// <summary>
    /// Assert the page is not busy asynchronously.
    /// </summary>
    public virtual async Task AssertNotBusyAsync(string? message = null)
    {
        if (await IsBusyAsync())
        {
            ThrowPageNotReady("AssertNotBusy", 
                message ?? $"Expected page '{Name}' to not be busy but it is currently busy.");
        }
    }
}

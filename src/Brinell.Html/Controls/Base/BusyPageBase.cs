using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls.Base;

/// <summary>
/// Alias for LoadingPageBase for naming consistency across platforms.
/// Some platforms use "Busy" terminology instead of "Loading".
/// </summary>
public abstract class BusyPageBase : LoadingPageBase
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

    protected BusyPageBase(SeleniumTestContext context) : base(context)
    {
    }

    /// <summary>
    /// Check if the page is busy.
    /// Alias for IsLoading.
    /// </summary>
    public virtual bool IsBusy() => IsLoading();

    /// <summary>
    /// Wait for the page to not be busy.
    /// Alias for WaitForLoaded.
    /// </summary>
    public virtual bool WaitForNotBusy(int? timeoutMs = null) => WaitForLoaded(timeoutMs);
}

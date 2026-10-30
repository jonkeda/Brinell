using Bruinet.Core.Locators;
using Bruinet.NativeAndroid.Interfaces;

namespace Bruinet.NativeAndroid.Controls.Container;

/// <summary>
/// A pull-to-refresh layout (<c>SwipeRefreshLayout</c>). Android does not report whether it is
/// refreshing, so the page waits for the refresh through its busy signal (design 4) or the content
/// that changes.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class SwipeRefreshLayout<TScope> : ViewBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public SwipeRefreshLayout(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public SwipeRefreshLayout(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }

    /// <summary>Pulls the content down once, from its top, to start a refresh.</summary>
    /// <param name="element">The pre-found view.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void PullToRefreshCore(INativeAndroidElement element, int? timeoutMs = null)
    {
        element.Swipe(GestureDirection.Down, 0.6);
    }
}

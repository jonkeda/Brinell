using Brinell.Maui.Controls.Internal;

namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI RefreshView control: a container that refreshes its content when pulled down.
/// </summary>
/// <remarks>
/// <para>
/// Refreshing is declared as a capability (<see cref="IRefreshableControlObject{TScope}"/>)
/// and delegated to <see cref="GestureHelper"/>, rather than inherited from a refreshable
/// base class. C# allows one base class, and a RefreshView wraps a scrollable child — a
/// control that may well need both capabilities.
/// </para>
/// <para>
/// <b>Not addressable on Windows.</b> RefreshView maps to the WinUI
/// <c>RefreshContainer</c>, whose automation peer must not be overridden — doing so
/// collapses the entire UIA tree. Pull-to-refresh is a mobile gesture in any case; on
/// Windows, drive the bound command instead. These members exist for the planned
/// Android/iOS phase.
/// </para>
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class RefreshView<TScope> : Base.ViewBase<TScope>, IRefreshableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new refresh view control within the specified scope.
    /// </summary>
    public RefreshView(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new refresh view control using the scope's default locator strategy.
    /// </summary>
    public RefreshView(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    /// <inheritdoc />
    public TScope PullToRefresh(int? timeoutMs = null)
        => RunDoWithElement(element => GestureHelper.TryPullToRefresh(element), timeoutMs);

    /// <inheritdoc />
    public bool? IsRefreshing() => GestureHelper.IsRefreshing(TryFindElement());

    /// <inheritdoc />
    public bool WaitRefreshing(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        return RunWaitWithOptionalElement(expected,
            element => GestureHelper.IsRefreshing(element) == expected.Value,
            timeoutMs);
    }

    /// <summary>
    /// Asserts the control is refreshing.
    /// </summary>
    public TScope AssertRefreshing(string? message = null, int? timeoutMs = null)
        => AssertRefreshing(true, message, timeoutMs);

    /// <inheritdoc />
    public TScope AssertRefreshing(bool? expected, string? message = null, int? timeoutMs = null)
        => RunAssertWithOptionalElement(expected,
            GestureHelper.IsRefreshing, (actual, expected1) => actual == expected1,
            message ?? $"Expected Refreshing to be '{expected}'. Locator: {Locator}",
            timeoutMs);
}

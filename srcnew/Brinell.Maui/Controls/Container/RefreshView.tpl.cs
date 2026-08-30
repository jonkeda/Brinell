namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI RefreshView control: a container that refreshes its content when pulled down.
/// </summary>
/// <remarks>
/// <para>
/// Refreshing is declared as a capability (<see cref="IRefreshableControlObject{TScope}"/>)
/// rather than inherited from a refreshable base class. C# allows one base class, and a
/// RefreshView wraps a scrollable child — a control that may well need both capabilities.
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
public partial class RefreshView<TScope> : Base.ViewBase<TScope>, IRefreshableControlObject<TScope>
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

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Performs the pull-to-refresh gesture: a downward swipe from the element's top.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void PullToRefreshCore(IMauiElement element, int? timeoutMs = null)
        => element.TrySwipeDown();

    /// <summary>
    /// Reads the refreshing state from the pre-found element.
    /// </summary>
    /// <remarks>
    /// Read here rather than in the gesture extensions: refreshing is what this control
    /// <em>means</em>, not a property of any element, and only a RefreshView has it. The
    /// attribute is spelled differently depending on the platform mapping, so both names are
    /// tried; a control reporting neither is treated as not refreshing rather than unknown.
    /// </remarks>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True when refreshing, false when idle, null only when the element is null.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsRefreshingCore(IMauiElement? element)
    {
        if (element == null) return null;

        foreach (var name in new[] { "IsRefreshing", "Refreshing" })
        {
            var value = element.GetAttribute(name);
            if (!string.IsNullOrEmpty(value))
            {
                return value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Asserts the control is refreshing.
    /// </summary>
    /// <remarks>
    /// A message-only overload of the generated <c>AssertRefreshing(bool?, string?, int?)</c>;
    /// the generator emits one member per Core method and cannot know this shorthand is
    /// wanted.
    /// </remarks>
    public TScope AssertRefreshing(string? message, int? timeoutMs = null)
        => AssertRefreshing(true, message, timeoutMs);

    #endregion
}

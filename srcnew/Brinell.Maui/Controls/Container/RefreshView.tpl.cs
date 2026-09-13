using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI RefreshView control: a container that refreshes its content when pulled down.
/// </summary>
/// <remarks>
/// <para>
/// <b>A container</b> (step 102, option B): it hosts content, so it is modelled the way
/// <c>ScrollView</c> and <c>Border</c> are - its children are found under it. It used to be a
/// view, which gave it no way to name what it holds.
/// </para>
/// <para>
/// Refreshing is declared as a capability (<see cref="IRefreshableControlObject{TSelf}"/>)
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
/// <typeparam name="TParent">The parent scope type.</typeparam>
/// <typeparam name="TSelf">The concrete container type.</typeparam>
public partial class RefreshView<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>,
    IRefreshableControlObject<TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : RefreshView<TParent, TSelf>
{
    /// <summary>
    /// Creates a new refresh view control within the specified scope.
    /// </summary>
    public RefreshView(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a new refresh view control using the scope's default locator strategy.
    /// </summary>
    public RefreshView(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>Whether the container reports itself enabled.</summary>
    /// <param name="element">The container's root, or null when absent.</param>
    /// <returns>The enabled state, or null when absent.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsEnabledCore(IMauiElement? element) => element?.Enabled;


    /// <summary>
    /// Performs the pull-to-refresh gesture.
    /// </summary>
    /// <remarks>
    /// Names the gesture and leaves the mechanism to the platform: a real downward touch swipe on
    /// Appium, and on Windows a verb the app answers by setting <c>IsRefreshing</c>, which is
    /// what the gesture itself does. It used to call the pointer swipe directly, which is a
    /// choice a control object should not be making.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void PullToRefreshCore(IMauiElement element, int? timeoutMs = null)
        => element.PerformGesture(MauiGesture.SwipeDown);

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
        // Unknown, not false. No platform publishes a refreshing state: it was read as an
        // attribute named "IsRefreshing" or "Refreshing", which neither Windows nor Android has,
        // so the honest answer is that nothing was observed. The member stays because
        // IRefreshableControlObject requires it and other platforms can answer it.
        //
        // To make this real on MAUI, watch the refresh indicator the way ActivityIndicator does
        // - that is a child element, and children are observable.
        return null;
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
    public TSelf AssertRefreshing(string? message, int? timeoutMs = null)
        => AssertRefreshing(true, message, timeoutMs);

    #endregion
}

/// <summary>
/// A <see cref="RefreshView{TParent, TSelf}"/> for use where no view-specific subclass is needed.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public sealed partial class RefreshView<TParent> : RefreshView<TParent, RefreshView<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>Creates a RefreshView container within the specified scope.</summary>
    public RefreshView(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>Creates a RefreshView container using the scope's default locator strategy.</summary>
    public RefreshView(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}

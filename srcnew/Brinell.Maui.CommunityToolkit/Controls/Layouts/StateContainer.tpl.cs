using Brinell.Maui.Containers;

namespace Brinell.Maui.CommunityToolkit.Controls.Layouts;

/// <summary>
/// CommunityToolkit.Maui <c>StateContainer</c>: a layout that swaps its content for a state view
/// (loading, error, empty) while a state is set.
/// </summary>
/// <remarks>
/// <para>
/// <b>Read from the tree.</b> The layout keeps only the views for the current state as children,
/// so "which state is shown" is "which state view is present". No bridge: the tree already
/// answers, so a verb would fail AD-008's first test.
/// </para>
/// <para>
/// The state is named by the AutomationId of its view, because that is what a user-facing test can
/// see; the toolkit's state key is not published.
/// </para>
/// <para>
/// <b>Windows requires the automation handlers</b> for the host layout to publish its AutomationId,
/// as for any MAUI layout container.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type.</typeparam>
/// <typeparam name="TSelf">The container type itself (self-referencing for fluent returns).</typeparam>
public partial class StateContainer<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : StateContainer<TParent, TSelf>
{
    /// <summary>
    /// Creates a state container within the specified scope.
    /// </summary>
    public StateContainer(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a state container using the scope's default locator strategy.
    /// </summary>
    public StateContainer(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Not cached: switching state replaces the layout's children, and the layout can be rebuilt
    /// with them.
    /// </remarks>
    protected override bool CacheContainerRoot => false;

    #region Hand-written Members

    // Hand-written: the generator forwards extra parameters only for Get* members, and the
    // question here is "is this view shown", which takes the view's id.

    /// <summary>
    /// Whether the view with the given AutomationId is currently shown inside the container.
    /// </summary>
    /// <param name="viewAutomationId">The AutomationId of a state view or of the normal content.</param>
    /// <returns>True when shown.</returns>
    public bool IsShowing(string viewAutomationId)
        => TryFindElement(Locator.ByAutomationId(viewAutomationId)) != null;

    /// <summary>
    /// Waits until the view with the given AutomationId is shown, or is no longer shown.
    /// </summary>
    /// <param name="viewAutomationId">The AutomationId of the view.</param>
    /// <param name="expected">True to wait for it to appear, false to wait for it to go.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True when the condition was met within the timeout.</returns>
    public bool WaitShowing(string viewAutomationId, bool expected = true, int? timeoutMs = null)
        => RunWait(() => IsShowing(viewAutomationId) == expected, timeoutMs);

    /// <summary>
    /// Asserts that the view with the given AutomationId is, or is not, shown.
    /// </summary>
    /// <param name="viewAutomationId">The AutomationId of the view.</param>
    /// <param name="expected">True to assert it is shown, false to assert it is not.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The container for fluent chaining.</returns>
    public TSelf AssertShowing(string viewAutomationId, bool expected = true, string? message = null, int? timeoutMs = null)
        => RunAssert<bool?>(expected, () => IsShowing(viewAutomationId), (actual, wanted) => actual == wanted,
            message ?? $"Expected '{viewAutomationId}' {(expected ? "to be" : "not to be")} shown in state container '{Locator.Value}'.",
            timeoutMs);

    #endregion
}

/// <summary>
/// A <see cref="StateContainer{TParent, TSelf}"/> for use where no view-specific subclass is needed.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public sealed partial class StateContainer<TParent> : StateContainer<TParent, StateContainer<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>Creates a state container within the specified scope.</summary>
    public StateContainer(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>Creates a state container using the scope's default locator strategy.</summary>
    public StateContainer(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}

using Brinell.Core.Testing;
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
    protected override bool CacheContainerRoot => false;

    #region Steps

    /// <summary>Asserts that the named state is shown, waiting for it.</summary>
    /// <param name="state">The state's name, as a scenario writes it; <see cref="StateViewId"/> maps it to its view.</param>
    /// <param name="timeoutMs">How long to wait; the default wait when null.</param>
    /// <returns>The container.</returns>
    [UatThen("{control} should show the {value} state", CommandId = "StateContainer.AssertShowsState")]
    public TSelf AssertShowsState(string state, int? timeoutMs = null)
        => AssertShowing(StateViewId(state), timeoutMs: timeoutMs);

    /// <summary>
    /// The AutomationId of the view shown for a state name. The name is the id by default; a
    /// subclass maps readable names (<c>Loading</c>) to its view ids.
    /// </summary>
    /// <param name="state">The state's name.</param>
    /// <returns>The view's AutomationId.</returns>
    protected virtual string StateViewId(string state) => state;

    #endregion

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Whether the view with the given AutomationId is currently shown inside the container.
    /// </summary>
    /// <param name="element">The container root, or null when it is absent.</param>
    /// <param name="viewAutomationId">The AutomationId of a state view or of the normal content.</param>
    /// <returns>True when shown.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsShowingCore(IMauiElement? element, string viewAutomationId)
        => element?.TryFindElement(Locator.ByAutomationId(viewAutomationId)) != null;

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

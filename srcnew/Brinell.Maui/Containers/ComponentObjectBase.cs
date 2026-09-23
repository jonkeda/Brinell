using Brinell.Maui.Controls.Display;

namespace Brinell.Maui.Containers;

/// <summary>
/// Base class for component objects: composite containers rooted at an element rather than at
/// the driver, hosting typed child controls (e.g., buttons, entries, labels, spinners)
/// and encapsulating component-level interactions.
/// </summary>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The component type itself (self-referencing for fluent returns).</typeparam>
public abstract class ComponentObjectBase<TParent, TSelf>
    : ContainerObjectBase<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : ComponentObjectBase<TParent, TSelf>
{
    /// <summary>
    /// Creates a component container within the given parent scope using a locator.
    /// </summary>
    /// <param name="parentScope">The parent scope (page or container).</param>
    /// <param name="locator">The locator for the component's root element.</param>
    protected ComponentObjectBase(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a component container within the given parent scope using a string locator value.
    /// </summary>
    /// <param name="parentScope">The parent scope (page or container).</param>
    /// <param name="locatorValue">The locator value (e.g. automation ID).</param>
    protected ComponentObjectBase(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }

    /// <summary>
    /// Gets a typed child <see cref="ActivityIndicator{TSelf}"/> scoped to this component.
    /// </summary>
    public ActivityIndicator<TSelf> ActivityIndicator(string automationId) => new(Self, automationId);
}

using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Container;

/// <summary>
/// A hierarchical tree: a scope over the whole tree that hands out nodes by automation id.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nodes are addressed, not enumerated.</b> A MAUI tree nests its rows in layouts, and a layout
/// publishes no automation peer unless the app registers the Brinell automation handlers, so there
/// is no row element to scope to and no row id that repeats. This is therefore a container, not a
/// collection: the app gives each node's label and toggle its own automation id, and a node object
/// is the pair of them. <c>GetItemCount</c> and index-based rows have no answer here, so there are
/// none.
/// </para>
/// <para>
/// <b>Windows:</b> the toggle is a real <c>Button</c>, invoked through the Invoke pattern. Expansion
/// is read from the toggle's accessible name, because neither the row nor the tree publishes an
/// ExpandCollapse pattern; the app must set that name to <see cref="ExpandedStateName"/> /
/// <see cref="CollapsedStateName"/> (MAUI: <c>SemanticProperties.Description</c>). A node whose
/// ancestors are collapsed is absent from the automation tree, not merely off screen.
/// </para>
/// <para>
/// Derive from this rather than instantiating it:
/// <code>
/// public class WorkspaceTree : TreeView&lt;PresenterPage, WorkspaceTree&gt;
/// {
///     public WorkspaceTree(IMauiScope&lt;PresenterPage&gt; scope) : base(scope, "WorkspaceTree") { }
///
///     public TreeNode&lt;WorkspaceTree&gt; Folder(string name) =&gt; Node($"Node_{name}", $"NodeToggle_{name}");
/// }
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The tree type itself (self-referencing for fluent returns).</typeparam>
public class TreeView<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : TreeView<TParent, TSelf>
{
    /// <summary>Creates a tree container within the specified scope.</summary>
    public TreeView(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>Creates a tree container using the scope's default locator strategy.</summary>
    public TreeView(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }

    /// <summary>The toggle's accessible name while the node is expanded.</summary>
    protected virtual string ExpandedStateName => "Expanded";

    /// <summary>The toggle's accessible name while the node is collapsed.</summary>
    protected virtual string CollapsedStateName => "Collapsed";

    /// <summary>The toggle id the app derives from a node id, when the caller gives only the node id.</summary>
    protected virtual string ToggleAutomationId(string nodeAutomationId) => $"{nodeAutomationId}Toggle";

    /// <summary>
    /// The node whose label carries <paramref name="nodeAutomationId"/>, with the toggle at
    /// <paramref name="toggleAutomationId"/> or at the id <see cref="ToggleAutomationId"/> derives.
    /// </summary>
    public TreeNode<TSelf> Node(string nodeAutomationId, string? toggleAutomationId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeAutomationId);

        return new TreeNode<TSelf>(
            Self,
            nodeAutomationId,
            toggleAutomationId ?? ToggleAutomationId(nodeAutomationId),
            ExpandedStateName,
            CollapsedStateName);
    }
}

/// <summary>
/// A <see cref="TreeView{TParent, TSelf}"/> for use where no tree-specific subclass is needed.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public sealed class TreeView<TParent> : TreeView<TParent, TreeView<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>Creates a tree container within the specified scope.</summary>
    public TreeView(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>Creates a tree container using the scope's default locator strategy.</summary>
    public TreeView(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}

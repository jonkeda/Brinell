namespace Brinell.Presenter.Controls;

/// <summary>Carries the node a <see cref="TreeView"/> moved its selection from and to.</summary>
/// <param name="previous">The node that was selected, or <see langword="null"/>.</param>
/// <param name="current">The node that is now selected, or <see langword="null"/>.</param>
public sealed class TreeViewSelectionChangedEventArgs(TreeViewNode? previous, TreeViewNode? current) : EventArgs
{
    /// <summary>The node that was selected before the change.</summary>
    public TreeViewNode? Previous { get; } = previous;

    /// <summary>The node that is selected after the change.</summary>
    public TreeViewNode? Current { get; } = current;
}

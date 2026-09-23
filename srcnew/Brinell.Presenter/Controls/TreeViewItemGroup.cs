namespace Brinell.Presenter.Controls;

/// <summary>
/// A branch in the data a <see cref="TreeView"/> is built from: nested groups plus leaf items.
/// </summary>
public sealed class TreeViewItemGroup
{
    /// <summary>The text the generated row shows.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The caller's own object for this branch, carried to <see cref="TreeViewNode.Item"/>.</summary>
    public object? Value { get; set; }

    /// <summary>The nested branches, rendered above <see cref="Items"/>.</summary>
    public IList<TreeViewItemGroup> Children { get; } = [];

    /// <summary>The leaves of this branch.</summary>
    public IList<TreeViewItem> Items { get; } = [];
}

namespace Brinell.Presenter.Controls;

/// <summary>A leaf in the data a <see cref="TreeView"/> is built from.</summary>
public sealed class TreeViewItem
{
    /// <summary>The text the generated row shows.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The caller's own object for this leaf, carried to <see cref="TreeViewNode.Item"/>.</summary>
    public object? Value { get; set; }
}

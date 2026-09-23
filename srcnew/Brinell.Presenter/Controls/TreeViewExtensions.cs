using System.Collections.ObjectModel;

namespace Brinell.Presenter.Controls;

/// <summary>Turns a hierarchy of data into the <see cref="TreeViewNode"/> tree a <see cref="TreeView"/> shows.</summary>
public static class TreeViewExtensions
{
    /// <summary>
    /// Builds nodes for <paramref name="groups"/>, nesting each group's own children above its
    /// items. Assign the result to <see cref="TreeView.RootNodes"/>.
    /// </summary>
    public static ObservableCollection<TreeViewNode> BuildNodes(
        this TreeView treeView,
        IEnumerable<TreeViewItemGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(treeView);
        ArgumentNullException.ThrowIfNull(groups);

        return [.. groups.Select(group => BuildGroupNode(treeView, group))];
    }

    /// <summary>
    /// Builds nodes for <paramref name="roots"/>, following <paramref name="childrenOf"/> down the
    /// hierarchy and labelling each row with <paramref name="nameOf"/>.
    /// </summary>
    public static ObservableCollection<TreeViewNode> BuildNodes<T>(
        this TreeView treeView,
        IEnumerable<T> roots,
        Func<T, IEnumerable<T>> childrenOf,
        Func<T, string> nameOf)
    {
        ArgumentNullException.ThrowIfNull(treeView);
        ArgumentNullException.ThrowIfNull(roots);
        ArgumentNullException.ThrowIfNull(childrenOf);
        ArgumentNullException.ThrowIfNull(nameOf);

        return [.. roots.Select(root => BuildNode(treeView, root, childrenOf, nameOf))];
    }

    /// <summary>Creates a node carrying <paramref name="item"/> and showing <paramref name="name"/>.</summary>
    public static TreeViewNode CreateNode(this TreeView treeView, string name, object? item = null)
    {
        ArgumentNullException.ThrowIfNull(treeView);

        return new TreeViewNode
        {
            NodeContent = CreateLabel(name),
            Item = item,
            IndentWidth = treeView.IndentWidth,
            ShowExpandButton = treeView.ShowExpandButton,
            ShowExpandButtonIfEmpty = treeView.ShowExpandButtonIfEmpty,
            SelectedBackgroundColor = treeView.SelectedBackgroundColor,
            SelectedBackgroundOpacity = treeView.SelectedBackgroundOpacity
        };
    }

    private static TreeViewNode BuildGroupNode(TreeView treeView, TreeViewItemGroup group)
    {
        var node = treeView.CreateNode(group.Name, group.Value ?? group);
        var children = new ObservableCollection<TreeViewNode>();

        foreach (var child in group.Children)
        {
            children.Add(BuildGroupNode(treeView, child));
        }

        foreach (var item in group.Items)
        {
            children.Add(treeView.CreateNode(item.Name, item.Value ?? item));
        }

        node.ChildrenList = children;
        return node;
    }

    private static TreeViewNode BuildNode<T>(
        TreeView treeView,
        T value,
        Func<T, IEnumerable<T>> childrenOf,
        Func<T, string> nameOf)
    {
        var node = treeView.CreateNode(nameOf(value), value);
        var children = new ObservableCollection<TreeViewNode>();

        foreach (var child in childrenOf(value) ?? [])
        {
            children.Add(BuildNode(treeView, child, childrenOf, nameOf));
        }

        node.ChildrenList = children;
        return node;
    }

    private static Label CreateLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(4, 0)
        };
        label.SetAppThemeColor(
            Label.TextColorProperty,
            TreeViewColors.Resource("PresenterTextLight", "#000000"),
            TreeViewColors.Resource("PresenterTextDark", "#FFFFFF"));
        return label;
    }
}

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;

namespace Brinell.Presenter.Controls;

/// <summary>
/// A scrolling hierarchy of <see cref="TreeViewNode"/>. The tree owns the scroll viewport and the
/// selection; every node draws itself and its own subtree, so depth is unlimited and only the
/// expanded part of the hierarchy is shown.
/// </summary>
/// <remarks>
/// Nodes come from either end: assign <see cref="RootNodes"/> to build them yourself (see
/// <see cref="TreeViewExtensions"/>), or bind <see cref="ItemsSource"/> and let the tree mirror a
/// hierarchy of view models through <see cref="ChildrenPath"/> and <see cref="ItemTemplate"/>.
/// </remarks>
public class TreeView : ScrollView
{
    private static readonly Dictionary<(Type Type, string Path), PropertyInfo?> PathCache = [];

    private readonly VerticalStackLayout _root;
    private INotifyCollectionChanged? _observedRoots;
    private INotifyCollectionChanged? _observedItems;
    private bool _syncingSelection;

    /// <summary>Creates an empty tree.</summary>
    public TreeView()
    {
        _root = new VerticalStackLayout { Spacing = 0 };
        Content = _root;
        Orientation = ScrollOrientation.Both;
    }

    /// <summary>Raised after <see cref="SelectedItem"/> changes, whoever changed it.</summary>
    public event EventHandler<TreeViewSelectionChangedEventArgs>? SelectedItemChanged;

    /// <summary>The nodes shown at depth zero.</summary>
    public static readonly BindableProperty RootNodesProperty = BindableProperty.Create(
        nameof(RootNodes), typeof(IList<TreeViewNode>), typeof(TreeView), null,
        propertyChanged: (bindable, _, newValue) =>
            ((TreeView)bindable).OnRootNodesChanged(newValue as IList<TreeViewNode>));

    /// <summary>The data the tree builds its root nodes from.</summary>
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(TreeView), null,
        propertyChanged: (bindable, _, newValue) =>
            ((TreeView)bindable).OnItemsSourceChanged(newValue as IEnumerable));

    /// <summary>The template for a data item's row; its binding context is the item.</summary>
    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(TreeView), null,
        propertyChanged: (bindable, _, _) => ((TreeView)bindable).RebuildFromItemsSource());

    /// <summary>The property each data item exposes its own children on.</summary>
    public static readonly BindableProperty ChildrenPathProperty = BindableProperty.Create(
        nameof(ChildrenPath), typeof(string), typeof(TreeView), "Children",
        propertyChanged: (bindable, _, _) => ((TreeView)bindable).RebuildFromItemsSource());

    /// <summary>
    /// The data item's expansion property, two-way bound so the item stays the source of truth;
    /// empty leaves expansion to the nodes.
    /// </summary>
    public static readonly BindableProperty IsExpandedPathProperty = BindableProperty.Create(
        nameof(IsExpandedPath), typeof(string), typeof(TreeView), "IsExpanded",
        propertyChanged: (bindable, _, _) => ((TreeView)bindable).RebuildFromItemsSource());

    /// <summary>The data item behind the selected node.</summary>
    public static readonly BindableProperty SelectedValueProperty = BindableProperty.Create(
        nameof(SelectedValue), typeof(object), typeof(TreeView), null, BindingMode.TwoWay,
        propertyChanged: (bindable, _, newValue) => ((TreeView)bindable).OnSelectedValueChanged(newValue));

    /// <summary>The selected node, or <see langword="null"/> when nothing is selected.</summary>
    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem), typeof(TreeViewNode), typeof(TreeView), null, BindingMode.TwoWay,
        propertyChanged: (bindable, oldValue, newValue) =>
            ((TreeView)bindable).OnSelectedItemChanged(oldValue as TreeViewNode, newValue as TreeViewNode));

    /// <summary>The indent one level of depth adds, pushed into every node.</summary>
    public static readonly BindableProperty IndentWidthProperty = BindableProperty.Create(
        nameof(IndentWidth), typeof(double), typeof(TreeView), TreeViewNode.DefaultIndentWidth,
        propertyChanged: (bindable, _, _) => ((TreeView)bindable).ApplyNodeDefaults());

    /// <summary>Whether nodes draw their own chevron, pushed into every node.</summary>
    public static readonly BindableProperty ShowExpandButtonProperty = BindableProperty.Create(
        nameof(ShowExpandButton), typeof(bool), typeof(TreeView), true,
        propertyChanged: (bindable, _, _) => ((TreeView)bindable).ApplyNodeDefaults());

    /// <summary>Whether childless nodes still draw a chevron, pushed into every node.</summary>
    public static readonly BindableProperty ShowExpandButtonIfEmptyProperty = BindableProperty.Create(
        nameof(ShowExpandButtonIfEmpty), typeof(bool), typeof(TreeView), false,
        propertyChanged: (bindable, _, _) => ((TreeView)bindable).ApplyNodeDefaults());

    /// <summary>The row fill while selected, pushed into every node.</summary>
    public static readonly BindableProperty SelectedBackgroundColorProperty = BindableProperty.Create(
        nameof(SelectedBackgroundColor), typeof(Color), typeof(TreeView), null,
        propertyChanged: (bindable, _, _) => ((TreeView)bindable).ApplyNodeDefaults());

    /// <summary>The alpha applied to the selection fill, pushed into every node.</summary>
    public static readonly BindableProperty SelectedBackgroundOpacityProperty = BindableProperty.Create(
        nameof(SelectedBackgroundOpacity), typeof(double), typeof(TreeView), 1d,
        propertyChanged: (bindable, _, _) => ((TreeView)bindable).ApplyNodeDefaults());

    /// <inheritdoc cref="RootNodesProperty"/>
    public IList<TreeViewNode>? RootNodes
    {
        get => (IList<TreeViewNode>?)GetValue(RootNodesProperty);
        set => SetValue(RootNodesProperty, value);
    }

    /// <inheritdoc cref="ItemsSourceProperty"/>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <inheritdoc cref="ItemTemplateProperty"/>
    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <inheritdoc cref="ChildrenPathProperty"/>
    public string ChildrenPath
    {
        get => (string)GetValue(ChildrenPathProperty);
        set => SetValue(ChildrenPathProperty, value);
    }

    /// <inheritdoc cref="IsExpandedPathProperty"/>
    public string IsExpandedPath
    {
        get => (string)GetValue(IsExpandedPathProperty);
        set => SetValue(IsExpandedPathProperty, value);
    }

    /// <inheritdoc cref="SelectedValueProperty"/>
    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    /// <inheritdoc cref="SelectedItemProperty"/>
    public TreeViewNode? SelectedItem
    {
        get => (TreeViewNode?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <inheritdoc cref="IndentWidthProperty"/>
    public double IndentWidth
    {
        get => (double)GetValue(IndentWidthProperty);
        set => SetValue(IndentWidthProperty, value);
    }

    /// <inheritdoc cref="ShowExpandButtonProperty"/>
    public bool ShowExpandButton
    {
        get => (bool)GetValue(ShowExpandButtonProperty);
        set => SetValue(ShowExpandButtonProperty, value);
    }

    /// <inheritdoc cref="ShowExpandButtonIfEmptyProperty"/>
    public bool ShowExpandButtonIfEmpty
    {
        get => (bool)GetValue(ShowExpandButtonIfEmptyProperty);
        set => SetValue(ShowExpandButtonIfEmptyProperty, value);
    }

    /// <inheritdoc cref="SelectedBackgroundColorProperty"/>
    public Color? SelectedBackgroundColor
    {
        get => (Color?)GetValue(SelectedBackgroundColorProperty);
        set => SetValue(SelectedBackgroundColorProperty, value);
    }

    /// <inheritdoc cref="SelectedBackgroundOpacityProperty"/>
    public double SelectedBackgroundOpacity
    {
        get => (double)GetValue(SelectedBackgroundOpacityProperty);
        set => SetValue(SelectedBackgroundOpacityProperty, value);
    }

    /// <summary>Every node in the tree, in display order, expanded or not.</summary>
    public IEnumerable<TreeViewNode> Nodes => (RootNodes ?? []).SelectMany(node => node.Flatten());

    /// <summary>The node carrying <paramref name="item"/>, or <see langword="null"/>.</summary>
    public TreeViewNode? NodeFor(object? item) =>
        item is null ? null : Nodes.FirstOrDefault(node => ReferenceEquals(node.Item, item));

    /// <summary>Expands every node in the tree.</summary>
    public void ExpandAll()
    {
        foreach (var node in RootNodes ?? [])
        {
            node.ExpandAll();
        }
    }

    /// <summary>Collapses every node in the tree.</summary>
    public void CollapseAll()
    {
        foreach (var node in RootNodes ?? [])
        {
            node.CollapseAll();
        }
    }

    /// <summary>Selects <paramref name="node"/> and expands whatever it is nested in.</summary>
    public void SelectAndReveal(TreeViewNode node)
    {
        for (var ancestor = node.ParentNode; ancestor is not null; ancestor = ancestor.ParentNode)
        {
            ancestor.IsExpanded = true;
        }

        SelectedItem = node;
    }

    private void OnRootNodesChanged(IList<TreeViewNode>? nodes)
    {
        if (_observedRoots is not null)
        {
            _observedRoots.CollectionChanged -= OnRootNodesCollectionChanged;
            _observedRoots = null;
        }

        if (nodes is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged += OnRootNodesCollectionChanged;
            _observedRoots = observable;
        }

        RebuildRoots();
    }

    private void OnRootNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildRoots();

    private void RebuildRoots()
    {
        _root.Children.Clear();

        foreach (var node in RootNodes ?? [])
        {
            _root.Children.Add(node);
        }

        ApplyNodeDefaults();

        // A rebuild drops the old visuals, so a selection pointing into them is stale.
        if (SelectedItem is { } selected && !Nodes.Contains(selected))
        {
            SelectedItem = null;
        }
    }

    private void ApplyNodeDefaults()
    {
        foreach (var node in Nodes)
        {
            node.IndentWidth = IndentWidth;
            node.ShowExpandButton = ShowExpandButton;
            node.ShowExpandButtonIfEmpty = ShowExpandButtonIfEmpty;
            node.SelectedBackgroundColor = SelectedBackgroundColor;
            node.SelectedBackgroundOpacity = SelectedBackgroundOpacity;
        }
    }

    private void OnItemsSourceChanged(IEnumerable? items)
    {
        if (_observedItems is not null)
        {
            _observedItems.CollectionChanged -= OnItemsCollectionChanged;
            _observedItems = null;
        }

        if (items is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged += OnItemsCollectionChanged;
            _observedItems = observable;
        }

        RebuildFromItemsSource();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildFromItemsSource();

    private void RebuildFromItemsSource()
    {
        if (ItemsSource is null)
        {
            return;
        }

        var nodes = new ObservableCollection<TreeViewNode>();
        foreach (var item in ItemsSource)
        {
            nodes.Add(CreateBoundNode(item));
        }

        RootNodes = nodes;

        if (SelectedValue is not null)
        {
            RestoreSelectionFromValue(SelectedValue);
        }
    }

    private TreeViewNode CreateBoundNode(object item)
    {
        var node = new TreeViewNode
        {
            Item = item,
            NodeContent = CreateRowContent(item),
            IndentWidth = IndentWidth,
            ShowExpandButton = ShowExpandButton,
            ShowExpandButtonIfEmpty = ShowExpandButtonIfEmpty,
            SelectedBackgroundColor = SelectedBackgroundColor,
            SelectedBackgroundOpacity = SelectedBackgroundOpacity
        };

        var children = new ObservableCollection<TreeViewNode>();
        foreach (var child in ResolveChildren(item))
        {
            children.Add(CreateBoundNode(child));
        }

        node.ChildrenList = children;

        // Bound last: the node reads HasChildren when expansion arrives.
        if (!string.IsNullOrEmpty(IsExpandedPath) && HasProperty(item, IsExpandedPath))
        {
            node.SetBinding(TreeViewNode.IsExpandedProperty, new Binding(IsExpandedPath, BindingMode.TwoWay, source: item));
        }

        return node;
    }

    private View CreateRowContent(object item)
    {
        var template = ItemTemplate switch
        {
            DataTemplateSelector selector => selector.SelectTemplate(item, this),
            { } plain => plain,
            _ => null
        };

        if (template?.CreateContent() is View view)
        {
            view.BindingContext = item;
            return view;
        }

        return new Label
        {
            Text = item.ToString() ?? string.Empty,
            VerticalTextAlignment = TextAlignment.Center,
            BindingContext = item
        };
    }

    private IEnumerable<object> ResolveChildren(object item)
    {
        if (string.IsNullOrEmpty(ChildrenPath)
            || ResolveProperty(item.GetType(), ChildrenPath)?.GetValue(item) is not IEnumerable children)
        {
            return [];
        }

        return children.Cast<object>();
    }

    private static bool HasProperty(object item, string path) => ResolveProperty(item.GetType(), path) is not null;

    private static PropertyInfo? ResolveProperty(Type type, string path)
    {
        var key = (type, path);
        if (PathCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var property = type.GetProperty(path, BindingFlags.Public | BindingFlags.Instance);
        PathCache[key] = property;
        return property;
    }

    private void OnSelectedItemChanged(TreeViewNode? oldNode, TreeViewNode? newNode)
    {
        if (oldNode is not null)
        {
            oldNode.IsSelected = false;
        }

        if (newNode is not null)
        {
            newNode.IsSelected = true;
        }

        _syncingSelection = true;
        SelectedValue = newNode?.Item;
        _syncingSelection = false;

        SelectedItemChanged?.Invoke(this, new TreeViewSelectionChangedEventArgs(oldNode, newNode));
    }

    private void OnSelectedValueChanged(object? value)
    {
        if (!_syncingSelection)
        {
            RestoreSelectionFromValue(value);
        }
    }

    private void RestoreSelectionFromValue(object? value)
    {
        if (value is null)
        {
            SelectedItem = null;
            return;
        }

        // A value set before the nodes exist is picked up by the rebuild that follows.
        if (NodeFor(value) is { } node)
        {
            SelectAndReveal(node);
        }
    }
}

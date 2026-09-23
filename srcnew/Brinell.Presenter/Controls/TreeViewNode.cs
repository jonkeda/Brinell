using System.Collections.Specialized;

namespace Brinell.Presenter.Controls;

/// <summary>
/// One row of a <see cref="TreeView"/> together with its subtree. A node draws an indent, an
/// expand/collapse affordance and its own <see cref="NodeContent"/>, and hosts its
/// <see cref="ChildrenList"/> - themselves nodes - in a layout that is only visible while
/// <see cref="IsExpanded"/> is set.
/// </summary>
[ContentProperty(nameof(NodeContent))]
public class TreeViewNode : VerticalStackLayout
{
    /// <summary>The indent one level of depth adds, in device-independent units.</summary>
    public const double DefaultIndentWidth = 18;

    private readonly Grid _header;
    private readonly BoxView _spacer;
    private readonly ContentView _expandHost;
    private readonly ContentView _contentHost;
    private readonly VerticalStackLayout _childrenHost;
    private INotifyCollectionChanged? _observedChildren;

    /// <summary>Creates an empty, collapsed node.</summary>
    public TreeViewNode()
    {
        Spacing = 0;

        _spacer = new BoxView { Color = Colors.Transparent, WidthRequest = 0 };
        _expandHost = new ContentView { VerticalOptions = LayoutOptions.Center };
        _contentHost = new ContentView
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill
        };

        _header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 0,
            Padding = new Thickness(0, 2)
        };
        AddToHeader(_spacer, 0);
        AddToHeader(_expandHost, 1);
        AddToHeader(_contentHost, 2);

        _childrenHost = new VerticalStackLayout { Spacing = 0, IsVisible = false };

        Children.Add(_header);
        Children.Add(_childrenHost);

        var toggle = new TapGestureRecognizer();
        toggle.Tapped += (_, _) => Toggle();
        _expandHost.GestureRecognizers.Add(toggle);

        var select = new TapGestureRecognizer();
        select.Tapped += (_, _) => Select();
        _contentHost.GestureRecognizers.Add(select);

        SemanticProperties.SetDescription(_expandHost, "Expand or collapse");
        RebuildExpandButton();
        UpdateSelectionVisual();
    }

    /// <summary>Raised whenever the node expands, so children can be filled on demand.</summary>
    public event EventHandler? Expanded;

    /// <summary>Raised whenever the node collapses.</summary>
    public event EventHandler? Collapsed;

    /// <summary>The view shown on this node's own row.</summary>
    public static readonly BindableProperty NodeContentProperty = BindableProperty.Create(
        nameof(NodeContent), typeof(View), typeof(TreeViewNode), null,
        propertyChanged: (bindable, _, newValue) =>
            ((TreeViewNode)bindable)._contentHost.Content = newValue as View);

    /// <summary>The child nodes of this node.</summary>
    public static readonly BindableProperty ChildrenListProperty = BindableProperty.Create(
        nameof(ChildrenList), typeof(IList<TreeViewNode>), typeof(TreeViewNode), null,
        propertyChanged: (bindable, oldValue, newValue) => ((TreeViewNode)bindable)
            .OnChildrenListChanged(oldValue as IList<TreeViewNode>, newValue as IList<TreeViewNode>));

    /// <summary>Whether the subtree is shown.</summary>
    public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(
        nameof(IsExpanded), typeof(bool), typeof(TreeViewNode), false,
        propertyChanged: (bindable, _, newValue) => ((TreeViewNode)bindable).OnIsExpandedChanged((bool)newValue));

    /// <summary>Whether this node is the tree's selected one.</summary>
    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
        nameof(IsSelected), typeof(bool), typeof(TreeViewNode), false,
        propertyChanged: (bindable, _, newValue) => ((TreeViewNode)bindable).OnIsSelectedChanged((bool)newValue));

    /// <summary>The indent one level of depth adds.</summary>
    public static readonly BindableProperty IndentWidthProperty = BindableProperty.Create(
        nameof(IndentWidth), typeof(double), typeof(TreeViewNode), DefaultIndentWidth,
        propertyChanged: (bindable, _, _) => ((TreeViewNode)bindable).UpdateIndent());

    /// <summary>Whether the node draws an expand affordance of its own.</summary>
    public static readonly BindableProperty ShowExpandButtonProperty = BindableProperty.Create(
        nameof(ShowExpandButton), typeof(bool), typeof(TreeViewNode), true,
        propertyChanged: (bindable, _, _) => ((TreeViewNode)bindable).RebuildExpandButton());

    /// <summary>Whether a childless node still draws a chevron.</summary>
    public static readonly BindableProperty ShowExpandButtonIfEmptyProperty = BindableProperty.Create(
        nameof(ShowExpandButtonIfEmpty), typeof(bool), typeof(TreeViewNode), false,
        propertyChanged: (bindable, _, _) => ((TreeViewNode)bindable).RebuildExpandButton());

    /// <summary>Replaces the chevron; the template's binding context is the node.</summary>
    public static readonly BindableProperty ExpandButtonTemplateProperty = BindableProperty.Create(
        nameof(ExpandButtonTemplate), typeof(DataTemplate), typeof(TreeViewNode), null,
        propertyChanged: (bindable, _, _) => ((TreeViewNode)bindable).RebuildExpandButton());

    /// <summary>The row fill while selected; unset means the Presenter selection colour.</summary>
    public static readonly BindableProperty SelectedBackgroundColorProperty = BindableProperty.Create(
        nameof(SelectedBackgroundColor), typeof(Color), typeof(TreeViewNode), null,
        propertyChanged: (bindable, _, _) => ((TreeViewNode)bindable).UpdateSelectionVisual());

    /// <summary>The alpha applied to the selection fill, which leaves the row's text opaque.</summary>
    public static readonly BindableProperty SelectedBackgroundOpacityProperty = BindableProperty.Create(
        nameof(SelectedBackgroundOpacity), typeof(double), typeof(TreeViewNode), 1d,
        propertyChanged: (bindable, _, _) => ((TreeViewNode)bindable).UpdateSelectionVisual());

    /// <summary>The automation id of the expand/collapse affordance.</summary>
    public static readonly BindableProperty ToggleAutomationIdProperty = BindableProperty.Create(
        nameof(ToggleAutomationId), typeof(string), typeof(TreeViewNode), null,
        propertyChanged: (bindable, _, newValue) =>
            ((TreeViewNode)bindable)._expandHost.AutomationId = newValue as string ?? string.Empty);

    /// <inheritdoc cref="NodeContentProperty"/>
    public View? NodeContent
    {
        get => (View?)GetValue(NodeContentProperty);
        set => SetValue(NodeContentProperty, value);
    }

    /// <inheritdoc cref="ChildrenListProperty"/>
    public IList<TreeViewNode>? ChildrenList
    {
        get => (IList<TreeViewNode>?)GetValue(ChildrenListProperty);
        set => SetValue(ChildrenListProperty, value);
    }

    /// <inheritdoc cref="IsExpandedProperty"/>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <inheritdoc cref="IsSelectedProperty"/>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
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

    /// <inheritdoc cref="ExpandButtonTemplateProperty"/>
    public DataTemplate? ExpandButtonTemplate
    {
        get => (DataTemplate?)GetValue(ExpandButtonTemplateProperty);
        set => SetValue(ExpandButtonTemplateProperty, value);
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

    /// <inheritdoc cref="ToggleAutomationIdProperty"/>
    public string? ToggleAutomationId
    {
        get => (string?)GetValue(ToggleAutomationIdProperty);
        set => SetValue(ToggleAutomationIdProperty, value);
    }

    /// <summary>The caller's object behind this row, set when the tree is built from data.</summary>
    public object? Item { get; set; }

    /// <summary>The node this one hangs under, or <see langword="null"/> for a root node.</summary>
    public TreeViewNode? ParentNode { get; private set; }

    /// <summary>How many nodes sit between this one and the root.</summary>
    public int Depth => ParentNode is null ? 0 : ParentNode.Depth + 1;

    /// <summary>Whether the node has a subtree to show.</summary>
    public bool HasChildren => ChildrenList is { Count: > 0 };

    /// <summary>The tree this node is currently attached to.</summary>
    public TreeView? ParentTreeView
    {
        get
        {
            Element? element = Parent;
            while (element is not null)
            {
                if (element is TreeView treeView)
                {
                    return treeView;
                }

                element = element.Parent;
            }

            return null;
        }
    }

    /// <summary>Flips <see cref="IsExpanded"/> when the node has children.</summary>
    public void Toggle()
    {
        if (HasChildren)
        {
            IsExpanded = !IsExpanded;
        }
    }

    /// <summary>Makes this node the tree's selection.</summary>
    public void Select()
    {
        if (ParentTreeView is { } treeView)
        {
            treeView.SelectedItem = this;
        }
        else
        {
            IsSelected = true;
        }
    }

    /// <summary>Expands this node and everything under it.</summary>
    public void ExpandAll()
    {
        if (HasChildren)
        {
            IsExpanded = true;
        }

        foreach (var child in ChildrenList ?? [])
        {
            child.ExpandAll();
        }
    }

    /// <summary>Collapses this node and everything under it.</summary>
    public void CollapseAll()
    {
        foreach (var child in ChildrenList ?? [])
        {
            child.CollapseAll();
        }

        IsExpanded = false;
    }

    /// <summary>This node followed by every node under it, in display order.</summary>
    public IEnumerable<TreeViewNode> Flatten()
    {
        yield return this;

        foreach (var child in ChildrenList ?? [])
        {
            foreach (var node in child.Flatten())
            {
                yield return node;
            }
        }
    }

    private void AddToHeader(View view, int column)
    {
        Grid.SetColumn(view, column);
        _header.Children.Add(view);
    }

    private void OnChildrenListChanged(IList<TreeViewNode>? oldChildren, IList<TreeViewNode>? newChildren)
    {
        if (_observedChildren is not null)
        {
            _observedChildren.CollectionChanged -= OnChildrenCollectionChanged;
            _observedChildren = null;
        }

        foreach (var child in oldChildren ?? [])
        {
            child.ParentNode = null;
        }

        if (newChildren is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged += OnChildrenCollectionChanged;
            _observedChildren = observable;
        }

        RebuildChildren();
    }

    private void OnChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildChildren();

    private void RebuildChildren()
    {
        _childrenHost.Children.Clear();

        foreach (var child in ChildrenList ?? [])
        {
            child.ParentNode = this;
            child.IndentWidth = IndentWidth;
            _childrenHost.Children.Add(child);
            child.UpdateIndent();
        }

        _childrenHost.IsVisible = IsExpanded && HasChildren;
        RebuildExpandButton();
    }

    private void OnIsExpandedChanged(bool isExpanded)
    {
        _childrenHost.IsVisible = isExpanded && HasChildren;
        RebuildExpandButton();

        if (isExpanded)
        {
            Expanded?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Collapsed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnIsSelectedChanged(bool isSelected)
    {
        UpdateSelectionVisual();

        if (isSelected && ParentTreeView is { } treeView && !ReferenceEquals(treeView.SelectedItem, this))
        {
            treeView.SelectedItem = this;
        }
    }

    private void RebuildExpandButton()
    {
        if (!ShowExpandButton)
        {
            // The row content supplies its own toggle; the node keeps only the indent.
            _expandHost.Content = null;
            return;
        }

        if (ExpandButtonTemplate is { } template)
        {
            // Re-created, not refreshed: the template binds node state that is plain CLR data on
            // the way in, so a kept instance would keep showing the old chevron.
            _expandHost.Content = template.CreateContent() as View;
            _expandHost.BindingContext = this;
            return;
        }

        if (_expandHost.Content is not ExpandButtonContent button)
        {
            button = new ExpandButtonContent();
            _expandHost.Content = button;
        }

        button.IsExpandable = HasChildren;
        button.IsExpanded = IsExpanded;
        button.ShowGlyphIfEmpty = ShowExpandButtonIfEmpty;
    }

    private void UpdateIndent()
    {
        _spacer.WidthRequest = Depth * IndentWidth;

        foreach (var child in ChildrenList ?? [])
        {
            child.IndentWidth = IndentWidth;
            child.UpdateIndent();
        }
    }

    private void UpdateSelectionVisual()
    {
        if (!IsSelected)
        {
            _header.ClearValue(BackgroundColorProperty);
            _header.BackgroundColor = Colors.Transparent;
            return;
        }

        var alpha = (float)Math.Clamp(SelectedBackgroundOpacity, 0d, 1d);

        if (SelectedBackgroundColor is { } color)
        {
            _header.ClearValue(BackgroundColorProperty);
            _header.BackgroundColor = color.WithAlpha(alpha);
            return;
        }

        _header.SetAppThemeColor(
            BackgroundColorProperty,
            TreeViewColors.Resource("PresenterSelectedLight", "#C7D9FB").WithAlpha(alpha),
            TreeViewColors.Resource("PresenterSelectedDark", "#2B3B5C").WithAlpha(alpha));
    }
}

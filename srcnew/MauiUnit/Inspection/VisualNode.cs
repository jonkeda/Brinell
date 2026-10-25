using Microsoft.Maui.Controls;

namespace MauiUnit.Inspection;

/// <summary>
/// One element as a test sees it, read at one moment: its type, text, identity and state, and its
/// children. The tree of these is the testing surface: queries walk it, assertion failures and
/// snapshots print it.
/// </summary>
/// <remarks>
/// A node is a reading, not a live view: read the tree again after an interaction. <see cref="IsVisible"/>
/// and <see cref="IsEnabled"/> are effective, so a label inside a hidden layout is not visible.
/// </remarks>
public sealed class VisualNode
{
    private readonly List<VisualNode> _children = [];

    internal VisualNode(Element element, VisualNode? parent, string? text, bool isVisible, bool isEnabled)
    {
        Element = element;
        Parent = parent;
        Text = text;
        IsVisible = isVisible;
        IsEnabled = isEnabled;
        Depth = parent is null ? 0 : parent.Depth + 1;
    }

    /// <summary>The element read.</summary>
    public Element Element { get; }

    /// <summary>The element's type.</summary>
    public Type Type => Element.GetType();

    /// <summary>The text a user would read on it, or null.</summary>
    public string? Text { get; }

    /// <summary>Its <c>AutomationId</c>, or null.</summary>
    public string? AutomationId => Element.AutomationId;

    /// <summary>Whether it and every ancestor are visible.</summary>
    public bool IsVisible { get; }

    /// <summary>Whether it and every ancestor are enabled.</summary>
    public bool IsEnabled { get; }

    /// <summary>Its binding context, or null.</summary>
    public object? BindingContext => Element.BindingContext;

    /// <summary>The node above it, or null at the root.</summary>
    public VisualNode? Parent { get; }

    /// <summary>How far below the root it is; the root is 0.</summary>
    public int Depth { get; }

    /// <summary>Its children, in the order a user would read them.</summary>
    public IReadOnlyList<VisualNode> Children => _children;

    internal void Add(VisualNode child) => _children.Add(child);

    /// <summary>This node and every node below it, depth first in reading order.</summary>
    public IEnumerable<VisualNode> DescendantsAndSelf()
    {
        var stack = new Stack<VisualNode>();
        stack.Push(this);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            for (var i = node._children.Count - 1; i >= 0; i--)
                stack.Push(node._children[i]);
        }
    }

    /// <summary>A one-line description: the type, #AutomationId and text.</summary>
    public override string ToString() => Snapshots.TreeSnapshot.Describe(this);
}

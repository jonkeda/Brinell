using MauiUnit.Snapshots;

namespace MauiUnit.Inspection;

/// <summary>A query found no element, or more than one where it wanted one; the message shows the tree searched.</summary>
public sealed class ElementNotFoundException(string message, VisualNode searched)
    : Exception($"{message}\n\nSearched:\n{TreeSnapshot.Format(searched, TreeSnapshot.ForMessages)}")
{
    /// <summary>The tree that was searched.</summary>
    public VisualNode Searched { get; } = searched;
}

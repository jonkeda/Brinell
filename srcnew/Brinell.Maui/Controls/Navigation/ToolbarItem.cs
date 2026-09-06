namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// One item in a <see cref="Toolbar{TParent}"/>: an object rather than a locator, so it can
/// be asked about itself.
/// </summary>
/// <remarks>
/// Everything an item does - <c>Click</c>, <c>GetText</c>, <c>IsEnabled</c> and their
/// <c>Wait</c>/<c>Assert</c> forms - comes from
/// <see cref="Base.ClickableItemBase{TCollection, TSelf}"/>, and existence and visibility
/// from the container base beneath it. Nothing here is toolbar-specific, which is the point:
/// a toolbar item is a leaf, and the type exists to name it and to scope anything a richer
/// item later holds.
/// </remarks>
/// <typeparam name="TParent">The scope the toolbar belongs to.</typeparam>
public class ToolbarItem<TParent> : Base.ClickableItemBase<Toolbar<TParent>, ToolbarItem<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>
    /// Creates an item bound to a root the toolbar has already found.
    /// </summary>
    /// <param name="toolbar">The owning toolbar.</param>
    /// <param name="itemRoot">The item's root element.</param>
    /// <param name="index">The item's zero-based position in the toolbar.</param>
    public ToolbarItem(Toolbar<TParent> toolbar, IMauiElement itemRoot, int index)
        : base(toolbar, itemRoot, index)
    {
    }
}

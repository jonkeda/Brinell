namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// One item in a <see cref="Menu{TParent}"/>.
/// </summary>
/// <remarks>
/// Like <see cref="ToolbarItem{TParent}"/>, everything it does comes from
/// <see cref="Base.ClickableItemBase{TCollection, TSelf}"/>. A menu item is a leaf; the type
/// exists to name it and to scope anything a richer item later holds.
/// </remarks>
/// <typeparam name="TParent">The scope the menu belongs to.</typeparam>
public class MenuItem<TParent> : Base.ClickableItemBase<Menu<TParent>, MenuItem<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>
    /// Creates an item bound to a root the menu has already found.
    /// </summary>
    /// <param name="menu">The owning menu.</param>
    /// <param name="itemRoot">The item's root element.</param>
    /// <param name="index">The item's zero-based position in the menu.</param>
    public MenuItem(Menu<TParent> menu, IMauiElement itemRoot, int index)
        : base(menu, itemRoot, index)
    {
    }
}

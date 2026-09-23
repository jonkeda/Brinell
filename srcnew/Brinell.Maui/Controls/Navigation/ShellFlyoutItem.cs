namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// One item in a MAUI Shell's flyout.
/// </summary>
/// <typeparam name="TParent">The scope the shell belongs to.</typeparam>
public class ShellFlyoutItem<TParent> : Base.SelectableItemBase<ShellFlyout<TParent>, ShellFlyoutItem<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>Creates an item bound to a root the flyout has already found.</summary>
    public ShellFlyoutItem(ShellFlyout<TParent> flyout, IMauiElement itemRoot, int index)
        : base(flyout, itemRoot, index)
    {
    }
}

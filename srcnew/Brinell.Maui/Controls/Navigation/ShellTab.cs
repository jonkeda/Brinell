namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// One tab of a MAUI <see cref="Shell{TParent}"/>.
/// </summary>
/// <remarks>
/// The item type is a tab rather than the <c>ShellContent</c> inside it: what the platform
/// draws, and what a test clicks, is the tab.
/// </remarks>
/// <typeparam name="TParent">The scope the shell belongs to.</typeparam>
public class ShellTab<TParent> : Base.SelectableItemBase<ShellTabs<TParent>, ShellTab<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>Creates a tab bound to a root the tab strip has already found.</summary>
    public ShellTab(ShellTabs<TParent> tabs, IMauiElement itemRoot, int index)
        : base(tabs, itemRoot, index)
    {
    }
}

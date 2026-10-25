using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace MUnit.Inspection;

/// <summary>
/// The tab bar a platform draws for a Shell: the current item's sections, one tab each, shown while
/// the item has more than one. A node of the tree, as a user sees it; not part of the Shell's own.
/// </summary>
public sealed class ShellTabBar : Element
{
    internal ShellTabBar(Shell shell)
    {
        Shell = shell;
        Parent = shell;
    }

    /// <summary>The Shell it belongs to.</summary>
    public Shell Shell { get; }

    /// <summary>The tabs: the current item's sections a user can choose.</summary>
    public IEnumerable<ShellSection> Tabs
        => Shell.CurrentItem is { } item && item.Items.Count > 1 ? item.Items.Where(Shown) : [];

    private static bool Shown(ShellSection section) => section.IsVisible && Shell.GetTabBarIsVisible(section);
}

/// <summary>
/// The flyout a platform draws for a Shell while <see cref="Shell.FlyoutIsPresented"/>: the items it
/// lists. A node of the tree, as a user sees it; not part of the Shell's own.
/// </summary>
public sealed class ShellFlyoutList : Element
{
    internal ShellFlyoutList(Shell shell)
    {
        Shell = shell;
        Parent = shell;
    }

    /// <summary>The Shell it belongs to.</summary>
    public Shell Shell { get; }

    /// <summary>The items the flyout lists.</summary>
    public IEnumerable<ShellItem> Items => Shell.Items.Where(item => item.IsVisible && item.FlyoutItemIsVisible);
}

/// <summary>One tab bar and one flyout list per Shell, kept so a node read twice is the same element.</summary>
internal static class ShellChromeNodes
{
    private static readonly ConditionalWeakTable<Shell, ShellTabBar> TabBars = new();
    private static readonly ConditionalWeakTable<Shell, ShellFlyoutList> Flyouts = new();

    public static ShellTabBar TabBar(Shell shell) => TabBars.GetValue(shell, s => new ShellTabBar(s));

    public static ShellFlyoutList Flyout(Shell shell) => Flyouts.GetValue(shell, s => new ShellFlyoutList(s));
}

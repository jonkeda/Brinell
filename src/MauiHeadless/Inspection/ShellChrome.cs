using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace MauiHeadless.Inspection;

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

/// <summary>
/// The back button a platform draws in a window's title bar while there is somewhere to go back to:
/// a modal over the page, or a page pushed on the current navigation. A node of the tree, as a user
/// sees it. Clicking it is the back button pressed: the page's <c>OnBackButtonPressed</c> decides,
/// and NavigationPage and Shell pop.
/// </summary>
public sealed class NavigationBackButton : Element
{
    internal NavigationBackButton(Window window)
    {
        Window = window;
        Parent = window;
        AutomationId = "BackButton";
    }

    /// <summary>The window it belongs to.</summary>
    public Window Window { get; }

    /// <summary>Whether there is somewhere to go back to, so the button is shown.</summary>
    public bool CanGoBack
        => Window.Navigation.ModalStack.Count > 0
           || (Screen.CurrentPage(Window) is { } current && current.Navigation.NavigationStack.Count > 1);
}

/// <summary>
/// The title a platform draws in a window's title bar: the shown page's <c>Title</c>, while it has
/// one. A node of the tree, as a user sees it.
/// </summary>
public sealed class NavigationTitle : Element
{
    internal NavigationTitle(Window window)
    {
        Window = window;
        Parent = window;
        AutomationId = "PageTitle";
    }

    /// <summary>The window it belongs to.</summary>
    public Window Window { get; }

    /// <summary>The title shown: the current page's, or null when it has none.</summary>
    public string? Text => Screen.CurrentPage(Window)?.Title is { Length: > 0 } title ? title : null;
}

/// <summary>One tab bar and one flyout list per Shell, kept so a node read twice is the same element.</summary>
internal static class ShellChromeNodes
{
    private static readonly ConditionalWeakTable<Shell, ShellTabBar> TabBars = new();
    private static readonly ConditionalWeakTable<Shell, ShellFlyoutList> Flyouts = new();

    public static ShellTabBar TabBar(Shell shell) => TabBars.GetValue(shell, s => new ShellTabBar(s));

    public static ShellFlyoutList Flyout(Shell shell) => Flyouts.GetValue(shell, s => new ShellFlyoutList(s));

    private static readonly ConditionalWeakTable<Window, NavigationBackButton> BackButtons = new();

    public static NavigationBackButton BackButton(Window window) => BackButtons.GetValue(window, w => new NavigationBackButton(w));

    private static readonly ConditionalWeakTable<Window, NavigationTitle> Titles = new();

    public static NavigationTitle Title(Window window) => Titles.GetValue(window, w => new NavigationTitle(w));
}

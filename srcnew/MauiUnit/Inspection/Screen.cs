using Microsoft.Maui.Controls;

namespace MauiUnit.Inspection;

/// <summary>What a window shows: the top modal page, else its page, down through the containers.</summary>
public static class Screen
{
    /// <summary>The page a user is looking at in <paramref name="window"/>.</summary>
    public static Page? CurrentPage(Window window)
        => window.Page is { } page ? Descend(VisiblePage(page, window)) : null;

    /// <summary>
    /// The page a Shell shows: its current page, else the current content's page, which Shell makes
    /// from a <c>ContentTemplate</c> only when the platform asks for it, so MauiUnit asks.
    /// </summary>
    public static Page? CurrentPage(Shell shell)
        => shell.CurrentPage
           ?? (shell.CurrentItem?.CurrentItem?.CurrentItem is IShellContentController content
               ? content.GetOrCreateContent()
               : null);

    internal static Page VisiblePage(Page page, Window window)
        => window.Navigation.ModalStack.LastOrDefault() ?? page;

    private static Page Descend(Page page) => page switch
    {
        NavigationPage { CurrentPage: { } current } => Descend(current),
        Shell shell when CurrentPage(shell) is { } current => Descend(current),
        TabbedPage { CurrentPage: { } current } => Descend(current),
        _ => page,
    };
}

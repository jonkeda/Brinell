using Brinell.Maui.Enums;

namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// Where each platform draws MAUI Shell's chrome.
/// </summary>
/// <remarks>
/// <para>
/// Shell is the one navigation surface an app author cannot mark up: the tab strip, the
/// flyout and its opener are drawn by the platform, and an <c>AutomationId</c> set on a
/// <c>Tab</c> or a <c>FlyoutItem</c> does not reach them - confirmed by dumping the tree,
/// where Windows reports WinUI's own <c>navViewItem</c> in place of anything the app wrote.
/// </para>
/// <para>
/// So Brinell has to know what each platform draws. That knowledge is here, in one file named
/// for exactly that, rather than spread across the Shell control objects - and never in a
/// test, which says <c>Shell.Tabs["Controls"]</c> on both platforms.
/// </para>
/// </remarks>
internal static class ShellChrome
{
    /// <summary>
    /// The element the tabs are searched within.
    /// </summary>
    /// <remarks>
    /// Windows names its tab strip; Android names nothing above a tab, so the search is rooted
    /// at the app's content frame and <see cref="Tab"/> does the discriminating.
    /// </remarks>
    internal static Locator TabHost(MauiPlatform platform) => platform switch
    {
        MauiPlatform.Windows => Locator.ByAutomationId("TopNavMenuItemsHost"),
        MauiPlatform.Android => Locator.ById("android:id/content"),
        _ => throw Unmapped(platform, "the tab strip")
    };

    /// <summary>
    /// One tab.
    /// </summary>
    /// <remarks>
    /// Android renders Shell's tabs as a bottom navigation bar whose items are frame layouts
    /// carrying the tab's title as their content description - the only frame layouts on the
    /// page that carry one, which is what makes this exact rather than merely plausible.
    /// </remarks>
    internal static Locator Tab(MauiPlatform platform) => platform switch
    {
        MauiPlatform.Windows => Locator.ByControlType("TabItem"),
        MauiPlatform.Android => Locator.ByXPath("//android.widget.FrameLayout[@content-desc!='']"),
        _ => throw Unmapped(platform, "a tab")
    };

    /// <summary>The element the flyout's items are searched within.</summary>
    internal static Locator FlyoutHost(MauiPlatform platform) => platform switch
    {
        MauiPlatform.Windows => Locator.ByAutomationId("MenuItemsHost"),
        MauiPlatform.Android => Locator.ById("android:id/content"),
        _ => throw Unmapped(platform, "the flyout")
    };

    /// <summary>
    /// One item within the flyout.
    /// </summary>
    /// <remarks>
    /// Android's drawer items are view groups carrying their title as a content description,
    /// and they leave the tree entirely while the drawer is shut. Windows keeps its own hidden,
    /// which is why openness is read from visible items rather than from their presence.
    /// </remarks>
    internal static Locator FlyoutItem(MauiPlatform platform) => platform switch
    {
        MauiPlatform.Windows => Locator.ByControlType("ListItem"),
        MauiPlatform.Android => Locator.ByXPath("//android.view.ViewGroup[@content-desc!='']"),
        _ => throw Unmapped(platform, "a flyout item")
    };

    /// <summary>What opens the flyout.</summary>
    internal static Locator FlyoutOpener(MauiPlatform platform) => platform switch
    {
        MauiPlatform.Windows => Locator.ByName("Open Navigation"),
        MauiPlatform.Android => Locator.ByAccessibilityId("Open navigation drawer"),
        _ => throw Unmapped(platform, "the flyout opener")
    };

    /// <summary>
    /// Dismisses an open flyout without choosing anything in it.
    /// </summary>
    /// <remarks>
    /// An action rather than a locator, because the platforms do not agree on there being an
    /// element to click: Windows draws a light-dismiss layer over the page, while Android's
    /// drawer is dismissed with the back gesture - and its hamburger changes its content
    /// description once open, so it is not a handle either.
    /// </remarks>
    internal static void DismissFlyout(IMauiTestContext context, MauiPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(context);

        switch (platform)
        {
            case MauiPlatform.Windows:
                // Through the pattern, not a pointer: the dismiss layer covers the page and a
                // click aimed at it can land on whatever it is covering.
                var dismiss = context.FindElement(Locator.ByAutomationId("LightDismiss"));
                if (!Containers.ActivationHelper.TryActivateByPattern(dismiss))
                {
                    dismiss.Click();
                }
                break;

            case MauiPlatform.Android:
                context.Driver.NavigateBack();
                break;

            default:
                throw Unmapped(platform, "dismissing the flyout");
        }
    }

    /// <summary>
    /// Refuses rather than guesses. A platform is added here by dumping its tree, not by
    /// reasoning about what it probably draws.
    /// </summary>
    private static PlatformNotSupportedException Unmapped(MauiPlatform platform, string what)
        => new($"Shell chrome on {platform} has not been mapped: nothing is known about {what}. " +
               "Dump the tree and add it to ShellChrome - see .my/navigation/design-shell-sample-app.md.");
}

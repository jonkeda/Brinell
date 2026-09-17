namespace Brinell.Maui.Interfaces;

/// <summary>
/// Where a platform draws MAUI Shell's tabs and flyout.
/// </summary>
/// <remarks>
/// <para>
/// Shell is the one navigation surface an app author cannot mark up: the tab strip and the flyout
/// are drawn by the platform, and an <c>AutomationId</c> set on a <c>Tab</c> or a
/// <c>FlyoutItem</c> does not reach them. So something has to know what each platform draws.
/// </para>
/// <para>
/// Each driver supplies these for its own platform; one that has not mapped Shell throws
/// <see cref="PlatformNotSupportedException"/>.
/// </para>
/// </remarks>
/// <param name="TabHost">The element the tabs are searched within.</param>
/// <param name="Tab">One tab.</param>
/// <param name="FlyoutHost">The element the flyout's items are searched within.</param>
/// <param name="FlyoutItem">One item within the flyout.</param>
public sealed record ShellChromeLocators(
    Locator TabHost,
    Locator Tab,
    Locator FlyoutHost,
    Locator FlyoutItem);

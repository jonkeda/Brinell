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
/// <b>The driver supplies these, not the control objects.</b> They used to be a
/// <c>MauiPlatform</c> switch in <c>ShellChrome</c>, the only platform branch left in the controls.
/// Each backend now answers for its own platform, and one that has not mapped Shell throws
/// <see cref="PlatformNotSupportedException"/> rather than guessing - a platform is added by
/// dumping its tree, not by reasoning about what it probably draws.
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

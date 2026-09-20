using Brinell.Maui.Enums;

namespace Brinell.Maui.Interfaces;

// R9: implements Core's non-generic ITestContext (unchanged by this work) and MAUI's own scope,
// not Core's generic ITestContext<T>. See .docs/decisions/ad-010-maui-ahead-of-core.md.
/// <summary>
/// MAUI test context: the driver, timeouts, logging and navigation, and the app-wide lookup scope.
/// As a scope, <see cref="IMauiElementScope.Context"/> returns the context itself.
/// </summary>
public interface IMauiTestContext : ITestContext, IMauiElementScope
{
    /// <summary>
    /// Gets the wrapped driver.
    /// </summary>
    IMauiDriver Driver { get; }

    /// <summary>
    /// Gets the target platform for this test context.
    /// </summary>
    MauiPlatform Platform { get; }

    /// <summary>
    /// The app itself, as an element: the application window on Windows, the hierarchy root on
    /// Android and iOS.
    /// </summary>
    /// <remarks>
    /// Control objects ask this element for app-level things - the flyout, the alert, the active
    /// dialog, and targets the platform's tree cannot show. Resolved freshly on each read.
    /// </remarks>
    IMauiElement AppElement { get; }

    /// <summary>Where this platform draws MAUI Shell's tabs and flyout.</summary>
    /// <exception cref="PlatformNotSupportedException">Shell has not been mapped on this platform.</exception>
    ShellChromeLocators ShellChrome { get; }

    /// <summary>When a call that succeeded is still logged as a near-miss.</summary>
    Context.NearMissSettings NearMiss { get; }
}

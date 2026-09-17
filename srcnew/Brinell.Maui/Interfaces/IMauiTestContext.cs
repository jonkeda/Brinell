using Brinell.Maui.Enums;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI test context interface with Appium driver access.
/// Combines test context capabilities with MAUI element scope.
/// </summary>
public interface IMauiTestContext : ITestContext<IMauiElement>, IMauiElementScope
{
    /// <summary>
    /// Gets the wrapped Appium driver for operations.
    /// </summary>
    IMauiDriver Driver { get; }
    
    /// <summary>
    /// Gets this context as the element scope.
    /// Implementation should return 'this'.
    /// </summary>
    new IMauiTestContext Context { get; }
    
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
}

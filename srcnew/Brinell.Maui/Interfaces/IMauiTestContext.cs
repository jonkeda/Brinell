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
    /// <para>
    /// <b>How a control object asks the app anything.</b> The flyout, the alert on screen, the
    /// active dialog and targets the platform's tree cannot show have no element of their own, so
    /// control objects used to call the driver for them - a second route beside the element one.
    /// They ask this element instead, and every control call goes through
    /// <see cref="IMauiElement"/>.
    /// </para>
    /// <para>
    /// Resolved freshly on each read: a window UI Automation has retired is attached again rather
    /// than answered for.
    /// </para>
    /// </remarks>
    IMauiElement AppElement { get; }

    /// <summary>Where this platform draws MAUI Shell's tabs and flyout.</summary>
    /// <exception cref="PlatformNotSupportedException">Shell has not been mapped on this platform.</exception>
    ShellChromeLocators ShellChrome { get; }
}

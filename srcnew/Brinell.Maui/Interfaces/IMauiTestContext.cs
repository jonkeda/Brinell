using Brinell.Core.Interfaces;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI-specific test context that narrows the generic TElement to AppiumElement.
/// Provides access to Appium driver and MAUI-specific functionality.
/// </summary>
public interface IMauiTestContext : ITestContext<AppiumElement>
{
    /// <summary>
    /// Gets the Appium driver instance for direct driver access when needed.
    /// </summary>
    AppiumDriver Driver { get; }

    /// <summary>
    /// Gets the current platform (Android/iOS/Windows) being tested.
    /// </summary>
    MauiPlatform Platform { get; }
}

/// <summary>
/// Supported MAUI platforms for mobile testing.
/// </summary>
public enum MauiPlatform
{
    /// <summary>
    /// Android platform using UIAutomator2/Espresso.
    /// </summary>
    Android,

    /// <summary>
    /// iOS platform using XCUITest.
    /// </summary>
    iOS,

    /// <summary>
    /// Windows platform using WinAppDriver.
    /// </summary>
    Windows
}

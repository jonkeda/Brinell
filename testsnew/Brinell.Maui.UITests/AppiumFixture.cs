using Brinell.Maui.Testing;
using Brinell.Maui.UITests.Pages;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.UITests;

/// <summary>
/// Test fixture for Brinell.Samples.Maui.App UI tests.
/// Inherits infrastructure from <see cref="MauiTestFixtureBase"/> and adds app-specific pages.
/// </summary>
public class AppiumFixture : MauiTestFixtureBase
{
    private readonly MainPage _mainPage;
    private readonly ContainerDemoPage _containerDemoPage;
    private readonly AppShellPage _appShell;

    public AppiumFixture()
    {
        _mainPage = new MainPage(Context);
        _containerDemoPage = new ContainerDemoPage(Context);
        _appShell = new AppShellPage(Context);
    }

    /// <summary>
    /// Gets the MainPage page object.
    /// </summary>
    public MainPage MainPage => _mainPage;

    /// <summary>
    /// Gets the ContainerDemoPage page object.
    /// </summary>
    public ContainerDemoPage ContainerDemoPage => _containerDemoPage;

    /// <summary>
    /// Gets the AppShell page object for flyout navigation.
    /// </summary>
    public AppShellPage AppShell => _appShell;

    /// <summary>
    /// Navigates to the Container Demo page via the flyout menu.
    /// Uses MauiFlyoutItemControl with XPath @Name strategy.
    /// </summary>
    public void NavigateToContainerDemo()
    {
        // Check if we're already on the Container Demo page using framework's element finding
        var userProfileLocator = new Locator(LocatorStrategy.XPath, "//*[@Name='User Profile']");
        if (Context.TryFindElement(userProfileLocator) != null)
        {
            return; // Already on the Container Demo page
        }
        
        // Use AppShellPage for navigation
        _appShell.ScrollFlyoutToBottom();
        _appShell.ContainerDemoFlyout.Click();
    }

    #region MauiTestFixtureBase Overrides

    /// <inheritdoc />
    protected override string GetDefaultAppPath(string platform)
    {
        var solutionDir = FindSolutionDirectory();
        
        return platform.ToLowerInvariant() switch
        {
            "windows" => Path.Combine(solutionDir, 
                "samples", "Brinell.Samples.Maui.App", "bin", "Debug", 
                "net10.0-windows10.0.19041.0", "win-x64", "Brinell.Samples.Maui.App.exe"),
            "android" => Path.Combine(solutionDir,
                "samples", "Brinell.Samples.Maui.App", "bin", "Debug",
                "net10.0-android", "com.brinell.samples.maui-Signed.apk"),
            "ios" => Path.Combine(solutionDir,
                "samples", "Brinell.Samples.Maui.App", "bin", "Debug",
                "net10.0-ios", "iossimulator-x64", "Brinell.Samples.Maui.App.app"),
            _ => ""
        };
    }

    /// <inheritdoc />
    protected override void ConfigureAndroidOptions(AppiumOptions options, string appPath)
    {
        base.ConfigureAndroidOptions(options, appPath);
        options.AddAdditionalAppiumOption("appPackage", "com.brinell.samples.maui");
        options.AddAdditionalAppiumOption("appActivity", "crc64hash.MainActivity");
    }

    /// <inheritdoc />
    protected override void ConfigureiOSOptions(AppiumOptions options, string appPath)
    {
        base.ConfigureiOSOptions(options, appPath);
        options.AddAdditionalAppiumOption("bundleId", "com.brinell.samples.maui");
    }

    #endregion
}

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
    private readonly AppShellPage _appShell;
    private readonly MainPage _mainPage;
    private readonly ContainerDemoPage _containerDemoPage;
    private readonly UserFormPage _userFormPage;
    private readonly MediaGalleryPage _mediaGalleryPage;

    public AppiumFixture()
    {
        _appShell = new AppShellPage(Context);
        _mainPage = new MainPage(Context);
        _containerDemoPage = new ContainerDemoPage(Context);
        _userFormPage = new UserFormPage(Context);
        _mediaGalleryPage = new MediaGalleryPage(Context);
    }

    /// <summary>
    /// Gets the AppShell page object for TabbedPage navigation.
    /// </summary>
    public AppShellPage AppShell => _appShell;

    /// <summary>
    /// Gets the MainPage page object (Basics tab content).
    /// </summary>
    public MainPage MainPage => _mainPage;

    /// <summary>
    /// Gets the ContainerDemoPage page object.
    /// </summary>
    public ContainerDemoPage ContainerDemoPage => _containerDemoPage;

    /// <summary>
    /// Gets the UserFormPage page object.
    /// </summary>
    public UserFormPage UserFormPage => _userFormPage;

    /// <summary>
    /// Gets the MediaGalleryPage page object.
    /// </summary>
    public MediaGalleryPage MediaGalleryPage => _mediaGalleryPage;

    /// <summary>
    /// Navigates to the Basics tab (first/main tab).
    /// </summary>
    public void NavigateToMain()
    {
        _appShell.BasicsTab.Click();
        _mainPage.WaitReady(5000);
    }

    /// <summary>
    /// Navigates to the Container Demo page via tab.
    /// </summary>
    public void NavigateToContainerDemo()
    {
        _appShell.ContainersTab.Click();
        // Wait for page to be ready
        if (!_containerDemoPage.WaitReady(5000))
        {
            throw new InvalidOperationException("ContainerDemoPage did not become ready after clicking ContainersTab. PageTitle may not be visible.");
        }
    }

    /// <summary>
    /// Navigates to the User Form page via tab.
    /// </summary>
    public void NavigateToUserForm()
    {
        _appShell.FormsTab.Click();
        // Wait for page to be ready
        if (!_userFormPage.WaitReady(5000))
        {
            throw new InvalidOperationException("UserFormPage did not become ready after clicking FormsTab. UserFormTitle may not be visible.");
        }
    }

    /// <summary>
    /// Navigates to the Media Gallery page via tab.
    /// </summary>
    public void NavigateToMediaGallery()
    {
        _appShell.MediaTab.Click();
        // Wait for page to be ready
        if (!_mediaGalleryPage.WaitReady(5000))
        {
            throw new InvalidOperationException("MediaGalleryPage did not become ready after clicking MediaTab. MediaGalleryTitle may not be visible.");
        }
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
    protected override void ConfigureWindowsOptions(AppiumOptions options, string appPath)
    {
        base.ConfigureWindowsOptions(options, appPath);
        // Increase timeout for app window discovery (TabView takes longer to initialize)
        options.AddAdditionalAppiumOption("appWaitDuration", 30000); // 30 seconds
        options.AddAdditionalAppiumOption("newCommandTimeout", 300); // 5 minutes for command execution
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

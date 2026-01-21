using Brinell.Maui;
using Brinell.Maui.Testing;
using Brinell.Maui.UITests.Pages;

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
    protected override void ConfigureAndroidOptions(MauiDriverOptions options)
    {
        base.ConfigureAndroidOptions(options);
        // MAUI generates hashed activity names (e.g., crc643b83d6491f48953d.MainActivity)
        // Don't specify appPackage/appActivity - let Appium extract from the APK
        options.AdditionalCapabilities["autoGrantPermissions"] = true;
        options.AdditionalCapabilities["newCommandTimeout"] = 300; // 5 minutes
        // MAUI apps take longer to initialize - increase wait times
        options.AdditionalCapabilities["appWaitDuration"] = 60000; // 60 seconds to wait for app
        options.AdditionalCapabilities["appWaitActivity"] = "*"; // Wait for any activity
        options.AdditionalCapabilities["adbExecTimeout"] = 60000; // 60 seconds for ADB commands
    }

    /// <inheritdoc />
    protected override void ConfigureiOSOptions(MauiDriverOptions options)
    {
        base.ConfigureiOSOptions(options);
        options.AdditionalCapabilities["bundleId"] = "com.brinell.samples.maui";
    }

    #endregion
}

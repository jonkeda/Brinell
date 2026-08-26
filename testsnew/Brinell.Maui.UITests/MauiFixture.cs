using Brinell.Maui.Configuration;
using Brinell.Maui.Enums;
using Brinell.Maui.Testing;
using Brinell.Maui.UITests.Containers;
using Brinell.Maui.UITests.Pages;
using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests;

/// <summary>
/// Test fixture for Brinell.Samples.Maui.App UI tests.
/// Inherits infrastructure from <see cref="MauiTestFixtureBase"/> and adds app-specific pages.
/// </summary>
[TestModuleScan(typeof(AppShellPage), NamespacePrefix = "Brinell.Maui.UITests.Pages")]
public class MauiFixture : MauiTestFixtureBase
{
    private readonly AppShellPage _appShell;
    private GridCollectionDemoPage? _gridCollectionDemoPage;
    private AutomationProbePage? _automationProbePage;

    public MauiFixture()
    {
        _appShell = new AppShellPage(Context);
        Composition = TestComposition.ForFixture(this, services =>
            services.AddSingleton<IMauiTestContext>(Context));
    }

    public TestComposition Composition { get; }

    /// <summary>
    /// Gets the AppShell page object for TabbedPage navigation.
    /// </summary>
    public AppShellPage AppShell => _appShell;

     /// <summary>
    /// Navigates to the Basics tab (first/main tab).
    /// </summary>
    public void NavigateToMain()
    {
        _appShell.ButtonsTab.Click();
    }

    /// <summary>
    /// Gets the Grid + CollectionView demo page object.
    /// </summary>
    /// <remarks>
    /// Cached so the form and collection keep their container-root caches across tests.
    /// </remarks>
    public GridCollectionDemoPage GridCollectionDemoPage
        => _gridCollectionDemoPage ??= new GridCollectionDemoPage(Context);

    /// <summary>
    /// Navigates to the container demo and restores its seeded state.
    /// </summary>
    /// <remarks>
    /// This collection shares one fixture and one Shell across test classes, and Shell
    /// may retain page instances, so navigation alone does not guarantee clean state.
    /// Resetting here makes each test order-independent. Every wait is on observed UI
    /// state, never a fixed delay.
    /// </remarks>
    public GridCollectionDemoPage NavigateToGridCollectionDemo()
    {
        _appShell.GridCollectionTab.Click();

        var page = GridCollectionDemoPage;
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        page.Products.Reset(TestConstants.DefaultTestTimeoutMs);
        page.Products.AssertLogicalCount(
            ProductCollection.SeedCount,
            "Container demo did not return to its seeded state after reset.",
            TestConstants.DefaultTestTimeoutMs);

        return page;
    }

    /// <summary>
    /// Gets the Phase 0 automation probe page object.
    /// </summary>
    public AutomationProbePage AutomationProbePage
        => _automationProbePage ??= new AutomationProbePage(Context);

    /// <summary>
    /// Navigates to the automation probe page.
    /// </summary>
    /// <remarks>
    /// The probe page is stateless — it has no data to seed and nothing to mutate —
    /// so unlike <see cref="NavigateToGridCollectionDemo"/> there is nothing to reset.
    /// </remarks>
    public AutomationProbePage NavigateToAutomationProbe()
    {
        _appShell.AutomationProbeTab.Click();

        var page = AutomationProbePage;
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        return page;
    }

    #region MauiTestFixtureBase Overrides

    /// <inheritdoc />
    protected override string GetDefaultAppPath(MauiPlatform platform)
    {
        var solutionDir = FindSolutionDirectory();
        
        return platform switch
        {
            MauiPlatform.Windows => Path.Combine(solutionDir, 
                "samples", "Brinell.Samples.Maui.App", "bin", "Debug", 
                "net10.0-windows10.0.19041.0", "win-x64", "Brinell.Samples.Maui.App.exe"),
            MauiPlatform.Android => Path.Combine(solutionDir,
                "samples", "Brinell.Samples.Maui.App", "bin", "Debug",
                "net10.0-android", "com.brinell.samples.maui-Signed.apk"),
            MauiPlatform.iOS => Path.Combine(solutionDir,
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
        // Don't specify appPackage/appActivity - let Maui extract from the APK
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

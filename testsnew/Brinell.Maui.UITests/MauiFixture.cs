using Brinell.Maui.Configuration;
using Brinell.Maui.Enums;
using Brinell.Maui.Testing;
using Brinell.Maui.UITests.Containers;
using Brinell.Maui.UITests.Pages;

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
    private NavigationDemoPage? _navigationDemoPage;
    private ContainerTestPage? _containerTestPage;

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
    /// <para>
    /// The probe page is stateless — it has no data to seed and nothing to mutate —
    /// so unlike <see cref="NavigateToGridCollectionDemo"/> there is nothing to reset.
    /// </para>
    /// <para>
    /// It does, however, have navigation state to undo: the probe page's module links push
    /// routes onto the Shell stack, so a previous test may have left the app on a pushed page
    /// with this tab still selected. Popping happens before <c>WaitLoaded</c> because a
    /// covered page never reports loaded.
    /// </para>
    /// </remarks>
    public AutomationProbePage NavigateToAutomationProbe()
    {
        _appShell.AutomationProbeTab.Click();

        var page = AutomationProbePage;
        EnsureProbeModuleLinksReachable(page);
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        return page;
    }

    /// <summary>
    /// Gets the navigation controls demo page object.
    /// </summary>
    /// <remarks>
    /// Cached so the toolbars and menu keep any element caches across tests.
    /// </remarks>
    public NavigationDemoPage NavigationDemoPage
        => _navigationDemoPage ??= new NavigationDemoPage(Context);

    /// <summary>
    /// Navigates to the navigation demo and restores its initial state.
    /// </summary>
    /// <remarks>
    /// The fixture is shared across test classes and Shell may retain page instances, so
    /// navigation alone does not guarantee clean state. Reset makes each test
    /// order-independent; the wait is on observed UI state, never a fixed delay.
    /// </remarks>
    public NavigationDemoPage NavigateToNavigationDemo()
    {
        _appShell.NavigationTab.Click();

        var page = NavigationDemoPage;
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        page.ResetButton.Click();
        page.LastAction.WaitText("none", TestConstants.DefaultTestTimeoutMs);

        return page;
    }

    /// <summary>
    /// Gets the container module test page object.
    /// </summary>
    public ContainerTestPage ContainerTestPage
        => _containerTestPage ??= new ContainerTestPage(Context);

    /// <summary>
    /// Navigates to the container module page and restores its initial state.
    /// </summary>
    /// <remarks>
    /// Reached through the Modules hub, not a tab: only 9 Shell tabs are clickable on
    /// Windows and the tab bar is full.
    /// </remarks>
    public ContainerTestPage NavigateToContainerModule()
    {
        // NavigateToAutomationProbe pops any route a previous test pushed, so the module
        // links are reachable by the time it returns.
        var probe = NavigateToAutomationProbe();

        probe.GoToContainerButton.Click();

        var page = ContainerTestPage;
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        page.ResetButton.Click();
        page.Status.WaitText("none", TestConstants.DefaultTestTimeoutMs);

        return page;
    }

    /// <summary>
    /// Ensures the probe page is showing its own content, popping pushed routes if needed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The module pages are reached with <c>Shell.Current.GoToAsync("ContainerPage")</c>,
    /// which <em>pushes</em> onto the tab's navigation stack rather than replacing the tab's
    /// content. So after one test the app sits on the pushed page with the Probe tab still
    /// selected, and the module links are not on screen.
    /// </para>
    /// <para>
    /// Clicking the Probe tab does not fix this: the tab is already current, so the click is
    /// a no-op and the pushed page stays on top. The stack has to be popped, which is what
    /// the driver's back navigation does — the same Shell back arrow a user would press.
    /// </para>
    /// <para>
    /// A test may have pushed more than one page, so this pops until the links are reachable,
    /// up to a bound. Failing that it throws immediately: the alternative is to fall through
    /// to a <c>Click</c> that spends its full timeout failing to find a button that cannot be
    /// there, reporting nothing about why. See
    /// <c>.my/maui/rca/rca-001-container-module-tests-navigation-stack.md</c>.
    /// </para>
    /// </remarks>
    private void EnsureProbeModuleLinksReachable(AutomationProbePage probe)
    {
        const int MaxPops = 3;

        // Probe timeout, not the standard wait: the button is either already rendered or the
        // app is on a pushed route, and no amount of waiting changes which. A full
        // ShortTestTimeoutMs here is paid on every test after the first, purely to confirm an
        // absence that popping is about to fix.
        const int PresenceProbeMs = 750;

        if (probe.GoToContainerButton.WaitExists(true, PresenceProbeMs))
        {
            return;
        }

        for (var pop = 0; pop < MaxPops; pop++)
        {
            Context.Driver.NavigateBack();

            if (probe.GoToContainerButton.WaitExists(true, PresenceProbeMs))
            {
                return;
            }
        }

        throw new InvalidOperationException(
            $"Could not return to the AutomationProbe page: 'GoToContainerButton' is still not " +
            $"present after {MaxPops} back-navigations. The app is most likely on a pushed Shell " +
            "route that did not pop. See .my/maui/rca/rca-001-container-module-tests-navigation-stack.md.");
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

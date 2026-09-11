using Brinell.Core.Diagnostics;
using Brinell.Maui.Containers;
using Brinell.Core.Utilities;
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
[TestModuleScan(typeof(HubPage), NamespacePrefix = "Brinell.Maui.UITests.Pages")]
public class MauiFixture : MauiTestFixtureBase
{
    /// <summary>
    /// Held for as long as this fixture's app is running. See <see cref="DesktopLease"/>.
    /// </summary>
    /// <remarks>
    /// <b>A field initializer, and it has to be.</b> These run before the base constructor,
    /// which is where the app is launched - so the desktop is taken before anything appears on
    /// it. Acquiring in the constructor body would take the lease after the window was already
    /// up, which is a lease over the wrong interval.
    /// </remarks>
    private readonly IDisposable _desktop = DesktopLease.Acquire();

    private readonly HubPage _hub;
    private readonly AppRoot _appRoot;

    public MauiFixture()
    {
        _hub = new HubPage(Context);
        _appRoot = new AppRoot(Context);
        Composition = TestComposition.ForFixture(this, services =>
            services.AddSingleton<IMauiTestContext>(Context));
    }

    public TestComposition Composition { get; }

    /// <summary>
    /// The "back to hub" toolbar item, which the hub attaches to every page it opens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scoped to the app, not to a page</b>, because that is where it is: MAUI draws a
    /// <c>ToolbarItem</c> into the window's chrome. It was once a <c>Button&lt;HubPage&gt;</c>,
    /// which is a contradiction - the hub is not loaded precisely when the way back to it is
    /// needed - and the readiness gates correctly refused, taking the whole suite down with them.
    /// The fix at the time replaced the control object with a hand-rolled lookup, which cost the
    /// activation ladder, the logging and the failure messages that come with one; see
    /// <c>.my/fix/rca-backtohub-is-not-a-control-object.md</c>. <see cref="AppRoot"/> is the
    /// scope that was missing.
    /// </para>
    /// <para>
    /// Located by accessibility id rather than automation id: MAUI surfaces a toolbar item's
    /// <c>AutomationId</c> as the platform accessibility label, and that one locator is the same
    /// string on Windows, Android and iOS.
    /// </para>
    /// </remarks>
    public ToolbarButton<AppRoot> BackToHub
        => new(_appRoot, Locator.ByAccessibilityId("BackToHub"));

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            // After the base, so the app is gone before the next collection is let in.
            _desktop.Dispose();
        }
    }

    /// <summary>
    /// Gets the hub page object: the app's flat page list.
    /// </summary>
    public HubPage Hub => _hub;


    /// <summary>
    /// Opens a page from the hub, returning to the hub first if another page is open.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The single navigation primitive: one click, identical on Windows, Android and iOS.
    /// It replaced Shell tab navigation, which was neither - Android hid tabs past the fifth
    /// behind an overflow menu, and Windows exposed only nine.
    /// </para>
    /// <para>
    /// Because the hub pushes a fresh page instance each time and this pops back to the hub
    /// first, a test cannot inherit the previous test's page or its position in a navigation
    /// stack — the absence of state to recover from, rather than a recovery routine.
    /// </para>
    /// </remarks>
    public void Open(SamplePage page)
    {
        ReturnToHub();
        _hub.OpenButton(page).Click();
    }

    /// <summary>
    /// Returns to the hub if a page is open.
    /// </summary>
    private void ReturnToHub()
    {
        var attempts = new List<string>();

        if (Context.Driver.TryNavigateBack())
        {
            attempts.Add("popped through the bridge");
        }
        else if (!_hub.IsLoaded())
        {
            attempts.Add("no bridge answered; falling back to the toolbar click");

            if (BackToHub.WaitExists(true, TestConstants.ShortTestTimeoutMs))
            {
                PhysicalInput.Used("MauiFixture.ReturnToHub", "a working activation route for ToolbarItem");
                BackToHub.Click();
            }
            else
            {
                attempts.Add("no 'BackToHub' item on this page either");
            }
        }

        if (!_hub.WaitLoaded(true, TestConstants.ShortTestTimeoutMs))
        {
            throw new InvalidOperationException(
                "Could not get back to the hub, so the next page cannot be opened. This is a "
                + "navigation failure, not a fault in whatever is opened next.\n  "
                + string.Join("\n  ", attempts));
        }
    }


    /// <summary>
    /// Navigates to the Buttons page.
    /// </summary>
    public void NavigateToMain()
    {
        Open(SamplePage.Buttons);
    }

    /// <summary>
    /// Gets the Grid + CollectionView demo page object.
    /// </summary>
    /// <remarks>
    /// Created per access, not cached. A container object caches the element it resolved as
    /// its root, and the hub builds a fresh page on every open — so a cached page object
    /// would hold roots belonging to a page instance that no longer exists. Shell used to
    /// retain page instances, which is what made caching viable before.
    /// </remarks>
    public GridCollectionDemoPage GridCollectionDemoPage => new(Context);

    /// <summary>
    /// Navigates to the container demo and restores its seeded state.
    /// </summary>
    /// <remarks>
    /// The hub builds a fresh page on every open, but this fixture is shared across test
    /// classes and the page object caches container roots, so the demo's data is still reset
    /// here to keep tests order-independent. Every wait is on observed UI state, never a
    /// fixed delay.
    /// </remarks>
    public GridCollectionDemoPage NavigateToGridCollectionDemo()
    {
        Open(SamplePage.GridCollection);

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
    /// Opens the scroll test page and returns it in its reset state.
    /// </summary>
    public ScrollTestPage NavigateToScroll()
    {
        Open(SamplePage.Scroll);

        var page = new ScrollTestPage(Context);
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);
        page.ResetButton.Click();
        page.StatusLabel.WaitText("none", TestConstants.DefaultTestTimeoutMs);
        return page;
    }

    /// <summary>
    /// Gets the Phase 0 automation probe page object.
    /// </summary>
    public AutomationProbePage AutomationProbePage => new(Context);

    /// <summary>
    /// Navigates to the automation probe page.
    /// </summary>
    /// <remarks>
    /// The probe page is stateless — it has no data to seed and nothing to mutate — so
    /// unlike <see cref="NavigateToGridCollectionDemo"/> there is nothing to reset.
    /// </remarks>
    public AutomationProbePage NavigateToAutomationProbe()
    {
        Open(SamplePage.AutomationProbe);

        var page = AutomationProbePage;
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        return page;
    }

    /// <summary>
    /// Gets the navigation controls demo page object.
    /// </summary>
    /// <remarks>
    /// Created per access — see <see cref="GridCollectionDemoPage"/> for why caching a page
    /// object is no longer safe.
    /// </remarks>
    public NavigationDemoPage NavigationDemoPage => new(Context);

    /// <summary>
    /// Navigates to the navigation demo and restores its initial state.
    /// </summary>
    /// <remarks>
    /// The page object is cached across test classes, so its state is reset explicitly to
    /// keep tests order-independent; the wait is on observed UI state, never a fixed delay.
    /// </remarks>
    public NavigationDemoPage NavigateToNavigationDemo()
    {
        Open(SamplePage.Navigation);

        var page = NavigationDemoPage;
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        page.ResetButton.Click();
        page.LastAction.WaitText("none", TestConstants.DefaultTestTimeoutMs);

        return page;
    }

    /// <summary>
    /// Gets the container module test page object.
    /// </summary>
    public ContainerTestPage ContainerTestPage => new(Context);

    /// <summary>
    /// Navigates to the container module page and restores its initial state.
    /// </summary>
    /// <remarks>
    /// Opened directly from the hub, so no other page's state can leak into it.
    /// </remarks>
    public ContainerTestPage NavigateToContainerModule()
    {
        Open(SamplePage.Container);

        var page = ContainerTestPage;
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        page.ResetButton.Click();
        page.Status.WaitText("none", TestConstants.DefaultTestTimeoutMs);

        return page;
    }

    /// <summary>
    /// Opens the dialogs sample page and restores its initial result.
    /// </summary>
    public DialogsTestPage NavigateToDialogs()
    {
        Open(SamplePage.Dialogs);

        var page = new DialogsTestPage(Context);
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);
        page.ResetButton.Click();
        page.Result.WaitText("none", TestConstants.DefaultTestTimeoutMs);
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
        options.AdditionalCapabilities["adbExecTimeout"] = 60000; // 60 seconds for ADB commands

        // MAUI hashes the activity name (crc64<hash>.MainActivity) and the hash changes with
        // the namespace, so it cannot be written literally here. Waiting on the package alone
        // lets UiAutomator2 accept whatever activity that package launches.
        options.AdditionalCapabilities["appWaitPackage"] = "com.brinell.samples.maui";

        // UiAutomator2 waits for the app to report idle before each command, defaulting to
        // 10 s. A MAUI app that animates may never report idle, so commands pay that wait.
        // See .my/scroll/perf-why-exists-was-slow.md for the measurements behind these settings.
        options.AdditionalCapabilities["settings[waitForIdleTimeout]"] = 100;

        // Reinstall even when the installed APK reports the same version: MAUI does not bump
        // versionCode between builds, so without this a run can silently test the previous one.
        options.AdditionalCapabilities["enforceAppInstall"] = true;
    }

    /// <inheritdoc />
    protected override void ConfigureiOSOptions(MauiDriverOptions options)
    {
        base.ConfigureiOSOptions(options);
        options.AdditionalCapabilities["bundleId"] = "com.brinell.samples.maui";
    }

    #endregion
}

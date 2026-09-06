using Brinell.Maui.Configuration;
using Brinell.Maui.Enums;
using Brinell.Maui.Testing;
using Brinell.Maui.UITests.Pages.Shell;

namespace Brinell.Maui.UITests;

/// <summary>
/// Test fixture for the Shell sample app.
/// </summary>
/// <remarks>
/// A second app, and so a second fixture, because the two navigation models want opposite
/// things from a fixture: the hub app wants "return to hub, open page"; Shell wants "pop this
/// tab's stack, then select a tab". See .my/navigation/design-shell-sample-app.md.
/// </remarks>
public class ShellFixture : MauiTestFixtureBase
{
    /// <summary>How many pushed pages the reset will pop before giving up.</summary>
    private const int MaxPops = 3;

    private readonly ShellSamplePage _page;

    public ShellFixture()
    {
        _page = new ShellSamplePage(Context);
    }

    /// <summary>The Shell sample app's page object.</summary>
    public ShellSamplePage Page => _page;

    /// <summary>
    /// Returns the app to a tab's root page and selects <paramref name="title"/>.
    /// </summary>
    /// <remarks>
    /// The one navigation primitive for this app, and the reason it exists is RCA-001:
    /// clicking a tab does <b>not</b> pop that tab's stack, so a pushed page left by one test
    /// is still there for the next. The stack is popped through the app's own back affordance
    /// first; only then is a tab selected.
    /// </remarks>
    /// <param name="title">The tab to select.</param>
    /// <returns>The page object.</returns>
    public ShellSamplePage OpenTab(string title)
    {
        ReturnToShellRoot();

        _page.Shell.Tabs[title].Click();
        return _page;
    }

    /// <summary>
    /// Puts the shell back to a known state: no flyout open, no pushed page.
    /// </summary>
    private void ReturnToShellRoot()
    {
        // An open flyout is an overlay across the tabs, so it goes first.
        _page.Shell.Flyout.Close();

        // Then pop whatever a previous test pushed. Only the Detail tab can push, so its
        // sub-page marker is the whole question.
        for (var pop = 0; pop < MaxPops && _page.IsSubPagePushed(); pop++)
        {
            _page.SubPageBackButton.Click();
        }

        // A flyout item that is not the tabbed section has no tabs, so a test that ended on one
        // leaves nothing to select. Going back through the flyout is the only way in, and it is
        // what a user would do.
        //
        // Asked by counting tabs, not by asking whether the strip is there: on Windows the host
        // element survives into a flyout section and simply reports no items, so "the strip
        // exists" is true on a page that has no tabs at all.
        if (_page.Shell.Tabs.GetItemCount() == 0)
        {
            _page.Shell.Flyout.Open()["Main"].Click();
        }
    }

    /// <inheritdoc />
    protected override string GetDefaultAppPath(MauiPlatform platform)
    {
        var solutionDir = FindSolutionDirectory();

        return platform switch
        {
            MauiPlatform.Windows => Path.Combine(solutionDir,
                "samples", "Brinell.Samples.Maui.ShellApp", "bin", "Debug",
                "net10.0-windows10.0.19041.0", "win-x64", "Brinell.Samples.Maui.ShellApp.exe"),
            MauiPlatform.Android => Path.Combine(solutionDir,
                "samples", "Brinell.Samples.Maui.ShellApp", "bin", "Debug",
                "net10.0-android", "com.brinell.samples.shell-Signed.apk"),
            _ => ""
        };
    }

    /// <inheritdoc />
    protected override void ConfigureAndroidOptions(MauiDriverOptions options)
    {
        base.ConfigureAndroidOptions(options);

        options.AdditionalCapabilities["autoGrantPermissions"] = true;
        options.AdditionalCapabilities["newCommandTimeout"] = 300;
        options.AdditionalCapabilities["appWaitDuration"] = 60000;
        options.AdditionalCapabilities["adbExecTimeout"] = 60000;

        // MAUI hashes the activity name, so wait on the package instead.
        options.AdditionalCapabilities["appWaitPackage"] = "com.brinell.samples.shell";

        // Both settings are carried over from the other fixture, where they were measured:
        // a MAUI app that animates may never report idle, and MAUI does not bump versionCode
        // between builds, so without the reinstall a run can silently test the previous one.
        options.AdditionalCapabilities["settings[waitForIdleTimeout]"] = 100;
        options.AdditionalCapabilities["enforceAppInstall"] = true;
    }
}

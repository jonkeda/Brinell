using Brinell.Maui.Context;
using Brinell.Maui.Testing;
using Brinell.Presenter.Uat.Tests.PageObjects;

namespace Brinell.Presenter.Uat.Tests.Runtime;

public sealed class PresenterFixture : MauiTestFixtureBase
{
    public PresenterFixture()
    {
        PresenterPage = new PresenterPage(Context);
    }

    public PresenterPage PresenterPage { get; }

    protected override MauiTestContextOptions CreateTestContextOptions()
    {
        var settingsDirectory = Path.Combine(
            Path.GetTempPath(),
            "BrinellPresenterUat",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(settingsDirectory);
        Environment.SetEnvironmentVariable(
            "BRINELL_PRESENTER_SETTINGS_PATH",
            Path.Combine(settingsDirectory, "presenter-settings.json"));

        return base.CreateTestContextOptions();
    }

    protected override string GetDefaultAppPath(string platform)
    {
        var solutionDirectory = FindSolutionDirectory();
        return platform.ToLowerInvariant() switch
        {
            "windows" => Path.Combine(
                solutionDirectory,
                "srcnew",
                "Brinell.Presenter",
                "bin",
                "Debug",
                "net10.0-windows10.0.19041.0",
                "win-x64",
                "Brinell.Presenter.exe"),
            _ => string.Empty
        };
    }
}

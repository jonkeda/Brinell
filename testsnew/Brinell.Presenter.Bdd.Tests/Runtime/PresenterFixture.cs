using Brinell.Maui.Context;
using Brinell.Maui.Enums;
using Brinell.Maui.Testing;
using Brinell.Presenter.Bdd.Tests.PageObjects;

namespace Brinell.Presenter.Bdd.Tests.Runtime;

[TestModuleScan(typeof(PresenterPage), NamespacePrefix = "Brinell.Presenter.Bdd.Tests.PageObjects")]
public sealed class PresenterFixture : MauiTestFixtureBase
{
    public PresenterFixture()
    {
        Composition = TestComposition.ForFixture(this, services =>
            services.AddSingleton<IMauiTestContext>(Context));
    }

    public TestComposition Composition { get; }

    /// <summary>Where Presenter's Record puts its videos during the suite.</summary>
    public string RecordingsFolder { get; private set; } = string.Empty;

    protected override MauiTestContextOptions CreateTestContextOptions()
    {
        var settingsDirectory = Path.Combine(
            Path.GetTempPath(),
            "BrinellPresenterUat",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(settingsDirectory);
        var settingsPath = Path.Combine(settingsDirectory, "presenter-settings.json");
        Environment.SetEnvironmentVariable("BRINELL_PRESENTER_SETTINGS_PATH", settingsPath);

        // Recordings go beside the settings, not into the repository's TestResults.
        RecordingsFolder = Path.Combine(settingsDirectory, "recordings");
        File.WriteAllText(settingsPath, System.Text.Json.JsonSerializer.Serialize(new { RecordingsFolder }));

        return base.CreateTestContextOptions();
    }

    protected override string GetDefaultAppPath(MauiPlatform platform)
    {
        var solutionDirectory = FindSolutionDirectory();
        return platform switch
        {
             MauiPlatform.Windows => Path.Combine(
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

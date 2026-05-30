using Brinell.Presenter.Services;

namespace Brinell.Presenter.Uat.Tests.Services;

public sealed class UatWorkspaceServiceTests
{
    [Fact]
    public void LoadFolder_ReportsMissingAppPathBeforeExecution()
    {
        var root = Path.Combine(Path.GetTempPath(), "BrinellPresenterUat", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(
                Path.Combine(root, "uat.config.md"),
                """
                # UAT Config

                ## Runtime

                | Field | Value |
                | --- | --- |
                | Target | MAUI |
                | Fixture | Appium |

                ## Assemblies

                | Kind | Assembly |
                | --- | --- |
                | Pages | Missing.Pages.dll |
                """);

            var result = new UatWorkspaceService().LoadFolder(root);

            Assert.True(result.Config.HasErrors);
            Assert.Contains("App missing", result.Config.Summary);
            Assert.Contains(result.Diagnostics, line => line.Contains("Runtime AppPath is required", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

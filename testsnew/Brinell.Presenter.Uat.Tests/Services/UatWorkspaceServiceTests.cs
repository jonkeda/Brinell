using Brinell.Presenter.Services;

namespace Brinell.Presenter.Uat.Tests.Services;

public sealed class UatWorkspaceServiceTests
{
    public static TheoryData<string> SupportedTargets => new()
    {
        "MAUI",
        "WPF",
        "WINFORMS",
        "BLAZOR",
        "HTML",
        "STRIDE"
    };

    [Theory]
    [MemberData(nameof(SupportedTargets))]
    public void LoadFolder_AcceptsSupportedDotNetTargets(string target)
    {
        var root = Path.Combine(Path.GetTempPath(), "BrinellPresenterUat", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var appPath = Path.Combine(root, $"{target}.exe");
            File.WriteAllText(appPath, string.Empty);

            File.WriteAllText(
                Path.Combine(root, "uat.config.md"),
                $$"""
                # UAT Config

                ## Runtime

                | Field | Value |
                | --- | --- |
                | Target | {{target}} |
                | Fixture | SampleFixture |
                | AppPath | {{appPath}} |

                ## Assemblies

                | Kind | Assembly |
                | --- | --- |
                | Pages | {{typeof(UatWorkspaceServiceTests).Assembly.Location}} |
                """);

            var result = new UatWorkspaceService().LoadFolder(root);

            Assert.False(result.Config.HasErrors, string.Join(Environment.NewLine, result.Config.Diagnostics));
            Assert.Equal(target, result.Config.Target);
            Assert.Contains(target, result.Config.Summary, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

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

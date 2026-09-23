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

            // SampleFixture does not exist, which is its own error; what this asserts is that
            // every supported target string is accepted and reported back.
            Assert.DoesNotContain(
                result.Config.Diagnostics,
                line => line.Contains("Runtime Target", StringComparison.Ordinal));
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
    public void LoadFolder_ConfiguredAppPathThatDoesNotExist_IsStillAnError()
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
                | AppPath | bin/Debug/net10.0-windows/win10-x64/Nope.exe |

                ## Assemblies

                | Kind | Assembly |
                | --- | --- |
                | Pages | Missing.Pages.dll |
                """);

            var result = new UatWorkspaceService().LoadFolder(root);

            Assert.Contains("App missing", result.Config.Summary);
            Assert.Contains(
                result.Diagnostics,
                line => line.Contains("Runtime AppPath was not found", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    /// <summary>
    /// A config without AppPath is complete: the fixture owns the app. Only a configured
    /// path that does not exist is an error.
    /// </summary>
    [Fact]
    public void LoadFolder_WithoutAppPath_ReportsNoAppError()
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

            Assert.Contains("App from fixture", result.Config.Summary);
            Assert.DoesNotContain(
                result.Diagnostics,
                line => line.Contains("AppPath", StringComparison.OrdinalIgnoreCase));

            // The missing Pages assembly is still an error; only the app stopped being one.
            Assert.True(result.Config.HasErrors);
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

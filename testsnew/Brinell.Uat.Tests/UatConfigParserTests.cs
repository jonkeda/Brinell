using Brinell.Core.Artifacts;
using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatConfigParserTests
{
    [Fact]
    public void Parse_ConfigMarkdown_ReturnsRuntimeAssembliesAndDiscoverySettings()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brinell-uat-config-{Guid.NewGuid():N}");
        using var environment = new ArtifactEnvironment(root, "run-1");
        try
        {
            var markdown = """
                # UAT Config

                ## Runtime

                | Field | Value |
                | --- | --- |
                | Target | MAUI |
                | Profile | Local |

                ## Assemblies

                | Kind | Assembly |
                | --- | --- |
                | Pages | Example.App.Pages.dll |
                | Controls | Example.App.Controls.dll |
                | Commands | Example.App.UatCommands.dll |

                ## Discovery

                | Field | Value |
                | --- | --- |
                | RequireExplicitUatAttributes | true |
                | AllowNameInference | false |

                ## Reporting

                | Field | Value |
                | --- | --- |
                | OutputDirectory | artifacts/uat |
                | ScreenshotOnFailure | true |
                | IncludeRuntimeTrace | true |

                ## Skip Rules

                | Tag | EnvironmentVariable |
                | --- | --- |
                | hardware | EXAMPLE_UAT_HARDWARE |
                | @live-api | EXAMPLE_UAT_LIVE_API |
                """;

            var config = UatConfigParser.Parse(markdown, "ConfigParserSuite");

            Assert.Equal("MAUI", config.Runtime["Target"]);
            Assert.Equal("Local", config.Runtime["Profile"]);
            Assert.Equal(3, config.Assemblies.Count);
            Assert.Contains(config.Assemblies, x => x.Kind == "Pages" && x.Assembly == "Example.App.Pages.dll");
            Assert.True(config.Discovery.RequireExplicitUatAttributes);
            Assert.False(config.Discovery.AllowNameInference);
            Assert.Equal(
                Path.Combine(root, "run-1", "suites", "ConfigParserSuite", "artifacts", "uat"),
                config.Reporting.OutputDirectory);
            Assert.True(config.Reporting.ScreenshotOnFailure);
            Assert.True(config.Reporting.IncludeRuntimeTrace);
            Assert.Equal(2, config.SkipRules.Count);
            Assert.Contains(config.SkipRules, x => x.Tag == "hardware" && x.EnvironmentVariable == "EXAMPLE_UAT_HARDWARE");
            Assert.Contains(config.SkipRules, x => x.Tag == "live-api" && x.EnvironmentVariable == "EXAMPLE_UAT_LIVE_API");
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
    public void Parse_ConfigWithoutOptionalSections_UsesDefaultSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brinell-uat-config-{Guid.NewGuid():N}");
        using var environment = new ArtifactEnvironment(root, "run-1");
        try
        {
            var markdown = """
                # UAT Config

                ## Runtime

                | Field | Value |
                | --- | --- |
                | Target | MAUI |
                """;

            var config = UatConfigParser.Parse(markdown, "ConfigParserSuite");

            Assert.False(config.Discovery.RequireExplicitUatAttributes);
            Assert.True(config.Discovery.AllowNameInference);
            Assert.Equal(
                Path.Combine(root, "run-1", "suites", "ConfigParserSuite", "uat"),
                config.Reporting.OutputDirectory);
            Assert.False(config.Reporting.ScreenshotOnFailure);
            Assert.False(config.Reporting.IncludeRuntimeTrace);
            Assert.Empty(config.SkipRules);
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
    public void EvaluateSkip_TagMatchesRuleAndEnvironmentDisabled_ReturnsSkipDecision()
    {
        var config = UatConfigParser.Parse("""
            # UAT Config

            ## Skip Rules

            | Tag | EnvironmentVariable |
            | --- | --- |
            | hardware | EXAMPLE_UAT_HARDWARE |
            """);

        var decision = config.EvaluateSkip(
            ["smoke", "@hardware"],
            name => name == "EXAMPLE_UAT_HARDWARE" ? "0" : null);

        Assert.True(decision.ShouldSkip);
        Assert.NotNull(decision.Rule);
        Assert.Equal("hardware", decision.Rule.Tag);
        Assert.Contains("EXAMPLE_UAT_HARDWARE", decision.Reason);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("yes")]
    [InlineData("on")]
    public void EvaluateSkip_TagMatchesRuleAndEnvironmentEnabled_ReturnsRunDecision(string enabledValue)
    {
        var config = UatConfigParser.Parse("""
            # UAT Config

            ## Skip Rules

            | Tag | EnvironmentVariable |
            | --- | --- |
            | live-api | EXAMPLE_UAT_LIVE_API |
            """);

        var decision = config.EvaluateSkip(
            ["live-api"],
            name => name == "EXAMPLE_UAT_LIVE_API" ? enabledValue : null);

        Assert.False(decision.ShouldSkip);
        Assert.Null(decision.Rule);
        Assert.Null(decision.Reason);
    }

    [Fact]
    public void EvaluateSkip_NoMatchingTag_ReturnsRunDecision()
    {
        var config = UatConfigParser.Parse("""
            # UAT Config

            ## Skip Rules

            | Tag | EnvironmentVariable |
            | --- | --- |
            | hardware | EXAMPLE_UAT_HARDWARE |
            """);

        var decision = config.EvaluateSkip(
            ["smoke"],
            _ => null);

        Assert.False(decision.ShouldSkip);
    }

    private sealed class ArtifactEnvironment : IDisposable
    {
        private readonly string? _rootDirectory;
        private readonly string? _runId;
        private readonly string? _suite;

        public ArtifactEnvironment(string rootDirectory, string runId)
        {
            _rootDirectory = Environment.GetEnvironmentVariable(TestArtifactOptions.RootDirectoryEnvironmentVariable);
            _runId = Environment.GetEnvironmentVariable(TestArtifactOptions.RunIdEnvironmentVariable);
            _suite = Environment.GetEnvironmentVariable(TestArtifactOptions.SuiteEnvironmentVariable);

            Environment.SetEnvironmentVariable(TestArtifactOptions.RootDirectoryEnvironmentVariable, rootDirectory);
            Environment.SetEnvironmentVariable(TestArtifactOptions.RunIdEnvironmentVariable, runId);
            Environment.SetEnvironmentVariable(TestArtifactOptions.SuiteEnvironmentVariable, null);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(TestArtifactOptions.RootDirectoryEnvironmentVariable, _rootDirectory);
            Environment.SetEnvironmentVariable(TestArtifactOptions.RunIdEnvironmentVariable, _runId);
            Environment.SetEnvironmentVariable(TestArtifactOptions.SuiteEnvironmentVariable, _suite);
        }
    }
}

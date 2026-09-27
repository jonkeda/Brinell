using Brinell.Core.Configuration;
using Brinell.Core.Interfaces;
using Brinell.Core.Settings;
using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatTestBaseTests
{
    [Fact]
    public async Task ScenarioTestBase_RunUatFileAsync_ExecutesScenarioAndProjectHooks()
    {
        var directory = CreateTempDirectory();
        try
        {
            var configFilePath = WriteConfig(directory);
            var scenarioFilePath = WriteScenario(directory, """
                # Feature: Base Harness

                ## Metadata

                | Field | Value |
                | --- | --- |
                | App | Harness |
                | Area | Runtime |
                | Target | TEST |
                | Tags | smoke |
                | Mode | Automated |
                | Requires | None |
                | Priority | Smoke |
                | Evidence | none |

                @smoke
                ## Scenario: Passing flow

                When I run the custom action
                Then the custom action should have run
                """);
            var fixture = new HarnessFixture();
            var harness = new ScenarioHarness(fixture, configFilePath, CreatePassingCatalog(fixture));

            await harness.RunFileAsync(scenarioFilePath);

            Assert.Equal(1, fixture.ActionCount);
            Assert.Equal("Passing flow", harness.BeforeScenarioName);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ScenarioTestBase_RunExpectedFailureUatFileAsync_AssertsFormattedFailureDetails()
    {
        var directory = CreateTempDirectory();
        try
        {
            var configFilePath = WriteConfig(directory, screenshotOnFailure: true);
            var scenarioFilePath = WriteScenario(directory, """
                # Feature: Base Harness

                @smoke
                ## Scenario: Expected failure flow

                When I fail the custom action
                """);
            var fixture = new HarnessFixture();
            var catalog = new UatCommandCatalog();
            catalog.Register(
                UatEffectiveStepKeyword.When,
                "I fail the custom action",
                "Harness.Fail",
                handler: (_, invocation, _) => Task.FromResult(
                    UatStepResult.Failed(invocation, "Intentional failure from harness")));
            var harness = new ScenarioHarness(fixture, configFilePath, catalog);

            await harness.RunExpectedFailureFileAsync(
                scenarioFilePath,
                "Harness.Fail",
                "Intentional failure from harness");

            Assert.Equal("Expected failure flow", harness.BeforeScenarioName);
            Assert.Equal("ScenarioHarness", fixture.ScreenshotService.TestClass);
            Assert.Equal("Expected failure flow", fixture.ScreenshotService.TestMethod);
            Assert.Equal("failure", fixture.ScreenshotService.Description);
            Assert.Equal("evidence.png", fixture.ScreenshotService.LastCapturePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SpecFormatTestBase_ValidatesMetadataBindingAndConfig()
    {
        var directory = CreateTempDirectory();
        try
        {
            var configFilePath = WriteConfig(directory);
            var scenarioFilePath = WriteScenario(directory, """
                # Feature: Base Harness

                ## Metadata

                | Field | Value |
                | --- | --- |
                | App | Harness |
                | Area | Runtime |
                | Target | TEST |
                | Tags | smoke |
                | Mode | Automated |
                | Requires | None |
                | Priority | Smoke |
                | Evidence | none |

                @smoke
                ## Scenario: Spec format flow

                When I run the custom action
                """);
            var harness = new SpecFormatHarness(configFilePath);

            harness.AssertMetadata(scenarioFilePath);
            harness.AssertBinds(scenarioFilePath);
            harness.AssertConfigParses();

            Assert.True(harness.ConfigAsserted);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ScenarioTestBase_RunUatFileAsync_LoadsScenarioSettings()
    {
        var directory = CreateTempDirectory();
        try
        {
            var settingsDirectory = Path.Combine(directory, "TestSettings");
            Directory.CreateDirectory(Path.Combine(settingsDirectory, "scenarios"));
            File.WriteAllText(Path.Combine(settingsDirectory, "testsettings.json"), """
                {
                  "settings": {
                    "harness": {
                      "mode": "default"
                    }
                  }
                }
                """);
            File.WriteAllText(Path.Combine(settingsDirectory, "scenarios", "uat-900-1.json"), """
                {
                  "settings": {
                    "harness": {
                      "mode": "scenario"
                    }
                  }
                }
                """);
            var configFilePath = WriteConfig(directory);
            var scenarioFilePath = WriteScenario(directory, """
                # Feature: Settings Harness

                @uat-900-1
                ## Scenario: Scenario settings flow

                Then the scenario setting should be available
                """);
            var fixture = new HarnessFixture();
            var catalog = new UatCommandCatalog();
            catalog.Register(
                UatEffectiveStepKeyword.Then,
                "the scenario setting should be available",
                "Harness.AssertSettings",
                handler: (context, invocation, _) =>
                {
                    return Task.FromResult(
                        context.GetSettings().GetRequired<string>("harness.mode") == "scenario"
                            ? UatStepResult.Passed(invocation)
                            : UatStepResult.Failed(invocation, "Scenario settings were not loaded."));
                });
            var harness = new ScenarioHarness(fixture, configFilePath, catalog);

            await harness.RunFileAsync(scenarioFilePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ScenarioTestBase_RunUatScenarioAsync_RunsOnlyTheNamedScenario_AndEntersEachFileOnce()
    {
        var directory = CreateTempDirectory();
        try
        {
            var configFilePath = WriteConfig(directory);
            var first = WriteScenario(directory, """
                # Feature: First file

                ## Scenario: One

                When I run the custom action

                ## Scenario: Two

                When I run the custom action
                """);
            var second = Path.Combine(directory, "second.feature.md");
            File.WriteAllText(second, """
                # Feature: Second file

                ## Scenario: Three

                When I run the custom action
                """);
            var fixture = new HarnessFixture();
            var harness = new ScenarioHarness(fixture, configFilePath, CreatePassingCatalog(fixture));

            await harness.RunScenarioAsync(first, "Two");
            await harness.RunScenarioAsync(first, "One");
            await harness.RunScenarioAsync(second, "Three");

            Assert.Equal(3, fixture.ActionCount);
            Assert.Equal(["First file", "Second file"], harness.FilesEntered);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ScenarioTestBase_RunUatScenarioAsync_NamesTheScenariosWhenTheNameIsUnknown()
    {
        var directory = CreateTempDirectory();
        try
        {
            var configFilePath = WriteConfig(directory);
            var file = WriteScenario(directory, """
                # Feature: Named

                ## Scenario: Present

                When I run the custom action
                """);
            var fixture = new HarnessFixture();
            var harness = new ScenarioHarness(fixture, configFilePath, CreatePassingCatalog(fixture));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.RunScenarioAsync(file, "Absent"));

            Assert.Contains("'Present'", exception.Message);
            Assert.Equal(0, fixture.ActionCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ScenarioSource_GivesOneCasePerScenario_GroupedByTestData()
    {
        var directory = CreateTempDirectory();
        try
        {
            var scenarios = Directory.CreateDirectory(Path.Combine(directory, "Features")).FullName;
            File.WriteAllText(Path.Combine(scenarios, "a.feature.md"), ScenarioFile("A", "two", "A1", "A2"));
            File.WriteAllText(Path.Combine(scenarios, "b.feature.md"), ScenarioFile("B", "one", "B1"));
            File.WriteAllText(Path.Combine(scenarios, "c.feature.md"), ScenarioFile("C", null, "C1"));
            File.WriteAllText(Path.Combine(scenarios, "d.feature.md"), """
                # Feature: D

                ## Scenario Outline: Row

                When I run the custom action <n>

                ### Examples

                | n |
                | --- |
                | 1 |
                | 2 |
                """);
            File.WriteAllText(Path.Combine(scenarios, "e.feature.md"), "not a uat file");

            var cases = UatScenarioSource.GetScenarioTheoryData(baseDirectory: directory)
                .Select(data => $"{Path.GetFileName((string)data[0])}:{data[1]}")
                .ToList();

            Assert.Equal(["c.feature.md:C1", "d.feature.md:Row [1]", "d.feature.md:Row [2]", "e.feature.md:", "b.feature.md:B1", "a.feature.md:A1", "a.feature.md:A2"], cases);
            Assert.True(
                string.CompareOrdinal(
                    UatScenarioSource.GetOrderKey(Path.Combine(scenarios, "b.feature.md"), "B1"),
                    UatScenarioSource.GetOrderKey(Path.Combine(scenarios, "a.feature.md"), "A1")) < 0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string ScenarioFile(string title, string? testData, params string[] scenarios)
    {
        var metadata = testData is null ? string.Empty : $"## Metadata\n\n| Field | Value |\n| --- | --- |\n| TestData | {testData} |\n\n";
        return $"# Feature: {title}\n\n{metadata}" + string.Concat(scenarios.Select(name => $"## Scenario: {name}\n\nWhen I run the custom action\n\n"));
    }

    private static UatCommandCatalog CreatePassingCatalog(HarnessFixture fixture)
    {
        var catalog = new UatCommandCatalog();
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I run the custom action",
            "Harness.Run",
            handler: (_, invocation, _) =>
            {
                fixture.ActionCount++;
                return Task.FromResult(UatStepResult.Passed(invocation));
            });
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "the custom action should have run",
            "Harness.AssertRun",
            handler: (_, invocation, _) =>
            {
                return Task.FromResult(
                    fixture.ActionCount > 0
                        ? UatStepResult.Passed(invocation)
                        : UatStepResult.Failed(invocation, "Action did not run."));
            });

        return catalog;
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"brinell-uat-base-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string WriteConfig(string directory, bool screenshotOnFailure = false)
    {
        var filePath = Path.Combine(directory, "uat.config.md");
        File.WriteAllText(filePath, $$"""
            # UAT Config

            ## Runtime

            | Field | Value |
            | --- | --- |
            | Target | TEST |

            ## Reporting

            | Field | Value |
            | --- | --- |
            | OutputDirectory | {{Path.Combine(directory, "uat")}} |
            | ScreenshotOnFailure | {{screenshotOnFailure}} |
            """);
        return filePath;
    }

    private static string WriteScenario(string directory, string markdown)
    {
        var filePath = Path.Combine(directory, "scenario.feature.md");
        File.WriteAllText(filePath, markdown);
        return filePath;
    }

    private sealed class HarnessFixture
    {
        public int ActionCount { get; set; }

        public FakeScreenshotService ScreenshotService { get; } = new();

        [BddWhen("I run the custom action")]
        public void RunCustomAction()
        {
        }
    }

    private sealed class ScenarioHarness(
        HarnessFixture fixture,
        string configFilePath,
        UatCommandCatalog catalog)
        : UatScenarioTestBase<HarnessFixture>(fixture)
    {
        public string? BeforeScenarioName { get; private set; }

        protected override string ConfigFilePath => configFilePath;

        protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
            new(RequireAssemblies: false);

        public Task RunFileAsync(string filePath) => RunUatFileAsync(filePath);

        public Task RunScenarioAsync(string filePath, string scenarioName) => RunUatScenarioAsync(filePath, scenarioName);

        public List<string> FilesEntered { get; } = [];

        protected override void BeforeFile(UatDocument document) => FilesEntered.Add(document.Title);

        public Task RunExpectedFailureFileAsync(string filePath, params string[] expectedDiagnostics) =>
            RunExpectedFailureUatFileAsync(filePath, expectedDiagnostics);

        protected override UatCommandCatalog CreateCommandCatalog(UatRuntime runtime) => catalog;

        protected override void BeforeScenario(UatBoundScenario scenario)
        {
            BeforeScenarioName = scenario.Source.Name;
        }
    }

    private sealed class SpecFormatHarness(string configFilePath)
        : UatSpecFormatTestBase
    {
        public bool ConfigAsserted { get; private set; }

        protected override string ConfigFilePath => configFilePath;

        protected override string? ExpectedApp => "Harness";

        protected override string? ExpectedTarget => "TEST";

        protected override Type? RuntimeRootType => typeof(HarnessFixture);

        public void AssertMetadata(string filePath) =>
            AssertUatFileParsesAndContainsRequiredMetadata(filePath);

        public void AssertBinds(string filePath) => AssertUatFileBindsThroughCatalog(filePath);

        public void AssertConfigParses() => AssertUatConfigParses();

        protected override void AssertConfig(UatConfig config)
        {
            Assert.Equal("TEST", config.Runtime["Target"]);
            ConfigAsserted = true;
        }
    }

    private sealed class FakeScreenshotService : IScreenshotService
    {
        public ScreenshotSettings Settings { get; } = ScreenshotSettings.Default;

        public string LastCapturePath { get; private set; } = string.Empty;

        public string? TestClass { get; private set; }

        public string? TestMethod { get; private set; }

        public string? Description { get; private set; }

        public string Capture(string? description = null)
        {
            Description = description;
            LastCapturePath = "evidence.png";
            return LastCapturePath;
        }

        public string Capture(string testClass, string testMethod, string description)
        {
            TestClass = testClass;
            TestMethod = testMethod;
            Description = description;
            LastCapturePath = "evidence.png";
            return LastCapturePath;
        }

        public string CaptureOnFailure(string testClass, string testMethod, Exception exception)
        {
            return Capture(testClass, testMethod, "failure");
        }
    }
}

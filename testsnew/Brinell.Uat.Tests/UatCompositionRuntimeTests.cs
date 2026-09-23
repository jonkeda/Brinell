using Brinell.Core.Composition;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Brinell.Uat.Tests.CompositionCases;

public sealed class UatCompositionRuntimeTests
{
    [Fact]
    public async Task ScenarioTestBase_UsesCompositionScopePerScenario()
    {
        var directory = CreateTempDirectory();
        try
        {
            var configFilePath = WriteConfig(directory);
            var scenarioFilePath = WriteScenario(directory, """
                # UAT: Composition Runtime

                @composition
                ## Scenario: First scoped flow

                Given I am on the Main page
                When I tap Save
                And I choose DI flow
                Then scoped action count should be 2

                @composition
                ## Scenario: Second scoped flow

                Given I am on the Main page
                When I tap Save
                And I choose DI flow
                Then scoped action count should be 2
                """);
            var fixture = new CompositionFixture();
            var harness = new CompositionScenarioHarness(fixture, configFilePath);

            await harness.RunFileAsync(scenarioFilePath);

            Assert.Equal(2, fixture.NavigationCalls);
            Assert.Equal(0, fixture.RootPhraseCalls);
            Assert.Equal(0, fixture.LegacyMainPage.SaveButton.ClickCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void UatRuntime_AcceptsTestCompositionPropertyAlias()
    {
        var directory = CreateTempDirectory();
        try
        {
            var configFilePath = WriteConfig(directory);
            var fixture = new AliasFixture();

            var runtime = new UatRuntime(
                fixture,
                configFilePath,
                new UatRuntimeValidationOptions(RequireAssemblies: false));

            Assert.Same(fixture.TestComposition, runtime.Composition);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void UatPhraseClassAttribute_RegistersPhraseClassAsScopedService()
    {
        var fixture = new CompositionFixture();

        Assert.Contains(fixture.Composition.Catalog.Services, service =>
            service.Type == typeof(CompositionPhrases));

        using var scope = fixture.Composition.CreateScope();

        Assert.Same(
            scope.ServiceProvider.GetRequiredService<CompositionPhrases>(),
            scope.ServiceProvider.GetRequiredService<CompositionPhrases>());
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"brinell-uat-composition-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string WriteConfig(string directory)
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
            """);
        return filePath;
    }

    private static string WriteScenario(string directory, string markdown)
    {
        var filePath = Path.Combine(directory, "composition.uat.md");
        File.WriteAllText(filePath, markdown);
        return filePath;
    }

    [TestModuleScan(typeof(UatCompositionRuntimeTests), NamespacePrefix = "Brinell.Uat.Tests.CompositionCases")]
    [UatIgnore]
    public sealed class CompositionFixture
    {
        public CompositionFixture()
        {
            Composition = TestComposition.ForFixture(this, services =>
                services.AddScoped<CompositionState>());
        }

        public TestComposition Composition { get; }

        public LegacyMainPage LegacyMainPage { get; } = new();

        public int NavigationCalls { get; private set; }

        public int RootPhraseCalls { get; private set; }

        public void NavigateToMain()
        {
            NavigationCalls++;
        }

        [UatPhrase(UatEffectiveStepKeyword.When, "I choose DI flow")]
        public void ChooseDiFlowOnFixture()
        {
            RootPhraseCalls++;
        }
    }

    [TestModuleScan(typeof(UatCompositionRuntimeTests), NamespacePrefix = "Brinell.Uat.Tests.CompositionCases")]
    [UatIgnore]
    public sealed class AliasFixture
    {
        public AliasFixture()
        {
            TestComposition = Brinell.Core.Composition.TestComposition.ForFixture(this);
        }

        public TestComposition TestComposition { get; }
    }

    [TestPage("Main")]
    [UatIgnore]
    public sealed class CompositionMainPage
    {
        private readonly CompositionState _state;

        public CompositionMainPage(CompositionState state)
        {
            _state = state;
            SaveButton = new CompositionSaveButton(state);
        }

        public CompositionSaveButton SaveButton { get; }

        public bool WaitReady(int? timeoutMs = null)
        {
            _state.ReadyCount++;
            return true;
        }
    }

    [UatPhraseClass]
    [UatIgnore]
    public sealed class CompositionPhrases : UatPhraseClassBase
    {
        private readonly CompositionState _state;

        public CompositionPhrases(CompositionState state)
        {
            _state = state;
        }

        [UatPhrase(UatEffectiveStepKeyword.When, "I choose DI flow")]
        public void ChooseDiFlow()
        {
            _state.ActionCount++;
        }

        [UatPhrase(UatEffectiveStepKeyword.Then, "scoped action count should be {count}")]
        public void AssertScopedActionCount(int count)
        {
            Assert.Equal(1, _state.ReadyCount);
            Assert.Equal(count, _state.ActionCount);
        }
    }

    public sealed class CompositionState
    {
        public int ReadyCount { get; set; }

        public int ActionCount { get; set; }
    }

    public sealed class CompositionSaveButton
    {
        private readonly CompositionState _state;

        public CompositionSaveButton(CompositionState state)
        {
            _state = state;
        }

        public void Click(int? timeoutMs = null)
        {
            _state.ActionCount++;
        }
    }

    public sealed class LegacyMainPage
    {
        public LegacySaveButton SaveButton { get; } = new();

        public bool WaitReady(int? timeoutMs = null) => true;
    }

    public sealed class LegacySaveButton
    {
        public int ClickCount { get; private set; }

        public void Click(int? timeoutMs = null)
        {
            ClickCount++;
        }
    }

    private sealed class CompositionScenarioHarness(
        CompositionFixture fixture,
        string configFilePath)
        : UatScenarioTestBase<CompositionFixture>(fixture)
    {
        protected override string ConfigFilePath => configFilePath;

        protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
            new(RequireAssemblies: false);

        public Task RunFileAsync(string filePath) => RunUatFileAsync(filePath);
    }
}

using System.Reflection;
using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatCatalogParityTests
{
    [Fact]
    public void SpecCatalog_ControlVerbs_ComeFromCoreStepAttributes()
    {
        var expected = typeof(UatPhraseAttribute).Assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .SelectMany(method => method.GetCustomAttributes<UatPhraseAttribute>(inherit: false))
            .Select(attribute => (attribute.Keyword, attribute.Phrase))
            .ToHashSet();

        var catalogPhrases = ControlVerbs(UatSpecCommandCatalog.CreateDefault())
            .Select(pattern => (pattern.Keyword, pattern.Phrase))
            .ToHashSet();

        Assert.NotEmpty(expected);
        Assert.Equal(expected, catalogPhrases);
    }

    [Fact]
    public void RuntimeAndSpecProjections_AgreeOnControlVerbs()
    {
        var runtime = ControlVerbs(UatReflectionRuntime.FromRoot(new Fixture()).CreateCommandCatalog())
            .Select(pattern => (pattern.Keyword, pattern.Phrase, Suffix: StripPrefix(pattern.CommandId)))
            .ToHashSet();

        var spec = ControlVerbs(UatSpecCommandCatalog.CreateDefault())
            .Select(pattern => (pattern.Keyword, pattern.Phrase, Suffix: StripPrefix(pattern.CommandId)))
            .ToHashSet();

        Assert.NotEmpty(spec);
        Assert.Equal(spec, runtime);
    }

    [Fact]
    public void CustomControlVerb_OnAppControl_IsDiscovered()
    {
        var catalog = UatReflectionRuntime.FromRoot(new GadgetFixture()).CreateCommandCatalog();

        Assert.Contains(catalog.Patterns, pattern =>
            pattern.Keyword == UatEffectiveStepKeyword.When &&
            pattern.Phrase == "I frobnicate {control}");
    }

    [Fact]
    public async Task UatWhen_OnAppControl_BindsAControlVerbWithItsValueArgument()
    {
        var fixture = new GadgetFixture();
        var runtime = UatReflectionRuntime.FromRoot(fixture);
        var catalog = runtime.CreateCommandCatalog();

        var pattern = Assert.Single(catalog.Patterns, pattern =>
            pattern.Phrase == "I set the level of {control} to {level}");
        Assert.Equal(UatEffectiveStepKeyword.When, pattern.Keyword);
        Assert.Equal("Builtin.SetLevel", pattern.CommandId);

        var result = await RunAsync(catalog, """
            Given I am on the Gadget page
            When I set the level of Gadget to "7"
            """);

        Assert.True(result.Passed, FormatResults(result));
        Assert.Equal("7", fixture.GadgetPage.GadgetButton.Level);
    }

    [Fact]
    public async Task UatThen_OnAppControl_CarriesItsLiteral()
    {
        var fixture = new GadgetFixture();
        var catalog = UatReflectionRuntime.FromRoot(fixture).CreateCommandCatalog();

        var pattern = Assert.Single(catalog.Patterns, pattern =>
            pattern.Phrase == "{control} should not be frobnicated");
        Assert.Equal(UatEffectiveStepKeyword.Then, pattern.Keyword);

        var result = await RunAsync(catalog, """
            Given I am on the Gadget page
            Then Gadget should not be frobnicated
            """);

        Assert.True(result.Passed, FormatResults(result));
        Assert.False(fixture.GadgetPage.GadgetButton.LastExpected);
    }

    [Fact]
    public void UatThen_OnControlMethod_IsNotRegisteredAsAPhrase()
    {
        var catalog = UatReflectionRuntime.FromRoot(new GadgetFixture()).CreateCommandCatalog();

        var patterns = catalog.Patterns
            .Where(pattern => pattern.Phrase == "{control} should not be frobnicated")
            .ToArray();

        var pattern = Assert.Single(patterns);
        Assert.StartsWith("Builtin.", pattern.CommandId, StringComparison.Ordinal);
    }

    private static async Task<UatScenarioRunResult> RunAsync(UatCommandCatalog catalog, string steps)
    {
        var parse = UatMarkdownParser.Parse($"""
            # UAT: Gadget

            ## Scenario: Gadget steps

            {steps}
            """);
        Assert.True(parse.Success);
        var bind = UatBinder.Bind(parse.Document!, catalog);
        Assert.True(bind.Success, string.Join(Environment.NewLine, bind.Diagnostics.Select(x => $"{x.Code} {x.Message}")));
        return await new UatScenarioRunner().RunAsync(Assert.Single(bind.Document!.Scenarios));
    }

    private static string FormatResults(UatScenarioRunResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Steps.Select(x => $"{x.Status}: {x.Invocation.Step.Text} {x.Message}"));
    }

    private static IEnumerable<UatCommandPattern> ControlVerbs(UatCommandCatalog catalog)
    {
        return catalog.Patterns.Where(pattern =>
            pattern.CommandId.Contains(".Control.", StringComparison.Ordinal));
    }

    // Command ids are "<projection>.Control.<verb>"; the verb is what must agree across projections.
    private static string StripPrefix(string commandId)
    {
        var firstDot = commandId.IndexOf('.', StringComparison.Ordinal);
        return firstDot < 0 ? commandId : commandId[(firstDot + 1)..];
    }

    private sealed class Fixture
    {
        public FrontPage FrontPage { get; } = new();
    }

    private sealed class FrontPage
    {
        public bool WaitReady(int? timeoutMs = null) => true;
    }

    private sealed class GadgetFixture
    {
        public GadgetPage GadgetPage { get; } = new();
    }

    private sealed class GadgetPage
    {
        public GadgetControl GadgetButton { get; } = new();

        public bool WaitReady(int? timeoutMs = null) => true;
    }

    private sealed class GadgetControl
    {
        public string? Level { get; private set; }

        public bool? LastExpected { get; private set; }

        [UatWhen("I frobnicate {control}", CommandId = "Gadget.Frobnicate")]
        public void Frobnicate(int? timeoutMs = null)
        {
        }

        [UatWhen("I set the level of {control} to {level}")]
        public void SetLevel(string level, int? timeoutMs = null)
        {
            Level = level;
        }

        [UatThen("{control} should not be frobnicated", HasLiteral = true, Literal = false)]
        public void AssertFrobnicated(bool expected, int? timeoutMs = null)
        {
            LastExpected = expected;
        }
    }
}

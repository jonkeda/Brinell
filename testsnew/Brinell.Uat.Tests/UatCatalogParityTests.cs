using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatCatalogParityTests
{
    [Fact]
    public void EveryCoreIVerb_HasANamedTwin_WithTheSameCommand()
    {
        // The built-in pairs cannot drift: "I tap {control}" has "{user} taps {control}" (AD-015).
        var patterns = UatSpecCommandCatalog.CreateDefault().Patterns;
        var twins = patterns
            .Where(pattern => pattern.Phrase.StartsWith("{user} ", StringComparison.Ordinal))
            .Select(pattern => (pattern.Keyword, pattern.CommandId))
            .ToHashSet();

        var missing = patterns
            .Where(pattern => pattern.Phrase.StartsWith("I ", StringComparison.Ordinal))
            .Where(pattern => !twins.Contains((pattern.Keyword, pattern.CommandId)))
            .Select(pattern => pattern.Phrase)
            .ToList();

        Assert.True(missing.Count == 0, "These built-in phrases have no {user} form: " + string.Join(", ", missing));
    }

    [Fact]
    public async Task ANamedTwin_PassesTheValue_NotTheUser()
    {
        var fixture = new GadgetFixture();
        var catalog = UatReflectionRuntime.FromRoot(fixture).CreateCommandCatalog();
        var apps = UatConfigParser.Parse("""
            # UAT Config

            ## Apps

            | Name |
            | --- |
            | Eva |
            """).Apps;
        var parse = UatMarkdownParser.Parse("""
            # UAT: Gadget

            ## Scenario: A named step with a value

            Given Eva is on the Gadget page
            When Eva sets the level of Gadget to "7"
            """);
        var bind = UatBinder.Bind(parse.Document!, new UatCatalogSet(catalog, apps, suiteHead: null, new Dictionary<string, UatCommandCatalog>(), []));
        Assert.True(bind.Success, string.Join(Environment.NewLine, bind.Diagnostics.Select(x => $"{x.Code} {x.Message}")));

        var runner = new UatScenarioRunner(new UatExecutionContext { SessionHost = new NoServices() });
        var result = await runner.RunAsync(Assert.Single(bind.Document!.Scenarios));

        Assert.True(result.Passed, FormatResults(result));
        Assert.Equal("7", fixture.GadgetPage.GadgetButton.Level);
    }

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

    [Fact]
    public async Task AVerbOnABaseClass_IsFoundThroughTheControlThePageDeclares()
    {
        var fixture = new GadgetFixture();
        var catalog = UatReflectionRuntime.FromRoot(fixture).CreateCommandCatalog();

        var result = await RunAsync(catalog, """
            Given I am on the Gadget page
            Then Dial should show the Warm state
            """);

        Assert.True(result.Passed, FormatResults(result));
        Assert.Equal("Warm", fixture.GadgetPage.Dial.LastState);
    }

    [Fact]
    public async Task AControlName_MayTakeThe()
    {
        var fixture = new GadgetFixture();
        var catalog = UatReflectionRuntime.FromRoot(fixture).CreateCommandCatalog();

        var result = await RunAsync(catalog, """
            Given I am on the Gadget page
            Then the dial should show the Cold state
            When I set the level of the gadget to "3"
            """);

        Assert.True(result.Passed, FormatResults(result));
        Assert.Equal("Cold", fixture.GadgetPage.Dial.LastState);
        Assert.Equal("3", fixture.GadgetPage.GadgetButton.Level);
    }

    [Fact]
    public async Task APropertyWhoseTypeDeclaresVerbs_IsAControl_WhateverItIsCalled()
    {
        var fixture = new GadgetFixture();
        var catalog = UatReflectionRuntime.FromRoot(fixture).CreateCommandCatalog();

        var result = await RunAsync(catalog, """
            Given I am on the Gadget page
            When I choose "Keep" in the popup
            """);

        Assert.True(result.Passed, FormatResults(result));
        Assert.Equal("Keep", fixture.GadgetPage.Popup.Chosen);
    }

    [Fact]
    public async Task AnItemVerb_PassesTheItemFirst_ThenTheValue()
    {
        var fixture = new GadgetFixture();
        var catalog = UatReflectionRuntime.FromRoot(fixture).CreateCommandCatalog();

        var pattern = Assert.Single(catalog.Patterns, pattern => pattern.Phrase == "the book {item} should be by {value}");
        Assert.Equal(["item", "value"], pattern.Parameters.Select(parameter => parameter.Name));

        var result = await RunAsync(catalog, """
            Given I am on the Gadget page
            When I open the book "Dune"
            Then the book "Dune" should be by "Herbert"
            And the book "Emma" should be lent
            And the book "Dune" should be in
            """);

        Assert.True(result.Passed, FormatResults(result));
        Assert.Equal(
            ["open Dune", "Dune by Herbert", "Emma lent True", "Dune lent False"],
            fixture.GadgetPage.Books.Calls);
    }

    [Fact]
    public async Task AVerbWithoutControl_TakesATypedValue()
    {
        var fixture = new GadgetFixture();
        var catalog = UatReflectionRuntime.FromRoot(fixture).CreateCommandCatalog();

        var result = await RunAsync(catalog, """
            Given I am on the Gadget page
            Then the shelf should hold "Dune", "Emma" and "Ulysses"
            """);

        Assert.True(result.Passed, FormatResults(result));
        Assert.Equal(["holds Dune|Emma|Ulysses"], fixture.GadgetPage.Books.Calls);
    }

    [Fact]
    public async Task AVerbWithoutControl_FailsOnAPageWhereNoControlHasIt()
    {
        var catalog = UatReflectionRuntime.FromRoot(new GadgetFixture()).CreateCommandCatalog();

        var result = await RunAsync(catalog, """
            Given I am on the Plain page
            When I open the book "Dune"
            """);

        var failed = Assert.Single(result.Steps, step => step.Status == UatStepResultStatus.Failed);
        Assert.Contains("No control on page 'Plain' has the step 'I open the book {item}'", failed.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AVerbWithoutControl_FailsWhenTwoControlsOfThePageHaveIt()
    {
        var catalog = UatReflectionRuntime.FromRoot(new GadgetFixture()).CreateCommandCatalog();

        var result = await RunAsync(catalog, """
            Given I am on the Library page
            When I open the book "Dune"
            """);

        var failed = Assert.Single(result.Steps, step => step.Status == UatStepResultStatus.Failed);
        Assert.Contains("is on several controls of page 'Library' (Fiction, Poetry)", failed.Message, StringComparison.Ordinal);
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

        public PlainPage PlainPage { get; } = new();

        public LibraryPage LibraryPage { get; } = new();
    }

    private sealed class PlainPage
    {
        public bool WaitReady(int? timeoutMs = null) => true;
    }

    private sealed class LibraryPage
    {
        public BookList FictionList { get; } = new();

        public BookList PoetryList { get; } = new();

        public bool WaitReady(int? timeoutMs = null) => true;
    }

    // A collection with item verbs: the row is named by {item}, and the collection finds it.
    private sealed class BookList
    {
        public List<string> Calls { get; } = [];

        [UatWhen("I open the book {item}")]
        public void Open(string title) => Calls.Add($"open {title}");

        [UatThen("the book {item} should be by {value}")]
        public void AssertAuthor(string title, string author) => Calls.Add($"{title} by {author}");

        [UatThen("the book {item} should be lent", HasLiteral = true, Literal = true)]
        [UatThen("the book {item} should be in", HasLiteral = true, Literal = false)]
        public void AssertLent(string title, bool lent) => Calls.Add($"{title} lent {lent}");

        [UatThen("the shelf should hold {value}")]
        public void AssertHolds(string[] titles) => Calls.Add($"holds {string.Join('|', titles)}");
    }

    private sealed class GadgetPage
    {
        public GadgetControl GadgetButton { get; } = new();

        public DialControl Dial { get; } = new();

        public Popup Popup { get; } = new();

        public BookList Books { get; } = new();

        public bool WaitReady(int? timeoutMs = null) => true;
    }

    // The gadget's page is a property of the fixture, so a session needs no services of its own.
    private sealed class NoServices : IUatSessionHost
    {
        public IServiceProvider UseSession(string name) => new ServiceCollection().BuildServiceProvider();
    }

    // The verb is declared on the base, as StateContainer's is under an app's own state container.
    private abstract class StateControlBase<TSelf>
        where TSelf : StateControlBase<TSelf>
    {
        public string? LastState { get; private set; }

        [UatThen("{control} should show the {value} state")]
        public TSelf AssertShowsState(string state, int? timeoutMs = null)
        {
            LastState = state;
            return (TSelf)this;
        }
    }

    private sealed class DialControl : StateControlBase<DialControl>
    {
    }

    // No control suffix in its name or its type's: its own verb makes it a control.
    private sealed class Popup
    {
        public string? Chosen { get; private set; }

        [UatWhen("I choose {value} in {control}")]
        public void Choose(string text) => Chosen = text;
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
        [UatWhen("{user} sets the level of {control} to {level}")]
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

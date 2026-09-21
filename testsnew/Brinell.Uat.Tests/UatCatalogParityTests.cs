using System.Reflection;
using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatCatalogParityTests
{
    [Fact]
    public void SpecCatalog_ControlVerbs_ComeFromCoreStepAttributes()
    {
        var expected = typeof(UatStepAttribute).Assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .SelectMany(method => method.GetCustomAttributes<UatStepAttribute>(inherit: false))
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
        [UatStep(UatEffectiveStepKeyword.When, "I frobnicate {control}", CommandId = "Gadget.Frobnicate")]
        public void Frobnicate(int? timeoutMs = null)
        {
        }
    }
}

using Frainet.Specs;

namespace Frainet.Specs.Tests;

/// <summary>
/// The work check on a small spec of three rules and test sources written here: a C# test names a
/// rule by a trait, a scenario by a tag.
/// </summary>
public sealed class RuleTraceTests
{
    private const string Spec = """
        # Spec: ABC.01 Do a thing

        ## Rules

        | Id | Rule |
        | --- | --- |
        | ABC.01.R1 | One. |
        | ABC.01.R2 | Two. |
        | ABC.01.R3 | Three. |
        """;

    private const string Code = """
        public sealed class ThingTests
        {
            [Fact]
            [Trait("Journey", "ABC.01.1")]
            [Trait("Rule", "ABC.01.R1")]
            public void One_Holds()
            {
            }

            [Theory]
            [Trait("Rule", "ABC.01.R1")]
            [Trait("Rule", "ABC.01.R2")]
            [InlineData(1)]
            public async Task OneAndTwo_Hold(int value)
            {
            }
        }
        """;

    private const string Scenario = """
        # Feature: ABC.01 Do a thing

        @ABC.01.1 @ABC.01.R3 @web
        ## Scenario: ABC.01.1 Three holds on the screen

        ```gherkin
        Then it holds
        ```
        """;

    [Fact]
    public void EveryRuleReached_HasNoFindings()
    {
        var result = RuleTrace.Trace(Spec, [("ThingTests.cs", Code), ("ABC.01.feature.md", Scenario)]);

        Assert.Empty(result.Findings);
        Assert.Equal(["One_Holds", "OneAndTwo_Hold"], result.Rules["ABC.01.R1"].Select(reach => reach.Name));
        Assert.Equal(["OneAndTwo_Hold"], result.Rules["ABC.01.R2"].Select(reach => reach.Name));
        Assert.Equal(["ABC.01.1 Three holds on the screen"], result.Rules["ABC.01.R3"].Select(reach => reach.Name));
    }

    [Fact]
    public void ARuleWithoutATest_IsFound()
    {
        var result = RuleTrace.Trace(Spec, [("ThingTests.cs", Code)]);

        var finding = Assert.Single(result.Findings);
        Assert.Equal("rule-untested", finding.Code);
        Assert.Contains("ABC.01.R3", finding.Message);
        Assert.Empty(result.Rules["ABC.01.R3"]);
    }

    [Fact]
    public void ATestNamingARuleTheSpecDoesNotHave_IsFound()
    {
        var code = Code.Replace("[Trait(\"Rule\", \"ABC.01.R2\")]", "[Trait(\"Rule\", \"ABC.01.R9\")]");

        var codes = RuleTrace.Trace(Spec, [("ThingTests.cs", code), ("ABC.01.feature.md", Scenario)]).Findings.Select(finding => finding.Code);

        Assert.Equal(["rule-unknown", "rule-untested"], codes);
    }

    [Fact]
    public void AnotherJourneysRules_AreLeftAlone()
    {
        var scenario = Scenario.Replace("@ABC.01.R3", "@ABC.01.R3 @ABC.02.R7");

        Assert.Empty(RuleTrace.Trace(Spec, [("ThingTests.cs", Code), ("ABC.01.feature.md", scenario)]).Findings);
    }

    [Fact]
    public void AReach_SaysItsFileAndLine()
    {
        var reach = RuleTrace.Trace(Spec, [("ABC.01.feature.md", Scenario)]).Rules["ABC.01.R3"].Single();

        Assert.Equal(("ABC.01.feature.md", 3), (reach.File, reach.Line));
    }
}

using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Bruinet.Bdd.Tests;

/// <summary>An opt-in test: skipped unless <c>BRUINET_BENCHMARK=1</c>, since its numbers are for reading, not asserting.</summary>
public sealed class BenchmarkFactAttribute : FactAttribute
{
    public BenchmarkFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("BRUINET_BENCHMARK") != "1")
        {
            Skip = "A benchmark that prints its timings. Set BRUINET_BENCHMARK=1 to run it.";
        }
    }
}

/// <summary>
/// A suite many times Tomorrow's (.my/test-selection/next-plan.md, item 7): 5,000 phrases shaped as Tomorrow's are (most
/// begin "the" or "I", then a second word; a few begin with a parameter), 10,000 steps each written for one of them, and
/// one feature file of 200 scenarios read once per scenario, as a suite's scenario theory reads it.
/// </summary>
public sealed class BddLargeSuiteBenchmarkTests(ITestOutputHelper output)
{
    private const int Phrases = 5000;
    private const int Steps = 10_000;
    private const int Scenarios = 200;

    private static readonly BddEffectiveStepKeyword[] Keywords = [BddEffectiveStepKeyword.Given, BddEffectiveStepKeyword.When, BddEffectiveStepKeyword.Then];

    // The phrase n and a step written for it: "the" and "I" lead, a second word varies, one in twenty begins with a parameter.
    internal static (string Phrase, string Step) PhraseAndStep(int n) => (n % 20) switch
    {
        0 => ($"{{name}} is shown on board {n}", $"\"Mara\" is shown on board {n}"),
        < 9 => ($"the {Word(n)} {n} should show {{value}}", $"the {Word(n)} {n} should show \"{n}\""),
        < 17 => ($"I {Verb(n)} the {Word(n)} {n}", $"I {Verb(n)} the {Word(n)} {n}"),
        _ => ($"its {Word(n)} should be {{value}} for {n}", $"its {Word(n)} should be \"x\" for {n}"),
    };

    private static string Word(int n) => (n % 37) switch { 0 => "map", 1 => "board", 2 => "pack", _ => $"part{n % 37}" };

    private static string Verb(int n) => (n % 23) switch { 0 => "open", 1 => "pick", 2 => "press", _ => $"verb{n % 23}" };

    [BenchmarkFact]
    public void TheCatalog_MatchesTenThousandStepsAgainstFiveThousandPhrases()
    {
        var clock = Stopwatch.StartNew();
        var catalog = new BddCommandCatalog();
        for (var n = 0; n < Phrases; n++)
        {
            catalog.Register(Keywords[n % Keywords.Length], PhraseAndStep(n).Phrase);
        }

        var registered = clock.Elapsed;
        var random = new Random(7);
        var steps = Enumerable.Range(0, Steps).Select(_ => random.Next(Phrases)).Select(n =>
            new BddStep(BddStepKeyword.Given, Keywords[n % Keywords.Length], PhraseAndStep(n).Step, null, new BddSourceLocation(null, 1))).ToList();

        clock.Restart();
        var bound = steps.Count(step => catalog.Match(step).Count == 1);
        var matched = clock.Elapsed;

        Assert.Equal(Steps, bound);
        output.WriteLine($"{Phrases} phrases registered in {registered.TotalMilliseconds:0} ms; {Steps} steps matched in {matched.TotalMilliseconds:0} ms ({matched.TotalMilliseconds * 1000 / Steps:0.0} µs a step)");
    }

    [BenchmarkFact]
    public void AFeatureFile_ReadOncePerScenario()
    {
        var file = Path.Combine(Path.GetTempPath(), $"bruinet-bench-{Guid.NewGuid():N}.feature.md");
        File.WriteAllText(file, "# Feature: Large\n\n" + string.Concat(Enumerable.Range(0, Scenarios).Select(n =>
            $"## Scenario: Scenario {n}\n\n```gherkin\nGiven I open the map {n}\nWhen I press the board {n}\nThen the pack {n} should show \"{n}\"\n```\n\n")));
        try
        {
            var clock = Stopwatch.StartNew();
            var scenarios = 0;
            for (var n = 0; n < Scenarios; n++)
            {
                scenarios = BddMarkdownParser.ParseFile(file).Document!.Scenarios.Count;
            }

            Assert.Equal(Scenarios, scenarios);
            output.WriteLine($"a file of {Scenarios} scenarios read {Scenarios} times in {clock.Elapsed.TotalMilliseconds:0} ms");
        }
        finally
        {
            File.Delete(file);
        }
    }
}

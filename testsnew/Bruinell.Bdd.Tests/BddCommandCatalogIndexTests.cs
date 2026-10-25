using Xunit;

namespace Bruinell.Bdd.Tests;

/// <summary>The catalog's index by leading words gives the matches trying every pattern gives, in the same order.</summary>
public sealed class BddCommandCatalogIndexTests
{
    private static readonly (BddEffectiveStepKeyword Keyword, string Phrase)[] Phrases =
    [
        (BddEffectiveStepKeyword.When, "I open the {page} page"),
        (BddEffectiveStepKeyword.When, "I open the map"),
        (BddEffectiveStepKeyword.When, "I open{thing}"),
        (BddEffectiveStepKeyword.When, "I {verb} the map"),
        (BddEffectiveStepKeyword.When, "{who} opens the map"),
        (BddEffectiveStepKeyword.When, "Done"),
        (BddEffectiveStepKeyword.Then, "Done"),
        (BddEffectiveStepKeyword.Then, "the   map should show {value}"),
        (BddEffectiveStepKeyword.Then, "the map should\tshow \"{value}\" twice"),
        (BddEffectiveStepKeyword.Given, "my survivor {name} is in {town}"),
    ];

    private static readonly (BddEffectiveStepKeyword Keyword, string Text)[] Steps =
    [
        (BddEffectiveStepKeyword.When, "I open the Admin page"),
        (BddEffectiveStepKeyword.When, "I open the map"),
        (BddEffectiveStepKeyword.When, "I openthe door"),
        (BddEffectiveStepKeyword.When, "I   open the map"),
        (BddEffectiveStepKeyword.When, "I tap the map"),
        (BddEffectiveStepKeyword.When, "Mara opens the map"),
        (BddEffectiveStepKeyword.When, " Mara opens the map"),
        (BddEffectiveStepKeyword.When, "Done"),
        (BddEffectiveStepKeyword.Then, "Done"),
        (BddEffectiveStepKeyword.Then, "the map should show \"6\""),
        (BddEffectiveStepKeyword.Then, "the map\tshould show \"6\" twice"),
        (BddEffectiveStepKeyword.Given, "my survivor \"Mara\" is in \"Hoorn\""),
        (BddEffectiveStepKeyword.Given, "I open the map"),
        (BddEffectiveStepKeyword.When, ""),
    ];

    private static BddStep Step(BddEffectiveStepKeyword keyword, string text)
        => new(BddStepKeyword.Given, keyword, text, null, new BddSourceLocation(null, 1));

    // What the catalog matched before its index: every pattern of the keyword tried, in registration order.
    private static string[] Everything(BddCommandCatalog catalog, BddStep step)
        => catalog.Patterns.Where(p => p.Keyword == step.EffectiveKeyword).Select(p => p.TryMatch(step)).OfType<BddCommandMatch>()
            .Select(m => $"{m.Pattern.Phrase}|{string.Join(",", m.Arguments.Select(a => $"{a.Key}={a.Value}"))}").ToArray();

    private static string[] Indexed(BddCommandCatalog catalog, BddStep step)
        => catalog.Candidates(step).Select(m => $"{m.Pattern.Phrase}|{string.Join(",", m.Arguments.Select(a => $"{a.Key}={a.Value}"))}").ToArray();

    [Fact]
    public void TheIndex_FindsWhatTryingEveryPatternFinds_InTheSameOrder()
    {
        var catalog = new BddCommandCatalog();
        foreach (var (keyword, phrase) in Phrases)
        {
            catalog.Register(keyword, phrase);
        }

        foreach (var (keyword, text) in Steps)
        {
            var step = Step(keyword, text);
            Assert.Equal(Everything(catalog, step), Indexed(catalog, step));
        }
    }

    [Fact]
    public void TheIndex_MatchesSomething_WhereItShould()
    {
        var catalog = new BddCommandCatalog();
        foreach (var (keyword, phrase) in Phrases)
        {
            catalog.Register(keyword, phrase);
        }

        // "I open{thing}" fits too, its parameter taking " the Admin page", as it did before the index; Select prefers neither.
        Assert.Equal(["I open the {page} page|page=Admin", "I open{thing}|thing=the Admin page"], Indexed(catalog, Step(BddEffectiveStepKeyword.When, "I open the Admin page")));
        Assert.Equal(["I open{thing}|thing=the door"], Indexed(catalog, Step(BddEffectiveStepKeyword.When, "I openthe door")));
        Assert.Equal(["{who} opens the map|who=Mara"], Indexed(catalog, Step(BddEffectiveStepKeyword.When, "Mara opens the map")));
    }

    [Theory]
    [InlineData("I open the {page} page", "I open")]
    [InlineData("I open{thing}", "I")]
    [InlineData("I {verb} the map", "I")]
    [InlineData("{who} opens the map", "")]
    [InlineData("Done", "Done")]
    [InlineData("the   map should show {value}", "the map")]
    public void APhrasesKey_IsItsFirstWholeWords(string phrase, string key)
        => Assert.Equal(key, new BddCommandPattern(BddEffectiveStepKeyword.When, phrase).IndexKey);

    // The benchmark's catalog, 5,000 phrases, against a step for each of its first 600.
    [Fact]
    public void TheIndex_AgreesOnALargeCatalog()
    {
        var catalog = new BddCommandCatalog();
        BddEffectiveStepKeyword[] keywords = [BddEffectiveStepKeyword.Given, BddEffectiveStepKeyword.When, BddEffectiveStepKeyword.Then];
        for (var n = 0; n < 5000; n++)
        {
            catalog.Register(keywords[n % 3], BddLargeSuiteBenchmarkTests.PhraseAndStep(n).Phrase);
        }

        for (var n = 0; n < 600; n++)
        {
            var step = Step(keywords[n % 3], BddLargeSuiteBenchmarkTests.PhraseAndStep(n).Step);
            Assert.Equal(Everything(catalog, step), Indexed(catalog, step));
        }
    }
}

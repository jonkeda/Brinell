namespace Bruinet.Narrator.Tests;

/// <summary>A spoken line: plain text or inline SSML, parsed once and rendered plain, as SSML and reduced.</summary>
public sealed class SpokenLineTests
{
    [Theory]
    [InlineData("Tap Save.")]
    [InlineData("Tom & Jerry <3 pizza, x < 5 and 7 > 2.")]
    [InlineData("It costs 5 & 6.")]
    [InlineData("")]
    public void APlainLine_IsUnchanged_InEveryRendering(string text)
    {
        var line = SpokenLine.Parse(text);

        Assert.False(line.HasMarkup);
        Assert.Equal(text, line.Plain);
        Assert.Equal(text, line.Reduced);
        Assert.Empty(line.Problems);
        Assert.Empty(line.ElementsUsed);
    }

    [Fact]
    public void ABreak_ShowsNothingInTheCaption_AndStaysInSsml()
    {
        var line = SpokenLine.Parse("and save.<break time=\"400ms\"/> Your todo is on the list.");

        Assert.True(line.HasMarkup);
        Assert.Equal("and save. Your todo is on the list.", line.Plain);
        Assert.Equal("and save. Your todo is on the list.", line.Reduced);
        Assert.Equal("and save.<break time=\"400ms\"/> Your todo is on the list.", line.ToSsml(SsmlSupport.Windows));
        Assert.Equal(["break"], line.ElementsUsed);
        Assert.Empty(line.Problems);
    }

    [Fact]
    public void Elements_Nest_AndKeepTheirAttributes()
    {
        var line = SpokenLine.Parse("<prosody rate=\"slow\">Your <emphasis level=\"strong\">todo</emphasis> is saved.</prosody>");

        Assert.Equal("Your todo is saved.", line.Plain);
        Assert.Equal("<prosody rate=\"slow\">Your <emphasis level=\"strong\">todo</emphasis> is saved.</prosody>", line.ToSsml(SsmlSupport.Windows));
        Assert.Equal(["prosody", "emphasis"], line.ElementsUsed);
    }

    [Fact]
    public void ASub_ShowsItsContentInTheCaption_AndItsAliasWhenSpoken()
    {
        var line = SpokenLine.Parse("Welcome to <sub alias=\"Brew-ee-net\">Bruinet</sub>.");

        Assert.Equal("Welcome to Bruinet.", line.Plain);
        Assert.Equal("Welcome to Brew-ee-net.", line.Reduced);
        Assert.Equal("Welcome to <sub alias=\"Brew-ee-net\">Bruinet</sub>.", line.ToSsml(SsmlSupport.Windows));
        Assert.Equal("Welcome to Brew-ee-net.", line.ToSsml(SsmlSupport.None));
    }

    [Fact]
    public void ASsmlVoice_DropsAnElementItLacks_KeepingTheWords()
    {
        var line = SpokenLine.Parse("There are <say-as interpret-as=\"cardinal\">3</say-as> steps<break time=\"1s\"/>.");

        Assert.Equal("There are 3 steps<break time=\"1s\"/>.", line.ToSsml(SsmlSupport.AzureSpeech).Replace("<say-as interpret-as=\"cardinal\">3</say-as>", "3", StringComparison.Ordinal));
        Assert.Equal("There are 3 steps<break time=\"1s\"/>.", line.ToSsml(SsmlSupport.Windows));
        Assert.Equal("There are 3 steps.", line.Reduced);
    }

    [Fact]
    public void Text_IsEscapedWhereSsmlIsBuilt_SoAmpersandsAndQuotesAreSafeBesideABreak()
    {
        var line = SpokenLine.Parse("Tom & Jerry <break time=\"1s\"/> say \"hi\" & 'bye' x < 5.");

        Assert.Equal("Tom &amp; Jerry <break time=\"1s\"/> say &quot;hi&quot; &amp; &apos;bye&apos; x &lt; 5.", line.ToSsml(SsmlSupport.Windows));
        Assert.Equal("Tom & Jerry say \"hi\" & 'bye' x < 5.", line.Plain);
    }

    [Fact]
    public void Entities_AreReadAsTheirCharacters()
    {
        var line = SpokenLine.Parse("Say &lt;brek/&gt; &amp; &#65;&#x42; <break time=\"1s\"/>.");

        Assert.Equal("Say <brek/> & AB .", line.Plain);
        Assert.Empty(line.Problems);
    }

    [Fact]
    public void ATagThatIsNoElement_IsAWarning_AndIsNeverSpoken()
    {
        var line = SpokenLine.Parse("Save the <brek/>todo.");

        Assert.False(line.HasMarkup);
        Assert.Equal("Save the todo.", line.Plain);
        Assert.Equal("Save the todo.", line.Reduced);
        var problem = Assert.Single(line.Problems);
        Assert.Equal(("VID021", SpokenProblemSeverity.Warning, 9), (problem.Code, problem.Severity, problem.Position));
        Assert.Contains("&lt;", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ATypoBesideARealElement_IsStillAWarning()
    {
        var line = SpokenLine.Parse("Save<brek/> the <emphasis>todo</emphasis>.");

        Assert.True(line.HasMarkup);
        Assert.Equal("Save the todo.", line.Plain);
        Assert.Equal("VID021", Assert.Single(line.Problems).Code);
    }

    [Theory]
    [InlineData("Your <emphasis>todo is saved.", "VID020", "never closed")]
    [InlineData("Your todo</emphasis> is saved.", "VID020", "closes nothing")]
    [InlineData("<prosody rate=\"slow\">Your <emphasis>todo</prosody></emphasis>", "VID020", "closes nothing")]
    public void MarkupThatDoesNotNest_IsAnError_AtItsPosition(string text, string code, string says)
    {
        var line = SpokenLine.Parse(text);

        var problem = Assert.Single(line.Problems, candidate => candidate.Message.Contains(says, StringComparison.Ordinal));
        Assert.Equal((code, SpokenProblemSeverity.Error), (problem.Code, problem.Severity));
        Assert.InRange(problem.Position, 0, text.Length);
    }

    [Theory]
    [InlineData("<say-as interpret-as=\"cardinal\">3 <emphasis>x</emphasis></say-as>")]
    [InlineData("<sub alias=\"a\"><emphasis>b</emphasis></sub>")]
    [InlineData("<phoneme ph=\"b\"><break time=\"1s\"/></phoneme>")]
    public void AnElementInsideSayAsSubOrPhoneme_IsAnError(string text)
        => Assert.Contains(SpokenLine.Parse(text).Problems, problem => problem is { Code: "VID024", Severity: SpokenProblemSeverity.Error });

    [Fact]
    public void ABreakWithNoSlash_IsReadAsEmpty()
    {
        var line = SpokenLine.Parse("Wait<break time=\"1s\"> then go.");

        Assert.Empty(line.Problems);
        Assert.Equal("Wait<break time=\"1s\"/> then go.", line.ToSsml(SsmlSupport.Windows));
    }

    [Fact]
    public void TheMsttsElements_AreElements()
    {
        var line = SpokenLine.Parse("<mstts:express-as style=\"excited\">Hurray</mstts:express-as><mstts:silence type=\"Sentenceboundary\" value=\"200ms\"/>");

        Assert.Equal("Hurray", line.Plain);
        Assert.Equal(["mstts:express-as", "mstts:silence"], line.ElementsUsed);
        Assert.Equal("Hurray", line.ToSsml(SsmlSupport.Windows));
        Assert.Equal("<mstts:express-as style=\"excited\">Hurray</mstts:express-as><mstts:silence type=\"Sentenceboundary\" value=\"200ms\"/>", line.ToSsml(SsmlSupport.AzureSpeech));
    }

    [Fact]
    public void WhichVoiceFollowsWhich_IsByThePrefix()
    {
        Assert.Same(SsmlSupport.AzureSpeech, SsmlSupport.Of("azure-speech-service:en-GB-Emily:MAI-Voice-2.1"));
        Assert.Same(SsmlSupport.None, SsmlSupport.Of("azure:coral"));
        Assert.Same(SsmlSupport.Windows, SsmlSupport.Of("Microsoft Zira"));
        Assert.Null(SsmlSupport.Of(null));
        Assert.DoesNotContain("say-as", SsmlSupport.Windows);
        Assert.Contains("say-as", SsmlSupport.AzureSpeech);
    }
}

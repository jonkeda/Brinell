using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatScenarioCheckTests
{
    [Fact]
    public void Snippet_EncodesTheStepInTheMethodName()
    {
        var snippet = UatScenarioCheck.Snippet(Step("When I archive \"Dune\" on 12-May-2026 for 1,250.50 hours"));

        Assert.Contains("[BddWhen]", snippet, StringComparison.Ordinal);
        Assert.Contains("public void When_I_archive_P1_on_P2_for_P3_hours(string p1, DateOnly p2, decimal p3)", snippet, StringComparison.Ordinal);
    }

    [Fact]
    public void Snippet_WithPunctuation_DeclaresThePhraseInText()
    {
        var snippet = UatScenarioCheck.Snippet(Step("Then the todo \"Dune\" should have a read-only status"));

        Assert.Contains("[BddThen(\"the todo {p1} should have a read-only status\")]", snippet, StringComparison.Ordinal);
        Assert.Contains("(string p1)", snippet, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyFixes_RewritesTheValueOnItsLine_KeepingLineEndings()
    {
        var text = "line one\r\nWhen I lend it until 2026-05-12\r\n";
        var fix = new UatDiagnostic(UatDiagnosticSeverity.Error, "UATA002", "date", new UatSourceLocation(null, 2))
        {
            Fix = new UatTextFix("2026-05-12", "12-May-2026"),
        };

        var (fixedText, count) = UatScenarioCheck.ApplyFixes(text, [fix]);

        Assert.Equal(1, count);
        Assert.Equal("line one\r\nWhen I lend it until 12-May-2026\r\n", fixedText);
    }

    [Fact]
    public void AnUnclosedDocString_IsAParseError()
    {
        var parse = UatMarkdownParser.Parse(""""
            # Feature: Doc strings

            ## Scenario: Unclosed

            When I write the note
              """
              Dear Eva,
            """");

        var error = Assert.Single(parse.Diagnostics);
        Assert.Equal("UAT023", error.Code);
        Assert.Equal(6, error.Location.LineNumber);
    }

    private static UatStep Step(string text)
    {
        var parse = UatMarkdownParser.Parse($"""
            # Feature: Snippets

            ## Scenario: One step

            {text}
            """);
        return Assert.Single(Assert.Single(parse.Document!.Scenarios).Steps);
    }
}

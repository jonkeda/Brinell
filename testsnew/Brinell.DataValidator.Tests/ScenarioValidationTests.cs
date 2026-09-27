using System.Text.Json;
using Brinell.Core.Composition;
using Brinell.Core.Testing;
using Brinell.Uat;
using Xunit;

namespace Brinell.DataValidator.Tests;

/// <summary>The scenario side of <c>brinell-data</c>: the gate's checks, from a fixture type (AD-016).</summary>
public sealed class ScenarioValidationTests : IDisposable
{
    private static readonly string Assembly = typeof(ScenarioValidationTests).Assembly.Location;

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Brinell", "ScenarioValidationTests", Guid.NewGuid().ToString("N"));

    public ScenarioValidationTests()
    {
        Directory.CreateDirectory(_folder);
        Write("uat.config.md", $"""
            # UAT Config

            ## Runtime

            | Field | Value |
            | --- | --- |
            | Fixture | {nameof(LibraryFixture)} |

            ## Apps

            | Name | User |
            | --- | --- |
            | Eva's PC | Eva |

            ## Assemblies

            | Kind | Assembly |
            | --- | --- |
            | Pages | {Assembly} |
            """);
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void Validate_CleanScenarios_ExitZero()
    {
        Scenario("""
            When I lend "Dune" to Eva until 12-May-2026
            And I shelve the books
              | Title | Pages |
              | ----- | ----- |
              | Dune  | 1,250 |
            """);

        var (exit, output, _) = Run("validate");

        Assert.Equal(0, exit);
        Assert.Contains("No problems in 1 scenario file(s).", output);
    }

    [Fact]
    public void Validate_ReportsEveryArgumentProblem_AtItsLine()
    {
        Scenario("""
            When I lend "Dune" to Eva until 2026-05-12
            And I lend "Dune" to Carol until 12-May-2026
            And I shelve the books
              | Title | Pages |
              | ----- | ----- |
              | Dune  | 1,5   |
            And I shelve the books
            And I forget the day
            """);

        var (exit, output, _) = Run("validate");

        Assert.Equal(1, exit);
        Assert.Contains("Scenarios/library.uat.md:5: UATA002", output);
        Assert.Contains("Write 12-May-2026.", output);
        Assert.Contains("Scenarios/library.uat.md:6: UATA005", output);
        Assert.Contains("Scenarios/library.uat.md:10: UATA003", output);
        Assert.Contains("Scenarios/library.uat.md:11: UATA007", output);
        Assert.Contains("Scenarios/library.uat.md:12: UATA010", output);
        Assert.Contains("5 error(s).", output);
    }

    [Fact]
    public void Validate_OneFile_AsJson()
    {
        var file = Scenario("""
            When I lend "Dune" to Carol until 12-May-2026
            """);

        var (exit, output, _) = Run("validate", file, "--format", "json");

        Assert.Equal(1, exit);
        var problem = Assert.Single(JsonDocument.Parse(output).RootElement.EnumerateArray());
        Assert.Equal("UATA005", problem.GetProperty("code").GetString());
    }

    [Fact]
    public void ValidateFix_RewritesWhatHasOneRightAnswer()
    {
        var file = Scenario("""
            When I lend "Dune" to Eva until 2026-05-12
            And I lend "Dune" to Eva until 3-jan-2027
            """);

        var (exit, output, _) = Run("validate", "--fix");

        Assert.Equal(0, exit);
        Assert.Contains("Fixed 1 value(s) in Scenarios/library.uat.md.", output);
        var text = File.ReadAllText(file);
        Assert.Contains("until 12-May-2026", text);
        Assert.Contains("until 3-jan-2027", text);
    }

    [Fact]
    public void Phrases_ListsTypesAndBrokenPhrases()
    {
        var (exit, output, _) = Run("phrases", "--keyword", "When");

        Assert.Equal(0, exit);
        Assert.Contains("When I lend {title: text} to {reader: user} until {due: date}", output);
        Assert.Contains("When I shelve the books  + table 'books': list of ShelvedBook", output);
        Assert.Contains("When I forget the day", output);
        Assert.Contains("warning UATA010", output);
        Assert.DoesNotContain("Then ", output);
    }

    [Fact]
    public void Validate_ChecksCallsToCallablePhrases_AndWarnsAboutBrokenOnes()
    {
        PhraseFile();
        var file = Scenario("""
            Given a loan of "Dune" to Eva on 2026-05-12
            """);

        var (exit, output, errors) = Run("validate");

        Assert.Equal(1, exit);
        Assert.Contains("Scenarios/library.uat.md:5: UATA002", output);
        Assert.Contains("Scenarios/Library.phrases.md:7: UATC001", errors);
        Assert.Contains("does not copy to its output", errors);

        (exit, output, _) = Run("validate", "--fix");

        Assert.Equal(0, exit);
        Assert.Contains("Fixed 1 value(s) in Scenarios/library.uat.md.", output);
        Assert.Contains("Given a loan of \"Dune\" to Eva on 12-May-2026", File.ReadAllText(file));
    }

    [Fact]
    public void Phrases_ListsCallablePhrases_WithTheirTypes()
    {
        PhraseFile();

        var (exit, output, _) = Run("phrases");

        Assert.Equal(0, exit);
        Assert.Contains("Callable a loan of {title: text} to {reader: user} on {due: date}  <- ", output);
        Assert.Contains("Library.phrases.md:3", output);
        Assert.Contains("warning UATC001", output);
    }

    [Fact]
    public void Snippet_WritesAMethodForAStepThatBindsToNothing()
    {
        Scenario("""
            When I archive "Dune" on 12-May-2026
            """);

        var (exit, output, _) = Run("snippet", "Scenarios/library.uat.md:5");

        Assert.Equal(0, exit);
        Assert.Contains("[UatWhen]", output);
        Assert.Contains("public void When_I_archive_P1_on_P2(string p1, DateOnly p2)", output);
    }

    private string Scenario(string steps) => Write(Path.Combine("Scenarios", "library.uat.md"), $"""
        # UAT: Library

        ## Scenario: Lending

        {steps}
        """);

    private void PhraseFile() => Write(Path.Combine("Scenarios", "Library.phrases.md"), """
        # Phrases: Library

        ## Phrase: a loan of {title} to {reader} on {due}

        When I lend <title> to <reader> until <due>

        ## Phrase: a broken one {spare}

        When I shelve the books
        """);

    private (int Exit, string Output, string Errors) Run(params string[] args)
    {
        var output = new StringWriter();
        var errors = new StringWriter();
        var exit = DataValidatorApp.Run(args, output, errors, _folder);
        return (exit, output.ToString(), errors.ToString());
    }

    private string Write(string relativePath, string content)
    {
        var path = Path.Combine(_folder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }
}

/// <summary>Loaded by the tool from this test assembly, by type only: it is never created.</summary>
[TestModuleScan(typeof(LibraryFixture), NamespacePrefix = "Brinell.DataValidator.Tests.Library")]
public sealed class LibraryFixture
{
    public TestComposition Composition => throw new NotSupportedException("The tool reads the fixture's type only.");
}

public sealed record ShelvedBook(string Title, int Pages);

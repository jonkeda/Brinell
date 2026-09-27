using Xunit;

namespace Brinell.Uat.Preview.Tests;

/// <summary>The parse-only preview: every Todo feature and step file, and what a broken file shows.</summary>
public sealed class UatPreviewTests
{
    private static readonly string Todo = FindTodo();

    /// <summary>Every feature and step file of the Todo suites, relative to <c>samples/Todo/tests</c>.</summary>
    public static IEnumerable<object[]> TodoFiles => Directory
        .EnumerateFiles(Path.Combine(Todo, "tests"), "*.md", SearchOption.AllDirectories)
        .Where(file => file.EndsWith(UatFileFormat.FeatureExtension, StringComparison.OrdinalIgnoreCase) ||
                       file.EndsWith(UatFileFormat.StepsExtension, StringComparison.OrdinalIgnoreCase))
        .Select(file => Path.GetRelativePath(Path.Combine(Todo, "tests"), file))
        .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
        .Order(StringComparer.Ordinal)
        .Select(file => new object[] { file.Replace('\\', '/') });

    [Theory]
    [MemberData(nameof(TodoFiles))]
    public void TodoFile_RendersAsItsSnapshot(string file)
    {
        var path = Path.Combine(Todo, "tests", file);

        var result = UatPreview.Render(File.ReadAllText(path), path);

        Snapshot.Match(result.Html, file.Replace('/', '.'));
    }

    [Fact]
    public void AParseError_ShowsAtItsLine_AndTheRestStillRenders()
    {
        var result = UatPreview.Render("""
            # Feature: Broken

            ## Scenario: One

            ```gherkin
            Given I am on the Todo List page
            Wonder whether this is a step
            Then I should be on the Todo List page
            ```
            """, "broken.feature.md");

        Assert.Contains("<li class=\"uat-step uat-not-a-step\" data-line=\"7\">", result.Html);
        Assert.Contains("data-code=\"UAT011\" data-line=\"7\"", result.Html);
        Assert.Contains("<li class=\"uat-step\" data-line=\"8\"><span class=\"uat-kw\">Then</span>", result.Html);
        Assert.True(
            result.Html.IndexOf("data-code=\"UAT011\"", StringComparison.Ordinal) < result.Html.IndexOf("data-line=\"8\"", StringComparison.Ordinal),
            "The diagnostic shows under its own step, before the next one.");
    }

    [Fact]
    public void AFileWithNoFeatureHeading_ShowsWhy_NotABlank()
    {
        var result = UatPreview.Render("Just some notes.\n", "notes.feature.md");

        Assert.Contains("Just some notes.", result.Html);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains("class=\"uat-diag\"", result.Html);
    }

    [Fact]
    public void AFileInTheOldForm_ShowsUat029_WithItsFix()
    {
        var result = UatPreview.Render("""
            # UAT: Old

            ## Scenario: One

            ```gherkin
            Given I am on the Todo List page
            ```
            """, "old.uat.md");

        Assert.Contains("'old.uat.md' is the old name of 'old.feature.md'", System.Net.WebUtility.HtmlDecode(result.Html));
        Assert.Contains("'# UAT: Old' is the old form of '# Feature: Old'", System.Net.WebUtility.HtmlDecode(result.Html));
        Assert.Contains("<span class=\"uat-kw\">Feature</span> Old", result.Html);
    }

    [Fact]
    public void RawHtml_InTheMarkdown_IsShownAsText()
    {
        var result = UatPreview.Render("""
            # Feature: Html

            <script>alert(1)</script>
            """, "html.feature.md");

        Assert.DoesNotContain("<script>", result.Html);
    }

    [Fact]
    public void ParseOnly_SaysSo_AndWhy()
    {
        var result = UatPreview.Render("# Feature: Plain\n", "plain.feature.md", new UatPreviewOptions { ParseOnlyReason = "Nothing is built yet." });

        Assert.Contains("Parsed only, not bound: Nothing is built yet.", result.Html);
    }

    [Fact]
    public void TheStylesheet_HasLightDarkAndHighContrast()
    {
        Assert.Contains("--uat-bg", UatPreview.Stylesheet);
        Assert.Contains("prefers-color-scheme: dark", UatPreview.Stylesheet);
        Assert.Contains("data-theme=\"high-contrast\"", UatPreview.Stylesheet);
        Assert.Contains("forced-colors", UatPreview.Stylesheet);
    }

    [Fact]
    public void RunButtons_OnTheTitleAndEachScenario_WhenTheSuiteCanRun()
    {
        var result = UatPreview.Render(Scenario, "shelf.feature.md", new UatPreviewOptions { Run = new UatPreviewRunState(true) });

        Assert.Contains("<button type=\"button\" class=\"uat-run\" data-run-file title=\"Run every scenario of the file\">", result.Html);
        Assert.Contains("<button type=\"button\" class=\"uat-run\" data-run=\"3\" title=\"Run this scenario\">", result.Html);
        Assert.Contains("<button type=\"button\" class=\"uat-run\" data-debug data-run=\"3\" title=\"Debug this scenario, stopping at step breakpoints\">", result.Html);
        Assert.Contains("<button type=\"button\" class=\"uat-run\" data-debug data-run-file title=\"Debug every scenario of the file, stopping at step breakpoints\">", result.Html);
    }

    [Fact]
    public void RunButtons_AreDisabled_WithTheReason_WhenTheSuiteCannotRun()
    {
        var result = UatPreview.Render(Scenario, "shelf.feature.md", new UatPreviewOptions { Run = new UatPreviewRunState(false, "Use [UatScenarioTheory].") });

        Assert.Contains("data-run=\"3\" disabled title=\"Use [UatScenarioTheory].\"", result.Html);
    }

    [Fact]
    public void NoRunButtons_OnAPageOfItsOwn()
        => Assert.DoesNotContain("uat-run", UatPreview.Render(Scenario, "shelf.feature.md").Html);

    private const string Scenario = """
        # Feature: Shelf

        ## Scenario: Shelve

        ```gherkin
        When I shelve "Dune"
        ```
        """;

    [Fact]
    public void TheHostPage_HasTheRootTheBarAndTheScript_AndNoRemoteContent()
    {
        var page = UatPreview.HostPage;

        Assert.Contains("<main id=\"uat-root\"></main>", page);
        Assert.Contains("id=\"uat-suite\"", page);
        Assert.Contains("case \"update\": update(message); break;", page);
        Assert.Contains("default-src 'none'", page);
        Assert.DoesNotContain("<script src=", page);
    }

    private static string FindTodo()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var todo = Path.Combine(folder.FullName, "samples", "Todo");
            if (Directory.Exists(todo))
            {
                return todo;
            }
        }

        throw new DirectoryNotFoundException("samples/Todo is not above the test's folder.");
    }
}

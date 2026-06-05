using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatMarkdownParserTests
{
    [Fact]
    public void Parse_SimpleScenarioWithMetadataAndTable_ReturnsDocument()
    {
        var markdown = """
            # UAT: Login

            ## Metadata

            | Field | Value |
            | --- | --- |
            | App | Example.Maui |
            | Area | Authentication |
            | Target | MAUI |
            | Tags | smoke, login |

            @smoke @login
            ## Scenario: Valid user can sign in

            Given I am on the Login page
            When I sign in with credentials
            | Field | Value |
            | --- | --- |
            | User name | ada@example.com |
            | Password | correct-password |
            Then I should see "Welcome Ada"
            """;

        var result = UatMarkdownParser.Parse(markdown, "login.uat.md");

        Assert.True(result.Success, FormatDiagnostics(result));
        Assert.NotNull(result.Document);
        var document = result.Document;
        Assert.Equal("Login", document.Title);
        Assert.Equal("Example.Maui", document.Metadata["App"]);
        var scenario = Assert.Single(document.Scenarios);
        Assert.Equal("Valid user can sign in", scenario.Name);
        Assert.Equal(["smoke", "login"], scenario.Tags);
        Assert.Equal(3, scenario.Steps.Count);
        Assert.Equal(UatStepKeyword.When, scenario.Steps[1].Keyword);
        Assert.Equal(UatEffectiveStepKeyword.When, scenario.Steps[1].EffectiveKeyword);
        Assert.NotNull(scenario.Steps[1].Table);
        Assert.Equal("ada@example.com", scenario.Steps[1].Table!.Rows[0].Cells["Value"]);
    }

    [Fact]
    public void Parse_ScenarioOutline_ExpandsExamplesIntoScenarios()
    {
        var markdown = """
            # UAT: Login

            ## Scenario Outline: Login result is shown

            Given I am on the Login page
            When I enter credentials
            | Field | Value |
            | --- | --- |
            | User name | <user> |
            | Password | <password> |
            Then I should see "<result>"

            ### Examples

            | user | password | result |
            | --- | --- | --- |
            | ada@example.com | correct-password | Dashboard |
            | locked@example.com | correct-password | Account locked |
            """;

        var result = UatMarkdownParser.Parse(markdown);

        Assert.True(result.Success, FormatDiagnostics(result));
        Assert.NotNull(result.Document);
        var document = result.Document;
        Assert.Equal(2, document.Scenarios.Count);
        Assert.Equal("Login result is shown [1]", document.Scenarios[0].Name);
        Assert.Equal("Dashboard", document.Scenarios[0].Steps[2].Text.Split('"')[1]);
        Assert.Equal("locked@example.com", document.Scenarios[1].Steps[1].Table!.Rows[0].Cells["Value"]);
        Assert.Equal("Login result is shown", document.Scenarios[1].OutlineName);
        Assert.Equal(2, document.Scenarios[1].ExampleIndex);
    }

    [Fact]
    public void Parse_Background_PrependsSharedSetupModel()
    {
        var markdown = """
            # UAT: Settings

            ## Background

            Given the application is running

            ## Scenario: Save display name

            Given I am on the Settings page
            When I enter "Ada Lovelace" into Display Name
            Then I should see "Settings saved"
            """;

        var result = UatMarkdownParser.Parse(markdown);

        Assert.True(result.Success, FormatDiagnostics(result));
        Assert.NotNull(result.Document);
        var document = result.Document;
        var background = Assert.Single(document.Background);
        Assert.Equal("the application is running", background.Text);
        Assert.Single(document.Scenarios);
    }

    [Fact]
    public void Parse_DefaultNamingExampleText_ParsesInferredAuthoringNames()
    {
        var markdown = """
            # UAT: Settings

            ## Metadata

            | Field | Value |
            | --- | --- |
            | App | Example.Maui |
            | Area | Settings |
            | Target | MAUI |
            | Tags | smoke, settings |

            @smoke @settings
            ## Scenario: Save display name

            Given I am on the Settings page
            When I enter "Ada Lovelace" into Display Name
            And I check Email Notifications
            And I tap Save
            Then I should see "Settings saved"
            """;

        var result = UatMarkdownParser.Parse(markdown);

        Assert.True(result.Success, FormatDiagnostics(result));
        Assert.NotNull(result.Document);
        var scenario = Assert.Single(result.Document.Scenarios);
        Assert.Equal(UatEffectiveStepKeyword.When, scenario.Steps[2].EffectiveKeyword);
        Assert.Equal("I check Email Notifications", scenario.Steps[2].Text);
        Assert.Equal("I tap Save", scenario.Steps[3].Text);
    }

    [Fact]
    public void Parse_StandardTemplateMetadataAndTags_ReturnsDocument()
    {
        var markdown = """
            # UAT: Camera Actions

            ## Metadata

            | Field | Value |
            | --- | --- |
            | App | Example.Maui |
            | Area | Camera Actions |
            | Target | MAUI |
            | Tags | smoke, maui, camera |
            | Mode | Automated |
            | Requires | Deterministic |
            | Owner | QA |
            | Priority | Smoke |
            | Evidence | screenshot, transcript |

            @smoke @maui @camera @deterministic @uat-003-6
            ## Scenario: UAT-003.6 Sub-button hides action rows during capture

            Given I am on the Main page
            When I tap Look
            Then Overview should be visible
            """;

        var result = UatMarkdownParser.Parse(markdown);

        Assert.True(result.Success, FormatDiagnostics(result));
        Assert.NotNull(result.Document);
        Assert.Equal("Automated", result.Document.Metadata[UatMetadataFields.Mode]);
        Assert.Equal("Deterministic", result.Document.Metadata[UatMetadataFields.Requires]);
        Assert.Equal("screenshot, transcript", result.Document.Metadata[UatMetadataFields.Evidence]);
        var scenario = Assert.Single(result.Document.Scenarios);
        Assert.Equal(
            ["smoke", "maui", "camera", "deterministic", "uat-003-6"],
            scenario.Tags);
    }

    [Fact]
    public void Parse_MissingUatHeading_ReturnsDiagnostic()
    {
        var markdown = """
            ## Scenario: Missing heading

            Given I am on the Login page
            """;

        var result = UatMarkdownParser.Parse(markdown);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, x => x.Code == "UAT000");
    }

    [Fact]
    public void Parse_MalformedStepTable_ReturnsDiagnostic()
    {
        var markdown = """
            # UAT: Broken

            ## Scenario: Bad table

            Given I am on the Login page
            When I enter credentials
            | Field | Value |
            | --- | --- |
            | User name |
            Then I should see "Welcome"
            """;

        var result = UatMarkdownParser.Parse(markdown);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, x => x.Code == "UAT018");
    }

    [Fact]
    public void Parse_OutlineWithMissingExampleColumn_ReturnsDiagnostic()
    {
        var markdown = """
            # UAT: Broken Outline

            ## Scenario Outline: Missing column

            Given I am on the <page> page

            ### Examples

            | user |
            | --- |
            | ada |
            """;

        var result = UatMarkdownParser.Parse(markdown);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, x => x.Code == "UAT015");
    }

    private static string FormatDiagnostics(UatParseResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Diagnostics.Select(x => $"{x.Location}: {x.Code} {x.Message}"));
    }
}

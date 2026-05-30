using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatBinderTests
{
    [Fact]
    public void Bind_KnownSteps_ReturnsBoundInvocations()
    {
        var document = ParseValid("""
            # UAT: Login

            ## Background

            Given the application is running

            ## Scenario: Valid user can sign in

            Given I am on the Login page
            When I enter "ada@example.com" into User name
            And I tap Sign in
            Then I should see the Dashboard page
            """);
        var catalog = CreateBasicCatalog();

        var result = UatBinder.Bind(document, catalog);

        Assert.True(result.Success, FormatDiagnostics(result));
        Assert.NotNull(result.Document);
        var scenario = Assert.Single(result.Document.Scenarios);
        Assert.Equal(5, scenario.Invocations.Count);
        Assert.True(scenario.Invocations[0].FromBackground);
        Assert.Equal("AppCommands.AssertRunning", scenario.Invocations[0].CommandId);
        Assert.Equal("Login", scenario.Invocations[1].Arguments["page"]);
        Assert.Equal("ada@example.com", scenario.Invocations[2].Arguments["value"]);
        Assert.Equal("User name", scenario.Invocations[2].Arguments["control"]);
        Assert.Equal("Sign in", scenario.Invocations[3].Arguments["control"]);
        Assert.Equal("Dashboard", scenario.Invocations[4].Arguments["page"]);
    }

    [Fact]
    public void Bind_ExactPhrase_BeatsParameterizedPhrase()
    {
        var document = ParseValid("""
            # UAT: Exact

            ## Scenario: Exact phrase

            When I tap Save
            """);
        var catalog = new UatCommandCatalog();
        catalog.Register(UatEffectiveStepKeyword.When, "I tap {control}", "InteractionCommands.Tap");
        catalog.Register(UatEffectiveStepKeyword.When, "I tap Save", "SettingsCommands.Save");

        var result = UatBinder.Bind(document, catalog);

        Assert.True(result.Success, FormatDiagnostics(result));
        var invocation = Assert.Single(Assert.Single(result.Document!.Scenarios).Invocations);
        Assert.Equal("SettingsCommands.Save", invocation.CommandId);
        Assert.Empty(invocation.Arguments);
    }

    [Fact]
    public void Bind_TableRequiredCommand_BindsStepTable()
    {
        var document = ParseValid("""
            # UAT: Login

            ## Scenario: Sign in

            When I sign in with credentials
            | Field | Value |
            | --- | --- |
            | User name | ada@example.com |
            | Password | correct-password |
            """);
        var catalog = new UatCommandCatalog();
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I sign in with credentials",
            "LoginPage.SignInWithCredentials",
            requiresTable: true);

        var result = UatBinder.Bind(document, catalog);

        Assert.True(result.Success, FormatDiagnostics(result));
        var invocation = Assert.Single(Assert.Single(result.Document!.Scenarios).Invocations);
        Assert.NotNull(invocation.Table);
        Assert.Equal("ada@example.com", invocation.Table!.Rows[0].Cells["Value"]);
    }

    [Fact]
    public void Bind_TableRequiredCommandWithoutTable_ReturnsDiagnostic()
    {
        var document = ParseValid("""
            # UAT: Login

            ## Scenario: Sign in

            When I sign in with credentials
            """);
        var catalog = new UatCommandCatalog();
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I sign in with credentials",
            requiresTable: true);

        var result = UatBinder.Bind(document, catalog);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, x => x.Code == "UATB003");
    }

    [Fact]
    public void Bind_UnknownStep_ReturnsDiagnostic()
    {
        var document = ParseValid("""
            # UAT: Unknown

            ## Scenario: Unknown step

            When I do something unsupported
            """);
        var catalog = CreateBasicCatalog();

        var result = UatBinder.Bind(document, catalog);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, x => x.Code == "UATB001");
    }

    [Fact]
    public void Bind_AmbiguousExactStep_ReturnsDiagnostic()
    {
        var document = ParseValid("""
            # UAT: Ambiguous

            ## Scenario: Ambiguous step

            When I tap Save
            """);
        var catalog = new UatCommandCatalog();
        catalog.Register(UatEffectiveStepKeyword.When, "I tap Save", "First");
        catalog.Register(UatEffectiveStepKeyword.When, "I tap Save", "Second");

        var result = UatBinder.Bind(document, catalog);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, x => x.Code == "UATB002");
    }

    [Fact]
    public void Bind_CommandThatDoesNotAllowTable_ReturnsDiagnostic()
    {
        var document = ParseValid("""
            # UAT: Extra table

            ## Scenario: Extra table

            When I tap Save
            | Field | Value |
            | --- | --- |
            | Ignored | Data |
            """);
        var catalog = new UatCommandCatalog();
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I tap {control}",
            "InteractionCommands.Tap",
            allowsTable: false);

        var result = UatBinder.Bind(document, catalog);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, x => x.Code == "UATB004");
    }

    private static UatCommandCatalog CreateBasicCatalog()
    {
        var catalog = new UatCommandCatalog();
        catalog.Register(UatEffectiveStepKeyword.Given, "the application is running", "AppCommands.AssertRunning");
        catalog.Register(UatEffectiveStepKeyword.Given, "I am on the {page} page", "PageCommands.AssertPageOpen");
        catalog.Register(UatEffectiveStepKeyword.When, "I tap {control}", "InteractionCommands.Tap", allowsTable: false);
        catalog.Register(UatEffectiveStepKeyword.When, "I enter {value} into {control}", "InputCommands.EnterText", allowsTable: false);
        catalog.Register(UatEffectiveStepKeyword.Then, "I should see the {page} page", "PageCommands.AssertPageOpen");
        catalog.Register(UatEffectiveStepKeyword.Then, "I should see {text}", "AssertCommands.AssertTextVisible");
        return catalog;
    }

    private static UatDocument ParseValid(string markdown)
    {
        var result = UatMarkdownParser.Parse(markdown);
        Assert.True(
            result.Success,
            string.Join(Environment.NewLine, result.Diagnostics.Select(x => $"{x.Code} {x.Message}")));
        Assert.NotNull(result.Document);
        return result.Document;
    }

    private static string FormatDiagnostics(UatBindResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Diagnostics.Select(x => $"{x.Location}: {x.Code} {x.Message}"));
    }
}

using Xunit;

namespace Brinell.Uat.Tests;

public sealed class UatExecutionTests
{
    [Fact]
    public async Task RunAsync_BoundScenario_ExecutesHandlersAndRecordsResults()
    {
        var scenario = CreateBoundScenario("""
            # UAT: Settings

            ## Background

            Given the application is running

            ## Scenario: Save display name

            Given I am on the Settings page
            When I tap Save
            Then I should see the Settings page
            """);
        var runner = new UatScenarioRunner();

        var result = await runner.RunAsync(scenario);

        Assert.True(result.Passed, FormatResults(result));
        Assert.Equal(4, result.Steps.Count);
        Assert.Equal("Settings", runner.Context.CurrentPageName);
        Assert.Contains("tap:Save", runner.Context.Diagnostics);
    }

    [Fact]
    public async Task RunNextAsync_ExecutesOneStepAtATime()
    {
        var scenario = CreateBoundScenario("""
            # UAT: Settings

            ## Scenario: Save

            Given I am on the Settings page
            When I tap Save
            """);
        var session = new UatScenarioRunner().CreateSession(scenario);

        var first = await session.RunNextAsync();

        Assert.Equal(UatStepResultStatus.Passed, first.Status);
        Assert.True(session.HasNext);
        Assert.Single(session.Results);

        var second = await session.RunNextAsync();

        Assert.Equal(UatStepResultStatus.Passed, second.Status);
        Assert.False(session.HasNext);
        Assert.Equal(2, session.Results.Count);
    }

    [Fact]
    public async Task RunAsync_MissingHandler_ReturnsFailedResult()
    {
        var document = ParseValid("""
            # UAT: Missing handler

            ## Scenario: Save

            When I tap Save
            """);
        var catalog = new UatCommandCatalog();
        catalog.Register(UatEffectiveStepKeyword.When, "I tap {control}", "InteractionCommands.Tap");
        var bind = UatBinder.Bind(document, catalog);
        Assert.True(bind.Success, FormatDiagnostics(bind));
        var scenario = Assert.Single(bind.Document!.Scenarios);
        var runner = new UatScenarioRunner();

        var result = await runner.RunAsync(scenario);

        var step = Assert.Single(result.Steps);
        Assert.False(result.Passed);
        Assert.Equal(UatStepResultStatus.Failed, step.Status);
        Assert.Contains("does not have an execution handler", step.Message);
    }

    [Fact]
    public async Task RunAsync_PreCanceledToken_ReturnsCanceledResult()
    {
        var scenario = CreateBoundScenario("""
            # UAT: Canceled

            ## Scenario: Save

            When I tap Save
            """);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await new UatScenarioRunner().RunAsync(scenario, cancellation.Token);

        var step = Assert.Single(result.Steps);
        Assert.False(result.Passed);
        Assert.Equal(UatStepResultStatus.Canceled, step.Status);
    }

    [Fact]
    public async Task RunAsync_ConfiguredSkipRule_ReturnsSkippedScenarioWithoutExecutingSteps()
    {
        var scenario = CreateBoundScenario("""
            # UAT: Hardware

            @hardware
            ## Scenario: Hardware camera can capture

            Given I am on the Settings page
            When I tap Save
            """);
        var config = UatConfigParser.Parse("""
            # UAT Config

            ## Skip Rules

            | Tag | EnvironmentVariable |
            | --- | --- |
            | hardware | EXAMPLE_UAT_HARDWARE |
            """);
        var runner = new UatScenarioRunner();

        var result = await runner.RunAsync(
            scenario,
            config,
            name => name == "EXAMPLE_UAT_HARDWARE" ? null : "1");

        Assert.True(result.Skipped);
        Assert.False(result.Passed);
        Assert.Empty(result.Steps);
        Assert.Empty(runner.Context.Diagnostics);
        Assert.NotNull(result.SkipDecision);
        Assert.Contains("EXAMPLE_UAT_HARDWARE", result.SkipDecision.Reason);
    }

    private static UatBoundScenario CreateBoundScenario(string markdown)
    {
        var document = ParseValid(markdown);
        var bind = UatBinder.Bind(document, CreateExecutableCatalog());
        Assert.True(bind.Success, FormatDiagnostics(bind));
        return Assert.Single(bind.Document!.Scenarios);
    }

    private static UatCommandCatalog CreateExecutableCatalog()
    {
        var catalog = new UatCommandCatalog();
        catalog.Register(
            UatEffectiveStepKeyword.Given,
            "the application is running",
            "AppCommands.AssertRunning",
            handler: (context, invocation, _) =>
            {
                context.Diagnostics.Add("app:running");
                return Task.FromResult(UatStepResult.Passed(invocation));
            });
        catalog.Register(
            UatEffectiveStepKeyword.Given,
            "I am on the {page} page",
            "PageCommands.AssertPageOpen",
            handler: SetCurrentPage);
        catalog.Register(
            UatEffectiveStepKeyword.Then,
            "I should see the {page} page",
            "PageCommands.AssertPageOpen",
            handler: SetCurrentPage);
        catalog.Register(
            UatEffectiveStepKeyword.When,
            "I tap {control}",
            "InteractionCommands.Tap",
            allowsTable: false,
            handler: (context, invocation, _) =>
            {
                context.Diagnostics.Add($"tap:{invocation.Arguments["control"]}");
                return Task.FromResult(UatStepResult.Passed(invocation));
            });

        return catalog;
    }

    private static Task<UatStepResult> SetCurrentPage(
        UatExecutionContext context,
        UatStepInvocation invocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.CurrentPageName = invocation.Arguments["page"];
        return Task.FromResult(UatStepResult.Passed(invocation));
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

    private static string FormatResults(UatScenarioRunResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Steps.Select(x => $"{x.Status}: {x.Invocation.Step.Source}: {x.Invocation.Step.Text} {x.Message}"));
    }
}

using Xunit;

namespace Bruinet.Bdd.Tests;

public sealed class BddExecutionTests
{
    [Fact]
    public async Task RunAsync_BoundScenario_ExecutesHandlersAndRecordsResults()
    {
        var scenario = CreateBoundScenario("""
            # Feature: Settings

            ## Background

            ```gherkin
            Given the application is running
            ```

            ## Scenario: Save display name

            ```gherkin
            Given I am on the Settings page
            When I tap Save
            Then I should see the Settings page
            ```
            """);
        var runner = new BddScenarioRunner();

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
            # Feature: Settings

            ## Scenario: Save

            ```gherkin
            Given I am on the Settings page
            When I tap Save
            ```
            """);
        var session = new BddScenarioRunner().CreateSession(scenario);

        var first = await session.RunNextAsync();

        Assert.Equal(BddStepResultStatus.Passed, first.Status);
        Assert.True(session.HasNext);
        Assert.Single(session.Results);

        var second = await session.RunNextAsync();

        Assert.Equal(BddStepResultStatus.Passed, second.Status);
        Assert.False(session.HasNext);
        Assert.Equal(2, session.Results.Count);
    }

    [Fact]
    public async Task SkipNext_PassesOverAStepWithoutRunningIt()
    {
        var scenario = CreateBoundScenario("""
            # Feature: Settings

            ## Scenario: Save

            ```gherkin
            Given I am on the Settings page
            When I tap Save
            ```
            """);
        var session = new BddScenarioRunner().CreateSession(scenario);

        Assert.Same(scenario.Invocations[0], session.Next);
        var skipped = session.SkipNext("Run from here");
        var ran = await session.RunNextAsync();

        Assert.Equal(BddStepResultStatus.Skipped, skipped.Status);
        Assert.Equal("Run from here", skipped.Message);
        Assert.Same(scenario.Invocations[1], ran.Invocation);
        Assert.Null(session.Next);
        Assert.Throws<InvalidOperationException>(() => session.SkipNext());
    }

    [Fact]
    public async Task Pacer_IsWaitedOnBeforeAndAfterEveryStep_AFailedOneToo()
    {
        var document = ParseValid("""
            # Feature: Settings

            ## Scenario: Save

            ```gherkin
            Given I am on the Settings page
            When I tap Crash
            ```
            """);
        var catalog = CreateExecutableCatalog();
        catalog.Register(BddEffectiveStepKeyword.When, "I tap Crash", "InteractionCommands.Crash", allowsTable: false,
            handler: (_, _, _) => throw new InvalidOperationException("It crashed."));
        var scenario = Assert.Single(BddBinder.Bind(document, catalog).Document!.Scenarios);
        var pacer = new RecordingPacer();
        var runner = new BddScenarioRunner(new BddExecutionContext { Breaks = null, Pacer = pacer });

        await runner.RunAsync(scenario);

        Assert.Equal(["before I am on the Settings page", "after Passed", "before I tap Crash", "after Failed It crashed."], pacer.Calls);
    }

    [Fact]
    public void WrittenLine_OfAScenarioStep_IsItsOwnLine()
    {
        var scenario = CreateBoundScenario("""
            # Feature: Settings

            ## Scenario: Save

            ```gherkin
            Given I am on the Settings page
            ```
            """);

        Assert.Equal(6, BddWrittenStep.Line(scenario.Invocations[0]));
    }

    private sealed class RecordingPacer : IBddStepPacer
    {
        public List<string> Calls { get; } = [];

        public Task BeforeAsync(BddStepInvocation invocation, BddExecutionContext context, CancellationToken cancellationToken)
        {
            Calls.Add($"before {invocation.Step.Text}");
            return Task.CompletedTask;
        }

        public Task AfterAsync(BddStepResult result, BddExecutionContext context, CancellationToken cancellationToken)
        {
            Calls.Add($"after {result.Status}{(result.Message is { } message ? " " + message : string.Empty)}");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task RunAsync_MissingHandler_ReturnsFailedResult()
    {
        var document = ParseValid("""
            # Feature: Missing handler

            ## Scenario: Save

            ```gherkin
            When I tap Save
            ```
            """);
        var catalog = new BddCommandCatalog();
        catalog.Register(BddEffectiveStepKeyword.When, "I tap {control}", "InteractionCommands.Tap");
        var bind = BddBinder.Bind(document, catalog);
        Assert.True(bind.Success, FormatDiagnostics(bind));
        var scenario = Assert.Single(bind.Document!.Scenarios);
        var runner = new BddScenarioRunner();

        var result = await runner.RunAsync(scenario);

        var step = Assert.Single(result.Steps);
        Assert.False(result.Passed);
        Assert.Equal(BddStepResultStatus.Failed, step.Status);
        Assert.Contains("does not have an execution handler", step.Message);
    }

    [Fact]
    public async Task RunAsync_PreCanceledToken_ReturnsCanceledResult()
    {
        var scenario = CreateBoundScenario("""
            # Feature: Canceled

            ## Scenario: Save

            ```gherkin
            When I tap Save
            ```
            """);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await new BddScenarioRunner().RunAsync(scenario, cancellation.Token);

        var step = Assert.Single(result.Steps);
        Assert.False(result.Passed);
        Assert.Equal(BddStepResultStatus.Canceled, step.Status);
    }

    [Fact]
    public async Task RunAsync_ConfiguredSkipRule_ReturnsSkippedScenarioWithoutExecutingSteps()
    {
        var scenario = CreateBoundScenario("""
            # Feature: Hardware

            @hardware
            ## Scenario: Hardware camera can capture

            ```gherkin
            Given I am on the Settings page
            When I tap Save
            ```
            """);
        var config = BddConfigParser.Parse("""
            # UAT Config

            ## Skip Rules

            | Tag | EnvironmentVariable |
            | --- | --- |
            | hardware | EXAMPLE_UAT_HARDWARE |
            """);
        var runner = new BddScenarioRunner();

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

    private static BddBoundScenario CreateBoundScenario(string markdown)
    {
        var document = ParseValid(markdown);
        var bind = BddBinder.Bind(document, CreateExecutableCatalog());
        Assert.True(bind.Success, FormatDiagnostics(bind));
        return Assert.Single(bind.Document!.Scenarios);
    }

    private static BddCommandCatalog CreateExecutableCatalog()
    {
        var catalog = new BddCommandCatalog();
        catalog.Register(
            BddEffectiveStepKeyword.Given,
            "the application is running",
            "AppCommands.AssertRunning",
            handler: (context, invocation, _) =>
            {
                context.Diagnostics.Add("app:running");
                return Task.FromResult(BddStepResult.Passed(invocation));
            });
        catalog.Register(
            BddEffectiveStepKeyword.Given,
            "I am on the {page} page",
            "PageCommands.AssertPageOpen",
            handler: SetCurrentPage);
        catalog.Register(
            BddEffectiveStepKeyword.Then,
            "I should see the {page} page",
            "PageCommands.AssertPageOpen",
            handler: SetCurrentPage);
        catalog.Register(
            BddEffectiveStepKeyword.When,
            "I tap {control}",
            "InteractionCommands.Tap",
            allowsTable: false,
            handler: (context, invocation, _) =>
            {
                context.Diagnostics.Add($"tap:{invocation.Arguments["control"]}");
                return Task.FromResult(BddStepResult.Passed(invocation));
            });

        return catalog;
    }

    private static Task<BddStepResult> SetCurrentPage(
        BddExecutionContext context,
        BddStepInvocation invocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.CurrentPageName = invocation.Arguments["page"];
        return Task.FromResult(BddStepResult.Passed(invocation));
    }

    private static BddDocument ParseValid(string markdown)
    {
        var result = BddMarkdownParser.Parse(markdown);
        Assert.True(
            result.Success,
            string.Join(Environment.NewLine, result.Diagnostics.Select(x => $"{x.Code} {x.Message}")));
        Assert.NotNull(result.Document);
        return result.Document;
    }

    private static string FormatDiagnostics(BddBindResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Diagnostics.Select(x => $"{x.Location}: {x.Code} {x.Message}"));
    }

    private static string FormatResults(BddScenarioRunResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Steps.Select(x => $"{x.Status}: {x.Invocation.Step.Source}: {x.Invocation.Step.Text} {x.Message}"));
    }
}

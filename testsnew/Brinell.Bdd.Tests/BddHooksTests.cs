using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Brinell.Bdd.Tests;

/// <summary>Hooks on step-definition classes: when they run, in which order, for which tags.</summary>
public sealed class BddHooksTests
{
    [Fact]
    public async Task Hooks_RunAroundTheScenario_InOrder_AndOnlyForTheirTags()
    {
        var (log, runs) = await RunAsync($"""
            # Feature: Hooks {Guid.NewGuid():N}

            ## Scenario: Plain

            When I work

            @backend
            ## Scenario: Backend

            When I work
            And I work
            """);

        Assert.All(runs, run => Assert.True(run.Passed));
        Assert.Equal(
            [
                "feature", "before", "work", "after-step Passed", "after",
                "before", "before-backend", "work", "after-step Passed", "work", "after-step Passed", "after",
            ],
            log);
    }

    [Fact]
    public async Task AfterScenario_RunsWhenAStepFails()
    {
        var (log, runs) = await RunAsync($"""
            # Feature: Failing {Guid.NewGuid():N}

            ## Scenario: Broken

            When I fail
            And I work
            """);

        var run = Assert.Single(runs);
        Assert.False(run.Passed);
        Assert.Equal(["feature", "before", "fail", "after-step Failed", "after"], log);
    }

    [Fact]
    public async Task AFailingHook_FailsTheStepItFollows()
    {
        var (_, runs) = await RunAsync($"""
            # Feature: Failing hook {Guid.NewGuid():N}

            @strict
            ## Scenario: Strict

            When I work
            """);

        var step = Assert.Single(Assert.Single(runs).Steps);
        Assert.Equal(BddStepResultStatus.Failed, step.Status);
        Assert.Contains("the hook HookSteps.RefuseAfterStep failed after it: no steps allowed", step.Message, StringComparison.Ordinal);
    }

    private static async Task<(List<string> Log, List<BddScenarioRunResult> Runs)> RunAsync(string markdown)
    {
        var parse = BddMarkdownParser.Parse(markdown, Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.feature.md"));
        Assert.True(parse.Success, string.Join(Environment.NewLine, parse.Diagnostics.Select(x => x.Message)));
        var catalog = BddStepDefinitionRuntime.FromTypes(new object(), typeof(HookSteps)).CreateCommandCatalog();
        var bind = BddBinder.Bind(parse.Document!, catalog);
        Assert.True(bind.Success, string.Join(Environment.NewLine, bind.Diagnostics.Select(x => x.Message)));

        List<string> log = [];
        using var services = new ServiceCollection().AddSingleton(log).AddTransient<HookSteps>().BuildServiceProvider();
        List<BddScenarioRunResult> runs = [];
        foreach (var scenario in bind.Document!.Scenarios)
        {
            var runner = new BddScenarioRunner();
            BddStepDefinitionRuntime.SetServiceProvider(runner.Context, services);
            HookSteps.FeatureLog = log;
            runs.Add(await runner.RunAsync(scenario));
        }

        return (log, runs);
    }

    [BddStepDefinitions]
    public sealed class HookSteps(List<string> log)
    {
        // A BeforeFeature hook is static; each test's log flows to it with the test's own async flow.
        private static readonly AsyncLocal<List<string>?> CurrentLog = new();

        public static List<string>? FeatureLog
        {
            get => CurrentLog.Value;
            set => CurrentLog.Value = value;
        }

        [BddBeforeFeature]
        public static void Feature() => FeatureLog?.Add("feature");

        [BddBeforeScenario]
        public void Before() => log.Add("before");

        [BddBeforeScenario("@backend")]
        public void BeforeBackend() => log.Add("before-backend");

        [BddAfterStep]
        public void AfterStep(BddStepResult result) => log.Add($"after-step {result.Status}");

        [BddAfterStep("strict")]
        public void RefuseAfterStep() => throw new InvalidOperationException("no steps allowed");

        [BddAfterScenario]
        public Task After()
        {
            log.Add("after");
            return Task.CompletedTask;
        }

        [BddWhen("I work")]
        public void Work() => log.Add("work");

        [BddWhen("I fail")]
        public void Fail()
        {
            log.Add("fail");
            throw new InvalidOperationException("broken");
        }
    }
}

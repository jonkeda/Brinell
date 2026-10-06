using System.Text.Json;
using Brinell.Bdd.Workspace.UITests.PageObjects;
using Brinell.Bdd.Workspace.UITests.TestBase;

namespace Brinell.Bdd.Workspace.UITests.Tests;

/// <summary>A debugged run in the preview: the paused bar, its moves, and the running label.</summary>
public sealed class PausedRunTests : IAsyncLifetime
{
    private const string Feature = """
        # Feature: Shelf

        ## Scenario: Shelve

        ```gherkin
        When I shelve "Dune"
        And I shelve "Emma"
        Then the shelf holds 2 books
        ```
        """;

    private PreviewIde _ide = null!;
    private ScenarioPreviewPage _preview = null!;

    public async Task InitializeAsync()
    {
        _ide = await PreviewIde.StartAsync();
        _preview = new ScenarioPreviewPage(_ide.Context);
        _ide.Show(Feature);
        _preview.BindState.AssertText("Parsed only");
    }

    public async Task DisposeAsync() => await _ide.DisposeAsync();

    [Fact]
    public void APausedRun_ShowsTheBar_SaysWhere_AndMarksTheStep()
    {
        _preview.PausedBar.AssertVisible(false);

        _ide.Send(Paused("I shelve \"Emma\"", line: 7));

        _preview.PausedBar.AssertVisible();
        _preview.PausedAt.AssertText("⏸ Paused before: I shelve \"Emma\" (line 7)");
        _preview.Step(7).AssertAttribute("data-paused", string.Empty);
    }

    [Fact]
    public void StepInto_IsOnlyForACall_AndStepOut_OnlyInsideOne()
    {
        _ide.Send(Paused("I shelve \"Emma\"", line: 7));
        _preview.StepInto.AssertEnabled(false);
        _preview.StepOut.AssertEnabled(false);

        _ide.Send(Paused("the shelf is full", line: 7, isCall: true, depth: 1, calls: ["Shelf.steps.md"]));

        _preview.StepInto.AssertEnabled();
        _preview.StepOut.AssertEnabled();
        _preview.PausedAt.AssertText("⏸ Paused at the call: the shelf is full (line 7 · in Shelf.steps.md)");
    }

    [Fact]
    public void AMove_IsSentToTheIde()
    {
        _ide.Send(Paused("I shelve \"Emma\"", line: 7));

        _preview.StepOver.Click();
        _preview.Continue.Click();

        Assert.Equal(["over", "continue"], Moves());
    }

    [Fact]
    public void GoingOn_HidesTheBar_AndUnmarksTheStep()
    {
        _ide.Send(Paused("I shelve \"Emma\"", line: 7));
        _preview.PausedBar.AssertVisible();

        _ide.Send(new { type = "paused" });

        _preview.PausedBar.AssertVisible(false);
        _preview.Step(7).AssertAttribute("data-paused", null);
    }

    [Fact]
    public void ARunPausedInTheDebugger_IsMarkedPaused_AndRunningAgainWhenItGoesOn()
    {
        _ide.Send(new { type = "running", line = 3 });
        _preview.Scenario(3).AssertAttribute("data-running-here", string.Empty);

        _ide.Send(new { type = "running", line = 3, paused = true });
        _preview.Scenario(3).AssertAttribute("data-running-here", "paused");

        _ide.Send(new { type = "running", line = 3, paused = false });
        _preview.Scenario(3).AssertAttribute("data-running-here", string.Empty);

        _ide.Send(new { type = "running" });
        _preview.Scenario(3).AssertAttribute("data-running-here", null);
    }

    [Fact]
    public void Step_AsksTheIdeToStepThroughTheScenario()
    {
        _ide.Show(Feature, canRun: true);

        _preview.StepThrough(3).Click();

        Assert.Contains(_ide.Sent(), message => Type(message) == "step" && message.GetProperty("line").GetInt32() == 3);
        Assert.DoesNotContain(_ide.Sent(), message => Type(message) == "run");
    }

    [Fact]
    public void AStopAfterAFailedStep_SaysSo_WithTheFailure()
    {
        _ide.Send(new { type = "paused", text = "the shelf holds 2 books", line = 8, here = true, isCall = false, depth = 0, calls = Array.Empty<string>(), failed = true, message = "It holds 1." });

        _preview.PausedAt.AssertText("⏸ Paused after the failed step: the shelf holds 2 books (line 8) · It holds 1.");
        _preview.Step(8).AssertAttribute("data-paused", string.Empty);
    }

    [Fact]
    public void AClickOnAStepsKeyword_TogglesItsBreakpoint()
    {
        _preview.StepKeyword(6).Click();

        Assert.Contains(_ide.Sent(), message => Type(message) == "toggleBreak" && message.GetProperty("line").GetInt32() == 6);
    }

    private static object Paused(string text, int line, bool isCall = false, int depth = 0, string[]? calls = null)
        => new { type = "paused", text, line, here = true, isCall, depth, calls = calls ?? [] };

    private List<string?> Moves()
        => _ide.Sent().Where(message => Type(message) == "debug").Select(message => message.GetProperty("move").GetString()).ToList();

    private static string? Type(JsonElement message) => message.GetProperty("type").GetString();
}

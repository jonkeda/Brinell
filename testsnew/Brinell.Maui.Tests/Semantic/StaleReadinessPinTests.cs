using System.Diagnostics;
using Brinell.Core.Logging;
using Brinell.Maui.Containers;
using Brinell.Maui.Context;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Controls.Dialogs;
using Brinell.Maui.Pages;

namespace Brinell.Maui.Tests.Semantic;

/// <summary>
/// Pins the behaviour the stale-readiness work sets out to fix, before it is fixed.
/// </summary>
/// <remarks>
/// <para>
/// Written in plan step 2 (<c>.my/stale-readiness/plan.md</c>). Each test states the behaviour
/// the design requires and fails on the code as it was then, for the reason given in its summary.
/// The <c>Pin</c> trait names the step expected to make it pass, so each later step can run its
/// own: <c>dotnet test --filter "Pin=step4"</c>.
/// </para>
/// <para>
/// Tests with <c>Pin=guard</c> are the R0 guards ("app bugs stay failures"): they pass before the
/// change and must keep passing after it. A later step that makes one fail has turned an app bug
/// into a pass.
/// </para>
/// <para>
/// Assertions are on behaviour (timings, call counts, log entries, messages), not on types the
/// later steps introduce, so the file compiles against the code it pins.
/// </para>
/// </remarks>
public class StaleReadinessPinTests : SemanticControlTestsBase
{
    private readonly Mock<ITestLogger> _logger = new();
    private readonly Mock<IMauiElement> _app = new();
    private readonly List<IMauiElement> _rows = [];
    private readonly Mock<IMauiElement> _listRoot;

    public StaleReadinessPinTests()
    {
        Context.Setup(c => c.Timeouts).Returns(new TimeoutSettings
        {
            DefaultWait = 1000,
            PageLoad = 1000,
            ElementFind = 1000,
            PollingInterval = 1,
            Animation = 300
        });
        Context.Setup(c => c.Logger).Returns(_logger.Object);
        Context.Setup(c => c.AppElement).Returns(_app.Object);

        _listRoot = CreateElement("Rows", 0, 0, 300, 400);
        Context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Rows"))).Returns(_listRoot.Object);
        Context.Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == "Rows"))).Returns(_listRoot.Object);
        _listRoot.Setup(e => e.FindElements(It.Is<Locator>(l => l.Value == "ListItem")))
            .Returns(() => _rows.ToArray());
    }

    #region Pages and objects under test

    private sealed class PinPage(IMauiTestContext context) : PageObjectBase<PinPage>(context)
    {
        public override string Name => "TestPage";

        public override bool IsLoaded() => true;

        public Button<PinPage> Target => new(this, "Target");

        public Label<PinPage> Status => new(this, "Target");

        public CheckBox<PinPage> Toggle => new(this, "Target");

        public ToolbarButton<PinPage> Toolbar => new(this, "Target");

        public PinRows Rows => new(this, "Rows");

        public SlowContainer Slow => new(this, "Slow");
    }

    private sealed class BusyPage(IMauiTestContext context) : PageObjectBase<BusyPage>(context)
    {
        public override string Name => "TestPage";

        protected override BusySignalPolicy BusySignalPolicy => BusySignalPolicy.Required;

        public ContentDialog<BusyPage> Dialog => new(this);
    }

    private sealed class PinRows(IMauiScope<PinPage> scope, string automationId)
        : CollectionObjectBase<PinPage, PinRows, PinRow>(
            scope,
            automationId,
            ItemStrategy.ByLocator(Locator.ByControlType("ListItem")),
            (collection, itemRoot, index) => new PinRow(collection, itemRoot, index));

    private sealed class PinRow(PinRows collection, IMauiElement itemRoot, int index)
        : ItemObjectBase<PinRows, PinRow>(collection, itemRoot, index)
    {
        public Label<PinRow> Name => new(this, "RowName");
    }

    /// <summary>A container whose content becomes ready only after a few checks.</summary>
    private sealed class SlowContainer(IMauiScope<PinPage> parentScope, string locatorValue)
        : ContainerObjectBase<PinPage, SlowContainer>(parentScope, locatorValue)
    {
        public int ContentChecks { get; private set; }

        public Label<SlowContainer> Status => new(this, "SlowStatus");

        protected override ScopeReadiness ProbeContentReadiness(IMauiElement root)
            => ++ContentChecks >= 3 ? ContentReady() : ContentNotReady("still loading");
    }

    private PinPage NewPage() => new(Context.Object);

    /// <summary>Makes the page root answer <c>Target</c> with whatever <paramref name="next"/> gives.</summary>
    private void GivenTarget(Func<IMauiElement?> next)
        => Context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Target"))).Returns(next);

    private static Mock<IMauiElement> Element(bool visible = true, bool enabled = true, string? text = null)
    {
        var element = CreateElement("Target", 0, 0, 40, 20);
        element.Setup(e => e.Visible).Returns(visible);
        element.Setup(e => e.Enabled).Returns(enabled);
        element.Setup(e => e.Text).Returns(text);
        return element;
    }

    private static Mock<IMauiElement> Row(string automationId, string name)
    {
        var row = CreateElement(automationId, 0, 0, 300, 40);
        row.Setup(e => e.AutomationId).Returns(automationId);
        row.Setup(e => e.TagName).Returns("ListItem");
        var label = CreateElement("RowName", 0, 0, 100, 20);
        label.Setup(e => e.Text).Returns(name);
        row.Setup(e => e.TryFindElement(It.Is<Locator>(l => l.Value == "RowName"))).Returns(label.Object);
        return row;
    }

    private static long Timed(Action act)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            act();
        }
        catch
        {
            // Timing only; the caller asserts on the outcome separately.
        }

        return stopwatch.ElapsedMilliseconds;
    }

    #endregion

    #region Original plan (a)-(e): one budget, found again on every attempt, one log unit

    /// <summary>
    /// (a) An element that is being replaced must not use up the call: the next attempt finds the
    /// new one. Fails today: the first element's nested visibility wait takes the whole budget.
    /// </summary>
    [Fact]
    [Trait("Pin", "step4")]
    public void A_Click_FindsTheElementAgain_WhenTheFirstOneNeverBecomesVisible()
    {
        var dead = Element(visible: false);
        var good = Element();
        var lookups = 0;
        GivenTarget(() => ++lookups == 1 ? dead.Object : good.Object);

        NewPage().Target.Click(timeoutMs: 1000);

        good.Verify(e => e.Invoke(), Times.Once);
        dead.Verify(e => e.Invoke(), Times.Never);
    }

    /// <summary>
    /// (b) The caller's timeout is the call's budget. Fails today: the nested visibility wait uses
    /// DefaultWait (1000 ms here) whatever the caller asked for.
    /// </summary>
    [Fact]
    [Trait("Pin", "step4")]
    public void B_Click_HonoursTheCallersTimeout_WhenTheElementNeverBecomesVisible()
    {
        var hidden = Element(visible: false);
        GivenTarget(() => hidden.Object);

        Exception? error = null;
        var elapsed = Timed(() => error = Record.Exception(() => NewPage().Target.Click(timeoutMs: 300)));

        Assert.NotNull(error);
        Assert.True(elapsed < 800, $"Click(timeoutMs: 300) took {elapsed} ms.");
    }

    /// <summary>
    /// (c) One public call is one log entry/exit pair. Fails today: the nested visibility wait
    /// logs a pair of its own.
    /// </summary>
    [Fact]
    [Trait("Pin", "step4")]
    public void C_Click_WritesOneLogEntryAndExitPair()
    {
        var reads = 0;
        var element = Element();
        element.Setup(e => e.Visible).Returns(() => ++reads > 1);
        GivenTarget(() => element.Object);

        NewPage().Target.Click(timeoutMs: 1000);

        _logger.Verify(l => l.LogEntry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
        _logger.Verify(l => l.LogExit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<LogResult>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Once);
    }

    /// <summary>(d) As (a), for <c>Get*</c>.</summary>
    [Fact]
    [Trait("Pin", "step4")]
    public void D_GetText_FindsTheElementAgain_WhenTheFirstOneNeverBecomesVisible()
    {
        var dead = Element(visible: false, text: "dead");
        var good = Element(text: "good");
        var lookups = 0;
        GivenTarget(() => ++lookups == 1 ? dead.Object : good.Object);

        Assert.Equal("good", NewPage().Status.GetText(timeoutMs: 1000));
    }

    /// <summary>(d) As (a), for <c>Wait*</c>.</summary>
    [Fact]
    [Trait("Pin", "step4")]
    public void D_WaitText_FindsTheElementAgain_WhenTheFirstOneNeverBecomesVisible()
    {
        var dead = Element(visible: false, text: "dead");
        var good = Element(text: "good");
        var lookups = 0;
        GivenTarget(() => ++lookups == 1 ? dead.Object : good.Object);

        Assert.True(NewPage().Status.WaitText("good", timeoutMs: 1000));
    }

    /// <summary>
    /// (d) As (a), for <c>Assert*</c>. Fails today: the assert keeps the first element it found and
    /// re-reads it until the budget runs out.
    /// </summary>
    [Fact]
    [Trait("Pin", "step4")]
    public void D_AssertText_FindsTheElementAgain_WhenTheFirstOneNeverBecomesVisible()
    {
        var dead = Element(visible: false, text: "dead");
        var good = Element(text: "good");
        var lookups = 0;
        GivenTarget(() => ++lookups == 1 ? dead.Object : good.Object);

        NewPage().Status.AssertText("good", timeoutMs: 1000);
    }

    /// <summary>
    /// (e) An action that throws runs once, and its failure is in the call's log entry. Fails
    /// today: the log pair closes before the action runs, and says Success.
    /// </summary>
    [Fact]
    [Trait("Pin", "step4")]
    public void E_Click_WhenTheActionThrows_InvokesOnceAndLogsTheError()
    {
        var element = Element();
        element.Setup(e => e.Invoke()).Throws(new InvalidOperationException("boom"));
        GivenTarget(() => element.Object);

        Assert.Throws<InvalidOperationException>(() => NewPage().Target.Click(timeoutMs: 1000));

        element.Verify(e => e.Invoke(), Times.Once);
        _logger.Verify(l => l.LogExit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), LogResult.Error, It.IsAny<int>(), It.Is<string?>(m => m != null && m.Contains("boom"))),
            Times.Once);
    }

    #endregion

    #region Trace (f)-(h): lookups make one attempt

    /// <summary>
    /// (f) A control on AppRoot honours the caller's budget when it is missing. Fails today:
    /// MauiTestContext.FindElement waits ElementFind (3000 ms) inside every poll tick.
    /// </summary>
    [Fact]
    [Trait("Pin", "step4")]
    public void F_AppRootControl_HonoursTheCallersTimeout_WhenMissing()
    {
        var driver = new Mock<IMauiDriver>();
        driver.Setup(d => d.FindElements(It.IsAny<Locator>())).Returns([]);
        driver.Setup(d => d.AppElement).Returns(_app.Object);
        using var context = new MauiTestContext(new MauiTestContextOptions
        {
            Driver = driver.Object,
            Timeouts = new TimeoutSettings { DefaultWait = 5000, ElementFind = 3000, PollingInterval = 10 }
        });
        var button = new Button<AppRoot>(new AppRoot(context), "Missing");

        var elapsed = Timed(() => button.Click(timeoutMs: 300));

        Assert.True(elapsed < 1500, $"Click(timeoutMs: 300) on a missing AppRoot control took {elapsed} ms.");
    }

    /// <summary>
    /// (g) A lookup that fails with an error is not "absent". Fails today: MauiTestContext's
    /// TryFindElement turns every exception into null, so WaitExists(false) passes on a broken
    /// driver.
    /// </summary>
    [Fact]
    [Trait("Pin", "step3")]
    public void G_WaitExistsFalse_DoesNotPass_WhenTheLookupThrows()
    {
        var driver = new Mock<IMauiDriver>();
        driver.Setup(d => d.FindElements(It.IsAny<Locator>())).Throws(new InvalidOperationException("driver broke"));
        driver.Setup(d => d.AppElement).Returns(_app.Object);
        using var context = new MauiTestContext(new MauiTestContextOptions
        {
            Driver = driver.Object,
            Timeouts = new TimeoutSettings { DefaultWait = 300, PollingInterval = 10 }
        });
        var button = new Button<AppRoot>(new AppRoot(context), "Anything");

        var passed = false;
        var error = Record.Exception(() => passed = button.WaitExists(false, timeoutMs: 300));

        Assert.False(passed && error == null, "WaitExists(false) passed although every lookup threw.");
    }

    /// <summary>
    /// (h) A missing control sweeps the scroller at most once per Animation interval. Fails
    /// today: it sweeps on every poll tick.
    /// </summary>
    [Fact]
    [Trait("Pin", "step4")]
    public void H_MissingControl_SweepsAtMostOncePerAnimationInterval()
    {
        GivenTarget(() => null);
        var sweeps = 0;
        _app.Setup(a => a.TryFindByScrolling(It.IsAny<Locator>())).Returns(() =>
        {
            sweeps++;
            return null;
        });

        _ = Timed(() => NewPage().Status.GetText(timeoutMs: 400));

        Assert.True(sweeps <= 3, $"{sweeps} sweeps in 400 ms; at most one per 300 ms expected.");
    }

    #endregion

    #region Scopes and collections

    /// <summary>
    /// (A3) A dialog shown over a busy page can be answered. Fails today: the dialog's button
    /// waits for the page underneath to stop being busy, which it cannot while the dialog is open.
    /// </summary>
    [Fact]
    [Trait("Pin", "step5")]
    public void A3_DialogButton_IsUsable_WhileThePageUnderneathIsBusy()
    {
        var busy = CreateElement("Busy", 0, 0, 1, 1);
        busy.Setup(e => e.Text).Returns("True");
        Context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Busy"))).Returns(busy.Object);
        var dialogRoot = CreateElement("ContentDialog", 0, 0, 300, 200);
        var delete = CreateInvokableElement("DialogDelete", 10, 150, 80, 40);
        dialogRoot.Setup(e => e.TryFindElement(It.Is<Locator>(l => l.Value == "Delete"))).Returns(delete.Object);
        _app.Setup(a => a.TryFindActiveDialog()).Returns(dialogRoot.Object);

        new BusyPage(Context.Object).Dialog.DialogButton("Delete").Click(timeoutMs: 500);

        delete.Verify(e => e.Invoke(), Times.Once);
    }

    /// <summary>
    /// (A1) A container's own readiness is on its children's path. Fails today: a child's call
    /// never asks the container, only the page.
    /// </summary>
    [Fact]
    [Trait("Pin", "step5")]
    public void A1_ChildCall_WaitsForItsContainersContentReadiness()
    {
        var slowRoot = CreateElement("Slow", 0, 0, 300, 300);
        Context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Slow"))).Returns(slowRoot.Object);
        Context.Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == "Slow"))).Returns(slowRoot.Object);
        var status = CreateElement("SlowStatus", 0, 0, 100, 20);
        status.Setup(e => e.Text).Returns("ready");
        slowRoot.Setup(e => e.TryFindElement(It.Is<Locator>(l => l.Value == "SlowStatus"))).Returns(status.Object);
        var container = NewPage().Slow;

        Assert.Equal("ready", container.Status.GetText(timeoutMs: 1000));
        Assert.True(container.ContentChecks >= 3, $"The container's readiness was checked {container.ContentChecks} times.");
    }

    /// <summary>
    /// (B3) A row never acts on another item. Fails today: a row whose root died is re-found by
    /// index, and the index now holds a different item, whose text is returned as the row's.
    /// </summary>
    [Fact]
    [Trait("Pin", "step7")]
    public void B3_Row_DoesNotAnswerForAnotherItem_AfterItsItemIsRemoved()
    {
        var a = Row("Row_A", "A");
        var b = Row("Row_B", "B");
        var c = Row("Row_C", "C");
        _rows.AddRange([a.Object, b.Object]);
        var row = NewPage().Rows.Item(1);

        // B is deleted: its element collapses, and C takes index 1.
        b.Setup(e => e.Rect).Returns(System.Drawing.Rectangle.Empty);
        _rows.Clear();
        _rows.AddRange([a.Object, c.Object]);

        var error = Record.Exception(() => row.Name.GetText(timeoutMs: 300));

        Assert.NotNull(error);
    }

    /// <summary>
    /// (Q7) A row whose element the list recycled for another item finds its own item again by
    /// its logical index, and reads that item, not the one now in its old element.
    /// </summary>
    [Fact]
    [Trait("Pin", "step7")]
    public void Row_FindsItsItemAgain_WhenItsElementIsRecycled()
    {
        var first = Row("Row", "Item 0");
        var second = Row("Row", "Item 1");
        first.Setup(e => e.PositionInSet).Returns(1);
        var secondPosition = 2;
        second.Setup(e => e.PositionInSet).Returns(() => secondPosition);
        _rows.AddRange([first.Object, second.Object]);
        var row = NewPage().Rows.Item(1);
        Assert.Equal(ItemKey.Logical(1), row.Key);

        // The list scrolls: the second element now shows item 7, and item 1 moves to a new element.
        secondPosition = 8;
        var moved = Row("Row", "Item 1 (moved)");
        moved.Setup(e => e.PositionInSet).Returns(2);
        _rows.Add(moved.Object);

        Assert.Equal("Item 1 (moved)", row.Name.GetText(timeoutMs: 500));
    }

    /// <summary>
    /// (D1) ScrollToItem honours the caller's budget. Fails today: each scroll step that jumps
    /// waits up to DefaultWait for the list to settle, whatever the caller asked for.
    /// </summary>
    [Fact]
    [Trait("Pin", "step7")]
    public void D1_ScrollToItem_HonoursTheCallersTimeout()
    {
        _rows.AddRange([Row("Row_A", "A").Object, Row("Row_B", "B").Object]);
        _listRoot.Setup(e => e.ScrollTowards(It.IsAny<int>())).Returns(ScrollStep.Jumped);

        var elapsed = Timed(() => NewPage().Rows.ScrollToItem(5, timeoutMs: 300));

        Assert.True(elapsed < 800, $"ScrollToItem(5, timeoutMs: 300) took {elapsed} ms.");
    }

    /// <summary>
    /// (D6) Item(index) waits like Item(key). Fails today: it answers "no item" at once, while a
    /// row that appears a moment later is exactly what a test is waiting for.
    /// </summary>
    [Fact]
    [Trait("Pin", "step7")]
    public void D6_ItemByIndex_WaitsForTheRowToAppear()
    {
        var reads = 0;
        var a = Row("Row_A", "A");
        _listRoot.Setup(e => e.FindElements(It.Is<Locator>(l => l.Value == "ListItem")))
            .Returns(() => ++reads < 4 ? [] : [a.Object]);

        var item = NewPage().Rows.Item(0);

        Assert.Equal("A", item.Name.GetText());
    }

    #endregion

    #region R0 guards (pass before and after) and near-misses

    /// <summary>
    /// (Step 8) An action the platform reports as not performed - the app's bridge did not answer
    /// yet - is asked again within the budget, and the call succeeds once it is performed.
    /// </summary>
    [Fact]
    [Trait("Pin", "step8")]
    public void Toolbar_ActionNotPerformedYet_IsAskedAgainWithinTheBudget()
    {
        var element = Element();
        var asks = 0;
        element.Setup(e => e.InvokeToolbarItem("Target")).Callback(() =>
        {
            if (++asks < 3)
            {
                throw new ElementNotReadyException(Locator.ByAutomationId("Target"), NotReadyReason.Other,
                    "no element published on the app's bridge answers InvokeToolbarItem.");
            }
        });
        GivenTarget(() => element.Object);

        NewPage().Toolbar.Click(timeoutMs: 1000);

        Assert.Equal(3, asks);
    }

    /// <summary>
    /// Guard (step 8): a toolbar item that stays disabled fails within its budget, as "disabled".
    /// </summary>
    [Fact]
    [Trait("Pin", "guard")]
    public void Guard_Toolbar_FailsWithinItsBudget_WhenTheItemStaysDisabled()
    {
        var element = Element();
        element.Setup(e => e.InvokeToolbarItem("Target")).Throws(
            new ElementNotReadyException(Locator.ByAutomationId("Target"), NotReadyReason.Disabled,
                "the toolbar item is disabled"));
        GivenTarget(() => element.Object);

        Exception? error = null;
        var elapsed = Timed(() => error = Record.Exception(() => NewPage().Toolbar.Click(timeoutMs: 300)));

        var notReady = Assert.IsType<ElementNotReadyException>(error);
        Assert.Equal(NotReadyReason.Disabled, notReady.Reason);
        Assert.True(elapsed < 800, $"Click(timeoutMs: 300) took {elapsed} ms.");
    }

    /// <summary>
    /// Guard (step 8): any other failure of an action ends the call at once and is not repeated -
    /// the action may have taken effect.
    /// </summary>
    [Fact]
    [Trait("Pin", "guard")]
    public void Guard_Toolbar_OtherActionFailure_IsNotRepeated()
    {
        var element = Element();
        element.Setup(e => e.InvokeToolbarItem("Target")).Throws(new BrinellException("the app threw"));
        GivenTarget(() => element.Object);

        Assert.Throws<BrinellException>(() => NewPage().Toolbar.Click(timeoutMs: 1000));

        element.Verify(e => e.InvokeToolbarItem("Target"), Times.Once);
    }

    /// <summary>
    /// Guard: a command that never re-enables fails the call within its budget, says so, and is
    /// never invoked.
    /// </summary>
    [Fact]
    [Trait("Pin", "guard")]
    public void Guard_Click_FailsWithinItsBudget_WhenTheControlStaysDisabled()
    {
        var element = Element(enabled: false);
        GivenTarget(() => element.Object);

        Exception? error = null;
        var elapsed = Timed(() => error = Record.Exception(() => NewPage().Target.Click(timeoutMs: 300)));

        Assert.NotNull(error);
        Assert.Contains("disabled", error!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(elapsed < 800, $"Click(timeoutMs: 300) took {elapsed} ms.");
        element.Verify(e => e.Invoke(), Times.Never);
    }

    /// <summary>
    /// Guard: an action whose effect never shows fails, and is not repeated.
    /// </summary>
    [Fact]
    [Trait("Pin", "guard")]
    public void Guard_Toggle_FailsAndActsOnce_WhenTheStateNeverChanges()
    {
        var element = Element();
        element.Setup(e => e.Checked).Returns(false);
        GivenTarget(() => element.Object);

        Assert.ThrowsAny<Exception>(() => NewPage().Toggle.Toggle(timeoutMs: 300));

        element.Verify(e => e.Toggle(), Times.Once);
    }

    /// <summary>
    /// A control replaced after its action says so, and the action is not repeated: the effect
    /// cannot be confirmed on an element the action never touched.
    /// </summary>
    [Fact]
    [Trait("Pin", "step6")]
    public void Toggle_ReplacedAfterTheAction_SaysSoAndActsOnce()
    {
        var reads = 0;
        var element = Element();
        element.Setup(e => e.Checked).Returns(() => ++reads == 1 ? false : throw new StaleElementException());
        GivenTarget(() => element.Object);

        Exception? error = null;
        var elapsed = Timed(() => error = Record.Exception(() => NewPage().Toggle.Toggle(timeoutMs: 1000)));

        var stale = Assert.IsType<StaleElementException>(error);
        Assert.Contains("replaced after Toggle", stale.Message);
        Assert.Contains("not repeated", stale.Message);
        element.Verify(e => e.Toggle(), Times.Once);
        Assert.True(elapsed < 800, $"The replaced element took {elapsed} ms to report.");
    }

    /// <summary>
    /// Guard (written in step 3, when the type exists): an app that is gone fails the call at once
    /// instead of waiting out the budget. Fails in step 3: every exception is still retried.
    /// </summary>
    [Fact]
    [Trait("Pin", "step4")]
    public void AppUnavailable_FailsTheCallAtOnce()
    {
        var element = Element();
        element.Setup(e => e.Visible).Throws(new AppUnavailableException("the application process has exited."));
        GivenTarget(() => element.Object);

        Exception? error = null;
        var elapsed = Timed(() => error = Record.Exception(() => NewPage().Target.Click(timeoutMs: 1000)));

        Assert.IsType<AppUnavailableException>(error);
        Assert.True(elapsed < 300, $"Click on a closed app took {elapsed} ms of its 1000 ms budget.");
    }

    /// <summary>
    /// A call that succeeds only after trouble passes, and says so in the log (R0 near-miss).
    /// Fails today: nothing reports a near-miss.
    /// </summary>
    [Fact]
    [Trait("Pin", "step4")]
    public void NearMiss_SlowToBecomeVisible_PassesAndLogsAWarning()
    {
        var stopwatch = Stopwatch.StartNew();
        var element = Element(text: "late");
        element.Setup(e => e.Visible).Returns(() => stopwatch.ElapsedMilliseconds > 250);
        GivenTarget(() => element.Object);

        Assert.Equal("late", NewPage().Status.GetText(timeoutMs: 400));

        _logger.Verify(l => l.LogExit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), LogResult.Warning, It.IsAny<int>(), It.IsAny<string?>()), Times.AtLeastOnce);
    }

    #endregion
}

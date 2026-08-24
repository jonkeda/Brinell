// =====================================================================================
// Defect record - VERIFIED against the code as it stands today (4 passed, 0 failed).
//
// This file pins the CURRENT behaviour described in container-and-collection-design.md
// section 3. It is deliberately NOT destined for the codebase: once the new bases land
// the behaviour it records no longer exists. DELETE it at implementation time.
//
// To re-run: needs a project referencing only Brinell.Core + Brinell.Maui, because
// Brinell.Maui.Extensions does not build at HEAD and blocks Brinell.Maui.Tests.
// =====================================================================================

namespace Brinell.Maui.Tests;

public class TempDefectRecordTests
{
    private readonly Mock<IMauiTestContext> _context;
    public TempDefectRecordTests()
    {
        _context = new Mock<IMauiTestContext>();
        _context.Setup(c => c.Timeouts).Returns(new TimeoutSettings { DefaultWait = 300, PageLoad = 300, PollingInterval = 20 });
        _context.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.AutomationId);
    }

    // CONFIRMED CLAIM 1: container's own inherited action returns the PAGE, not the container.
    [Fact]
    public void Defect_ContainerOwnAction_ReturnsPage()
    {
        var page = new LegacyTestPage(_context.Object);
        SetupRootElement("Container");
        var result = page.Container.AssertVisible(true);
        Assert.IsType<LegacyTestPage>(result);
        Assert.Same(page, result);
    }

    // CONFIRMED CLAIM 2: the row ROOT is resolved page-wide, so rows need globally unique ids.
    // With a repeating row id, every index collapses onto the SAME element.
    [Fact]
    public void Defect_RepeatingRowId_AllIndexesCollapseToSameRow()
    {
        var page = new RepeatingIdPage(_context.Object);

        // One shared root registered under the repeating id "RowRoot".
        var shared = SetupRootElement("RowRoot");
        SetupChild(shared, "RowLabel", "FIRST ROW ONLY");

        // Two different indexes, same text - rows are not distinguishable.
        Assert.Equal("FIRST ROW ONLY", page.Rows.Item(0).RowLabel.GetText());
        Assert.Equal("FIRST ROW ONLY", page.Rows.Item(1).RowLabel.GetText());
    }

    // CONFIRMED CLAIM 3: GetItemCount probes sequentially and caps at 100.
    [Fact]
    public void Defect_GetItemCount_CapsAt100()
    {
        var page = new LegacyTestPage(_context.Object);
        var any = new Mock<IMauiElement>();
        any.Setup(e => e.Visible).Returns(true);
        _context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns(any.Object);
        Assert.Equal(100, page.Rows.GetItemCount());
    }

    // CONFIRMED CLAIM 4 (counter-evidence): a CONTROL inside a container DOES return
    // the container, and IS resolved element-relative. This part already works.
    [Fact]
    public void Works_ControlInContainer_IsScopedAndReturnsContainer()
    {
        var page = new LegacyTestPage(_context.Object);
        var root = SetupRootElement("Container");
        SetupChild(root, "ContainerButton");

        var result = page.Container.ContainerButton.Click();

        Assert.Same(page.Container, result);
        root.Verify(e => e.FindElement(It.Is<Locator>(l => l.Value == "ContainerButton"), It.IsAny<int>()), Times.AtLeastOnce);
    }

    private Mock<IMauiElement> SetupRootElement(string automationId)
    {
        var element = new Mock<IMauiElement>();
        element.Setup(e => e.Visible).Returns(true);
        element.Setup(e => e.Enabled).Returns(true);
        element.Setup(e => e.TagName).Returns("Grid");
        element.Setup(e => e.Rect).Returns(new System.Drawing.Rectangle(0, 0, 300, 200));
        _context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == automationId))).Returns(element.Object);
        _context.Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == automationId))).Returns(element.Object);
        return element;
    }

    private static Mock<IMauiElement> SetupChild(Mock<IMauiElement> parent, string automationId, string? text = null)
    {
        var child = new Mock<IMauiElement>();
        child.Setup(e => e.Visible).Returns(true);
        child.Setup(e => e.Enabled).Returns(true);
        child.Setup(e => e.TagName).Returns("Element");
        child.Setup(e => e.Rect).Returns(new System.Drawing.Rectangle(0, 0, 100, 30));
        if (text != null) child.Setup(e => e.Text).Returns(text);
        parent.Setup(e => e.FindElement(It.Is<Locator>(l => l.Value == automationId), It.IsAny<int>())).Returns(child.Object);
        parent.Setup(e => e.FindElements(It.Is<Locator>(l => l.Value == automationId), It.IsAny<int>())).Returns(new[] { child.Object });
        return child;
    }

    private class LegacyTestPage : PageObjectBase<LegacyTestPage>
    {
        public LegacyTestPage(IMauiTestContext context) : base(context)
        {
            Container = new LegacyContainer(this, Locator.ByAutomationId("Container"));
            Rows = new Brinell.Maui.Controls.List<LegacyTestPage, LegacyRow>(
                this, "Rows", "Row_", (scope, index) => new LegacyRow(this, index));
        }
        public override string Name => "LegacyTestPage";
        public override bool IsLoaded(int? timeoutMs = null) => true;
        public LegacyContainer Container { get; }
        public Brinell.Maui.Controls.List<LegacyTestPage, LegacyRow> Rows { get; }
    }

    private class RepeatingIdPage : PageObjectBase<RepeatingIdPage>
    {
        public RepeatingIdPage(IMauiTestContext context) : base(context)
        {
            Rows = new Brinell.Maui.Controls.List<RepeatingIdPage, RepeatingRow>(
                this, "Rows", "RowRoot", (scope, index) => new RepeatingRow(this, index));
        }
        public override string Name => "RepeatingIdPage";
        public override bool IsLoaded(int? timeoutMs = null) => true;
        public Brinell.Maui.Controls.List<RepeatingIdPage, RepeatingRow> Rows { get; }
    }

    private class RepeatingRow : ContainerBase<RepeatingIdPage, RepeatingRow>
    {
        public RepeatingRow(IMauiScope<RepeatingIdPage> scope, int index)
            : base(scope, Locator.ByAutomationId("RowRoot")) { }
        public Label<RepeatingRow> RowLabel => new(this, "RowLabel");
    }

    private class LegacyContainer : ContainerBase<LegacyTestPage, LegacyContainer>
    {
        public LegacyContainer(IMauiScope<LegacyTestPage> scope, Locator locator) : base(scope, locator) { }
        public Button<LegacyContainer> ContainerButton => new(this, "ContainerButton");
    }

    private class LegacyRow : ContainerBase<LegacyTestPage, LegacyRow>
    {
        public LegacyRow(IMauiScope<LegacyTestPage> scope, int index) : base(scope, Locator.ByAutomationId($"Row_{index}")) { }
        public Label<LegacyRow> RowLabel => new(this, "RowLabel");
    }
}

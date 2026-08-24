namespace Brinell.Maui.Tests;

// =====================================================================================
// Unit tests for the container/collection design, using Moq - no device required.
// Target: testsnew/Brinell.Maui.Tests/ContainerCollectionTests.cs
//
// STAGED - not yet part of the codebase. Move to the destination above only on an
// explicit instruction to start implementing. See ../README.md#destinations-when-implementing.
//
// Two groups:
//
//   [A] CurrentBehaviour_*  - run against the CODE AS IT IS TODAY and pass now.
//                             They pin the defects described in the design doc so the
//                             migration has a before/after record. Each one is expected
//                             to be INVERTED when the new bases land.
//
//   [B] everything else     - written against the PROPOSED bases. They do not compile
//                             until migration steps 1-5 land.
//
// Keeping [A] runnable matters: it is the evidence that the defects are real, rather
// than an assertion in a design document.
// =====================================================================================

/// <summary>
/// Unit tests for ContainerObject and CollectionObject scoping and fluent behaviour.
/// Uses xUnit Assert per SPEC-017b design principles (never FluentAssertions).
/// </summary>
public class ContainerCollectionTests
{
    private readonly Mock<IMauiTestContext> _context;

    public ContainerCollectionTests()
    {
        _context = new Mock<IMauiTestContext>();
        _context.Setup(c => c.Timeouts).Returns(new TimeoutSettings
        {
            DefaultWait = 1000,
            PageLoad = 2000,
            PollingInterval = 20
        });
        _context.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.AutomationId);
    }

    // =================================================================================
    // [A] Current behaviour - these compile and pass against today's code
    // =================================================================================

    #region Current behaviour (defect record)

    // These four are VERIFIED: they compile and pass against today's code.
    // See samples/VerifiedDefectRecordTests.cs for the standalone runnable copy.

    /// <summary>
    /// Counter-evidence, and a guard: a control inside a container ALREADY resolves
    /// element-relative and ALREADY returns the container. The new bases must not
    /// regress this.
    /// </summary>
    [Fact]
    [Trait("Record", "Works")]
    public void CurrentBehaviour_ControlInContainer_IsScopedAndReturnsContainer()
    {
        var page = new LegacyTestPage(_context.Object);
        var root = SetupRootElement("Container");
        SetupChild(root, "ContainerButton");

        var result = page.Container.ContainerButton.Click();

        Assert.Same(page.Container, result);
        root.Verify(e => e.FindElement(
            It.Is<Locator>(l => l.Value == "ContainerButton"), It.IsAny<int>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// Records design 3.1: the container's OWN inherited action returns TParent (the
    /// page), because ContainerBase derives from ControlBase&lt;TParent&gt;.
    /// After migration this asserts Assert.Same(page.Container, result) instead.
    /// </summary>
    [Fact]
    [Trait("Record", "Defect-3.1")]
    public void CurrentBehaviour_ContainerOwnAction_ReturnsPage()
    {
        var page = new LegacyTestPage(_context.Object);
        SetupRootElement("Container");

        var result = page.Container.AssertVisible(true);

        Assert.IsType<LegacyTestPage>(result);
        Assert.Same(page, result);
    }

    /// <summary>
    /// Records design 3.2: a row ROOT is resolved by a page-wide locator, so rows must
    /// carry globally unique ids. With a repeating id every index collapses onto the
    /// same element - which is what a normal MAUI item template produces.
    /// </summary>
    [Fact]
    [Trait("Record", "Defect-3.2")]
    public void CurrentBehaviour_RepeatingRowId_AllIndexesCollapseToSameRow()
    {
        var page = new RepeatingIdPage(_context.Object);
        var shared = SetupRootElement("RowRoot");
        SetupChild(shared, "RowLabel", "FIRST ROW ONLY");

        Assert.Equal("FIRST ROW ONLY", page.Rows.Item(0).RowLabel.GetText());
        Assert.Equal("FIRST ROW ONLY", page.Rows.Item(1).RowLabel.GetText());
    }

    /// <summary>
    /// Records design 3.2: GetItemCount probes indices one at a time and stops at a
    /// hardcoded ceiling of 100, silently truncating.
    /// </summary>
    [Fact]
    [Trait("Record", "Defect-3.2")]
    public void CurrentBehaviour_GetItemCount_CapsAt100()
    {
        var page = new LegacyTestPage(_context.Object);
        var any = new Mock<IMauiElement>();
        any.Setup(e => e.Visible).Returns(true);
        _context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns(any.Object);

        Assert.Equal(100, page.Rows.GetItemCount());
    }

    #endregion

    // =================================================================================
    // [B] Proposed behaviour - compiles once ContainerObjectBase lands
    // =================================================================================

    #region Container scoping

    [Fact]
    [Trait("Pattern", "Scoping")]
    public void Container_ResolvesChild_ThroughItsRoot()
    {
        var page = new TestPage(_context.Object);
        var root = SetupRootElement("Form");
        SetupChild(root, "FormButton");

        Assert.True(page.Form.FormButton.IsExists());

        // Resolved element-relative, not page-wide.
        root.Verify(e => e.FindElement(
            It.Is<Locator>(l => l.Value == "FormButton"), It.IsAny<int>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// A container must not fall back to the parent scope when a child is missing.
    /// </summary>
    [Fact]
    [Trait("Pattern", "NoParentFallback")]
    public void Container_DoesNotFallBackToParent()
    {
        var page = new TestPage(_context.Object);
        var root = SetupRootElement("Form");

        // The child is NOT under the container root...
        root.Setup(e => e.FindElement(It.IsAny<Locator>(), It.IsAny<int>()))
            .Throws(new ElementNotFoundException("not in container"));

        // ...but it does exist at page level.
        var pageWide = new Mock<IMauiElement>();
        pageWide.Setup(e => e.Visible).Returns(true);
        _context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Elsewhere")))
            .Returns(pageWide.Object);

        var control = new Label<FormContainer>(page.Form, "Elsewhere");

        Assert.False(control.IsExists());
    }

    [Fact]
    [Trait("Pattern", "NestedScoping")]
    public void NestedContainer_ResolvesThroughBothRoots()
    {
        var page = new TestPage(_context.Object);
        var outerRoot = SetupRootElement("Form");
        var innerRoot = SetupChild(outerRoot, "Options");
        SetupChild(innerRoot, "OptionsCheckBox");

        Assert.True(page.Form.Options.OptionsCheckBox.IsExists());
    }

    #endregion

    #region Fluent return type

    /// <summary>
    /// The headline fix for design 3.1.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentReturn")]
    public void ControlInsideContainer_ReturnsContainer()
    {
        var page = new TestPage(_context.Object);
        var root = SetupRootElement("Form");
        SetupChild(root, "FormButton");

        FormContainer result = page.Form.FormButton.Click();

        Assert.Same(page.Form, result);
    }

    [Fact]
    [Trait("Pattern", "FluentReturn")]
    public void ContainerAssertion_ReturnsContainer()
    {
        var page = new TestPage(_context.Object);
        SetupRootElement("Form");

        FormContainer result = page.Form.AssertExists(true);

        Assert.Same(page.Form, result);
    }

    [Fact]
    [Trait("Property", "Parent")]
    public void Parent_ReturnsPage()
    {
        var page = new TestPage(_context.Object);
        SetupRootElement("Form");

        Assert.Same(page, page.Form.Parent);
    }

    [Fact]
    [Trait("Property", "Parent")]
    public void Parent_ChainsUpFromNestedContainer()
    {
        var page = new TestPage(_context.Object);
        var outerRoot = SetupRootElement("Form");
        SetupChild(outerRoot, "Options");

        Assert.Same(page.Form, page.Form.Options.Parent);
        Assert.Same(page, page.Form.Options.Parent.Parent);
    }

    #endregion

    #region Container readiness and caching

    /// <summary>
    /// Design 8.3: readiness is parent-ready, then root-exists, then WaitContentReadyCore.
    /// The default hook returns true, so overriding nothing preserves today's behaviour.
    /// </summary>
    [Fact]
    [Trait("Method", "WaitContentReady")]
    public void WaitContentReady_DefaultsToTrue()
    {
        var page = new TestPage(_context.Object);
        SetupRootElement("Form");

        Assert.True(page.Form.WaitContentReady());
        Assert.True(page.Form.IsReady());
    }

    [Fact]
    [Trait("Method", "IsReady")]
    public void IsReady_FalseWhenRootMissing()
    {
        var page = new TestPage(_context.Object);
        _context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns((IMauiElement?)null);

        Assert.False(page.Form.IsReady());
    }

    /// <summary>
    /// The root is cached, so repeated child lookups do not re-find it.
    /// </summary>
    [Fact]
    [Trait("Pattern", "RootCaching")]
    public void ContainerRoot_IsCachedAcrossLookups()
    {
        var page = new TestPage(_context.Object);
        var root = SetupRootElement("Form");
        SetupChild(root, "FormButton");
        var form = page.Form;

        _ = form.FormButton.IsExists();
        _ = form.FormButton.IsExists();
        _ = form.FormButton.IsExists();

        _context.Verify(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Form")), Times.Once);
    }

    [Fact]
    [Trait("Method", "InvalidateCache")]
    public void InvalidateCache_ForcesRootRefind()
    {
        var page = new TestPage(_context.Object);
        var root = SetupRootElement("Form");
        SetupChild(root, "FormButton");
        var form = page.Form;

        _ = form.FormButton.IsExists();
        form.InvalidateCache();
        _ = form.FormButton.IsExists();

        _context.Verify(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Form")), Times.Exactly(2));
    }

    /// <summary>
    /// A stale root is re-found once rather than surfacing the exception.
    /// </summary>
    [Fact]
    [Trait("Pattern", "StaleRecovery")]
    public void StaleRoot_IsRecoveredOnce()
    {
        var page = new TestPage(_context.Object);
        var stale = new Mock<IMauiElement>();
        stale.Setup(e => e.TagName).Throws(new StaleElementReferenceException("stale"));
        stale.Setup(e => e.FindElement(It.IsAny<Locator>(), It.IsAny<int>()))
             .Throws(new StaleElementReferenceException("stale"));

        var fresh = new Mock<IMauiElement>();
        fresh.Setup(e => e.Visible).Returns(true);
        fresh.Setup(e => e.TagName).Returns("Grid");
        SetupChild(fresh, "FormButton");

        _context.SetupSequence(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Form")))
            .Returns(stale.Object)
            .Returns(fresh.Object);

        var form = page.Form;
        _ = form.FormButton.IsExists();

        Assert.True(form.FormButton.IsExists());
    }

    #endregion

    #region Collection item scoping

    /// <summary>
    /// The core requirement: each row resolves its controls within its own root, so the
    /// same AutomationId on every row still yields distinct values.
    /// </summary>
    [Fact]
    [Trait("Pattern", "ItemScoping")]
    public void Rows_WithRepeatingIds_ResolveIndependently()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot",
            ("Alpha", "1.00"),
            ("Beta", "2.00"));

        Assert.Equal("Alpha", page.Rows.Item(0).Name.GetText());
        Assert.Equal("Beta", page.Rows.Item(1).Name.GetText());
    }

    [Fact]
    [Trait("Pattern", "ItemScoping")]
    public void Row_ResolvesThroughItsOwnRoot_NotThePage()
    {
        var page = new TestPage(_context.Object);
        var roots = SetupCollection("Rows", "RowRoot", ("Alpha", "1.00"));

        _ = page.Rows.Item(0).Name.GetText();

        roots[0].Verify(e => e.FindElement(
            It.Is<Locator>(l => l.Value == "RowName"), It.IsAny<int>()), Times.AtLeastOnce);
        _context.Verify(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "RowName")), Times.Never);
    }

    [Fact]
    [Trait("Property", "Index")]
    public void Row_TracksItsIndex()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1.00"), ("Beta", "2.00"));

        Assert.Equal(0, page.Rows.Item(0).Index);
        Assert.Equal(1, page.Rows.Item(1).Index);
    }

    #endregion

    #region Collection counting and retrieval

    /// <summary>
    /// Count comes from one enumeration of the item strategy, not N sequential probes,
    /// and is not capped at 100.
    /// </summary>
    [Fact]
    [Trait("Method", "GetItemCount")]
    public void GetItemCount_EnumeratesInOneCall()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("A", "1"), ("B", "2"), ("C", "3"));

        Assert.Equal(3, page.Rows.GetItemCount());
    }

    /// <summary>
    /// Items is lazy (design 8.2): enumerating with First stops at the match rather
    /// than materializing every row.
    /// </summary>
    [Fact]
    [Trait("Property", "Items")]
    public void Items_IsLazy_StopsAtFirstMatch()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1"), ("Beta", "2"), ("Gamma", "3"));

        var first = page.Rows.Items.First(r => r.Name.GetText() == "Alpha");

        Assert.Equal(0, first.Index);
    }

    [Fact]
    [Trait("Method", "ToList")]
    public void ToList_MaterializesEveryRow()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1"), ("Beta", "2"));

        Assert.Equal(2, page.Rows.ToList().Count);
    }

    [Fact]
    [Trait("Method", "GetItemCount")]
    public void GetItemCount_NotCappedAt100()
    {
        var page = new TestPage(_context.Object);
        var rows = Enumerable.Range(0, 150).Select(i => ($"Item{i}", "1")).ToArray();
        SetupCollection("Rows", "RowRoot", rows);

        Assert.Equal(150, page.Rows.GetItemCount());
    }

    [Fact]
    [Trait("Method", "Indexer")]
    public void Indexer_AndItem_AreEquivalent()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1"), ("Beta", "2"));

        Assert.Equal(page.Rows.Item(1).Index, page.Rows[1].Index);
    }

    [Fact]
    [Trait("Method", "TryItem")]
    public void TryItem_OutOfRange_ReturnsNull()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1"));

        Assert.Null(page.Rows.TryItem(5));
    }

    [Fact]
    [Trait("Method", "Item")]
    public void Item_OutOfRange_Throws()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1"));

        Assert.Throws<ElementNotFoundException>(() => page.Rows.Item(5));
    }

    [Fact]
    [Trait("Method", "Item")]
    public void Item_NegativeIndex_Throws()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1"));

        Assert.Throws<ArgumentOutOfRangeException>(() => page.Rows.Item(-1));
    }

    [Fact]
    [Trait("Method", "IsEmpty")]
    public void IsEmpty_TrueWhenNoItems()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot");

        Assert.True(page.Rows.IsEmpty());
        Assert.Equal(0, page.Rows.GetItemCount());
    }

    #endregion

    #region Collection search

    [Fact]
    [Trait("Method", "FindItem")]
    public void FindItem_ReturnsMatchingRow()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1"), ("Beta", "2"));

        var row = page.Rows.FindItem(r => r.Name.GetText() == "Beta");

        Assert.NotNull(row);
        Assert.Equal(1, row!.Index);
    }

    [Fact]
    [Trait("Method", "FindItem")]
    public void FindItem_ReturnsNullWhenNoMatch()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1"));

        Assert.Null(page.Rows.FindItem(r => r.Name.GetText() == "Zeta"));
    }

    [Fact]
    [Trait("Method", "ItemWhere")]
    public void ItemWhere_ThrowsWhenNoMatch()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1"));

        Assert.Throws<ElementNotFoundException>(
            () => page.Rows.ItemWhere(r => r.Name.GetText() == "Zeta"));
    }

    #endregion

    #region Collection as a scope

    /// <summary>
    /// A collection is a container, so it resolves its own non-item controls.
    /// </summary>
    [Fact]
    [Trait("Pattern", "CollectionIsScope")]
    public void Collection_ResolvesItsOwnControls()
    {
        var page = new TestPage(_context.Object);
        var root = SetupCollectionRoot("Rows");
        SetupChild(root, "RowsEmptyLabel");

        Assert.True(page.Rows.EmptyLabel.IsExists());
    }

    [Fact]
    [Trait("Pattern", "FluentReturn")]
    public void CollectionAssertion_ReturnsCollection()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1"));

        RowCollection result = page.Rows.AssertItemCount(1);

        Assert.Same(page.Rows, result);
    }

    [Fact]
    [Trait("Pattern", "FluentReturn")]
    public void RowAction_ReturnsRow_ParentReturnsCollection()
    {
        var page = new TestPage(_context.Object);
        SetupCollection("Rows", "RowRoot", ("Alpha", "1"));
        var row = page.Rows.Item(0);

        RowContainer afterClick = row.Delete.Click();

        Assert.Same(row, afterClick);
        Assert.Same(page.Rows, afterClick.Parent);
        Assert.Same(page, afterClick.Parent.Parent);
    }

    #endregion

    // =================================================================================
    // Mock helpers
    // =================================================================================

    #region Helpers

    /// <summary>Creates a page-level element and registers it with the context.</summary>
    private Mock<IMauiElement> SetupRootElement(string automationId)
    {
        var element = new Mock<IMauiElement>();
        element.Setup(e => e.Visible).Returns(true);
        element.Setup(e => e.Enabled).Returns(true);
        element.Setup(e => e.TagName).Returns("Grid");
        element.Setup(e => e.Rect).Returns(new System.Drawing.Rectangle(0, 0, 300, 200));

        _context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == automationId)))
            .Returns(element.Object);
        _context.Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == automationId)))
            .Returns(element.Object);

        return element;
    }

    /// <summary>Attaches a child element beneath a parent element.</summary>
    private static Mock<IMauiElement> SetupChild(
        Mock<IMauiElement> parent, string automationId, string? text = null)
    {
        var child = new Mock<IMauiElement>();
        child.Setup(e => e.Visible).Returns(true);
        child.Setup(e => e.Enabled).Returns(true);
        child.Setup(e => e.TagName).Returns("Element");
        child.Setup(e => e.Rect).Returns(new System.Drawing.Rectangle(0, 0, 100, 30));
        if (text != null)
        {
            child.Setup(e => e.Text).Returns(text);
        }

        parent.Setup(e => e.FindElement(
                It.Is<Locator>(l => l.Value == automationId), It.IsAny<int>()))
            .Returns(child.Object);
        parent.Setup(e => e.FindElements(
                It.Is<Locator>(l => l.Value == automationId), It.IsAny<int>()))
            .Returns(new[] { child.Object });

        IMauiElement? outElement = child.Object;
        parent.Setup(e => e.TryFindElement(
                It.Is<Locator>(l => l.Value == automationId), out outElement, It.IsAny<int>()))
            .Returns(true);

        return child;
    }

    private Mock<IMauiElement> SetupCollectionRoot(string automationId)
    {
        var root = SetupRootElement(automationId);
        root.Setup(e => e.FindElements(It.IsAny<Locator>(), It.IsAny<int>()))
            .Returns(Array.Empty<IMauiElement>());
        return root;
    }

    /// <summary>
    /// Builds a collection root whose item-locator lookup returns one root per row,
    /// each carrying a RowName / RowPrice / RowDeleteButton child.
    /// Every row deliberately uses the SAME child AutomationIds.
    /// </summary>
    private List<Mock<IMauiElement>> SetupCollection(
        string collectionId, string itemLocatorValue, params (string Name, string Price)[] rows)
    {
        var root = SetupRootElement(collectionId);
        var rowMocks = new List<Mock<IMauiElement>>();

        foreach (var (name, price) in rows)
        {
            var rowRoot = new Mock<IMauiElement>();
            rowRoot.Setup(e => e.Visible).Returns(true);
            rowRoot.Setup(e => e.Enabled).Returns(true);
            rowRoot.Setup(e => e.TagName).Returns("Grid");
            rowRoot.Setup(e => e.Rect).Returns(new System.Drawing.Rectangle(0, 0, 300, 40));

            SetupChild(rowRoot, "RowName", name);
            SetupChild(rowRoot, "RowPrice", price);
            SetupChild(rowRoot, "RowDeleteButton");

            rowMocks.Add(rowRoot);
        }

        root.Setup(e => e.FindElements(
                It.Is<Locator>(l => l.Value == itemLocatorValue), It.IsAny<int>()))
            .Returns(rowMocks.Select(m => m.Object).ToArray());

        return rowMocks;
    }

    #endregion

    // =================================================================================
    // Test page objects
    // =================================================================================

    #region Proposed-base page objects

    private class TestPage : PageObjectBase<TestPage>
    {
        public TestPage(IMauiTestContext context) : base(context)
        {
            Form = new FormContainer(this, "Form");
            Rows = new RowCollection(this, "Rows");
        }

        public override string Name => "TestPage";
        public override bool IsLoaded(int? timeoutMs = null) => true;

        public FormContainer Form { get; }
        public RowCollection Rows { get; }
    }

    private class FormContainer : ContainerObjectBase<TestPage, FormContainer>
    {
        public FormContainer(IMauiScope<TestPage> scope, string automationId)
            : base(scope, automationId) { }

        public Button<FormContainer> FormButton => new(this, "FormButton");
        public OptionsContainer Options => new(this, "Options");
    }

    private class OptionsContainer : ContainerObjectBase<FormContainer, OptionsContainer>
    {
        public OptionsContainer(IMauiScope<FormContainer> scope, string automationId)
            : base(scope, automationId) { }

        public CheckBox<OptionsContainer> OptionsCheckBox => new(this, "OptionsCheckBox");
    }

    private class RowCollection : CollectionObjectBase<TestPage, RowCollection, RowContainer>
    {
        public RowCollection(IMauiScope<TestPage> scope, string automationId)
            : base(scope,
                   automationId,
                   ItemStrategy.ByLocator(Locator.ByAutomationId("RowRoot")),
                   (collection, itemRoot, index) => new RowContainer(collection, itemRoot, index)) { }

        public Label<RowCollection> EmptyLabel => new(this, "RowsEmptyLabel");
    }

    private class RowContainer : ItemContainerBase<RowCollection, RowContainer>
    {
        public RowContainer(RowCollection collection, IMauiElement itemRoot, int index)
            : base(collection, itemRoot, index) { }

        public Label<RowContainer> Name => new(this, "RowName");
        public Label<RowContainer> Price => new(this, "RowPrice");
        public Button<RowContainer> Delete => new(this, "RowDeleteButton");
    }

    #endregion

    #region Legacy-base page objects (group [A] only)

    private class LegacyTestPage : PageObjectBase<LegacyTestPage>
    {
        public LegacyTestPage(IMauiTestContext context) : base(context)
        {
            Container = new LegacyContainer(this, Locator.ByAutomationId("Container"));
            Rows = new Brinell.Maui.Controls.List<LegacyTestPage, LegacyRow>(
                this,
                "Rows",
                "Row_",
                (scope, index) => new LegacyRow(this, index));
        }

        public override string Name => "LegacyTestPage";
        public override bool IsLoaded(int? timeoutMs = null) => true;

        public LegacyContainer Container { get; }
        public Brinell.Maui.Controls.List<LegacyTestPage, LegacyRow> Rows { get; }
    }

    private class LegacyContainer : ContainerBase<LegacyTestPage, LegacyContainer>
    {
        public LegacyContainer(IMauiScope<LegacyTestPage> scope, Locator locator)
            : base(scope, locator) { }

        public Button<LegacyContainer> ContainerButton => new(this, "ContainerButton");
    }

    private class LegacyRow : ContainerBase<LegacyTestPage, LegacyRow>
    {
        public LegacyRow(IMauiScope<LegacyTestPage> scope, int index)
            : base(scope, Locator.ByAutomationId($"Row_{index}")) { }

        public Label<LegacyRow> RowLabel => new(this, "RowLabel");
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

    /// <summary>A row located by a REPEATING id, as a normal item template produces.</summary>
    private class RepeatingRow : ContainerBase<RepeatingIdPage, RepeatingRow>
    {
        public RepeatingRow(IMauiScope<RepeatingIdPage> scope, int index)
            : base(scope, Locator.ByAutomationId("RowRoot")) { }

        public Label<RepeatingRow> RowLabel => new(this, "RowLabel");
    }

    #endregion
}

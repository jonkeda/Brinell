using Brinell.Core.Exceptions;
using Brinell.Core.Scopes;
using Brinell.Html.Collections;
using Brinell.Html.Controls.Buttons;

namespace Brinell.Html.Tests;

/// <summary>Rows stand for their items (design 7.6, plan step 7).</summary>
public class CollectionTests
{
    private readonly Mock<IHtmlTestContext> _context = new();
    private readonly Mock<IHtmlElement> _list = new();
    private readonly List<IHtmlElement> _rows = [];

    public CollectionTests()
    {
        _context.Setup(c => c.Timeouts).Returns(new TimeoutSettings { DefaultWait = 1000, PollingInterval = 1 });
        _context.Setup(c => c.Logger).Returns(Mock.Of<ITestLogger>());
        _context.Setup(c => c.Context).Returns(() => _context.Object);
        _context.Setup(c => c.IsIdle()).Returns(true);
        _context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "List"))).Returns(_list.Object);
        _list.Setup(l => l.FindElements(It.Is<Locator>(l => l.Value == "Row"))).Returns(() => _rows.ToArray());
    }

    private sealed class Page(IHtmlTestContext context) : HtmlPageObjectBase<Page>(context)
    {
        public Rows List => new(this);
    }

    private sealed class Rows(IHtmlScope<Page> page)
        : CollectionObjectBase<Page, Rows, Row>(page, Locator.ByAutomationId("List"), Locator.ByAutomationId("Row"))
    {
        public override Rows Self => this;

        protected override Row CreateItem(ItemKey key, int position) => new(this, key, position);
    }

    private sealed class Row(IHtmlCollection rows, ItemKey key, int position) : ItemObjectBase<Row>(rows, key, position)
    {
        public override Row Self => this;

        public ButtonControl<Row> Delete => new(this, Locator.ByAutomationId("Delete"));
    }

    private static (Mock<IHtmlElement> Row, Mock<IHtmlElement> Delete) RowFor(string? key)
    {
        var row = new Mock<IHtmlElement>();
        var delete = new Mock<IHtmlElement>();
        row.Setup(r => r.GetAttribute(ItemKey.Attribute)).Returns(key);
        row.Setup(r => r.TryFindElement(It.Is<Locator>(l => l.Value == "Delete"))).Returns(delete.Object);
        delete.Setup(d => d.Visible).Returns(true);
        delete.Setup(d => d.Enabled).Returns(true);
        return (row, delete);
    }

    [Fact]
    public void ARow_ActsOnItsOwnItem_AfterTheListIsReordered()
    {
        var a = RowFor("a");
        var b = RowFor("b");
        _rows.AddRange([a.Row.Object, b.Row.Object]);
        var row = new Page(_context.Object).List.Item(0);

        _rows.Reverse();
        row.Delete.Click();

        a.Delete.Verify(d => d.Click(), Times.Once);
        b.Delete.Verify(d => d.Click(), Times.Never);
    }

    [Fact]
    public void ARowWhoseItemIsGone_FailsAsItemChanged_AndActsOnNothing()
    {
        var a = RowFor("a");
        var b = RowFor("b");
        _rows.AddRange([a.Row.Object, b.Row.Object]);
        var row = new Page(_context.Object).List.Item(0);

        _rows.RemoveAt(0);
        var error = Record.Exception(() => row.Delete.Click(timeoutMs: 200));

        var notReady = Assert.IsType<ScopeNotReadyException>(error);
        Assert.Equal(ScopeReadinessState.ItemChanged, notReady.Readiness.State);
        a.Delete.Verify(d => d.Click(), Times.Never);
        b.Delete.Verify(d => d.Click(), Times.Never);
    }

    [Fact]
    public void RowsWithoutAKey_AreKeptByPosition()
    {
        _rows.Add(RowFor(null).Row.Object);

        Assert.Equal(ItemKeyKind.Position, new Page(_context.Object).List.Item(0).Key.Kind);
    }

    [Fact]
    public void Item_WaitsForTheRow_TryItem_AnswersNow()
    {
        var list = new Page(_context.Object).List;
        Assert.Null(list.TryItem(0));

        var lookups = 0;
        _list.Setup(l => l.FindElements(It.Is<Locator>(l => l.Value == "Row")))
            .Returns(() => ++lookups < 3 ? [] : [RowFor("late").Row.Object]);

        Assert.Equal("late", list.Item(0).Key.Value);
    }

    [Fact]
    public void Item_Fails_WhenTheRowNeverComes()
    {
        Assert.Throws<ElementNotFoundException>(() => new Page(_context.Object).List.Item(5, timeoutMs: 100));
    }
}

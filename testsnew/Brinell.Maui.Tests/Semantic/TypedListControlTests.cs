namespace Brinell.Maui.Tests.Semantic;

public class TypedListControlTests : SemanticControlTestsBase
{
    /// <summary>
    /// Selecting an item uses the row's own selection pattern rather than clicking it.
    /// </summary>
    /// <remarks>
    /// The row is reached through the collection root, not page-wide: the collection
    /// resolves "TestList" from the page, then finds rows <i>within</i> that element.
    /// Rows share one control type and carry no indexed ids, so this only works if item
    /// scoping is real — which is the point of the assertion.
    /// </remarks>
    [Fact]
    public void TypedList_SelectItem_UsesRowSelectionPattern()
    {
        var row = CreateSelectableElement("ListItem", 40, 40, 220, 50);
        var listRoot = CreateElement("TestList", 0, 0, 300, 400);

        // The collection root is found on the page...
        Context
            .Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == "TestList")))
            .Returns(listRoot.Object);
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestList")))
            .Returns(listRoot.Object);

        // ...and rows are found within it, never through the page.
        listRoot
            .Setup(e => e.FindElements(
                It.Is<Locator>(l => l.Strategy == LocatorStrategy.ControlType
                                    && l.Value == "ListItem"),
                It.IsAny<int>()))
            .Returns(new[] { row.Object });

        var result = Page.TypedList.TrySelectItem(0);

        Assert.True(result);
        row.As<ISelectionItemPatternElement>().Verify(e => e.SelectItemPattern(), Times.Once);
        row.Verify(e => e.Click(), Times.Never);
    }

    /// <summary>
    /// Rows are not searched for page-wide.
    /// </summary>
    /// <remarks>
    /// A regression guard on the behaviour the old <c>List&lt;&gt;</c> had: it resolved
    /// items from the page scope, so a matching element outside the collection would be
    /// picked up. Here the page offers no rows at all and the collection must still find
    /// its own.
    /// </remarks>
    [Fact]
    public void TypedList_DoesNotResolveRowsThroughThePage()
    {
        var row = CreateSelectableElement("ListItem", 40, 40, 220, 50);
        var listRoot = CreateElement("TestList", 0, 0, 300, 400);

        Context
            .Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == "TestList")))
            .Returns(listRoot.Object);
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestList")))
            .Returns(listRoot.Object);

        listRoot
            .Setup(e => e.FindElements(
                It.Is<Locator>(l => l.Value == "ListItem"), It.IsAny<int>()))
            .Returns(new[] { row.Object });

        Assert.Equal(1, Page.TypedList.GetItemCount());

        // The page was never asked for rows.
        Context.Verify(
            c => c.FindElements(It.Is<Locator>(l => l.Value == "ListItem")),
            Times.Never);
    }
}

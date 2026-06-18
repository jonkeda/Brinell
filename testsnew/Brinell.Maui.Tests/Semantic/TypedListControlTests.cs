namespace Brinell.Maui.Tests.Semantic;

public class TypedListControlTests : SemanticControlTestsBase
{
    [Fact]
    public void TypedList_SelectItem_UsesIndexedContainerRoot()
    {
        var child = CreateElement("Item_0", 50, 50, 80, 20);
        var row = CreateSelectableElement("ListItem", 40, 40, 220, 50);

        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Item_0")))
            .Returns(child.Object);
        Context
            .Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == "Item_0")))
            .Returns(child.Object);
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "ListItem")))
            .Returns(new[] { row.Object });

        var result = Page.TypedList.TrySelectItem(0);

        Assert.True(result);
        row.As<ISelectionItemPatternElement>().Verify(e => e.SelectItemPattern(), Times.Once);
        child.Verify(e => e.Click(), Times.Never);
    }
}

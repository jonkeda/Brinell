namespace Brinell.Maui.Tests.Semantic;

public class SelectionListControlTests : SemanticControlTestsBase
{
    [Fact]
    public void SelectionList_SelectByAutomationId_UsesContainingListItemPattern()
    {
        var child = CreateElement("EquipmentSelection_Item_2001", 50, 50, 80, 20);
        var row = CreateSelectableElement("ListItem", 40, 40, 220, 50);

        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "EquipmentSelection_Item_2001")))
            .Returns(new[] { child.Object });
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "ListItem")))
            .Returns(new[] { row.Object });

        var result = Page.List.TrySelectByAutomationId("EquipmentSelection_Item_2001");

        Assert.True(result);
        row.As<ISelectionItemPatternElement>().Verify(e => e.SelectItemPattern(), Times.Once);
        child.Verify(e => e.Click(), Times.Never);
    }
}

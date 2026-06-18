namespace Brinell.Maui.Tests.Semantic;

public class TabMenuControlTests : SemanticControlTestsBase
{
    [Fact]
    public void TabMenu_Select_UsesMatchingInvokableButtonByCaptionIndex()
    {
        var projectsCaption = CreateElement("TabMenuView_Caption", 10, 10, 60, 20);
        projectsCaption.Setup(e => e.Text).Returns("Projects");
        var projectsButton = CreateInvokableElement("TabMenuView_Button", 0, 0, 80, 60);

        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "TabMenuView_Caption")))
            .Returns(new[] { projectsCaption.Object });
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "TabMenuView_Button")))
            .Returns(new[] { projectsButton.Object });
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "TabMenuView_Grid")))
            .Returns(Array.Empty<IMauiElement>());

        var result = Page.Tabs.TrySelect("Projects");

        Assert.True(result);
        projectsButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        projectsCaption.Verify(e => e.Click(), Times.Never);
    }
}

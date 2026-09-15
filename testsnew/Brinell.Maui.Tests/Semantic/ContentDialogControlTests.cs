namespace Brinell.Maui.Tests.Semantic;

public class ContentDialogControlTests : SemanticControlTestsBase
{
    private readonly Mock<IMauiElement> _app = new();

    public ContentDialogControlTests()
    {
        Context.Setup(c => c.AppElement).Returns(_app.Object);
    }

    [Fact]
    public void ContentDialog_DialogButton_UsesActiveDialogRoot()
    {
        var dialogRoot = CreateElement("ContentDialog", 0, 0, 300, 200);
        var deleteButton = CreateInvokableElement("DialogDelete", 10, 150, 80, 40);
        dialogRoot
            .Setup(e => e.FindElement(
                It.Is<Locator>(l => l.Strategy == LocatorStrategy.Name && l.Value == "Delete"), 0))
            .Returns(deleteButton.Object);
        _app
            .Setup(a => a.TryFindActiveDialog())
            .Returns(dialogRoot.Object);

        var exists = Page.Dialog.DialogButton("Delete").IsExists();

        Assert.True(exists);
        dialogRoot.Verify(e => e.FindElement(It.IsAny<Locator>(), 0), Times.Once);
        Context.Verify(c => c.TryFindElement(It.IsAny<Locator>()), Times.Never);
    }

    [Fact]
    public void ContentDialog_DialogButton_DoesNotFallBackToParentScope()
    {
        var dialogRoot = CreateElement("ContentDialog", 0, 0, 300, 200);
        dialogRoot
            .Setup(e => e.FindElement(It.IsAny<Locator>(), 0))
            .Throws(new ElementNotFoundException("not in dialog"));
        _app
            .Setup(a => a.TryFindActiveDialog())
            .Returns(dialogRoot.Object);
        Context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns(CreateElement("Delete", 0, 0, 80, 40).Object);

        var exists = Page.Dialog.DialogButton("Delete").IsExists();

        Assert.False(exists);
        Context.Verify(c => c.TryFindElement(It.IsAny<Locator>()), Times.Never);
    }

    [Fact]
    public void ContentDialog_PromptInput_UsesActiveDialogRoot()
    {
        var dialogRoot = CreateElement("ContentDialog", 0, 0, 300, 200);
        var promptInput = CreateElement("PromptInput", 20, 80, 260, 40);
        dialogRoot
            .Setup(e => e.FindElement(
                It.Is<Locator>(l => l.Strategy == LocatorStrategy.ControlType && l.Value == "entry"), 0))
            .Returns(promptInput.Object);
        _app
            .Setup(a => a.TryFindActiveDialog())
            .Returns(dialogRoot.Object);

        var exists = Page.Dialog.PromptInput.IsExists();

        Assert.True(exists);
        dialogRoot.Verify(e => e.FindElement(It.IsAny<Locator>(), 0), Times.Once);
    }

    [Fact]
    public void ContentDialog_ButtonClick_ReturnsDialogForDismissalWait()
    {
        var dismissed = false;
        var dialogRoot = CreateElement("ContentDialog", 0, 0, 300, 200);
        var okButton = CreateInvokableElement("DialogOk", 10, 150, 80, 40);
        okButton.Setup(e => e.Invoke()).Callback(() => dismissed = true);

        dialogRoot.Setup(e => e.TagName).Returns("ContentDialog");
        dialogRoot.Setup(e => e.FindElement(It.IsAny<Locator>(), 0)).Returns(okButton.Object);
        _app
            .Setup(a => a.TryFindActiveDialog())
            .Returns(() => dismissed ? null : dialogRoot.Object);

        var dismissedResult = Page.Dialog
            .DialogButton("OK")
            .Click(timeoutMs: 100)
            .WaitExists(false, timeoutMs: 100);

        Assert.True(dismissedResult);
        Assert.True(dismissed);
    }
}

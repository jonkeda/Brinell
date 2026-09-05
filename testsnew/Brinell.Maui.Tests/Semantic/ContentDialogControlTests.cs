namespace Brinell.Maui.Tests.Semantic;

public class ContentDialogControlTests : SemanticControlTestsBase
{
    [Fact]
    public void ContentDialog_DialogButton_UsesActiveDialogRoot()
    {
        var driver = new Mock<IMauiDriver>();
        var dialogRoot = CreateElement("ContentDialog", 0, 0, 300, 200);
        var deleteButton = CreateLegacyAccessibleElement("DialogDelete", 10, 150, 80, 40);
        dialogRoot
            .Setup(e => e.FindElement(
                It.Is<Locator>(l => l.Strategy == LocatorStrategy.Name && l.Value == "Delete"), 0))
            .Returns(deleteButton.Object);
        driver
            .Setup(d => d.TryFindActiveDialogRoot())
            .Returns(dialogRoot.Object);
        Context.Setup(c => c.Driver).Returns(driver.Object);

        var exists = Page.Dialog.DialogButton("Delete").IsExists();

        Assert.True(exists);
        dialogRoot.Verify(e => e.FindElement(It.IsAny<Locator>(), 0), Times.Once);
        Context.Verify(c => c.TryFindElement(It.IsAny<Locator>()), Times.Never);
    }

    [Fact]
    public void ContentDialog_DialogButton_DoesNotFallBackToParentScope()
    {
        var driver = new Mock<IMauiDriver>();
        var dialogRoot = CreateElement("ContentDialog", 0, 0, 300, 200);
        dialogRoot
            .Setup(e => e.FindElement(It.IsAny<Locator>(), 0))
            .Throws(new ElementNotFoundException("not in dialog"));
        driver
            .Setup(d => d.TryFindActiveDialogRoot())
            .Returns(dialogRoot.Object);
        Context.Setup(c => c.Driver).Returns(driver.Object);
        Context.Setup(c => c.TryFindElement(It.IsAny<Locator>())).Returns(CreateElement("Delete", 0, 0, 80, 40).Object);

        var exists = Page.Dialog.DialogButton("Delete").IsExists();

        Assert.False(exists);
        Context.Verify(c => c.TryFindElement(It.IsAny<Locator>()), Times.Never);
    }

    [Fact]
    public void ContentDialog_PromptInput_UsesActiveDialogRoot()
    {
        var driver = new Mock<IMauiDriver>();
        var dialogRoot = CreateElement("ContentDialog", 0, 0, 300, 200);
        var promptInput = CreateElement("PromptInput", 20, 80, 260, 40);
        dialogRoot
            .Setup(e => e.FindElement(
                It.Is<Locator>(l => l.Strategy == LocatorStrategy.ControlType && l.Value == "entry"), 0))
            .Returns(promptInput.Object);
        driver
            .Setup(d => d.TryFindActiveDialogRoot())
            .Returns(dialogRoot.Object);
        Context.Setup(c => c.Driver).Returns(driver.Object);

        var exists = Page.Dialog.PromptInput.IsExists();

        Assert.True(exists);
        dialogRoot.Verify(e => e.FindElement(It.IsAny<Locator>(), 0), Times.Once);
    }

    [Fact]
    public void ContentDialog_ButtonClick_ReturnsDialogForDismissalWait()
    {
        var dismissed = false;
        var driver = new Mock<IMauiDriver>();
        var dialogRoot = CreateElement("ContentDialog", 0, 0, 300, 200);
        var okButton = CreateInvokableElement("DialogOk", 10, 150, 80, 40);
        okButton.As<IInvokePatternElement>()
            .Setup(e => e.InvokePattern())
            .Callback(() => dismissed = true)
            .Returns(true);

        dialogRoot.Setup(e => e.TagName).Returns("ContentDialog");
        dialogRoot.Setup(e => e.FindElement(It.IsAny<Locator>(), 0)).Returns(okButton.Object);
        driver
            .Setup(d => d.TryFindActiveDialogRoot())
            .Returns(() => dismissed ? null : dialogRoot.Object);
        Context.Setup(c => c.Driver).Returns(driver.Object);

        var dismissedResult = Page.Dialog
            .DialogButton("OK")
            .Click(timeoutMs: 100)
            .WaitExists(false, timeoutMs: 100);

        Assert.True(dismissedResult);
        Assert.True(dismissed);
    }
}

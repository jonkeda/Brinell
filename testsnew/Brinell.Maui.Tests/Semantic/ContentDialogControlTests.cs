namespace Brinell.Maui.Tests.Semantic;

public class ContentDialogControlTests : SemanticControlTestsBase
{
    [Fact]
    public void ContentDialog_ClickButton_UsesPopupRootAndWaitsForDismissal()
    {
        var dismissed = false;
        var driver = new Mock<IMauiDriver>();
        var dialogRoot = CreateElement("ContentDialog", 0, 0, 300, 200);
        var deleteButton = CreateLegacyAccessibleElement("DialogDelete", 10, 150, 80, 40);
        deleteButton.As<ILegacyIAccessiblePatternElement>()
            .Setup(e => e.DoDefaultActionPattern())
            .Callback(() => dismissed = true)
            .Returns(true);

        dialogRoot
            .Setup(e => e.FindElement(It.IsAny<Locator>(), It.IsAny<int>()))
            .Returns(deleteButton.Object);

        IMauiElement? popupElement = dialogRoot.Object;
        driver
            .Setup(d => d.TryFindPopupElement(It.IsAny<Locator>(), out popupElement, It.IsAny<int>()))
            .Returns(false);
        driver
            .Setup(d => d.FindPopupElement(It.IsAny<Locator>(), It.IsAny<int>()))
            .Returns(dialogRoot.Object);
        Context.Setup(c => c.Driver).Returns(driver.Object);

        var result = Page.Dialog.TryClickButtonAndWaitDismissed("*!delete!*", timeoutMs: 100);

        Assert.True(result);
        Assert.True(dismissed);
        deleteButton.As<ILegacyIAccessiblePatternElement>().Verify(e => e.DoDefaultActionPattern(), Times.Once);
    }

    [Fact]
    public void ContentDialog_ClickButton_CanActivateButtonFoundDirectlyInPopupWindow()
    {
        var dismissed = false;
        var driver = new Mock<IMauiDriver>();
        var deleteButton = CreateLegacyAccessibleElement("DialogDelete", 10, 150, 80, 40);
        deleteButton.Setup(e => e.Text).Returns("*!delete!*");
        deleteButton.As<ILegacyIAccessiblePatternElement>()
            .Setup(e => e.DoDefaultActionPattern())
            .Callback(() => dismissed = true)
            .Returns(true);

        IMauiElement? dialogRoot = null;
        IMauiElement? popupButton = deleteButton.Object;
        driver
            .Setup(d => d.TryFindPopupElement(It.Is<Locator>(l => l.Strategy == LocatorStrategy.ClassName), out dialogRoot, It.IsAny<int>()))
            .Returns(false);
        driver
            .Setup(d => d.TryFindPopupElement(It.Is<Locator>(l => l.Strategy == LocatorStrategy.Name && l.Value == "*!delete!*"), out popupButton, It.IsAny<int>()))
            .Returns(() => !dismissed);
        Context.Setup(c => c.Driver).Returns(driver.Object);

        var result = Page.Dialog.TryClickButtonAndWaitDismissed("*!delete!*", timeoutMs: 100);

        Assert.True(result);
        Assert.True(dismissed);
        deleteButton.As<ILegacyIAccessiblePatternElement>().Verify(e => e.DoDefaultActionPattern(), Times.Once);
    }

    [Fact]
    public void ContentDialog_ClickButton_MatchesPopupButtonTextCaseInsensitively()
    {
        var dismissed = false;
        var driver = new Mock<IMauiDriver>();
        var continueButton = CreateLegacyAccessibleElement("PrimaryButton", 10, 150, 80, 40);
        continueButton.Setup(e => e.Text).Returns("continue");
        continueButton.As<ILegacyIAccessiblePatternElement>()
            .Setup(e => e.DoDefaultActionPattern())
            .Callback(() => dismissed = true)
            .Returns(true);

        IMauiElement? dialogRoot = null;
        IMauiElement? popupButton = continueButton.Object;
        driver
            .Setup(d => d.TryFindPopupElement(It.Is<Locator>(l => l.Strategy == LocatorStrategy.ClassName), out dialogRoot, It.IsAny<int>()))
            .Returns(false);
        driver
            .Setup(d => d.TryFindPopupElement(It.Is<Locator>(l => l.Strategy == LocatorStrategy.ControlType && l.Value == "button"), out popupButton, It.IsAny<int>()))
            .Returns(() => !dismissed);
        Context.Setup(c => c.Driver).Returns(driver.Object);

        var result = Page.Dialog.TryClickButtonAndWaitDismissed("Continue", timeoutMs: 100);

        Assert.True(result);
        Assert.True(dismissed);
        continueButton.As<ILegacyIAccessiblePatternElement>().Verify(e => e.DoDefaultActionPattern(), Times.Once);
    }

    [Fact]
    public void ContentDialog_ClickButton_PrefersButtonControlOverMatchingTitleText()
    {
        var dismissed = false;
        var driver = new Mock<IMauiDriver>();
        var titleText = CreateElement("DialogTitle", 10, 10, 200, 30);
        titleText.Setup(e => e.Text).Returns("*!delete!*");
        var deleteButton = CreateLegacyAccessibleElement("DialogDelete", 10, 150, 80, 40);
        deleteButton.Setup(e => e.Text).Returns("*!delete!*");
        deleteButton.As<ILegacyIAccessiblePatternElement>()
            .Setup(e => e.DoDefaultActionPattern())
            .Callback(() => dismissed = true)
            .Returns(true);

        IMauiElement? popupElement = null;
        driver
            .Setup(d => d.TryFindPopupElement(It.IsAny<Locator>(), out popupElement, It.IsAny<int>()))
            .Returns(false);
        Context.Setup(c => c.Driver).Returns(driver.Object);
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Strategy == LocatorStrategy.ControlType && l.Value == "button")))
            .Returns(() => dismissed ? Array.Empty<IMauiElement>() : new[] { deleteButton.Object });
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Strategy == LocatorStrategy.Name && l.Value == "*!delete!*")))
            .Returns(titleText.Object);

        var result = Page.Dialog.TryClickButtonAndWaitDismissed("*!delete!*", timeoutMs: 100);

        Assert.True(result);
        Assert.True(dismissed);
        titleText.Verify(e => e.Click(), Times.Never);
        deleteButton.As<ILegacyIAccessiblePatternElement>().Verify(e => e.DoDefaultActionPattern(), Times.Once);
    }
}

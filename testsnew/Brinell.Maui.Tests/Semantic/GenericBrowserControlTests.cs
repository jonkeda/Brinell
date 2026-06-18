namespace Brinell.Maui.Tests.Semantic;

public class GenericBrowserControlTests : SemanticControlTestsBase
{
    [Fact]
    public void GenericBrowser_SelectItem_InvokesNativeItemButtonBeforeRowClick()
    {
        var selected = false;
        var child = CreateElement("GenericBrowserItem_801", 50, 50, 80, 20);
        var nativeButton = CreateInvokableElement("GenericBrowserItemButton_801", 40, 40, 220, 50);
        var row = CreateSelectableElement("ListItem", 40, 40, 220, 50);
        nativeButton.As<IInvokePatternElement>()
            .Setup(e => e.InvokePattern())
            .Callback(() => selected = true)
            .Returns(true);

        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItemButton_801")))
            .Returns(() => selected ? Array.Empty<IMauiElement>() : new[] { nativeButton.Object });
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItem_801")))
            .Returns(() => selected ? Array.Empty<IMauiElement>() : new[] { child.Object });
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "ListItem")))
            .Returns(new[] { row.Object });

        var result = Page.Browser.TrySelectItem("801");

        Assert.True(result);
        nativeButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        row.As<ISelectionItemPatternElement>().Verify(e => e.SelectItemPattern(), Times.Never);
        child.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void GenericBrowser_SelectItem_SanitizesCompositeIdentifiers()
    {
        var selected = false;
        var nativeButton = CreateInvokableElement("GenericBrowserItemButton_0_3555", 40, 40, 220, 50);
        nativeButton.As<IInvokePatternElement>()
            .Setup(e => e.InvokePattern())
            .Callback(() => selected = true)
            .Returns(true);

        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItemButton_0_3555")))
            .Returns(() => selected ? Array.Empty<IMauiElement>() : new[] { nativeButton.Object });
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItem_0_3555")))
            .Returns(() => selected ? Array.Empty<IMauiElement>() : new[] { CreateElement("GenericBrowserItem_0_3555", 50, 50, 80, 20).Object });

        var result = Page.Browser.TrySelectItem("0:3555");

        Assert.True(result);
        nativeButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
    }

    [Fact]
    public void GenericBrowser_SelectItem_DoesNotUseVisibleTextOutsideBrowser()
    {
        var pageLabel = CreateElement("HoursEdit_RowLabel", 40, 40, 220, 50);
        pageLabel.Setup(e => e.Text).Returns("Mock labour");
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.Name && l.Value == "Mock labour")))
            .Returns(new[] { pageLabel.Object });

        var result = Page.Browser.TrySelectItem("602", "Mock labour", timeoutMs: 1);

        Assert.False(result);
        pageLabel.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void GenericBrowser_SelectItem_UsesVisibleTextInsideBrowser()
    {
        var browserRoot = CreateElement("GenericBrowser", 0, 80, 560, 520);
        var browserLabel = CreateElement("GenericBrowser_Label", 40, 120, 220, 50);
        browserLabel.Setup(e => e.Text).Returns("Mock labour");
        browserRoot
            .Setup(e => e.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.Name && l.Value == "Mock labour"), It.IsAny<int>()))
            .Returns(new[] { browserLabel.Object });
        browserLabel
            .Setup(e => e.Click())
            .Verifiable();
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowser")))
            .Returns(new[] { browserRoot.Object });
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItem_602")))
            .Returns(Array.Empty<IMauiElement>());

        var result = Page.Browser.TrySelectItem("602", "Mock labour", timeoutMs: 1);

        Assert.True(result);
        browserLabel.Verify(e => e.Click(), Times.Once);
    }

    [Fact]
    public void GenericBrowser_ToggleItem_DoesNotWaitForDrawerToClose()
    {
        var nativeButton = CreateInvokableElement("GenericBrowserItemButton_0_3555", 40, 40, 220, 50);
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItemButton_0_3555")))
            .Returns(new[] { nativeButton.Object });
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItem_0_3555")))
            .Returns(new[] { CreateElement("GenericBrowserItem_0_3555", 50, 50, 80, 20).Object });

        var result = Page.Browser.TryToggleItem("0:3555");

        Assert.True(result);
        nativeButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
    }

    [Fact]
    public void GenericBrowser_Close_PrefersNativeCloseButtonAndWaitsForDismissal()
    {
        var closed = false;
        var browserRoot = CreateElement("GenericBrowser", 0, 80, 560, 520);
        var nativeClose = CreateInvokableElement("DrawerView_Cancel_NativeButton", 0, 0, 48, 48);
        var gestureClose = CreateElement("DrawerView_Cancel", 0, 0, 48, 48);
        nativeClose.As<IInvokePatternElement>()
            .Setup(e => e.InvokePattern())
            .Callback(() => closed = true)
            .Returns(true);

        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "DrawerView_Cancel_NativeButton")))
            .Returns(() => closed ? Array.Empty<IMauiElement>() : new[] { nativeClose.Object });
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "DrawerView_Cancel")))
            .Returns(() => closed ? Array.Empty<IMauiElement>() : new[] { gestureClose.Object });
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowser")))
            .Returns(() => closed ? Array.Empty<IMauiElement>() : new[] { browserRoot.Object });

        var result = Page.Browser.TryClose();

        Assert.True(result);
        nativeClose.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        gestureClose.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void GenericBrowser_SelectItem_SelectsContainingListItemPatternBeforeChildClick()
    {
        var selected = false;
        var child = CreateElement("GenericBrowserItem_801", 50, 50, 80, 20);
        var row = CreateSelectableElement("ListItem", 40, 40, 220, 50, () => selected = true);

        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItem_801")))
            .Returns(() => selected ? Array.Empty<IMauiElement>() : new[] { child.Object });
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "ListItem")))
            .Returns(new[] { row.Object });

        var result = Page.Browser.TrySelectItem("801");

        Assert.True(result);
        row.As<ISelectionItemPatternElement>().Verify(e => e.SelectItemPattern(), Times.Once);
        child.Verify(e => e.Click(), Times.Never);
    }
}

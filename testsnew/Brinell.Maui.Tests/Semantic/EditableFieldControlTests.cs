using Brinell.Maui.Configuration;

namespace Brinell.Maui.Tests.Semantic;

public class EditableFieldControlTests : SemanticControlTestsBase
{
    [Fact]
    public void EditableField_Open_ActivatesNativeButtonChild()
    {
        var fieldRoot = CreateElement("FieldRoot", 0, 0, 200, 40);
        var nativeButton = CreateInvokableElement("EditableFieldView_NativeButton", 10, 10, 80, 20);
        fieldRoot
            .Setup(e => e.FindElements(It.Is<Locator>(l => l.Value == "EditableFieldView_NativeButton"), It.IsAny<int>()))
            .Returns(new[] { nativeButton.Object });
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);

        var result = Page.TestField.TryOpen();

        Assert.True(result);
        nativeButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        fieldRoot.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void EditableField_Open_ActivatesTextEditorNativeButtonChild()
    {
        var fieldRoot = CreateElement("FieldRoot", 0, 0, 200, 120);
        var nativeButton = CreateInvokableElement("EditableFieldView_TextEditorNativeButton", 10, 10, 160, 100);
        fieldRoot
            .Setup(e => e.FindElements(It.Is<Locator>(l => l.Value == "EditableFieldView_TextEditorNativeButton"), It.IsAny<int>()))
            .Returns(new[] { nativeButton.Object });
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);

        var result = Page.TestField.TryOpen();

        Assert.True(result);
        nativeButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        fieldRoot.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void EditableField_SetText_SetsNestedTextEntry()
    {
        var fieldRoot = CreateElement("FieldRoot", 0, 0, 200, 40);
        var textEntry = CreateElement("EditableFieldView_TextEntry", 10, 10, 120, 20);
        fieldRoot
            .Setup(e => e.FindElements(It.Is<Locator>(l => l.Value == "EditableFieldView_TextEntry"), It.IsAny<int>()))
            .Returns(new[] { textEntry.Object });
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);

        var result = Page.TestField.TrySetText("42.5");

        Assert.True(result);
        textEntry.Verify(e => e.Clear(), Times.Once);
        textEntry.Verify(e => e.SendKeys("42.5", TextInputMethod.SetValue), Times.Once);
    }

    [Fact]
    public void EditableField_SetText_UsesTextEditorDrawerWhenInlineEntryIsMissing()
    {
        var drawerOpen = false;
        var drawerClosed = false;
        var fieldRoot = CreateElement("FieldRoot", 0, 0, 200, 40);
        fieldRoot
            .Setup(e => e.Click())
            .Callback(() => drawerOpen = true);
        var editor = CreateElement("TextEditor", 0, 50, 420, 300);
        var okButton = CreateInvokableElement("IconButton_btnIcon", 360, 0, 48, 48);
        okButton.As<IInvokePatternElement>()
            .Setup(e => e.InvokePattern())
            .Callback(() => drawerClosed = true)
            .Returns(true);

        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "Edit")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { editor.Object } : Array.Empty<IMauiElement>());
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "IconButton_btnIcon")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { okButton.Object } : Array.Empty<IMauiElement>());

        var result = Page.TestField.TrySetText("Journal note");

        Assert.True(result);
        fieldRoot.Verify(e => e.Click(), Times.Once);
        editor.Verify(e => e.Clear(), Times.Once);
        editor.Verify(e => e.SendKeys("Journal note", TextInputMethod.SetValue), Times.Once);
        okButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
    }

    [Fact]
    public void EditableField_SetText_PrefersNamedTextEditorAndNativeOkButton()
    {
        var drawerOpen = false;
        var drawerClosed = false;
        var fieldRoot = CreateElement("FieldRoot", 0, 0, 200, 40);
        var textEditorButton = CreateInvokableElement("EditableFieldView_TextEditorNativeButton", 0, 0, 200, 40);
        textEditorButton.As<IInvokePatternElement>()
            .Setup(e => e.InvokePattern())
            .Callback(() => drawerOpen = true)
            .Returns(true);
        fieldRoot
            .Setup(e => e.FindElements(It.Is<Locator>(l => l.Value == "EditableFieldView_TextEditorNativeButton"), It.IsAny<int>()))
            .Returns(new[] { textEditorButton.Object });
        var editor = CreateElement("TextEditorView_Editor", 0, 50, 420, 300);
        var nativeOkButton = CreateInvokableElement("IconButton_NativeButton", 360, 0, 48, 48);
        nativeOkButton.As<IInvokePatternElement>()
            .Setup(e => e.InvokePattern())
            .Callback(() => drawerClosed = true)
            .Returns(true);

        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "TextEditorView_Editor")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { editor.Object } : Array.Empty<IMauiElement>());
        Context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "IconButton_NativeButton")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { nativeOkButton.Object } : Array.Empty<IMauiElement>());

        var result = Page.TestField.TrySetText("Named journal note");

        Assert.True(result);
        editor.Verify(e => e.SendKeys("Named journal note", TextInputMethod.SetValue), Times.Once);
        nativeOkButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        fieldRoot.Verify(e => e.Click(), Times.Never);
    }

}

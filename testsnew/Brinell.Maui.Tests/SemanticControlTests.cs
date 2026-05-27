namespace Brinell.Maui.Tests;

public class SemanticControlTests
{
    private readonly Mock<IMauiTestContext> _context = new();
    private readonly TestPage _page;

    public SemanticControlTests()
    {
        _context.Setup(c => c.Timeouts).Returns(new TimeoutSettings
        {
            DefaultWait = 100,
            PageLoad = 100,
            PollingInterval = 1
        });
        _context.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.AutomationId);

        _page = new TestPage(_context.Object);
    }

    [Fact]
    public void EditableField_Open_ActivatesNativeButtonChild()
    {
        var fieldRoot = CreateElement("FieldRoot", 0, 0, 200, 40);
        var nativeButton = CreateInvokableElement("EditableFieldView_NativeButton", 10, 10, 80, 20);
        fieldRoot
            .Setup(e => e.FindElements(It.Is<Locator>(l => l.Value == "EditableFieldView_NativeButton"), It.IsAny<int>()))
            .Returns(new[] { nativeButton.Object });
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);

        var result = _page.TestField.TryOpen();

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
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);

        var result = _page.TestField.TryOpen();

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
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);

        var result = _page.TestField.TrySetText("42.5");

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

        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "Edit")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { editor.Object } : Array.Empty<IMauiElement>());
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "IconButton_btnIcon")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { okButton.Object } : Array.Empty<IMauiElement>());

        var result = _page.TestField.TrySetText("Journal note");

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

        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "TextEditorView_Editor")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { editor.Object } : Array.Empty<IMauiElement>());
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "IconButton_NativeButton")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { nativeOkButton.Object } : Array.Empty<IMauiElement>());

        var result = _page.TestField.TrySetText("Named journal note");

        Assert.True(result);
        editor.Verify(e => e.SendKeys("Named journal note", TextInputMethod.SetValue), Times.Once);
        nativeOkButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        fieldRoot.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void EditableField_SetText_ConfirmsTextEditorWithKeyboardWhenPointerFallbackIsDisabled()
    {
        var drawerOpen = false;
        var drawerClosed = false;
        var fieldRoot = CreateElement("FieldRoot", 0, 0, 200, 40);
        fieldRoot
            .Setup(e => e.Click())
            .Callback(() => drawerOpen = true);
        var editor = CreateElement("TextEditor", 0, 50, 420, 300);
        var okButton = CreateElement("IconButton_btnIcon", 360, 0, 48, 48);
        okButton
            .Setup(e => e.Click())
            .Throws(new InvalidOperationException(
                "Pointer gestures are disabled. Brinell will not move the system mouse unless BRINELL_ALLOW_POINTER_INPUT=true is set for this test run."));
        okButton
            .Setup(e => e.SendKeys(Keys.Space, TextInputMethod.Keys))
            .Callback(() => drawerClosed = true);

        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "Edit")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { editor.Object } : Array.Empty<IMauiElement>());
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "IconButton_btnIcon")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { okButton.Object } : Array.Empty<IMauiElement>());

        var result = _page.TestField.TrySetText("Keyboard confirmed note");

        Assert.True(result);
        editor.Verify(e => e.SendKeys("Keyboard confirmed note", TextInputMethod.SetValue), Times.Once);
        okButton.Verify(e => e.SendKeys(Keys.Space, TextInputMethod.Keys), Times.Once);
    }

    [Fact]
    public void EditableField_SetText_UsesKeyboardActivationForTextEditorDrawerBeforePointerFallback()
    {
        var drawerOpen = false;
        var drawerClosed = false;
        var fieldRoot = CreateElement("FieldRoot", 0, 0, 200, 120);
        fieldRoot
            .Setup(e => e.Click())
            .Throws(new InvalidOperationException(
                "Pointer gestures are disabled. Brinell will not move the system mouse unless BRINELL_ALLOW_POINTER_INPUT=true is set for this test run."));
        fieldRoot
            .Setup(e => e.SendKeys(Keys.Enter, TextInputMethod.Keys))
            .Callback(() => drawerOpen = true);
        var editor = CreateElement("TextEditor", 0, 50, 420, 300);
        var okButton = CreateInvokableElement("IconButton_btnIcon", 360, 0, 48, 48);
        okButton.As<IInvokePatternElement>()
            .Setup(e => e.InvokePattern())
            .Callback(() => drawerClosed = true)
            .Returns(true);

        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "TestField")))
            .Returns(fieldRoot.Object);
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "Edit")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { editor.Object } : Array.Empty<IMauiElement>());
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "IconButton_btnIcon")))
            .Returns(() => drawerOpen && !drawerClosed ? new[] { okButton.Object } : Array.Empty<IMauiElement>());

        var result = _page.TestField.TrySetText("Keyboard note");

        Assert.True(result);
        fieldRoot.Verify(e => e.SendKeys(Keys.Enter, TextInputMethod.Keys), Times.Once);
        editor.Verify(e => e.SendKeys("Keyboard note", TextInputMethod.SetValue), Times.Once);
        okButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
    }

    [Fact]
    public void Editor_SetText_PrefersNestedTextValuePatternFallback()
    {
        var editor = CreateElement("Notes", 0, 0, 200, 80);
        editor.As<INestedTextElement>()
            .Setup(e => e.SetTextWithFallback("Toolbox note"))
            .Returns(true);
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Notes")))
            .Returns(editor.Object);

        _page.Notes.SetText("Toolbox note");

        editor.As<INestedTextElement>().Verify(e => e.SetTextWithFallback("Toolbox note"), Times.Once);
        editor.Verify(e => e.SendKeys(It.IsAny<string>(), It.IsAny<TextInputMethod>()), Times.Never);
    }

    [Fact]
    public void Button_Click_InvokesContainedNativeButtonChild()
    {
        var buttonRoot = CreateElement("PromptDialogView_OKButton", 0, 0, 200, 48);
        var nativeButton = CreateInvokableElement("PromptDialogView_OKButton_Native", 0, 0, 200, 48);
        buttonRoot
            .Setup(e => e.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "Button"), It.IsAny<int>()))
            .Returns(new[] { nativeButton.Object });
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "PromptDialogView_OKButton")))
            .Returns(buttonRoot.Object);

        var result = _page.PromptOk.TryClick();

        Assert.True(result);
        nativeButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        buttonRoot.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void Button_Click_InvokesSmallestContainedButtonFromScopeFallback()
    {
        var buttonRoot = CreateElement("NativeDialog_Delete", 0, 0, 200, 80);
        var outsideButton = CreateInvokableElement("OutsideButton", 260, 0, 48, 48);
        var largeContainedButton = CreateInvokableElement("NativeDialog_Delete_FrameButton", 0, 0, 200, 80);
        var nativeButton = CreateInvokableElement("NativeDialog_Delete_Native", 20, 10, 48, 48);
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "NativeDialog_Delete")))
            .Returns(buttonRoot.Object);
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "Button")))
            .Returns(new[] { outsideButton.Object, largeContainedButton.Object, nativeButton.Object });

        var result = _page.NativeDialogDelete.TryClick();

        Assert.True(result);
        nativeButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        largeContainedButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Never);
        outsideButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Never);
        buttonRoot.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void Button_Click_UsesLocatedButtonItselfBeforeOverlappingScopeFallback()
    {
        var buttonRoot = CreateInvokableElement("NativeDialog_Delete", 0, 0, 48, 48);
        buttonRoot.Setup(e => e.TagName).Returns("Button");
        var overlappingButton = CreateInvokableElement("OtherNativeButton", 0, 0, 48, 48);
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "NativeDialog_Delete")))
            .Returns(buttonRoot.Object);
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "Button")))
            .Returns(new[] { overlappingButton.Object });

        var result = _page.NativeDialogDelete.TryClick();

        Assert.True(result);
        buttonRoot.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        overlappingButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Never);
    }

    [Fact]
    public void Button_Click_PrefersInvokeBeforeLegacyAccessibleDefaultAction()
    {
        var buttonRoot = CreateInvokableElement("NativeDialog_Delete", 0, 0, 120, 48);
        buttonRoot.As<ILegacyIAccessiblePatternElement>()
            .Setup(e => e.SupportsLegacyIAccessiblePattern)
            .Returns(true);
        buttonRoot.As<ILegacyIAccessiblePatternElement>()
            .Setup(e => e.DoDefaultActionPattern())
            .Returns(true);
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "NativeDialog_Delete")))
            .Returns(buttonRoot.Object);

        var result = _page.NativeDialogDelete.TryClick();

        Assert.True(result);
        buttonRoot.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        buttonRoot.As<ILegacyIAccessiblePatternElement>().Verify(e => e.DoDefaultActionPattern(), Times.Never);
        buttonRoot.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void Button_Press_FocusesLocatedButtonAndSendsSpace()
    {
        var buttonRoot = CreateElement("NativeDialog_Delete", 0, 0, 120, 48);
        buttonRoot.Setup(e => e.TagName).Returns("Button");
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "NativeDialog_Delete")))
            .Returns(buttonRoot.Object);

        var result = _page.NativeDialogDelete.TryPress();

        Assert.True(result);
        buttonRoot.Verify(e => e.SendKeys(Keys.Space, TextInputMethod.Keys), Times.Once);
        buttonRoot.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void Button_Press_UsesContainedNativeButtonChild()
    {
        var buttonRoot = CreateElement("PromptDialogView_OKButton", 0, 0, 200, 48);
        var nativeButton = CreateElement("PromptDialogView_OKButton_Native", 0, 0, 200, 48);
        buttonRoot
            .Setup(e => e.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "Button"), It.IsAny<int>()))
            .Returns(new[] { nativeButton.Object });
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "PromptDialogView_OKButton")))
            .Returns(buttonRoot.Object);

        var result = _page.PromptOk.TryPress();

        Assert.True(result);
        nativeButton.Verify(e => e.SendKeys(Keys.Space, TextInputMethod.Keys), Times.Once);
        buttonRoot.Verify(e => e.SendKeys(It.IsAny<string>(), It.IsAny<TextInputMethod>()), Times.Never);
    }

    [Fact]
    public void Button_Click_UsesLegacyAccessibleDefaultActionBeforeKeyboardFallback()
    {
        var buttonRoot = CreateLegacyAccessibleElement("NativeDialog_Delete", 0, 0, 120, 48);
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "NativeDialog_Delete")))
            .Returns(buttonRoot.Object);

        var result = _page.NativeDialogDelete.TryClick();

        Assert.True(result);
        buttonRoot.As<ILegacyIAccessiblePatternElement>().Verify(e => e.DoDefaultActionPattern(), Times.Once);
        buttonRoot.Verify(e => e.Click(), Times.Never);
        buttonRoot.Verify(e => e.SendKeys(It.IsAny<string>(), It.IsAny<TextInputMethod>()), Times.Never);
    }

    [Fact]
    public void Button_Click_UsesKeyboardActivationWhenPointerFallbackIsDisabled()
    {
        var buttonRoot = CreateElement("NativeDialog_Delete", 0, 0, 120, 48);
        buttonRoot
            .Setup(e => e.Click())
            .Throws(new InvalidOperationException(
                "Pointer gestures are disabled. Brinell will not move the system mouse unless BRINELL_ALLOW_POINTER_INPUT=true is set for this test run."));
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "NativeDialog_Delete")))
            .Returns(buttonRoot.Object);

        var result = _page.NativeDialogDelete.TryClick();

        Assert.True(result);
        buttonRoot.Verify(e => e.SendKeys(Keys.Space, TextInputMethod.Keys), Times.Once);
    }

    [Fact]
    public void CheckBox_SetChecked_UsesTogglePatternBeforeClick()
    {
        var checkBox = CreateToggleElement("IncludeProblemReports", 0, 0, 32, 32, initialState: false);
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "IncludeProblemReports")))
            .Returns(checkBox.Object);

        _page.IncludeProblemReports.Check();

        checkBox.As<ITogglePatternElement>().Verify(e => e.SetToggleStatePattern(true), Times.Once);
        checkBox.Verify(e => e.Click(), Times.Never);
        checkBox.Verify(e => e.SendKeys(It.IsAny<string>(), It.IsAny<TextInputMethod>()), Times.Never);
        Assert.True(_page.IncludeProblemReports.IsChecked());
    }

    [Fact]
    public void CheckBox_Toggle_UsesKeyboardFallbackWhenSemanticActivationCannotChangeState()
    {
        var isChecked = false;
        var checkBox = CreateElement("IncludeProblemReports", 0, 0, 32, 32);
        checkBox.Setup(e => e.Selected).Returns(() => isChecked);
        checkBox
            .Setup(e => e.Click())
            .Throws(new InvalidOperationException(
                "Pointer gestures are disabled. Brinell will not move the system mouse unless BRINELL_ALLOW_POINTER_INPUT=true is set for this test run."));
        checkBox
            .Setup(e => e.SendKeys(Keys.Space, TextInputMethod.Keys))
            .Callback(() => isChecked = !isChecked);
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "IncludeProblemReports")))
            .Returns(checkBox.Object);

        _page.IncludeProblemReports.Toggle();

        checkBox.Verify(e => e.SendKeys(Keys.Space, TextInputMethod.Keys), Times.Once);
        Assert.True(_page.IncludeProblemReports.IsChecked());
    }

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
        _context.Setup(c => c.Driver).Returns(driver.Object);

        var result = _page.Dialog.TryClickButtonAndWaitDismissed("*!delete!*", timeoutMs: 100);

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
        _context.Setup(c => c.Driver).Returns(driver.Object);

        var result = _page.Dialog.TryClickButtonAndWaitDismissed("*!delete!*", timeoutMs: 100);

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
        _context.Setup(c => c.Driver).Returns(driver.Object);

        var result = _page.Dialog.TryClickButtonAndWaitDismissed("Continue", timeoutMs: 100);

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
        _context.Setup(c => c.Driver).Returns(driver.Object);
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Strategy == LocatorStrategy.ControlType && l.Value == "button")))
            .Returns(() => dismissed ? Array.Empty<IMauiElement>() : new[] { deleteButton.Object });
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Strategy == LocatorStrategy.Name && l.Value == "*!delete!*")))
            .Returns(titleText.Object);

        var result = _page.Dialog.TryClickButtonAndWaitDismissed("*!delete!*", timeoutMs: 100);

        Assert.True(result);
        Assert.True(dismissed);
        titleText.Verify(e => e.Click(), Times.Never);
        deleteButton.As<ILegacyIAccessiblePatternElement>().Verify(e => e.DoDefaultActionPattern(), Times.Once);
    }

    [Fact]
    public void IconCommandButton_Click_InvokesTemplateNativeButtonChild()
    {
        var buttonRoot = CreateElement("SaveButton", 0, 0, 200, 40);
        var nativeButton = CreateInvokableElement("IconLabelButtonView_NativeButton", 10, 10, 80, 20);
        buttonRoot
            .Setup(e => e.FindElements(It.Is<Locator>(l => l.Value == "IconLabelButtonView_NativeButton"), It.IsAny<int>()))
            .Returns(new[] { nativeButton.Object });
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "SaveButton")))
            .Returns(buttonRoot.Object);

        var result = _page.Save.TryClick();

        Assert.True(result);
        nativeButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        buttonRoot.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void RoundButton_Click_InvokesTemplateNativeButtonChild()
    {
        var buttonRoot = CreateElement("AddButton", 0, 0, 80, 80);
        var nativeButton = CreateInvokableElement("RoundButtonView_NativeButton", 10, 10, 60, 60);
        buttonRoot
            .Setup(e => e.FindElements(It.Is<Locator>(l => l.Value == "RoundButtonView_NativeButton"), It.IsAny<int>()))
            .Returns(new[] { nativeButton.Object });
        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "AddButton")))
            .Returns(buttonRoot.Object);

        var result = _page.Add.TryClick();

        Assert.True(result);
        nativeButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        buttonRoot.Verify(e => e.Click(), Times.Never);
    }

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

        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItemButton_801")))
            .Returns(() => selected ? Array.Empty<IMauiElement>() : new[] { nativeButton.Object });
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItem_801")))
            .Returns(() => selected ? Array.Empty<IMauiElement>() : new[] { child.Object });
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "ListItem")))
            .Returns(new[] { row.Object });

        var result = _page.Browser.TrySelectItem("801");

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

        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItemButton_0_3555")))
            .Returns(() => selected ? Array.Empty<IMauiElement>() : new[] { nativeButton.Object });
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItem_0_3555")))
            .Returns(() => selected ? Array.Empty<IMauiElement>() : new[] { CreateElement("GenericBrowserItem_0_3555", 50, 50, 80, 20).Object });

        var result = _page.Browser.TrySelectItem("0:3555");

        Assert.True(result);
        nativeButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
    }

    [Fact]
    public void GenericBrowser_SelectItem_DoesNotUseVisibleTextOutsideBrowser()
    {
        var pageLabel = CreateElement("HoursEdit_RowLabel", 40, 40, 220, 50);
        pageLabel.Setup(e => e.Text).Returns("Mock labour");
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.Name && l.Value == "Mock labour")))
            .Returns(new[] { pageLabel.Object });

        var result = _page.Browser.TrySelectItem("602", "Mock labour", timeoutMs: 1);

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
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowser")))
            .Returns(new[] { browserRoot.Object });
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItem_602")))
            .Returns(Array.Empty<IMauiElement>());

        var result = _page.Browser.TrySelectItem("602", "Mock labour", timeoutMs: 1);

        Assert.True(result);
        browserLabel.Verify(e => e.Click(), Times.Once);
    }

    [Fact]
    public void GenericBrowser_ToggleItem_DoesNotWaitForDrawerToClose()
    {
        var nativeButton = CreateInvokableElement("GenericBrowserItemButton_0_3555", 40, 40, 220, 50);
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItemButton_0_3555")))
            .Returns(new[] { nativeButton.Object });
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItem_0_3555")))
            .Returns(new[] { CreateElement("GenericBrowserItem_0_3555", 50, 50, 80, 20).Object });

        var result = _page.Browser.TryToggleItem("0:3555");

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

        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "DrawerView_Cancel_NativeButton")))
            .Returns(() => closed ? Array.Empty<IMauiElement>() : new[] { nativeClose.Object });
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "DrawerView_Cancel")))
            .Returns(() => closed ? Array.Empty<IMauiElement>() : new[] { gestureClose.Object });
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowser")))
            .Returns(() => closed ? Array.Empty<IMauiElement>() : new[] { browserRoot.Object });

        var result = _page.Browser.TryClose();

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

        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "GenericBrowserItem_801")))
            .Returns(() => selected ? Array.Empty<IMauiElement>() : new[] { child.Object });
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "ListItem")))
            .Returns(new[] { row.Object });

        var result = _page.Browser.TrySelectItem("801");

        Assert.True(result);
        row.As<ISelectionItemPatternElement>().Verify(e => e.SelectItemPattern(), Times.Once);
        child.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void SelectionList_SelectByAutomationId_UsesContainingListItemPattern()
    {
        var child = CreateElement("EquipmentSelection_Item_2001", 50, 50, 80, 20);
        var row = CreateSelectableElement("ListItem", 40, 40, 220, 50);

        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "EquipmentSelection_Item_2001")))
            .Returns(new[] { child.Object });
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "ListItem")))
            .Returns(new[] { row.Object });

        var result = _page.List.TrySelectByAutomationId("EquipmentSelection_Item_2001");

        Assert.True(result);
        row.As<ISelectionItemPatternElement>().Verify(e => e.SelectItemPattern(), Times.Once);
        child.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void TypedList_SelectItem_UsesIndexedContainerRoot()
    {
        var child = CreateElement("Item_0", 50, 50, 80, 20);
        var row = CreateSelectableElement("ListItem", 40, 40, 220, 50);

        _context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Item_0")))
            .Returns(child.Object);
        _context
            .Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == "Item_0")))
            .Returns(child.Object);
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l =>
                l.Strategy == LocatorStrategy.ControlType && l.Value == "ListItem")))
            .Returns(new[] { row.Object });

        var result = _page.TypedList.TrySelectItem(0);

        Assert.True(result);
        row.As<ISelectionItemPatternElement>().Verify(e => e.SelectItemPattern(), Times.Once);
        child.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void TabMenu_Select_UsesMatchingInvokableButtonByCaptionIndex()
    {
        var projectsCaption = CreateElement("TabMenuView_Caption", 10, 10, 60, 20);
        projectsCaption.Setup(e => e.Text).Returns("Projects");
        var projectsButton = CreateInvokableElement("TabMenuView_Button", 0, 0, 80, 60);

        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "TabMenuView_Caption")))
            .Returns(new[] { projectsCaption.Object });
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "TabMenuView_Button")))
            .Returns(new[] { projectsButton.Object });
        _context
            .Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "TabMenuView_Grid")))
            .Returns(Array.Empty<IMauiElement>());

        var result = _page.Tabs.TrySelect("Projects");

        Assert.True(result);
        projectsButton.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        projectsCaption.Verify(e => e.Click(), Times.Never);
    }

    private static Mock<IMauiElement> CreateElement(
        string automationId,
        int x,
        int y,
        int width,
        int height)
    {
        var element = new Mock<IMauiElement>();
        element.Setup(e => e.Visible).Returns(true);
        element.Setup(e => e.Enabled).Returns(true);
        element.Setup(e => e.Rect).Returns(new System.Drawing.Rectangle(x, y, width, height));
        element.Setup(e => e.GetAttribute("AutomationId")).Returns(automationId);
        return element;
    }

    private static Mock<IMauiElement> CreateInvokableElement(
        string automationId,
        int x,
        int y,
        int width,
        int height)
    {
        var element = CreateElement(automationId, x, y, width, height);
        element.As<IInvokePatternElement>()
            .Setup(e => e.SupportsInvokePattern)
            .Returns(true);
        element.As<IInvokePatternElement>()
            .Setup(e => e.InvokePattern())
            .Returns(true);
        return element;
    }

    private static Mock<IMauiElement> CreateSelectableElement(
        string automationId,
        int x,
        int y,
        int width,
        int height,
        Action? onSelect = null)
    {
        var element = CreateElement(automationId, x, y, width, height);
        element.As<ISelectionItemPatternElement>()
            .Setup(e => e.SupportsSelectionItemPattern)
            .Returns(true);
        element.As<ISelectionItemPatternElement>()
            .Setup(e => e.SelectItemPattern())
            .Callback(() => onSelect?.Invoke())
            .Returns(true);
        return element;
    }

    private static Mock<IMauiElement> CreateToggleElement(
        string automationId,
        int x,
        int y,
        int width,
        int height,
        bool initialState)
    {
        var isChecked = initialState;
        var element = CreateElement(automationId, x, y, width, height);
        element.Setup(e => e.Selected).Returns(() => isChecked);
        element.As<ITogglePatternElement>()
            .Setup(e => e.SupportsTogglePattern)
            .Returns(true);
        element.As<ITogglePatternElement>()
            .Setup(e => e.IsTogglePatternChecked())
            .Returns(() => isChecked);
        element.As<ITogglePatternElement>()
            .Setup(e => e.TogglePattern())
            .Callback(() => isChecked = !isChecked)
            .Returns(true);
        element.As<ITogglePatternElement>()
            .Setup(e => e.SetToggleStatePattern(It.IsAny<bool>()))
            .Callback<bool>(value => isChecked = value)
            .Returns(true);
        return element;
    }

    private static Mock<IMauiElement> CreateLegacyAccessibleElement(
        string automationId,
        int x,
        int y,
        int width,
        int height)
    {
        var element = CreateElement(automationId, x, y, width, height);
        element.As<ILegacyIAccessiblePatternElement>()
            .Setup(e => e.SupportsLegacyIAccessiblePattern)
            .Returns(true);
        element.As<ILegacyIAccessiblePatternElement>()
            .Setup(e => e.DoDefaultActionPattern())
            .Returns(true);
        return element;
    }

    private sealed class TestPage : PageObjectBase<TestPage>
    {
        public TestPage(IMauiTestContext context)
            : base(context)
        {
        }

        public override string Name => "TestPage";

        public override bool IsLoaded(int? timeoutMs = null) => true;

        public EditableField<TestPage> TestField => new(this, "TestField");

        public Editor<TestPage> Notes => new(this, "Notes");

        public IconCommandButton<TestPage> Save => new(this, "SaveButton");

        public RoundButton<TestPage> Add => new(this, "AddButton");

        public Button<TestPage> PromptOk => new(this, "PromptDialogView_OKButton");

        public Button<TestPage> NativeDialogDelete => new(this, "NativeDialog_Delete");

        public ContentDialog<TestPage> Dialog => new(this);

        public GenericBrowser<TestPage> Browser => new(this);

        public SelectionList<TestPage> List => new(this);

        public TabMenu<TestPage> Tabs => new(this);

        public CheckBox<TestPage> IncludeProblemReports => CheckBox("IncludeProblemReports");

        public List<TestPage, TestListItem> TypedList => new(
            this,
            "TestList",
            "Item_",
            (scope, index) => new TestListItem(scope, index));
    }

    private sealed class TestListItem : ContainerBase<TestPage, TestListItem>
    {
        public TestListItem(IMauiScope<TestPage> scope, int index)
            : base(scope, Locator.ByAutomationId($"Item_{index}"))
        {
        }
    }
}

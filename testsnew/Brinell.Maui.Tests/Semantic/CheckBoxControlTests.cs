namespace Brinell.Maui.Tests.Semantic;

public class CheckBoxControlTests : SemanticControlTestsBase
{
    [Fact]
    public void CheckBox_SetChecked_UsesTogglePatternBeforeClick()
    {
        var checkBox = CreateToggleElement("IncludeProblemReports", 0, 0, 32, 32, initialState: false);
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "IncludeProblemReports")))
            .Returns(checkBox.Object);

        Page.IncludeProblemReports.Check();

        checkBox.As<ITogglePatternElement>().Verify(e => e.SetToggleStatePattern(true), Times.Once);
        checkBox.Verify(e => e.Click(), Times.Never);
        checkBox.Verify(e => e.SendKeys(It.IsAny<string>(), It.IsAny<TextInputMethod>()), Times.Never);
        Assert.True(Page.IncludeProblemReports.IsChecked());
    }

    [Fact]
    public void CheckBox_Toggle_ThrowsPolicyExceptionWhenPointerFallbackIsDisabled()
    {
        var isChecked = false;
        var checkBox = CreateElement("IncludeProblemReports", 0, 0, 32, 32);
        checkBox.Setup(e => e.Selected).Returns(() => isChecked);
        checkBox
            .Setup(e => e.Click())
            .Throws(new WindowsInteractionPolicyException(
                "The 'Click' action requires pointer input, but BRINELL_ALLOW_POINTER_INPUT is not enabled."));
        checkBox
            .Setup(e => e.SendKeys(Keys.Space, TextInputMethod.Keys))
            .Callback(() => isChecked = !isChecked);
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "IncludeProblemReports")))
            .Returns(checkBox.Object);

        var ex = Assert.Throws<WindowsInteractionPolicyException>(
            () => Page.IncludeProblemReports.Toggle());

        Assert.Contains("pointer input", ex.Message);
        checkBox.Verify(e => e.SendKeys(It.IsAny<string>(), It.IsAny<TextInputMethod>()), Times.Never);
        Assert.False(Page.IncludeProblemReports.IsChecked());
    }
}

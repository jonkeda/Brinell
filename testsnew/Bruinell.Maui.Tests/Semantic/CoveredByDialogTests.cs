using Bruinell.Core.Exceptions;

namespace Bruinell.Maui.Tests.Semantic;

/// <summary>
/// On Android a control under an open dialog is found and counted as visible; an action on it is
/// not ready, "covered by a dialog", and never taps (the tap would land on the dialog).
/// </summary>
public class CoveredByDialogTests : SemanticControlTestsBase
{
    private readonly Mock<IMauiElement> _app = new();
    private readonly Mock<IMauiDriver> _driver = new();

    public CoveredByDialogTests()
    {
        Context.Setup(c => c.AppElement).Returns(_app.Object);
        Context.Setup(c => c.Driver).Returns(_driver.Object);
        _driver.Setup(d => d.FoundMeansVisible).Returns(true);
        _app.Setup(a => a.TryFindActiveDialog()).Returns(CreateElement("Dialog", 50, 300, 300, 200).Object);
    }

    private Mock<IMauiElement> GivenButton(string automationId, int x, int y)
    {
        var button = CreateInvokableElement(automationId, x, y, 80, 40);
        Context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == automationId))).Returns(button.Object);
        return button;
    }

    [Fact]
    public void AButtonOutsideTheDialog_IsNotTapped_AndFailsAsCovered()
    {
        var button = GivenButton("NativeDialog_Delete", 10, 10);

        var error = Assert.IsType<ElementNotReadyException>(Record.Exception(() => Page.NativeDialogDelete.Click(timeoutMs: 100)));

        Assert.Contains("covered by a dialog", error.Message, StringComparison.Ordinal);
        button.Verify(e => e.Invoke(), Times.Never);
    }

    [Fact]
    public void TheDialogsOwnButton_IsTapped()
    {
        var button = GivenButton("PromptDialogView_OKButton", 100, 400);

        Page.PromptOk.Click(timeoutMs: 100);

        button.Verify(e => e.Invoke(), Times.Once);
    }

    [Fact]
    public void WhereAFoundControlIsNotCountedAsVisible_TheDialogIsNotAsked()
    {
        _driver.Setup(d => d.FoundMeansVisible).Returns(false);
        var button = GivenButton("NativeDialog_Delete", 10, 10);

        Page.NativeDialogDelete.Click(timeoutMs: 100);

        button.Verify(e => e.Invoke(), Times.Once);
        _app.Verify(a => a.TryFindActiveDialog(), Times.Never);
    }
}

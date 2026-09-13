using Brinell.Maui.Configuration;

namespace Brinell.Maui.Tests.Semantic;

public class CheckBoxControlTests : SemanticControlTestsBase
{
    [Fact]
    public void CheckBox_SetChecked_AsksForTheStateWhereThePlatformCan()
    {
        var checkBox = CreateToggleElement("IncludeProblemReports", 0, 0, 32, 32, initialState: false);
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "IncludeProblemReports")))
            .Returns(checkBox.Object);
        // Toggle actions resolve through FindElement; state reads go through TryFindElement.
        // Stubbing only the latter leaves FindElement returning null and the control NREs.
        Context
            .Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == "IncludeProblemReports")))
            .Returns(checkBox.Object);

        Page.IncludeProblemReports.Check();

        checkBox.Verify(e => e.SetChecked(true), Times.Once);
        checkBox.Verify(e => e.Toggle(), Times.Never);
        checkBox.Verify(e => e.Click(), Times.Never);
        checkBox.Verify(e => e.SendKeys(It.IsAny<string>(), It.IsAny<TextInputMethod>()), Times.Never);
        Assert.True(Page.IncludeProblemReports.IsChecked());
    }

}

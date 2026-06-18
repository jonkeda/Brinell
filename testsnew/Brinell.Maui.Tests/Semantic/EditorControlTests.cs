namespace Brinell.Maui.Tests.Semantic;

public class EditorControlTests : SemanticControlTestsBase
{
    [Fact]
    public void Editor_SetText_PrefersNestedTextValuePatternFallback()
    {
        var editor = CreateElement("Notes", 0, 0, 200, 80);
        editor.As<INestedTextElement>()
            .Setup(e => e.SetTextWithFallback("Toolbox note"))
            .Returns(true);
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Notes")))
            .Returns(editor.Object);

        Page.Notes.SetText("Toolbox note");

        editor.As<INestedTextElement>().Verify(e => e.SetTextWithFallback("Toolbox note"), Times.Once);
        editor.Verify(e => e.SendKeys(It.IsAny<string>(), It.IsAny<TextInputMethod>()), Times.Never);
    }
}

namespace Brinell.Maui.Tests.Semantic;

public class EditorControlTests : SemanticControlTestsBase
{
    /// <summary>
    /// Setting text clears then writes through the element, with no capability probe.
    /// </summary>
    /// <remarks>
    /// This replaces a test that asserted the control probed for <c>INestedTextElement</c> and
    /// preferred its fallback. That interface is gone: MAUI nesting a text control inside a
    /// wrapper is a Windows rendering detail, so resolving it belongs in the Windows element,
    /// not in a branch every control repeats. Android maps <c>Editor</c> straight to
    /// <c>android.widget.EditText</c> and never needed it.
    /// <para>
    /// What matters now is that the control expresses intent — clear, then write — and each
    /// driver decides how. See <c>.my/maui/plan-appium-text-entry.md</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public void Editor_SetText_ClearsThenWritesThroughTheElement()
    {
        var editor = CreateElement("Notes", 0, 0, 200, 80);
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Notes")))
            .Returns(editor.Object);
        Context
            .Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == "Notes")))
            .Returns(editor.Object);

        Page.Notes.SetText("Toolbox note");

        editor.Verify(e => e.Clear(), Times.Once);
        editor.Verify(e => e.SendKeys("Toolbox note", TextInputMethod.SetValue), Times.Once);
    }
}

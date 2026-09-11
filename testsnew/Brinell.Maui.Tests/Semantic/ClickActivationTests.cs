namespace Brinell.Maui.Tests.Semantic;

/// <summary>
/// Covers what a control does when it is clicked: it names one operation and asks for it.
/// </summary>
/// <remarks>
/// <para>
/// These replace <c>ClickLadderTests</c>, which specified the activation ladder -
/// SelectionItem, then Invoke, then a pointer click, first one that reported success. The
/// ladder is gone, and so is the question it answered. A control no longer discovers how to
/// activate itself; it states it, and the element implements that statement per platform.
/// See <c>.my/fix/design-controls-know-how-to-click.md</c>.
/// </para>
/// <para>
/// <b>What is worth asserting changed with it.</b> The old tests checked which rung ran for a
/// given combination of advertised capabilities - eight tests about a search. These check that
/// each control asks for the right thing and that nothing is tried behind its back, which is one
/// assertion per control and does not multiply with the number of platforms.
/// </para>
/// </remarks>
public class ClickActivationTests : SemanticControlTestsBase
{
    private const string ButtonId = "PromptDialogView_OKButton";

    /// <remarks>
    /// Both finders are stubbed: <c>Click</c> resolves through <c>FindElement</c>, while the
    /// visibility guard on the way in uses <c>TryFindElement</c>.
    /// </remarks>
    private void GivenElement(Mock<IMauiElement> element)
    {
        Context
            .Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == ButtonId)))
            .Returns(element.Object);
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == ButtonId)))
            .Returns(element.Object);
    }

    /// <summary>
    /// A plain command control invokes, and does nothing else.
    /// </summary>
    [Fact]
    public void Click_AsksTheElementToInvoke()
    {
        var element = CreateElement(ButtonId, 0, 0, 80, 24);
        GivenElement(element);

        Page.PromptOk.Click();

        element.Verify(e => e.Invoke(), Times.Once);
        element.Verify(e => e.Click(), Times.Never);
    }

    /// <summary>
    /// It does not probe patterns, on any platform.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The assertion that pins the design rather than the behaviour. Which automation pattern
    /// serves an invoke is the element's business and differs per platform - the Invoke pattern
    /// on Windows, a tap on Android and iOS. A control reaching past <c>Invoke</c> to ask about
    /// patterns is reintroducing the ladder one control at a time.
    /// </para>
    /// <para>
    /// The element here advertises every capability the old ladder knew about, so any surviving
    /// probe would find something and be caught.
    /// </para>
    /// </remarks>
    [Fact]
    public void Click_DoesNotReachPastInvokeToAPattern()
    {
        var element = CreateSelectableElement(ButtonId, 0, 0, 80, 24);
        element.As<IInvokePatternElement>().Setup(e => e.SupportsInvokePattern).Returns(true);
        element.As<IInvokePatternElement>().Setup(e => e.InvokePattern()).Returns(true);
        GivenElement(element);

        Page.PromptOk.Click();

        element.Verify(e => e.Invoke(), Times.Once);
        element.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Never);
        element.As<ISelectionItemPatternElement>().Verify(e => e.SelectItemPattern(), Times.Never);
        element.Verify(e => e.Click(), Times.Never);
    }

    /// <summary>
    /// A failed activation reaches the caller instead of becoming a pointer click.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the behavioural change the whole design turns on, so it is asserted directly.
    /// Under the ladder, an element that could not be invoked was clicked instead and the test
    /// passed; the suite would then be driving the app a different way than it said, and nobody
    /// would know until a control that cannot be clicked came along.
    /// </para>
    /// <para>
    /// Two rungs were measured reporting success while doing nothing - LegacyIAccessible on a
    /// Switch, Invoke on a ToolbarItem - which is why "fall back on failure" was never the
    /// safety net it appeared to be: it cannot fire for the failures that actually happen.
    /// </para>
    /// </remarks>
    [Fact]
    public void Click_PropagatesActivationFailure_RatherThanClickingInstead()
    {
        var element = CreateElement(ButtonId, 0, 0, 80, 24);
        element.Setup(e => e.Invoke()).Throws(new NotSupportedException("no Invoke pattern here"));
        GivenElement(element);

        var ex = Assert.Throws<NotSupportedException>(() => Page.PromptOk.Click());

        Assert.Contains("no Invoke pattern here", ex.Message);
        element.Verify(e => e.Click(), Times.Never);
    }

    /// <summary>
    /// A disabled element is rejected by the guard before anything is activated.
    /// </summary>
    [Fact]
    public void Click_ThrowsBeforeActivating_WhenElementIsDisabled()
    {
        var element = CreateElement(ButtonId, 0, 0, 80, 24);
        element.Setup(e => e.Enabled).Returns(false);
        GivenElement(element);

        Assert.Throws<TimeoutException>(() => Page.PromptOk.Click());

        element.Verify(e => e.Invoke(), Times.Never);
        element.Verify(e => e.Click(), Times.Never);
    }
}

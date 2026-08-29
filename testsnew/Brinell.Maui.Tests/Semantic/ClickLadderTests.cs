namespace Brinell.Maui.Tests.Semantic;

/// <summary>
/// Covers the activation ladder in <c>ClickableControlBase.ClickCore</c>.
/// </summary>
/// <remarks>
/// These replace the coverage that lived implicitly in the deleted <c>ElementClicker</c>.
/// Each capability is exercised in both states — supported and unsupported — because the
/// unsupported branch is what Android and iOS take: <c>AppiumMauiElement</c> implements no
/// pattern interface, so on mobile every probe misses and the ladder must fall through to
/// <see cref="IElement{TSelf}.Click"/>. Mocking the element lets that path be verified here,
/// in seconds, rather than on a device.
/// </remarks>
public class ClickLadderTests : SemanticControlTestsBase
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

    [Fact]
    public void Click_PrefersSelectionItemPattern_OverPointerClick()
    {
        var selected = false;
        var element = CreateSelectableElement(ButtonId, 0, 0, 80, 24, onSelect: () => selected = true);
        GivenElement(element);

        Page.PromptOk.Click();

        Assert.True(selected);
        element.Verify(e => e.Click(), Times.Never);
    }

    [Fact]
    public void Click_UsesInvokePattern_WhenSelectionItemIsUnsupported()
    {
        var element = CreateInvokableElement(ButtonId, 0, 0, 80, 24);
        GivenElement(element);

        Page.PromptOk.Click();

        element.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        element.Verify(e => e.Click(), Times.Never);
    }

    /// <summary>
    /// LegacyIAccessible is not part of the default ladder, by design.
    /// </summary>
    /// <remarks>
    /// A WinUI toggle advertises the pattern and its <c>DoDefaultAction</c> returns success
    /// without changing state, so treating it as activation makes <c>Click</c> on a Switch
    /// report success while doing nothing — which is exactly how it broke the Switch UI tests
    /// when it was included. A control that needs it overrides the ladder.
    /// </remarks>
    [Fact]
    public void Click_IgnoresLegacyAccessible_AndUsesPointerClick()
    {
        var element = CreateLegacyAccessibleElement(ButtonId, 0, 0, 80, 24);
        GivenElement(element);

        Page.PromptOk.Click();

        element.As<ILegacyIAccessiblePatternElement>()
            .Verify(e => e.DoDefaultActionPattern(), Times.Never);
        element.Verify(e => e.Click(), Times.Once);
    }

    /// <summary>
    /// The mobile path: no capability interface is implemented at all.
    /// </summary>
    [Fact]
    public void Click_FallsBackToPointerClick_WhenNoPatternIsSupported()
    {
        var element = CreateElement(ButtonId, 0, 0, 80, 24);
        GivenElement(element);

        Page.PromptOk.Click();

        element.Verify(e => e.Click(), Times.Once);
    }

    /// <summary>
    /// A pattern that is advertised but declines is not success — the ladder continues.
    /// </summary>
    [Fact]
    public void Click_ContinuesLadder_WhenSupportedPatternReturnsFalse()
    {
        var element = CreateElement(ButtonId, 0, 0, 80, 24);
        element.As<IInvokePatternElement>().Setup(e => e.SupportsInvokePattern).Returns(true);
        element.As<IInvokePatternElement>().Setup(e => e.InvokePattern()).Returns(false);
        GivenElement(element);

        Page.PromptOk.Click();

        element.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Once);
        element.Verify(e => e.Click(), Times.Once);
    }

    /// <summary>
    /// The behaviour change this ladder was written for.
    /// </summary>
    /// <remarks>
    /// The deleted <c>ElementClicker.TryClick</c> wrapped the whole ladder in
    /// <c>catch { return false; }</c>, so a pattern that threw was indistinguishable from one
    /// that was absent, and the click silently did nothing — surfacing later as an unrelated
    /// assertion failure. A failing pattern is a real fault and must reach the caller.
    /// </remarks>
    [Fact]
    public void Click_PropagatesPatternFailure_RatherThanSwallowingIt()
    {
        var element = CreateElement(ButtonId, 0, 0, 80, 24);
        element.As<IInvokePatternElement>().Setup(e => e.SupportsInvokePattern).Returns(true);
        element.As<IInvokePatternElement>()
            .Setup(e => e.InvokePattern())
            .Throws(new InvalidOperationException("UIA Invoke failed"));
        GivenElement(element);

        var ex = Assert.Throws<InvalidOperationException>(() => Page.PromptOk.Click());

        Assert.Contains("UIA Invoke failed", ex.Message);
        element.Verify(e => e.Click(), Times.Never);
    }

    /// <summary>
    /// A disabled element is rejected by the guard before any pattern is tried.
    /// </summary>
    [Fact]
    public void Click_ThrowsBeforeActivating_WhenElementIsDisabled()
    {
        var element = CreateInvokableElement(ButtonId, 0, 0, 80, 24);
        element.Setup(e => e.Enabled).Returns(false);
        GivenElement(element);

        Assert.Throws<TimeoutException>(() => Page.PromptOk.Click());

        element.As<IInvokePatternElement>().Verify(e => e.InvokePattern(), Times.Never);
        element.Verify(e => e.Click(), Times.Never);
    }
}

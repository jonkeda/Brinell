namespace Brinell.Maui.Tests.Semantic;

/// <summary>
/// Covers how controls behave when a platform capability is present versus absent.
/// </summary>
/// <remarks>
/// <para>
/// The Windows element (<c>FlaUIMauiElement</c>) implements seven capability interfaces; the
/// mobile element (<c>AppiumMauiElement</c>) implements two. Every control therefore runs
/// down a different branch depending on platform, and until Android actually runs, these
/// mocked tests are the only thing verifying the branch mobile will take.
/// </para>
/// <para>
/// A mock that implements a capability stands in for Windows; one that does not stands in for
/// mobile. That is exactly how the production code decides — an <c>is</c> test plus a
/// <c>Supports*</c> probe — so the substitution is faithful rather than approximate.
/// </para>
/// <para>
/// These do not test <c>AppiumMauiElement</c> itself, which wraps a sealed Appium type and
/// needs a device. They test the contract every control depends on: that an absent capability
/// degrades to the generic path instead of failing.
/// </para>
/// </remarks>
public class CapabilityNegotiationTests : SemanticControlTestsBase
{
    private const string ToggleId = "IncludeProblemReports";

    private void GivenElement(Mock<IMauiElement> element)
    {
        Context
            .Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == ToggleId)))
            .Returns(element.Object);
        Context
            .Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == ToggleId)))
            .Returns(element.Object);
    }

    /// <summary>
    /// A mobile-shaped element: no capability interface, state read from an attribute.
    /// </summary>
    /// <remarks>
    /// Mirrors what <c>AppiumMauiElement</c> exposes on Android, where checked state lives in
    /// the <c>checked</c> attribute and there is no Toggle command to call.
    /// </remarks>
    private static Mock<IMauiElement> CreateAttributeBackedToggle(bool initialState)
    {
        var isChecked = initialState;
        var element = CreateElement(ToggleId, 0, 0, 32, 32);

        element.Setup(e => e.GetAttribute("checked"))
            .Returns(() => isChecked ? "true" : "false");
        element.Setup(e => e.Selected).Returns(() => isChecked);
        element.Setup(e => e.Click()).Callback(() => isChecked = !isChecked);

        return element;
    }

    #region Toggle

    [Fact]
    public void Toggle_UsesTogglePattern_WhenCapabilityIsPresent()
    {
        var element = CreateToggleElement(ToggleId, 0, 0, 32, 32, initialState: false);
        GivenElement(element);

        Page.IncludeProblemReports.Toggle();

        element.As<ITogglePatternElement>().Verify(e => e.TogglePattern(), Times.Once);
        element.Verify(e => e.Click(), Times.Never);
    }

    /// <summary>
    /// The mobile path: no Toggle capability, so the control falls through to a tap.
    /// </summary>
    [Fact]
    public void Toggle_FallsBackToClick_WhenCapabilityIsAbsent()
    {
        var element = CreateAttributeBackedToggle(initialState: false);
        GivenElement(element);

        Page.IncludeProblemReports.Toggle();

        element.Verify(e => e.Click(), Times.Once);
        Assert.True(Page.IncludeProblemReports.IsChecked());
    }

    /// <summary>
    /// State is readable without the capability, from the platform's own attribute.
    /// </summary>
    [Fact]
    public void IsChecked_ReadsAttribute_WhenTogglePatternIsAbsent()
    {
        var element = CreateAttributeBackedToggle(initialState: true);
        GivenElement(element);

        Assert.True(Page.IncludeProblemReports.IsChecked());
    }

    /// <summary>
    /// A capability that is advertised but declines does not end the ladder.
    /// </summary>
    /// <remarks>
    /// <c>SupportsTogglePattern</c> true with <c>TogglePattern()</c> false is the shape of a
    /// pattern that is present but ineffective. Treating that as success is what made
    /// LegacyIAccessible unusable for clicking a Switch (see <see cref="ClickLadderTests"/>);
    /// the control must carry on to the next rung.
    /// </remarks>
    [Fact]
    public void Toggle_ContinuesToClick_WhenTogglePatternDeclines()
    {
        var isChecked = false;
        var element = CreateElement(ToggleId, 0, 0, 32, 32);
        element.Setup(e => e.GetAttribute("checked")).Returns(() => isChecked ? "true" : "false");
        element.Setup(e => e.Selected).Returns(() => isChecked);
        element.Setup(e => e.Click()).Callback(() => isChecked = !isChecked);
        element.As<ITogglePatternElement>().Setup(e => e.SupportsTogglePattern).Returns(true);
        element.As<ITogglePatternElement>().Setup(e => e.IsTogglePatternChecked()).Returns(() => isChecked);
        element.As<ITogglePatternElement>().Setup(e => e.TogglePattern()).Returns(false);
        GivenElement(element);

        Page.IncludeProblemReports.Toggle();

        element.Verify(e => e.Click(), Times.Once);
        Assert.True(Page.IncludeProblemReports.IsChecked());
    }

    #endregion

    #region SetChecked

    [Fact]
    public void SetChecked_IsNoOp_WhenAlreadyInTargetState()
    {
        var element = CreateAttributeBackedToggle(initialState: true);
        GivenElement(element);

        Page.IncludeProblemReports.Check();

        element.Verify(e => e.Click(), Times.Never);
    }

    /// <summary>
    /// Reaching a requested state works without the capability, on the mobile path.
    /// </summary>
    [Fact]
    public void SetChecked_ReachesTargetState_WithoutTogglePattern()
    {
        var element = CreateAttributeBackedToggle(initialState: false);
        GivenElement(element);

        Page.IncludeProblemReports.Check();

        Assert.True(Page.IncludeProblemReports.IsChecked());
        element.Verify(e => e.Click(), Times.Once);
    }

    #endregion
}

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
        // Toggling is what the control asks for; on this platform the element performs it with a
        // tap, which is why AppiumMauiElement implements Toggle as Click. The mock mirrors that:
        // the operation exists and works, without any Toggle pattern behind it.
        element.Setup(e => e.Toggle()).Callback(() => isChecked = !isChecked);

        return element;
    }

    #region Toggle

    /// <summary>
    /// A toggle control asks to be toggled, whatever the platform has underneath.
    /// </summary>
    /// <remarks>
    /// This used to assert that the Toggle <i>pattern</i> was called, which made the control
    /// responsible for a platform detail. It now asks for the operation and the element chooses
    /// the mechanism - the pattern on Windows, a tap on mobile - so the same assertion holds on
    /// both and the next test is the same scenario with the other platform underneath.
    /// </remarks>
    [Fact]
    public void Toggle_AsksTheElementToToggle()
    {
        var element = CreateToggleElement(ToggleId, 0, 0, 32, 32, initialState: false);
        GivenElement(element);

        Page.IncludeProblemReports.Toggle();

        element.Verify(e => e.Toggle(), Times.Once);
        element.As<ITogglePatternElement>().Verify(e => e.TogglePattern(), Times.Never);
        element.Verify(e => e.Click(), Times.Never);
    }

    /// <summary>
    /// The mobile path: the same request, served by a tap inside the element.
    /// </summary>
    /// <remarks>
    /// The control is identical here - it asks to toggle and checks the state moved. What
    /// differs is entirely below the interface, which is the point of the split: no branch in
    /// any control object depends on which platform it is running on.
    /// </remarks>
    [Fact]
    public void Toggle_WorksWithoutATogglePattern()
    {
        var element = CreateAttributeBackedToggle(initialState: false);
        GivenElement(element);

        Page.IncludeProblemReports.Toggle();

        element.Verify(e => e.Toggle(), Times.Once);
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
    /// A toggle that accepts the call and does not move is reported, not worked around.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the case the whole design exists for, kept from the test it replaces but with the
    /// opposite expectation. An operation that reports success without changing anything was
    /// measured twice in this codebase - <c>LegacyIAccessible.DoDefaultAction</c> on a Switch,
    /// and Invoke on a <c>ToolbarItem</c> - and it is undetectable from inside a ladder, because
    /// the rung never admits failure.
    /// </para>
    /// <para>
    /// The old behaviour was to notice the state had not moved and quietly click instead. That
    /// worked, and it meant the suite drove the app by a route it never reported. Checking the
    /// outcome stays; trying something else afterwards does not.
    /// </para>
    /// </remarks>
    [Fact]
    public void Toggle_ReportsAToggleThatDidNothing()
    {
        var element = CreateElement(ToggleId, 0, 0, 32, 32);
        element.Setup(e => e.GetAttribute("checked")).Returns("false");
        element.Setup(e => e.Selected).Returns(false);

        // Accepts the call, changes nothing - exactly what a lying pattern looks like.
        element.Setup(e => e.Toggle());
        GivenElement(element);

        var ex = Assert.Throws<InvalidOperationException>(
            () => Page.IncludeProblemReports.Toggle());

        Assert.Contains("did not change", ex.Message);
        element.Verify(e => e.Click(), Times.Never);
    }

    #endregion

    #region SetChecked

    [Fact]
    public void SetChecked_IsNoOp_WhenAlreadyInTargetState()
    {
        var element = CreateAttributeBackedToggle(initialState: true);
        GivenElement(element);

        Page.IncludeProblemReports.Check();

        element.Verify(e => e.Toggle(), Times.Never);
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
        element.Verify(e => e.Toggle(), Times.Once);
    }

    #endregion
}

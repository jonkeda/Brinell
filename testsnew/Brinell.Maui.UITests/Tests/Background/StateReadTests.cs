using Brinell.Core.Locators;
using Brinell.Maui.Interfaces;
using Brinell.Maui.UITests.Pages;
using Xunit;

namespace Brinell.Maui.UITests.Tests.Background;

/// <summary>
/// Step 23: the assertions that used to pass for the wrong reason.
/// </summary>
/// <remarks>
/// <para>
/// <b>A green test that is right by accident is worse than a red one.</b> Each read here replaced
/// an inference drawn from what the accessibility tree could see, and in every case the inference
/// was true of things it should have been false of. Nobody re-examines a passing test, so these
/// had been reporting for months on something other than what they named.
/// </para>
/// <para>
/// <b>The boundary is what a user could perceive.</b> A source, a progress, whether a load is in
/// flight - not the view model behind them. An assertion needing the view model is a unit test,
/// and arbitrary property reflection would make this a slower one.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "GestureBridge")]
[Trait("Stage", "Background")]
public class StateReadTests
{
    private readonly MauiFixture _fixture;

    public StateReadTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.Open(SamplePage.Display);
    }

    private IMauiElement Element(string automationId)
        => _fixture.Context.TryFindElement(Locator.ByAutomationId(automationId))
           ?? throw new InvalidOperationException($"'{automationId}' was not found.");

    /// <summary>
    /// A working image and a broken one occupy space alike, and are told apart anyway.
    /// </summary>
    /// <remarks>
    /// <b>The headline of this step.</b> The old check was "the element has a non-zero size", and
    /// the layout reserves that box whether the bitmap arrives or not - so it was true of
    /// <c>BrokenImage</c>, whose source names a file that does not exist. The test asserting an
    /// image had loaded was asserting that MAUI had done arithmetic.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task ImageSource_SeparatesABrokenImageFromAWorkingOne()
    {
        var working = Element("TestImage");
        var broken = Element("BrokenImage");

        Assert.Equal("testimage.png", working.ReadState("Source"));
        Assert.Equal("this-file-does-not-exist.png", broken.ReadState("Source"));

        // The old inference, shown to be true of both - which is what made it useless.
        Assert.True(
            working.Size.Width > 0 && broken.Size.Width > 0,
            "Neither image occupies space, so this test is no longer demonstrating the thing it "
            + "was written to demonstrate: that occupying space cannot tell them apart.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Progress comes back in MAUI's units, with no rescaling in between.
    /// </summary>
    /// <remarks>
    /// The client used to read this through the UIA range pattern, where WinUI reports 0-100, and
    /// rescale it against the reported minimum and maximum. That arithmetic is correct and is a
    /// second home for the definition of "progress": a platform reporting a different range, or
    /// none, quietly changes what the number means.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task Progress_IsReportedInMauisOwnUnits()
    {
        var reported = Element("TestProgressBar").ReadState("Progress");

        Assert.True(
            double.TryParse(
                reported,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var progress),
            $"The progress bar answered '{reported}', which is not a number.");

        Assert.InRange(progress, 0d, 1d);

        return Task.CompletedTask;
    }

    /// <summary>
    /// The app can say when it has finished the work it had queued.
    /// </summary>
    /// <remarks>
    /// <b>What <c>AD-004</c> needs to exist.</b> "No arbitrary sleeps" is only a followable rule
    /// if there is something to wait on; without one, a test that needs the UI to settle either
    /// sleeps or invents a sentinel element whose appearance approximates settling. Posting to
    /// the dispatcher and waiting for the callback establishes that everything queued before the
    /// call has run. Asked of the driver rather than an element: a dispatcher belongs to the app,
    /// so every element would give the same answer and each would have to declare a verb that has
    /// nothing to do with it.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task IsIdle_AnswersOnceTheDispatcherHasDrained()
    {
        Assert.True(
            _fixture.Context.Driver.IsIdle(TestConstants.ShortTestTimeoutMs),
            "The app never drained its dispatcher queue, so nothing in the suite can wait on it "
            + "settling and every such wait has to go back to being a sleep.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// An unknown property is refused rather than answered with an empty string.
    /// </summary>
    /// <remarks>
    /// The boundary, enforced. Named cases in the app's provider are the whole surface; a read
    /// that quietly returned empty for a name nobody implemented would let an assertion compare
    /// two blanks and pass.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task ReadState_AnUnknownProperty_IsRefused()
    {
        Assert.Throws<NotSupportedException>(
            () => Element("TestProgressBar").ReadState("BindingContext"));

        return Task.CompletedTask;
    }
}

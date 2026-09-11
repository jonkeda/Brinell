using Brinell.Core.Locators;
using Brinell.Maui.Interfaces;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Gestures;

/// <summary>
/// Step 19: which gesture targets a control object could be pointed at, and which not.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two different questions, and the page answers both.</b> "Can Windows automation see this
/// element" and "can the bridge reach it" are independent: <c>SwipeView</c> is invisible to UIA
/// and answers verbs perfectly well, because the bridge publishes a separate element addressed by
/// <c>AutomationId</c>. Conflating them is what made the gesture API driver-addressed in the
/// first place.
/// </para>
/// <para>
/// <b>Why it is worth a test rather than a paragraph.</b> A control object finds its element and
/// then acts on it, so it can only exist for a target the first question says yes to. These
/// measurements are the precondition for moving the gesture tests onto control-object members,
/// and a change to the app's automation handlers would silently remove it.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "GestureBridge")]
public class GestureAddressabilityTests
{
    private readonly MauiFixture _fixture;

    public GestureAddressabilityTests(MauiFixture fixture) => _fixture = fixture;

    /// <summary>
    /// The gestures page's rows are ordinary elements, and can be found.
    /// </summary>
    /// <remarks>
    /// They are <c>Border</c>s, which resolve on Windows only because the app registers the
    /// Brinell automation handlers - a stock MAUI <c>Border</c> maps to a WinUI
    /// <c>ContentPanel</c> with no peer. This is what makes a control object possible for them
    /// and impossible for <c>SwipeView</c>.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task GestureRows_AreAddressable()
    {
        _fixture.OpenGestures();

        foreach (var id in new[]
                 {
                     GesturesTestPage.Targets.Tap,
                     GesturesTestPage.Targets.DoubleTap,
                     GesturesTestPage.Targets.Swipe,
                     GesturesTestPage.Targets.LongPress,
                     GesturesTestPage.Targets.Sink,
                 })
        {
            Assert.True(
                _fixture.Context.TryFindElement(Locator.ByAutomationId(id)) is not null,
                $"'{id}' could not be found. A control object cannot drive a gesture on an "
                + "element it cannot resolve, so this is the precondition for step 19's "
                + "remaining work. Check that the app still registers the Brinell automation "
                + "handlers for Border.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// A control Windows cannot see still answers a verb.
    /// </summary>
    /// <remarks>
    /// The complement, and the reason <c>IMauiDriver.PerformGesture</c> takes an id rather than
    /// an element. <c>SwipeView</c> maps to a WinUI <c>SwipeControl</c> whose peer must not be
    /// overridden, so nothing will ever find it - and the bridge reaches it anyway.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task SwipeView_IsUnfindableAndStillAnswersItsVerb()
    {
        _fixture.NavigateToContainerModule();

        Assert.Null(_fixture.Context.TryFindElement(Locator.ByAutomationId("TestSwipeView")));

        Assert.True(
            _fixture.Context.Driver.SupportsGesture("TestSwipeView", MauiGesture.SwipeRight),
            "TestSwipeView is neither findable nor on the bridge, so nothing can drive it.");

        return Task.CompletedTask;
    }
}

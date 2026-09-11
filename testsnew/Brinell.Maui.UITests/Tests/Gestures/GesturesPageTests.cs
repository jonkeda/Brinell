using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Gestures;

/// <summary>
/// Step 17: the gestures page exists, opens, and starts from a known state.
/// </summary>
/// <remarks>
/// <para>
/// <b>The arrangement, tested on its own.</b> Every gesture test that follows asserts that a
/// status label changed; all of them would fail identically if the page simply had not opened,
/// or if a row's label were bound to the wrong property. Proving the starting state separately
/// means a later failure is about the verb under test rather than about the page under it.
/// </para>
/// <para>
/// It also pins the thing most likely to rot: the row that declares nothing and the row that
/// declares a verb it cannot serve both assert <i>absence of change</i>, and an assertion like
/// that passes just as well when the label was never found. Reading them here, before anything
/// has been sent, is what makes them mean something afterwards.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "GestureBridge")]
public class GesturesPageTests
{
    private readonly MauiFixture _fixture;

    public GesturesPageTests(MauiFixture fixture) => _fixture = fixture;

    /// <summary>
    /// The hub opens it, and the page reports itself loaded.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task Hub_OpensTheGesturesPage()
    {
        var page = _fixture.OpenGestures();

        Assert.True(
            page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs),
            "The hub has a Gestures button, but the page it opened did not report itself loaded.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Every row is on screen and has had nothing done to it.
    /// </summary>
    /// <remarks>
    /// Asserted row by row rather than in a loop so a failure names which one. The page is built
    /// from seven independent declarations and a mistake in any of them is local to it.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task EveryRow_StartsUntouched()
    {
        var page = _fixture.OpenGestures();
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        Assert.Equal(GesturesTestPage.Untouched, page.TapStatus.GetText());
        Assert.Equal(GesturesTestPage.Untouched, page.DoubleTapStatus.GetText());
        Assert.Equal(GesturesTestPage.Untouched, page.SwipeStatus.GetText());
        Assert.Equal(GesturesTestPage.Untouched, page.LongPressStatus.GetText());
        Assert.Equal(GesturesTestPage.Untouched, page.SinkStatus.GetText());
        Assert.Equal(GesturesTestPage.Untouched, page.NoDeclarationStatus.GetText());
        Assert.Equal(GesturesTestPage.Untouched, page.UnbindableStatus.GetText());

        return Task.CompletedTask;
    }
}

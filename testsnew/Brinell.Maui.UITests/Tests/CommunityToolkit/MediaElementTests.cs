using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.CommunityToolkit;

/// <summary>
/// UI tests for the CommunityToolkit MediaElement, playing a six-second local tone.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "MediaElement")]
public class MediaElementTests
{
    private readonly MauiFixture _fixture;

    public MediaElementTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.Open(SamplePage.CommunityToolkitMedia);
    }

    private CommunityToolkitMediaTestPage GetPage() => new(_fixture.Context);

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task MediaElement_ResolvesAndIsVisible()
    {
        GetPage().TestMediaElement.AssertExists()
            .TestMediaElement.AssertVisible();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsPlaying")]
    public Task MediaElement_DoesNotAutoPlay()
    {
        var page = GetPage();

        page.TestMediaElement.AssertPlaying(false);
        Assert.False(page.TestMediaElement.IsPlaying());
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Play")]
    public Task MediaElement_Play_StartsPlaybackAndAdvances()
    {
        var page = GetPage();

        page.TestMediaElement.Play()
            .TestMediaElement.AssertPlaying(true);

        Assert.True(page.TestMediaElement.WaitProgressPasses(0));
        page.TestMediaElement.Pause();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Pause")]
    public Task MediaElement_PlayThenPause_Pauses()
    {
        GetPage().TestMediaElement.Play()
            .TestMediaElement.Pause()
            .TestMediaElement.AssertPlaying(false);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "SetPlaying")]
    public Task MediaElement_PauseWhenNotPlaying_IsNoOp()
    {
        GetPage().TestMediaElement.Pause()
            .TestMediaElement.SetPlaying(null)
            .TestMediaElement.AssertPlaying(false);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetProgress")]
    public Task MediaElement_Progress_StartsAtZero()
    {
        GetPage().TestMediaElement.AssertProgress(0);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetDuration")]
    public Task MediaElement_Duration_IsClipLength()
    {
        var page = GetPage();

        Assert.True(page.TestMediaElement.WaitDurationKnown());

        var duration = page.TestMediaElement.GetDuration();
        Assert.NotNull(duration);
        Assert.InRange(duration.Value, 5, 7);
        return Task.CompletedTask;
    }
}

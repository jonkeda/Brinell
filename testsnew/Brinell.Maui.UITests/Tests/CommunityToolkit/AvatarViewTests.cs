using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.CommunityToolkit;

/// <summary>
/// UI tests for the CommunityToolkit AvatarView.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "AvatarView")]
public class AvatarViewTests
{
    private readonly MauiFixture _fixture;

    public AvatarViewTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.Open(SamplePage.CommunityToolkit);
    }

    private CommunityToolkitTestPage GetPage() => new(_fixture.Context);

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task AvatarView_IsVisible()
    {
        GetPage().TestAvatarView.AssertExists()
            .TestAvatarView.AssertVisible();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetText")]
    public Task AvatarView_GetText_ReturnsInitials()
    {
        var page = GetPage();

        Assert.Equal("BR", page.TestAvatarView.GetText());
        page.TestAvatarView.AssertText("BR")
            .TestAvatarView.AssertTextEmpty(false);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsShowingImage")]
    public Task AvatarView_WithoutImage_IsNotShowingImage()
    {
        GetPage().TestAvatarView.AssertShowingImage(false);
        return Task.CompletedTask;
    }
}

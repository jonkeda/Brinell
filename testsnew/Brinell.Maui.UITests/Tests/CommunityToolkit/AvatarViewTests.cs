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
            .AssertVisible();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetInitials")]
    public Task AvatarView_GetInitials_ReturnsInitials()
    {
        var page = GetPage();

        Assert.Equal("BR", page.TestAvatarView.GetInitials());
        page.TestAvatarView.AssertInitials("BR")
            .AssertInitialsEmpty(false);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Initials")]
    public Task AvatarView_Initials_IsAPartWithTheInitials()
    {
        GetPage().TestAvatarView.Initials.AssertExists()
            .Initials.AssertInitials("BR");
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

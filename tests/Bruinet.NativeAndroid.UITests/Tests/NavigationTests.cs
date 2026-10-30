namespace Bruinet.NativeAndroid.UITests.Tests;

/// <summary>Tabs, a bottom navigation bar, a toolbar's actions and its up button.</summary>
[Collection(GalleryCollection.Name)]
public sealed class NavigationTests(GalleryFixture gallery)
{
    private NavigationPage Open()
    {
        gallery.Open("navigation");
        var page = new NavigationPage(gallery.Context);
        page.AssertIdle();
        return page;
    }

    [Fact]
    public void Tabs_SelectByTitle()
    {
        var page = Open();

        page.Tabs.AssertSelectedText("One");
        page.Tabs.SelectByText("Three");

        page.Status.AssertText("Tab Three, Home");
        Assert.Equal(["One", "Two", "Three"], page.Tabs.GetItemTexts());
    }

    [Fact]
    public void TheBottomBar_SelectsASection()
    {
        var page = Open();

        page.Sections.SelectByText("Profile");

        page.Status.AssertText("Tab One, Profile");
        page.Sections.AssertSelectedText("Profile");
    }

    [Fact]
    public void AToolbarAction_IsTapped()
    {
        var page = Open();

        page.Toolbar.Action("Refresh").Click();

        page.Status.AssertText("Refresh chosen");
    }

    [Fact]
    public void TheUpButton_LeavesTheScreen()
    {
        var page = Open();

        page.Toolbar.Title.AssertText("Navigation");
        page.Toolbar.NavigationButton.Click();

        page.WaitLoaded(false);
    }
}

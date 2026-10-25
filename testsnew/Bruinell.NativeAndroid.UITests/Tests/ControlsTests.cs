namespace Bruinell.NativeAndroid.UITests.Tests;

/// <summary>Each input control acts once and confirms its effect; the app's status line says what it saw.</summary>
[Collection(GalleryCollection.Name)]
public sealed class ControlsTests(GalleryFixture gallery)
{
    private ControlsPage Open()
    {
        gallery.Open("controls");
        var page = new ControlsPage(gallery.Context);
        page.AssertIdle();
        return page;
    }

    [Fact]
    public void ACheckBox_ChecksAndUnchecks()
    {
        var page = Open();

        page.Agree.Check();
        page.Status.AssertText("Agree on");
        page.Agree.Uncheck();
        page.Status.AssertText("Agree off");
        page.Agree.AssertChecked(false);
    }

    [Fact]
    public void ASwitch_AToggleButton_AndAChip_Toggle()
    {
        var page = Open();

        page.Notifications.Check();
        page.Status.AssertText("Notifications on");
        page.Bold.Toggle();
        page.Status.AssertText("Bold on");
        page.Express.Check();
        page.Status.AssertText("Express on");
        page.Express.AssertChecked(true);
    }

    [Fact]
    public void ARadioGroup_SelectsAnOption()
    {
        var page = Open();

        page.Size.AssertSelectedText("Small");
        page.Size.SelectByText("Large");
        page.Status.AssertText("Size Large");
        page.Size.AssertSelectedText("Large");
        Assert.Equal(["Small", "Medium", "Large"], page.Size.GetItemTexts());
    }

    [Fact]
    public void ASpinner_SelectsAnOption_ScrollingItsPopup()
    {
        var page = Open();

        page.Color.SelectByText("Navy");
        page.Status.AssertText("Color Navy");
        page.Color.AssertSelectedText("Navy");
    }

    [Fact]
    public void ASeekBar_IsSetToAValue()
    {
        var page = Open();

        page.Volume.AssertValue(20);
        page.Volume.SetValue(65);
        page.Status.AssertText("Volume 65");
        page.Volume.Increment();
        page.Volume.AssertValue(66);
    }

    [Fact]
    public void ARatingBar_IsSetToARating()
    {
        var page = Open();

        page.Rating.SetValue(3.5);
        page.Status.AssertText("Rating 3.5");
    }

    [Fact]
    public void AnAutoComplete_ChoosesASuggestion()
    {
        var page = Open();

        page.Country.Append("Ne");
        page.Country.ChooseSuggestion("New Zealand");
        page.Status.AssertText("Country New Zealand");
    }

    [Fact]
    public void AnImage_IsDescribed()
    {
        var page = Open();

        page.Logo.AssertContentDescription("Bruinell logo");
    }

    /// <summary>A control below the fold is not in UiAutomator2's tree: the call scrolls it in, then taps it once.</summary>
    [Fact]
    public void AControlBelowTheFold_IsScrolledIntoTheTree()
    {
        var page = Open();

        page.BottomButton.Click();

        page.Status.AssertText("Bottom tapped");
    }

    [Fact]
    public void Hover_HasNoRouteOnAndroid()
    {
        var page = Open();

        Assert.Throws<RouteUnavailableException>(() => page.Agree.Hover());
    }
}

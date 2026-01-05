using Brinell.Samples.Blazor.UITests.PageObjects;
using Brinell.Samples.Blazor.UITests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.Tests;

/// <summary>
/// Tests for the MediaGallery page functionality.
/// Tests are aligned with actual MediaGallery.razor content.
/// </summary>
[Collection("BlazorUITests")]
public class MediaGalleryTests : BlazorSampleTestBase
{
    public MediaGalleryTests(ITestOutputHelper output) : base(output)
    {
    }

    // ═══════════════════════════════════════════════════════════════
    // PAGE DISPLAY TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MediaGallery_InitialLoad_DisplaysGallery()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.AssertDisplayed("MediaGallery page should be displayed");
        mediaGalleryPage.MediaGalleryTitle.AssertVisible("Title should be visible");
    }

    // ═══════════════════════════════════════════════════════════════
    // FILTER TABS TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MediaGallery_FilterTabs_Exist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.TabAll.AssertExists("All tab should exist");
        mediaGalleryPage.TabImages.AssertExists("Images tab should exist");
        mediaGalleryPage.TabVideos.AssertExists("Videos tab should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // IMAGE VIEWER TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MediaGallery_ImageNavigation_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.PreviousButton.AssertExists("Previous image button should exist");
        mediaGalleryPage.NextButton.AssertExists("Next image button should exist");
    }

    [Fact]
    public void MediaGallery_ImageViewerSection_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.ImageViewerSection.AssertExists("Image viewer section should exist");
        mediaGalleryPage.ViewerTitle.AssertExists("Viewer title should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // THUMBNAIL GALLERY TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MediaGallery_ThumbnailSection_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.ThumbnailSection.AssertExists("Thumbnail section should exist");
        mediaGalleryPage.ThumbnailGrid.AssertExists("Thumbnail grid should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // VIDEO/MEDIA PLAYER TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MediaGallery_VideoPlayerSection_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.VideoPlayerSection.AssertExists("Video player section should exist");
        mediaGalleryPage.VideoPlayerTitle.AssertExists("Video player title should exist");
    }

    [Fact]
    public void MediaGallery_PlaybackControls_Exist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.PlayPauseButton.AssertExists("Play/Pause button should exist");
        mediaGalleryPage.StopButton.AssertExists("Stop button should exist");
        mediaGalleryPage.SkipButton.AssertExists("Skip button should exist");
    }

    [Fact]
    public void MediaGallery_VolumeControls_Exist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.VolumeSlider.AssertExists("Volume slider should exist");
        mediaGalleryPage.MuteSwitch.AssertExists("Mute switch should exist");
    }

    [Fact]
    public void MediaGallery_TimeDisplay_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.CurrentTime.AssertExists("Current time should exist");
        mediaGalleryPage.Duration.AssertExists("Duration should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // WEB CONTENT TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MediaGallery_WebContentSection_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.WebContentSection.AssertExists("Web content section should exist");
        mediaGalleryPage.WebContentTitle.AssertExists("Web content title should exist");
    }

    [Fact]
    public void MediaGallery_UrlControls_Exist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.UrlInput.AssertExists("URL input should exist");
        mediaGalleryPage.GoButton.AssertExists("Go button should exist");
        mediaGalleryPage.BackButton.AssertExists("Back button should exist");
        mediaGalleryPage.ForwardButton.AssertExists("Forward button should exist");
        mediaGalleryPage.ReloadButton.AssertExists("Reload button should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // UPLOAD SECTION TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MediaGallery_UploadSection_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/mediagallery");

        var mediaGalleryPage = new MediaGalleryPage(Context!);
        mediaGalleryPage.WaitForDisplayed();

        // Assert
        mediaGalleryPage.UploadSection.AssertExists("Upload section should exist");
        mediaGalleryPage.UploadTitle.AssertExists("Upload title should exist");
        mediaGalleryPage.UploadButton.AssertExists("Upload button should exist");
        mediaGalleryPage.DropZone.AssertExists("Drop zone should exist");
    }
}

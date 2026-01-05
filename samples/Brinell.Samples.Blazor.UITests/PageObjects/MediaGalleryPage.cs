using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for the MediaGallery page.
/// Uses [data-automation-id='...'] selectors to match MediaGallery.razor
/// </summary>
public class MediaGalleryPage : PageBase
{
    public override string AutomationId => "[data-automation-id='MediaGalleryTitle']";

    // ═══════════════════════════════════════════════════════════════
    // HEADER
    // ═══════════════════════════════════════════════════════════════

    public LabelControl MediaGalleryTitle { get; }

    // ═══════════════════════════════════════════════════════════════
    // FILTER TABS
    // ═══════════════════════════════════════════════════════════════

    public LabelControl MediaTabs { get; }
    public ButtonControl TabAll { get; }
    public ButtonControl TabImages { get; }
    public ButtonControl TabVideos { get; }

    // ═══════════════════════════════════════════════════════════════
    // IMAGE VIEWER
    // ═══════════════════════════════════════════════════════════════

    public LabelControl ImageViewerSection { get; }
    public LabelControl ViewerTitle { get; }
    public ButtonControl PreviousButton { get; }
    public ButtonControl NextButton { get; }
    public LabelControl ImageContainer { get; }

    // ═══════════════════════════════════════════════════════════════
    // THUMBNAIL GALLERY
    // ═══════════════════════════════════════════════════════════════

    public LabelControl ThumbnailSection { get; }
    public LabelControl GalleryTitle { get; }
    public LabelControl ThumbnailGrid { get; }

    // ═══════════════════════════════════════════════════════════════
    // VIDEO/MEDIA PLAYER
    // ═══════════════════════════════════════════════════════════════

    public LabelControl VideoPlayerSection { get; }
    public LabelControl VideoPlayerTitle { get; }
    public LabelControl VideoContainer { get; }
    public ButtonControl PlayPauseButton { get; }
    public ButtonControl StopButton { get; }
    public ButtonControl SkipButton { get; }
    public RangeInputControl VolumeSlider { get; }
    public RangeInputControl ProgressSlider { get; }
    public LabelControl CurrentTime { get; }
    public LabelControl Duration { get; }
    public CheckBoxControl MuteSwitch { get; }

    // ═══════════════════════════════════════════════════════════════
    // WEB CONTENT SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl WebContentSection { get; }
    public LabelControl WebContentTitle { get; }
    public TextInputControl UrlInput { get; }
    public ButtonControl BackButton { get; }
    public ButtonControl ForwardButton { get; }
    public ButtonControl ReloadButton { get; }
    public ButtonControl GoButton { get; }
    public LabelControl WebViewFrame { get; }
    public LabelControl DisplayedUrl { get; }

    // ═══════════════════════════════════════════════════════════════
    // UPLOAD SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl UploadSection { get; }
    public LabelControl UploadTitle { get; }
    public LabelControl DropZone { get; }
    public ButtonControl UploadButton { get; }

    public MediaGalleryPage(SeleniumTestContext context) : base(context)
    {
        // Header
        MediaGalleryTitle = new LabelControl(context, this, "[data-automation-id='MediaGalleryTitle']");

        // Filter tabs
        MediaTabs = new LabelControl(context, this, "[data-automation-id='MediaTabs']");
        TabAll = new ButtonControl(context, this, "[data-automation-id='TabAll']");
        TabImages = new ButtonControl(context, this, "[data-automation-id='TabImages']");
        TabVideos = new ButtonControl(context, this, "[data-automation-id='TabVideos']");

        // Image viewer
        ImageViewerSection = new LabelControl(context, this, "[data-automation-id='ImageViewerSection']");
        ViewerTitle = new LabelControl(context, this, "[data-automation-id='ViewerTitle']");
        PreviousButton = new ButtonControl(context, this, "[data-automation-id='PreviousButton']");
        NextButton = new ButtonControl(context, this, "[data-automation-id='NextButton']");
        ImageContainer = new LabelControl(context, this, "[data-automation-id='ImageContainer']");

        // Thumbnail gallery
        ThumbnailSection = new LabelControl(context, this, "[data-automation-id='ThumbnailSection']");
        GalleryTitle = new LabelControl(context, this, "[data-automation-id='GalleryTitle']");
        ThumbnailGrid = new LabelControl(context, this, "[data-automation-id='ThumbnailGrid']");

        // Video/Media player
        VideoPlayerSection = new LabelControl(context, this, "[data-automation-id='VideoPlayerSection']");
        VideoPlayerTitle = new LabelControl(context, this, "[data-automation-id='VideoPlayerTitle']");
        VideoContainer = new LabelControl(context, this, "[data-automation-id='VideoContainer']");
        PlayPauseButton = new ButtonControl(context, this, "[data-automation-id='PlayPauseButton']");
        StopButton = new ButtonControl(context, this, "[data-automation-id='StopButton']");
        SkipButton = new ButtonControl(context, this, "[data-automation-id='SkipButton']");
        VolumeSlider = new RangeInputControl(context, this, "[data-automation-id='VolumeSlider']");
        ProgressSlider = new RangeInputControl(context, this, "[data-automation-id='ProgressSlider']");
        CurrentTime = new LabelControl(context, this, "[data-automation-id='CurrentTime']");
        Duration = new LabelControl(context, this, "[data-automation-id='Duration']");
        MuteSwitch = new CheckBoxControl(context, this, "[data-automation-id='MuteSwitch']");

        // Web content
        WebContentSection = new LabelControl(context, this, "[data-automation-id='WebContentSection']");
        WebContentTitle = new LabelControl(context, this, "[data-automation-id='WebContentTitle']");
        UrlInput = new TextInputControl(context, this, "[data-automation-id='UrlInput']");
        BackButton = new ButtonControl(context, this, "[data-automation-id='BackButton']");
        ForwardButton = new ButtonControl(context, this, "[data-automation-id='ForwardButton']");
        ReloadButton = new ButtonControl(context, this, "[data-automation-id='ReloadButton']");
        GoButton = new ButtonControl(context, this, "[data-automation-id='GoButton']");
        WebViewFrame = new LabelControl(context, this, "[data-automation-id='WebViewFrame']");
        DisplayedUrl = new LabelControl(context, this, "[data-automation-id='DisplayedUrl']");

        // Upload section
        UploadSection = new LabelControl(context, this, "[data-automation-id='UploadSection']");
        UploadTitle = new LabelControl(context, this, "[data-automation-id='UploadTitle']");
        DropZone = new LabelControl(context, this, "[data-automation-id='DropZone']");
        UploadButton = new ButtonControl(context, this, "[data-automation-id='UploadButton']");
    }

    public override bool IsDisplayed()
    {
        return MediaGalleryTitle.IsVisible();
    }

    // ═══════════════════════════════════════════════════════════════
    // WORKFLOW METHODS
    // ═══════════════════════════════════════════════════════════════

    public MediaGalleryPage FilterAll()
    {
        Log("FilterAll()");
        TabAll.Click();
        return this;
    }

    public MediaGalleryPage FilterImages()
    {
        Log("FilterImages()");
        TabImages.Click();
        return this;
    }

    public MediaGalleryPage FilterVideos()
    {
        Log("FilterVideos()");
        TabVideos.Click();
        return this;
    }

    public MediaGalleryPage NextImage()
    {
        Log("NextImage()");
        NextButton.Click();
        return this;
    }

    public MediaGalleryPage PreviousImage()
    {
        Log("PreviousImage()");
        PreviousButton.Click();
        return this;
    }

    public MediaGalleryPage TogglePlayPause()
    {
        Log("TogglePlayPause()");
        PlayPauseButton.Click();
        return this;
    }

    public MediaGalleryPage StopMedia()
    {
        Log("StopMedia()");
        StopButton.Click();
        return this;
    }

    public MediaGalleryPage Skip()
    {
        Log("Skip()");
        SkipButton.Click();
        return this;
    }

    public MediaGalleryPage SetVolume(int value)
    {
        Log($"SetVolume({value})");
        VolumeSlider.SetValue(value);
        return this;
    }

    public MediaGalleryPage ToggleMute()
    {
        Log("ToggleMute()");
        MuteSwitch.Toggle();
        return this;
    }

    public MediaGalleryPage NavigateToUrl(string url)
    {
        Log($"NavigateToUrl({url})");
        UrlInput.SetText(url);
        GoButton.Click();
        return this;
    }

    public MediaGalleryPage GoBack()
    {
        Log("GoBack()");
        BackButton.Click();
        return this;
    }

    public MediaGalleryPage GoForward()
    {
        Log("GoForward()");
        ForwardButton.Click();
        return this;
    }

    public MediaGalleryPage Reload()
    {
        Log("Reload()");
        ReloadButton.Click();
        return this;
    }
}

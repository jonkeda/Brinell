using Microsoft.Maui.Controls;

namespace Brinell.Maui.UITests.Pages2;

/// <summary>
/// Page object for the MediaGalleryPage of the Brinell sample MAUI app.
/// Exposes controls from MediaGalleryPage.xaml with their AutomationIds.
/// </summary>
public class MediaGalleryPage : PageObjectBase<MediaGalleryPage>
{
    public MediaGalleryPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "MediaGalleryPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Page is loaded when the title label exists
        return MediaGalleryTitle.IsExists();
    }

    #region Labels

    /// <summary>
    /// The main title label "Media Gallery".
    /// </summary>
    public Label<MediaGalleryPage> MediaGalleryTitle => new(this,"MediaGalleryTitle");

    /// <summary>
    /// The image section label.
    /// </summary>
    public Label<MediaGalleryPage> ImageSectionLabel => new(this,"ImageSectionLabel");

    /// <summary>
    /// The media player section label.
    /// </summary>
    public Label<MediaGalleryPage> MediaPlayerLabel => new(this,"MediaPlayerLabel");

    /// <summary>
    /// The web view section label.
    /// </summary>
    public Label<MediaGalleryPage> WebViewLabel => new(this,"WebViewLabel");

    /// <summary>
    /// The media position label.
    /// </summary>
    public Label<MediaGalleryPage> PositionLabel => new(this,"PositionLabel");

    /// <summary>
    /// The media duration label.
    /// </summary>
    public Label<MediaGalleryPage> DurationLabel => new(this,"DurationLabel");

    #endregion

    #region Image Controls

    /// <summary>
    /// The main image display.
    /// </summary>
    public Image<MediaGalleryPage> MainImage => new(this,"MainImage");

    #endregion

    #region Activity Indicator Controls

    /// <summary>
    /// The web loading indicator.
    /// </summary>
    public ActivityIndicator<MediaGalleryPage> WebLoadingIndicator => new(this,"WebLoadingIndicator");

    #endregion

    #region Collection Controls

    /// <summary>
    /// The thumbnail collection view - accessed as generic control.
    /// Note: For typed item access, use CollectionView directly with item factory.
    /// </summary>
    public CollectionView<MediaGalleryPage> ThumbnailCollection => new(this, "ThumbnailCollection");

    #endregion

    #region WebView Controls

    /// <summary>
    /// The content web view.
    /// </summary>
    public WebView<MediaGalleryPage> ContentWebView => new(this,"ContentWebView");

    #endregion

    #region Entry Controls

    /// <summary>
    /// The URL entry field.
    /// </summary>
    public Entry<MediaGalleryPage> UrlEntry => new(this,"UrlEntry");

    #endregion

    #region Slider Controls

    /// <summary>
    /// The media progress slider.
    /// </summary>
    public Slider<MediaGalleryPage> MediaProgressSlider => new(this,"MediaProgressSlider");

    /// <summary>
    /// The volume slider.
    /// </summary>
    public Slider<MediaGalleryPage> VolumeSlider => new(this,"VolumeSlider");

    #endregion

    #region Switch Controls

    /// <summary>
    /// The mute switch.
    /// </summary>
    public Switch<MediaGalleryPage> MuteSwitch => new(this,"MuteSwitch");

    #endregion

    #region Button Controls

    /// <summary>
    /// The navigate button for web view.
    /// </summary>
    public Button<MediaGalleryPage> NavigateButton => new(this,"NavigateButton");

    /// <summary>
    /// The web back button.
    /// </summary>
    public Button<MediaGalleryPage> WebBackButton => new(this,"WebBackButton");

    /// <summary>
    /// The web forward button.
    /// </summary>
    public Button<MediaGalleryPage> WebForwardButton => new(this,"WebForwardButton");

    /// <summary>
    /// The web reload button.
    /// </summary>
    public Button<MediaGalleryPage> WebReloadButton => new(this,"WebReloadButton");

    /// <summary>
    /// The stop playback button.
    /// </summary>
    public Button<MediaGalleryPage> StopButton => new(this,"StopButton");

    /// <summary>
    /// The play/pause toggle button.
    /// </summary>
    public Button<MediaGalleryPage> PlayPauseButton => new(this,"PlayPauseButton");

    /// <summary>
    /// The pause button.
    /// </summary>
    public Button<MediaGalleryPage> PauseButton => new(this,"PauseButton");

    #endregion
}

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
    public Label<MediaGalleryPage> MediaGalleryTitle => Label("MediaGalleryTitle");

    /// <summary>
    /// The image section label.
    /// </summary>
    public Label<MediaGalleryPage> ImageSectionLabel => Label("ImageSectionLabel");

    /// <summary>
    /// The media player section label.
    /// </summary>
    public Label<MediaGalleryPage> MediaPlayerLabel => Label("MediaPlayerLabel");

    /// <summary>
    /// The web view section label.
    /// </summary>
    public Label<MediaGalleryPage> WebViewLabel => Label("WebViewLabel");

    /// <summary>
    /// The media position label.
    /// </summary>
    public Label<MediaGalleryPage> PositionLabel => Label("PositionLabel");

    /// <summary>
    /// The media duration label.
    /// </summary>
    public Label<MediaGalleryPage> DurationLabel => Label("DurationLabel");

    #endregion

    #region Image Controls

    /// <summary>
    /// The main image display.
    /// </summary>
    public Image<MediaGalleryPage> MainImage => Image("MainImage");

    #endregion

    #region Activity Indicator Controls

    /// <summary>
    /// The web loading indicator.
    /// </summary>
    public ActivityIndicator<MediaGalleryPage> WebLoadingIndicator => ActivityIndicator("WebLoadingIndicator");

    #endregion

    #region Collection Controls

    /// <summary>
    /// The thumbnail collection view - accessed as generic control.
    /// Note: For typed item access, use CollectionView directly with item factory.
    /// </summary>
    public CollectionView<MediaGalleryPage> ThumbnailCollection => CollectionView("ThumbnailCollection");

    #endregion

    #region WebView Controls

    /// <summary>
    /// The content web view.
    /// </summary>
    public WebView<MediaGalleryPage> ContentWebView => WebView("ContentWebView");

    #endregion

    #region Entry Controls

    /// <summary>
    /// The URL entry field.
    /// </summary>
    public Entry<MediaGalleryPage> UrlEntry => Entry("UrlEntry");

    #endregion

    #region Slider Controls

    /// <summary>
    /// The media progress slider.
    /// </summary>
    public Slider<MediaGalleryPage> MediaProgressSlider => Slider("MediaProgressSlider");

    /// <summary>
    /// The volume slider.
    /// </summary>
    public Slider<MediaGalleryPage> VolumeSlider => Slider("VolumeSlider");

    #endregion

    #region Switch Controls

    /// <summary>
    /// The mute switch.
    /// </summary>
    public Switch<MediaGalleryPage> MuteSwitch => Switch("MuteSwitch");

    #endregion

    #region Button Controls

    /// <summary>
    /// The navigate button for web view.
    /// </summary>
    public Button<MediaGalleryPage> NavigateButton => Button("NavigateButton");

    /// <summary>
    /// The web back button.
    /// </summary>
    public Button<MediaGalleryPage> WebBackButton => Button("WebBackButton");

    /// <summary>
    /// The web forward button.
    /// </summary>
    public Button<MediaGalleryPage> WebForwardButton => Button("WebForwardButton");

    /// <summary>
    /// The web reload button.
    /// </summary>
    public Button<MediaGalleryPage> WebReloadButton => Button("WebReloadButton");

    /// <summary>
    /// The stop playback button.
    /// </summary>
    public Button<MediaGalleryPage> StopButton => Button("StopButton");

    /// <summary>
    /// The play/pause toggle button.
    /// </summary>
    public Button<MediaGalleryPage> PlayPauseButton => Button("PlayPauseButton");

    /// <summary>
    /// The pause button.
    /// </summary>
    public Button<MediaGalleryPage> PauseButton => Button("PauseButton");

    #endregion
}

using Brinell.Html.Playwright.Controls;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Samples.Blazor.PlaywrightTests.PageObjects;

/// <summary>
/// Page object for the Blazor Form Controls page (Playwright version).
/// </summary>
public class FormControlsPage : PageBase
{
    #region Section Titles

    /// <summary>
    /// The main page title.
    /// </summary>
    public LabelControl PageTitle { get; }

    /// <summary>
    /// The checkbox section title.
    /// </summary>
    public LabelControl CheckboxSectionTitle { get; }

    /// <summary>
    /// The select section title.
    /// </summary>
    public LabelControl SelectSectionTitle { get; }

    /// <summary>
    /// The links section title.
    /// </summary>
    public LabelControl LinksSectionTitle { get; }

    /// <summary>
    /// The textarea section title.
    /// </summary>
    public LabelControl TextAreaSectionTitle { get; }

    /// <summary>
    /// The range section title.
    /// </summary>
    public LabelControl RangeSectionTitle { get; }

    /// <summary>
    /// The progress section title.
    /// </summary>
    public LabelControl ProgressSectionTitle { get; }

    #endregion

    #region Checkbox Controls

    /// <summary>
    /// The terms and conditions checkbox.
    /// </summary>
    public CheckBoxControl TermsCheckbox { get; }

    /// <summary>
    /// The newsletter subscription checkbox.
    /// </summary>
    public CheckBoxControl NewsletterCheckbox { get; }

    /// <summary>
    /// The disabled checkbox.
    /// </summary>
    public CheckBoxControl DisabledCheckbox { get; }

    /// <summary>
    /// The checkbox status label.
    /// </summary>
    public LabelControl CheckboxStatus { get; }

    #endregion

    #region Select Controls

    /// <summary>
    /// The country selection dropdown.
    /// </summary>
    public SelectControl CountrySelect { get; }

    /// <summary>
    /// The colors select (single selection).
    /// </summary>
    public SelectControl ColorsSelect { get; }

    /// <summary>
    /// The select status label.
    /// </summary>
    public LabelControl SelectStatus { get; }

    #endregion

    #region Link Controls

    /// <summary>
    /// The internal navigation link.
    /// </summary>
    public LinkControl InternalLink { get; }

    /// <summary>
    /// The external link that opens in a new tab.
    /// </summary>
    public LinkControl ExternalLink { get; }

    /// <summary>
    /// The download link.
    /// </summary>
    public LinkControl DownloadLink { get; }

    #endregion

    #region TextArea Controls

    /// <summary>
    /// The comments text area.
    /// </summary>
    public TextAreaControl CommentsTextArea { get; }

    /// <summary>
    /// The textarea status label.
    /// </summary>
    public LabelControl TextAreaStatus { get; }

    #endregion

    #region Range Controls

    /// <summary>
    /// The volume range slider.
    /// </summary>
    public RangeInputControl VolumeRange { get; }

    /// <summary>
    /// The brightness range slider.
    /// </summary>
    public RangeInputControl BrightnessRange { get; }

    #endregion

    #region Progress Controls

    /// <summary>
    /// The upload progress bar.
    /// </summary>
    public ProgressControl UploadProgress { get; }

    /// <summary>
    /// The indeterminate progress bar.
    /// </summary>
    public ProgressControl IndeterminateProgress { get; }

    /// <summary>
    /// The simulate upload button.
    /// </summary>
    public ButtonControl SimulateUploadButton { get; }

    /// <summary>
    /// The progress status label.
    /// </summary>
    public LabelControl ProgressStatus { get; }

    #endregion

    public FormControlsPage(PlaywrightTestContext context)
        : base(context)
    {
        // Section titles
        PageTitle = new LabelControl(context, this, "#form-controls-title");
        CheckboxSectionTitle = new LabelControl(context, this, "#checkbox-section-title");
        SelectSectionTitle = new LabelControl(context, this, "#select-section-title");
        LinksSectionTitle = new LabelControl(context, this, "#links-section-title");
        TextAreaSectionTitle = new LabelControl(context, this, "#textarea-section-title");
        RangeSectionTitle = new LabelControl(context, this, "#range-section-title");
        ProgressSectionTitle = new LabelControl(context, this, "#progress-section-title");

        // Checkbox controls
        TermsCheckbox = new CheckBoxControl(context, this, "#terms-checkbox");
        NewsletterCheckbox = new CheckBoxControl(context, this, "#newsletter-checkbox");
        DisabledCheckbox = new CheckBoxControl(context, this, "#disabled-checkbox");
        CheckboxStatus = new LabelControl(context, this, "#checkbox-status");

        // Select controls
        CountrySelect = new SelectControl(context, this, "#country-select");
        ColorsSelect = new SelectControl(context, this, "#colors-select");
        SelectStatus = new LabelControl(context, this, "#select-status");

        // Link controls
        InternalLink = new LinkControl(context, this, "#internal-link");
        ExternalLink = new LinkControl(context, this, "#external-link");
        DownloadLink = new LinkControl(context, this, "#download-link");

        // TextArea controls
        CommentsTextArea = new TextAreaControl(context, this, "#comments-textarea");
        TextAreaStatus = new LabelControl(context, this, "#textarea-status");

        // Range controls
        VolumeRange = new RangeInputControl(context, this, "#volume-range");
        BrightnessRange = new RangeInputControl(context, this, "#brightness-range");

        // Progress controls
        UploadProgress = new ProgressControl(context, this, "#upload-progress");
        IndeterminateProgress = new ProgressControl(context, this, "#indeterminate-progress");
        SimulateUploadButton = new ButtonControl(context, this, "#simulate-upload-btn");
        ProgressStatus = new LabelControl(context, this, "#progress-status");
    }

    /// <summary>
    /// CSS selector that identifies this page.
    /// </summary>
    public override string AutomationId => "#form-controls-title";

    /// <summary>
    /// Check if the form controls page is displayed.
    /// </summary>
    public override bool IsDisplayed()
    {
        return PageTitle.IsVisible() && PageTitle.GetText() == "Form Controls";
    }

    /// <summary>
    /// Check if the form controls page is displayed asynchronously.
    /// </summary>
    public override async Task<bool> IsDisplayedAsync()
    {
        if (!await PageTitle.IsVisibleAsync())
            return false;
        var text = await PageTitle.GetTextAsync();
        return text == "Form Controls";
    }

    #region Actions

    /// <summary>
    /// Accept terms and conditions.
    /// </summary>
    public async Task AcceptTermsAsync()
    {
        Log("AcceptTermsAsync()");
        await TermsCheckbox.CheckAsync();
    }

    /// <summary>
    /// Subscribe to newsletter.
    /// </summary>
    public async Task SubscribeToNewsletterAsync()
    {
        Log("SubscribeToNewsletterAsync()");
        await NewsletterCheckbox.CheckAsync();
    }

    /// <summary>
    /// Select a country.
    /// </summary>
    public async Task SelectCountryAsync(string value)
    {
        Log($"SelectCountryAsync({value})");
        await CountrySelect.SelectByValueAsync(value);
    }

    /// <summary>
    /// Enter comments.
    /// </summary>
    public async Task EnterCommentsAsync(string text)
    {
        Log($"EnterCommentsAsync({text})");
        await CommentsTextArea.ClearAndEnterAsync(text);
    }

    /// <summary>
    /// Set volume level.
    /// </summary>
    public async Task SetVolumeAsync(double value)
    {
        Log($"SetVolumeAsync({value})");
        await VolumeRange.SetValueAsync(value);
    }

    /// <summary>
    /// Click simulate upload button.
    /// </summary>
    public async Task SimulateUploadAsync()
    {
        Log("SimulateUploadAsync()");
        await SimulateUploadButton.ClickAsync();
    }

    #endregion
}

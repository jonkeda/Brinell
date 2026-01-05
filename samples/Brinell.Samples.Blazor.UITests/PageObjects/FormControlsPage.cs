using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for the FormControls page.
/// </summary>
public class FormControlsPage : PageBase
{
    public override string AutomationId => "#form-controls-title";

    // ═══════════════════════════════════════════════════════════════
    // HEADER
    // ═══════════════════════════════════════════════════════════════

    public LabelControl FormControlsTitle { get; }

    // ═══════════════════════════════════════════════════════════════
    // CHECKBOX SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl CheckboxSectionTitle { get; }
    public CheckBoxControl TermsCheckbox { get; }
    public CheckBoxControl NewsletterCheckbox { get; }
    public CheckBoxControl DisabledCheckbox { get; }
    public LabelControl CheckboxStatus { get; }

    // ═══════════════════════════════════════════════════════════════
    // SELECT SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl SelectSectionTitle { get; }
    public SelectControl CountrySelect { get; }
    public SelectControl ColorsSelect { get; }
    public LabelControl SelectStatus { get; }

    // ═══════════════════════════════════════════════════════════════
    // LINKS SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl LinksSectionTitle { get; }
    public LinkControl InternalLink { get; }
    public LinkControl ExternalLink { get; }
    public LinkControl DownloadLink { get; }

    // ═══════════════════════════════════════════════════════════════
    // TEXTAREA SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl TextAreaSectionTitle { get; }
    public TextAreaControl CommentsTextArea { get; }
    public LabelControl TextAreaStatus { get; }

    // ═══════════════════════════════════════════════════════════════
    // RANGE SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl RangeSectionTitle { get; }
    public RangeInputControl VolumeRange { get; }
    public RangeInputControl BrightnessRange { get; }

    // ═══════════════════════════════════════════════════════════════
    // PROGRESS SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl ProgressSectionTitle { get; }
    public ProgressControl UploadProgress { get; }
    public LabelControl ProgressStatus { get; }
    public ButtonControl SimulateUploadButton { get; }

    public FormControlsPage(SeleniumTestContext context) : base(context)
    {
        FormControlsTitle = new LabelControl(context, this, "#form-controls-title");

        // Checkbox section
        CheckboxSectionTitle = new LabelControl(context, this, "#checkbox-section-title");
        TermsCheckbox = new CheckBoxControl(context, this, "#terms-checkbox");
        NewsletterCheckbox = new CheckBoxControl(context, this, "#newsletter-checkbox");
        DisabledCheckbox = new CheckBoxControl(context, this, "#disabled-checkbox");
        CheckboxStatus = new LabelControl(context, this, "#checkbox-status");

        // Select section
        SelectSectionTitle = new LabelControl(context, this, "#select-section-title");
        CountrySelect = new SelectControl(context, this, "#country-select");
        ColorsSelect = new SelectControl(context, this, "#colors-select");
        SelectStatus = new LabelControl(context, this, "#select-status");

        // Links section
        LinksSectionTitle = new LabelControl(context, this, "#links-section-title");
        InternalLink = new LinkControl(context, this, "#internal-link");
        ExternalLink = new LinkControl(context, this, "#external-link");
        DownloadLink = new LinkControl(context, this, "#download-link");

        // TextArea section
        TextAreaSectionTitle = new LabelControl(context, this, "#textarea-section-title");
        CommentsTextArea = new TextAreaControl(context, this, "#comments-textarea");
        TextAreaStatus = new LabelControl(context, this, "#textarea-status");

        // Range section
        RangeSectionTitle = new LabelControl(context, this, "#range-section-title");
        VolumeRange = new RangeInputControl(context, this, "#volume-range");
        BrightnessRange = new RangeInputControl(context, this, "#brightness-range");

        // Progress section
        ProgressSectionTitle = new LabelControl(context, this, "#progress-section-title");
        UploadProgress = new ProgressControl(context, this, "#upload-progress");
        ProgressStatus = new LabelControl(context, this, "#progress-status");
        SimulateUploadButton = new ButtonControl(context, this, "#simulate-upload-btn");
    }

    public override bool IsDisplayed()
    {
        return FormControlsTitle.IsVisible();
    }

    // ═══════════════════════════════════════════════════════════════
    // WORKFLOW METHODS
    // ═══════════════════════════════════════════════════════════════

    public FormControlsPage AcceptTerms()
    {
        Log("AcceptTerms()");
        TermsCheckbox.Check();
        return this;
    }

    public FormControlsPage ToggleNewsletter()
    {
        Log("ToggleNewsletter()");
        NewsletterCheckbox.Toggle();
        return this;
    }

    public FormControlsPage SelectCountry(string country)
    {
        Log($"SelectCountry({country})");
        CountrySelect.SelectByText(country);
        return this;
    }

    public FormControlsPage SelectColor(string color)
    {
        Log($"SelectColor({color})");
        ColorsSelect.SelectByText(color);
        return this;
    }

    public FormControlsPage EnterComments(string text)
    {
        Log($"EnterComments({text.Substring(0, Math.Min(20, text.Length))}...)");
        CommentsTextArea.ClearAndEnter(text);
        return this;
    }

    public FormControlsPage SetVolume(int value)
    {
        Log($"SetVolume({value})");
        VolumeRange.SetValue(value);
        return this;
    }

    public FormControlsPage SetBrightness(int value)
    {
        Log($"SetBrightness({value})");
        BrightnessRange.SetValue(value);
        return this;
    }

    public FormControlsPage SimulateUpload()
    {
        Log("SimulateUpload()");
        SimulateUploadButton.Click();
        return this;
    }

    public CounterPage NavigateToCounter()
    {
        Log("NavigateToCounter()");
        InternalLink.Click();
        var counterPage = new CounterPage(_context);
        counterPage.WaitForDisplayed();
        return counterPage;
    }
}

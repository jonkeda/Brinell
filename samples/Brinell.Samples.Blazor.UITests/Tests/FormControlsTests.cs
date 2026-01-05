using Brinell.Samples.Blazor.UITests.PageObjects;
using Brinell.Samples.Blazor.UITests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.Tests;

/// <summary>
/// Tests for the FormControls page functionality.
/// </summary>
[Collection("BlazorUITests")]
public class FormControlsTests : BlazorSampleTestBase
{
    public FormControlsTests(ITestOutputHelper output) : base(output)
    {
    }

    // ═══════════════════════════════════════════════════════════════
    // PAGE DISPLAY TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FormControls_InitialLoad_DisplaysAllSections()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/form-controls");

        var formControlsPage = new FormControlsPage(Context!);
        formControlsPage.WaitForDisplayed();

        // Assert
        formControlsPage.AssertDisplayed("FormControls page should be displayed");
        formControlsPage.FormControlsTitle.AssertVisible("Title should be visible");
    }

    // ═══════════════════════════════════════════════════════════════
    // CHECKBOX TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FormControls_Checkbox_CanBeChecked()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/form-controls");

        var formControlsPage = new FormControlsPage(Context!);
        formControlsPage.WaitForDisplayed();

        // Act
        formControlsPage.AcceptTerms();

        // Assert
        formControlsPage.TermsCheckbox.AssertChecked("Terms checkbox should be checked");
    }

    [Fact]
    public void FormControls_Newsletter_CanBeToggled()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/form-controls");

        var formControlsPage = new FormControlsPage(Context!);
        formControlsPage.WaitForDisplayed();

        // Act
        formControlsPage.ToggleNewsletter();

        // Assert
        formControlsPage.NewsletterCheckbox.AssertExists("Newsletter checkbox should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // SELECT TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FormControls_Select_CanSelectCountry()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/form-controls");

        var formControlsPage = new FormControlsPage(Context!);
        formControlsPage.WaitForDisplayed();

        // Assert selects exist
        formControlsPage.CountrySelect.AssertVisible("Country select should be visible");
    }

    // ═══════════════════════════════════════════════════════════════
    // TEXTAREA TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FormControls_TextArea_CanEnterText()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/form-controls");

        var formControlsPage = new FormControlsPage(Context!);
        formControlsPage.WaitForDisplayed();

        // Act
        formControlsPage.EnterComments("This is a test comment for automation testing.");

        // Assert
        formControlsPage.CommentsTextArea.AssertTextContains("test comment");
    }

    // ═══════════════════════════════════════════════════════════════
    // RANGE INPUT TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FormControls_RangeInputs_Exist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/form-controls");

        var formControlsPage = new FormControlsPage(Context!);
        formControlsPage.WaitForDisplayed();

        // Assert
        formControlsPage.VolumeRange.AssertVisible("Volume range should be visible");
        formControlsPage.BrightnessRange.AssertVisible("Brightness range should be visible");
    }

    // ═══════════════════════════════════════════════════════════════
    // PROGRESS TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FormControls_Progress_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/form-controls");

        var formControlsPage = new FormControlsPage(Context!);
        formControlsPage.WaitForDisplayed();

        // Assert
        formControlsPage.UploadProgress.AssertExists("Upload progress should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // LINK TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FormControls_Links_AreVisible()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/form-controls");

        var formControlsPage = new FormControlsPage(Context!);
        formControlsPage.WaitForDisplayed();

        // Assert
        formControlsPage.InternalLink.AssertVisible("Internal link should be visible");
        formControlsPage.ExternalLink.AssertVisible("External link should be visible");
    }
}

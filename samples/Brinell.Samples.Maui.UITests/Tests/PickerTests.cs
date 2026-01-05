using Brinell.Samples.Maui.UITests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.Tests;

/// <summary>
/// Tests for Picker, DatePicker, and TimePicker controls on MainPage.
/// </summary>
public class PickerTests : MauiTestBase
{
    private readonly MainPageObject _mainPage;

    public PickerTests(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject(Context);
    }

    // ═══════════════════════════════════════════════════════════════
    // PICKER CONTROL TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ColorPicker_IsVisible_OnPageLoad()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Assert
        _mainPage.ColorPicker.AssertExists("ColorPicker should exist on page");
    }

    [Fact]
    public void ColorPicker_InitialState_NoColorSelected()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Assert
        _mainPage.SelectedColorLabel.AssertTextContains("No color selected");
    }

    [Fact]
    public void ColorPicker_CanInteract_IsEnabled()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Assert
        _mainPage.ColorPicker.AssertEnabled("ColorPicker should be enabled for interaction");
    }

    [Fact]
    public void ColorPicker_SelectByIndex_UpdatesSelection()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Act - select first color (Red)
        _mainPage.ColorPicker.SelectByIndex(0);

        // Assert - label should update to show selected color
        var labelText = _mainPage.SelectedColorLabel.GetText();
        Assert.Contains("Red", labelText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ColorPicker_SelectByText_UpdatesSelection()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Act - select by text
        _mainPage.ColorPicker.SelectByText("Green");

        // Assert - label should update to show selected color
        var labelText = _mainPage.SelectedColorLabel.GetText();
        Assert.Contains("Green", labelText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ColorPicker_SelectDifferentColors_UpdatesLabel()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Act & Assert - select multiple colors and verify
        _mainPage.ColorPicker.SelectByIndex(0); // Red
        Assert.Contains("Red", _mainPage.SelectedColorLabel.GetText(), StringComparison.OrdinalIgnoreCase);

        _mainPage.ColorPicker.SelectByIndex(1); // Green
        Assert.Contains("Green", _mainPage.SelectedColorLabel.GetText(), StringComparison.OrdinalIgnoreCase);

        _mainPage.ColorPicker.SelectByIndex(2); // Blue
        Assert.Contains("Blue", _mainPage.SelectedColorLabel.GetText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectedColorLabel_InitialState_DisplaysDefaultText()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Assert - verify label exists and has expected initial state
        _mainPage.SelectedColorLabel.AssertExists("Selected color label should exist");
        _mainPage.SelectedColorLabel.AssertVisible("Selected color label should be visible");
    }

    // ═══════════════════════════════════════════════════════════════
    // DATEPICKER CONTROL TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DatePicker_IsVisible_OnPageLoad()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Assert
        _mainPage.BirthDatePicker.AssertExists("DatePicker should exist on page");
    }

    [Fact]
    public void DatePicker_CanInteract_IsEnabled()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Assert
        _mainPage.BirthDatePicker.AssertEnabled("DatePicker should be enabled");
    }

    // ═══════════════════════════════════════════════════════════════
    // TIMEPICKER CONTROL TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TimePicker_IsVisible_OnPageLoad()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Assert
        _mainPage.ReminderTimePicker.AssertExists("TimePicker should exist on page");
    }

    [Fact]
    public void TimePicker_CanInteract_IsEnabled()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Assert
        _mainPage.ReminderTimePicker.AssertEnabled("TimePicker should be enabled");
    }
}

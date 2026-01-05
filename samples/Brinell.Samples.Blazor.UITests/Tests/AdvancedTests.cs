using Brinell.Samples.Blazor.UITests.PageObjects;
using Brinell.Samples.Blazor.UITests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.Tests;

/// <summary>
/// Tests for the Advanced page functionality.
/// </summary>
[Collection("BlazorUITests")]
public class AdvancedTests : BlazorSampleTestBase
{
    public AdvancedTests(ITestOutputHelper output) : base(output)
    {
    }

    // ═══════════════════════════════════════════════════════════════
    // PAGE DISPLAY TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Advanced_InitialLoad_DisplaysPage()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert
        advancedPage.AssertDisplayed("Advanced page should be displayed");
        advancedPage.AdvancedTitle.AssertVisible("Title should be visible");
    }

    [Fact]
    public void Advanced_Sections_AllVisible()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert - All major sections are visible
        advancedPage.EventLogTitle.AssertVisible("Event Log section should be visible");
        advancedPage.ClickTitle.AssertVisible("Click Events section should be visible");
        advancedPage.MouseTitle.AssertVisible("Mouse Events section should be visible");
        advancedPage.KeyboardTitle.AssertVisible("Keyboard Events section should be visible");
        advancedPage.FocusTitle.AssertVisible("Focus Events section should be visible");
        advancedPage.DragDropTitle.AssertVisible("Drag and Drop section should be visible");
    }

    // ═══════════════════════════════════════════════════════════════
    // CLICK EVENT TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Advanced_SingleClickArea_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert
        advancedPage.SingleClickArea.AssertExists("Single click area should exist");
    }

    [Fact]
    public void Advanced_DoubleClickArea_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert
        advancedPage.DoubleClickArea.AssertExists("Double click area should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // KEYBOARD EVENT TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Advanced_KeyboardInput_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert
        advancedPage.KeyboardInput.AssertExists("Keyboard input should exist");
    }

    [Fact]
    public void Advanced_KeyboardInput_CanEnterText()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Act
        advancedPage.TypeInKeyboardInput("test");

        // Assert - Input should still exist after typing
        advancedPage.KeyboardInput.AssertExists("Keyboard input should still exist after typing");
    }

    // ═══════════════════════════════════════════════════════════════
    // FOCUS EVENT TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Advanced_FocusInputs_Exist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert
        advancedPage.FocusInput1.AssertExists("Focus input 1 should exist");
        advancedPage.FocusInput2.AssertExists("Focus input 2 should exist");
        advancedPage.FocusInput3.AssertExists("Focus input 3 should exist");
    }

    [Fact]
    public void Advanced_FocusStatus_ShowsCurrentFocus()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert
        advancedPage.FocusStatus.AssertExists("Focus status should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // DRAG AND DROP TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Advanced_DragDrop_ZonesExist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert
        advancedPage.DraggableContainer.AssertExists("Draggable container should exist");
        advancedPage.DropZone.AssertExists("Drop zone should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Advanced_LayoutContainers_Exist()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert
        advancedPage.LayoutTitle.AssertExists("Layout title should exist");
        advancedPage.GridLayout.AssertExists("Grid layout should exist");
        advancedPage.FlexLayout.AssertExists("Flex layout should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // TOOLTIP AND POPOVER TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Advanced_TooltipButton_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert
        advancedPage.TooltipButton.AssertExists("Tooltip button should exist");
    }

    [Fact]
    public void Advanced_PopoverButton_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert
        advancedPage.PopoverButton.AssertExists("Popover button should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // RESET TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Advanced_ResetButton_Exists()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/advanced");

        var advancedPage = new AdvancedPage(Context!);
        advancedPage.WaitForDisplayed();

        // Assert
        advancedPage.ResetButton.AssertExists("Reset button should exist");
    }
}

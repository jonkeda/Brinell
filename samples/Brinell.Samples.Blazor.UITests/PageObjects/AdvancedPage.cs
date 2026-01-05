using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for the Advanced page demonstrating advanced HTML features.
/// </summary>
public class AdvancedPage : PageBase
{
    public override string AutomationId => "[data-automation-id='AdvancedTitle']";

    // ═══════════════════════════════════════════════════════════════
    // HEADER
    // ═══════════════════════════════════════════════════════════════

    public LabelControl AdvancedTitle { get; }
    public LabelControl AdvancedSubtitle { get; }

    // ═══════════════════════════════════════════════════════════════
    // EVENT LOG SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl EventLogTitle { get; }
    public ButtonControl ClearLogButton { get; }
    public LabelControl EventLogContent { get; }

    // ═══════════════════════════════════════════════════════════════
    // CLICK EVENTS SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl ClickTitle { get; }
    public LabelControl SingleClickArea { get; }
    public LabelControl DoubleClickArea { get; }
    public LabelControl RightClickArea { get; }

    // ═══════════════════════════════════════════════════════════════
    // MOUSE EVENTS SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl MouseTitle { get; }
    public LabelControl HoverArea { get; }
    public LabelControl MouseTrackArea { get; }
    public LabelControl MousePosition { get; }

    // ═══════════════════════════════════════════════════════════════
    // KEYBOARD EVENTS SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl KeyboardTitle { get; }
    public TextInputControl KeyboardInput { get; }
    public LabelControl LastKeyValue { get; }
    public LabelControl KeyCodeValue { get; }
    public LabelControl ModifiersValue { get; }

    // ═══════════════════════════════════════════════════════════════
    // FOCUS EVENTS SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl FocusTitle { get; }
    public TextInputControl FocusInput1 { get; }
    public TextInputControl FocusInput2 { get; }
    public TextInputControl FocusInput3 { get; }
    public LabelControl FocusStatus { get; }

    // ═══════════════════════════════════════════════════════════════
    // DRAG AND DROP SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl DragDropTitle { get; }
    public LabelControl DraggableContainer { get; }
    public LabelControl DropZone { get; }

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl LayoutTitle { get; }
    public LabelControl GridLayout { get; }
    public LabelControl FlexLayout { get; }

    // ═══════════════════════════════════════════════════════════════
    // TOOLTIP AND POPOVER SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl TooltipTitle { get; }
    public ButtonControl TooltipButton { get; }
    public ButtonControl PopoverButton { get; }

    // ═══════════════════════════════════════════════════════════════
    // RESET
    // ═══════════════════════════════════════════════════════════════

    public ButtonControl ResetButton { get; }

    public AdvancedPage(SeleniumTestContext context) : base(context)
    {
        AdvancedTitle = new LabelControl(context, this, "[data-automation-id='AdvancedTitle']");
        AdvancedSubtitle = new LabelControl(context, this, "[data-automation-id='AdvancedSubtitle']");

        // Event Log
        EventLogTitle = new LabelControl(context, this, "[data-automation-id='EventLogTitle']");
        ClearLogButton = new ButtonControl(context, this, "[data-automation-id='ClearLogButton']");
        EventLogContent = new LabelControl(context, this, "[data-automation-id='EventLogContent']");

        // Click Events
        ClickTitle = new LabelControl(context, this, "[data-automation-id='ClickTitle']");
        SingleClickArea = new LabelControl(context, this, "[data-automation-id='SingleClickArea']");
        DoubleClickArea = new LabelControl(context, this, "[data-automation-id='DoubleClickArea']");
        RightClickArea = new LabelControl(context, this, "[data-automation-id='RightClickArea']");

        // Mouse Events
        MouseTitle = new LabelControl(context, this, "[data-automation-id='MouseTitle']");
        HoverArea = new LabelControl(context, this, "[data-automation-id='HoverArea']");
        MouseTrackArea = new LabelControl(context, this, "[data-automation-id='MouseTrackArea']");
        MousePosition = new LabelControl(context, this, "[data-automation-id='MousePosition']");

        // Keyboard Events
        KeyboardTitle = new LabelControl(context, this, "[data-automation-id='KeyboardTitle']");
        KeyboardInput = new TextInputControl(context, this, "[data-automation-id='KeyboardInput']");
        LastKeyValue = new LabelControl(context, this, "[data-automation-id='LastKeyValue']");
        KeyCodeValue = new LabelControl(context, this, "[data-automation-id='KeyCodeValue']");
        ModifiersValue = new LabelControl(context, this, "[data-automation-id='ModifiersValue']");

        // Focus Events
        FocusTitle = new LabelControl(context, this, "[data-automation-id='FocusTitle']");
        FocusInput1 = new TextInputControl(context, this, "[data-automation-id='FocusInput1']");
        FocusInput2 = new TextInputControl(context, this, "[data-automation-id='FocusInput2']");
        FocusInput3 = new TextInputControl(context, this, "[data-automation-id='FocusInput3']");
        FocusStatus = new LabelControl(context, this, "[data-automation-id='FocusStatus']");

        // Drag and Drop
        DragDropTitle = new LabelControl(context, this, "[data-automation-id='DragDropTitle']");
        DraggableContainer = new LabelControl(context, this, "[data-automation-id='DraggableContainer']");
        DropZone = new LabelControl(context, this, "[data-automation-id='DropZone']");

        // Layout
        LayoutTitle = new LabelControl(context, this, "[data-automation-id='LayoutTitle']");
        GridLayout = new LabelControl(context, this, "[data-automation-id='GridLayout']");
        FlexLayout = new LabelControl(context, this, "[data-automation-id='FlexLayout']");

        // Tooltip and Popover
        TooltipTitle = new LabelControl(context, this, "[data-automation-id='TooltipTitle']");
        TooltipButton = new ButtonControl(context, this, "[data-automation-id='TooltipButton']");
        PopoverButton = new ButtonControl(context, this, "[data-automation-id='PopoverButton']");

        // Reset
        ResetButton = new ButtonControl(context, this, "[data-automation-id='ResetButton']");
    }

    public override bool IsDisplayed()
    {
        return AdvancedTitle.IsVisible();
    }

    // ═══════════════════════════════════════════════════════════════
    // WORKFLOW METHODS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Clear the event log.
    /// </summary>
    public AdvancedPage ClearLog()
    {
        Log("ClearLog()");
        ClearLogButton.Click();
        return this;
    }

    /// <summary>
    /// Click the single-click area.
    /// </summary>
    public AdvancedPage ClickSingleClickArea()
    {
        Log("ClickSingleClickArea()");
        SingleClickArea.Click();
        return this;
    }

    /// <summary>
    /// Double-click the double-click area.
    /// </summary>
    public AdvancedPage DoubleClickDoubleClickArea()
    {
        Log("DoubleClickDoubleClickArea()");
        DoubleClickArea.DoubleClick();
        return this;
    }

    /// <summary>
    /// Type in the keyboard input to trigger keyboard events.
    /// </summary>
    public AdvancedPage TypeInKeyboardInput(string text)
    {
        Log($"TypeInKeyboardInput({text})");
        KeyboardInput.ClearAndEnter(text);
        return this;
    }

    /// <summary>
    /// Focus on a specific input.
    /// </summary>
    public AdvancedPage FocusOnInput(int inputNumber)
    {
        Log($"FocusOnInput({inputNumber})");
        var input = inputNumber switch
        {
            1 => FocusInput1,
            2 => FocusInput2,
            3 => FocusInput3,
            _ => FocusInput1
        };
        input.Focus();
        return this;
    }

    /// <summary>
    /// Toggle the popover.
    /// </summary>
    public AdvancedPage TogglePopover()
    {
        Log("TogglePopover()");
        PopoverButton.Click();
        return this;
    }

    /// <summary>
    /// Reset all state.
    /// </summary>
    public AdvancedPage ResetAll()
    {
        Log("ResetAll()");
        ResetButton.Click();
        return this;
    }

    /// <summary>
    /// Get the current focus status text.
    /// </summary>
    public string GetFocusStatus()
    {
        return FocusStatus.GetText();
    }

    /// <summary>
    /// Get the last key pressed.
    /// </summary>
    public string GetLastKeyPressed()
    {
        return LastKeyValue.GetText();
    }
}

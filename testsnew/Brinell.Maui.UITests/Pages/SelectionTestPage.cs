using Brinell.Maui.Controls.Selection;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the SelectionTestView. Exposes all selection controls and their interactions.
/// Tests the Picker control behavior, selection, and state management.
/// </summary>
public class SelectionTestPage : PageObjectBase<SelectionTestPage>
{
    public SelectionTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "SelectionTestPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Page is loaded when the status label exists
        return StatusLabel.IsExists();
    }

    #region Picker

    /// <summary>
    /// The Picker control for item selection testing.
    /// </summary>
    public Picker<SelectionTestPage> TestPicker => new(this, "TestPicker");

    /// <summary>
    /// A picker holding two items that read alike.
    /// </summary>
    /// <remarks>
    /// Selecting the second of them freezes MAUI, so the verb refuses it. Nothing but the
    /// refusal test should drive this control - see the warning in SelectionView.xaml.
    /// </remarks>
    public Picker<SelectionTestPage> DuplicatePicker => new(this, "DuplicatePicker");

    /// <summary>
    /// A picker holding more items than its dropdown renders at once.
    /// </summary>
    /// <remarks>
    /// It exists so that "what does this picker hold" has a wrong answer available: reading the
    /// items out of the open popup returns only the ones it has realized.
    /// </remarks>
    public Picker<SelectionTestPage> LongPicker => new(this, "LongPicker");

    #endregion

    #region Labels

    /// <summary>
    /// The status message label showing test results.
    /// </summary>
    public Label<SelectionTestPage> StatusLabel => new(this, "StatusLabel");

    #endregion

    #region Buttons

    /// <summary>
    /// The Reset button to clear selection state.
    /// </summary>
    public Button<SelectionTestPage> ResetButton => new(this, "ResetButton");

    #endregion
}

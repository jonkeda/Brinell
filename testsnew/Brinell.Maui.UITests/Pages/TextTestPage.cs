namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the TextTestView. Exposes all text input controls (Entry, Editor, SearchBar).
/// Demonstrates the page object pattern with control locators and action methods.
/// </summary>
public class TextTestPage : PageObjectBase<TextTestPage>
{
    public TextTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "TextTestPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Page is loaded when the Entry control exists
        return TestEntry.IsExists();
    }

    #region Entry Controls

    /// <summary>
    /// The Entry test control for single-line text input.
    /// </summary>
    public Entry<TextTestPage> TestEntry => Entry("TestEntry");

    /// <summary>
    /// The Editor test control for multi-line text input.
    /// </summary>
    public Editor<TextTestPage> TestEditor => Editor("TestEditor");

    /// <summary>
    /// The SearchBar test control for search input.
    /// </summary>
    public SearchBar<TextTestPage> TestSearchBar => SearchBar("TestSearchBar");

    #endregion

    #region Buttons

    /// <summary>
    /// The Clear Entry button.
    /// </summary>
    public Button<TextTestPage> ClearEntryButton => Button("ClearEntryButton");

    /// <summary>
    /// The Clear Editor button.
    /// </summary>
    public Button<TextTestPage> ClearEditorButton => Button("ClearEditorButton");

    /// <summary>
    /// The Clear Search button.
    /// </summary>
    public Button<TextTestPage> ClearSearchButton => Button("ClearSearchButton");

    /// <summary>
    /// The Reset All button.
    /// </summary>
    public Button<TextTestPage> ResetAllButton => Button("ResetAllButton");

    #endregion

    #region Labels

    /// <summary>
    /// The Entry status message label.
    /// </summary>
    public Label<TextTestPage> EntryStatusLabel => Label("EntryStatusLabel");

    /// <summary>
    /// The Editor status message label.
    /// </summary>
    public Label<TextTestPage> EditorStatusLabel => Label("EditorStatusLabel");

    /// <summary>
    /// The Search status message label.
    /// </summary>
    public Label<TextTestPage> SearchStatusLabel => Label("SearchStatusLabel");

    #endregion
}

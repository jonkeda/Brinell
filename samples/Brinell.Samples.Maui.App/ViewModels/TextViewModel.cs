using System.Windows.Input;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for Text controls testing (Entry, Editor, and SearchBar).
/// Tests text input, multi-line text, and search functionality.
/// </summary>
public class TextViewModel : ParentViewModel
{
    private string entryText = string.Empty;
    private string editorText = string.Empty;
    private string searchText = string.Empty;
    private string entryStatusMessage = "Ready. Type in the Entry field.";
    private string editorStatusMessage = "Ready. Type in the Editor field.";
    private string searchStatusMessage = "Ready. Type and search.";

    public TextViewModel()
    {
        // Initialize status messages
        UpdateEntryStatus();
        UpdateEditorStatus();
        UpdateSearchStatus();
    }

    /// <summary>
    /// Gets or sets the Entry text.
    /// </summary>
    public string EntryText
    {
        get => entryText;
        set
        {
            if (SetProperty(ref entryText, value))
            {
                UpdateEntryStatus();
            }
        }
    }

    /// <summary>
    /// Gets or sets the Editor text.
    /// </summary>
    public string EditorText
    {
        get => editorText;
        set
        {
            if (SetProperty(ref editorText, value))
            {
                UpdateEditorStatus();
            }
        }
    }

    /// <summary>
    /// Gets or sets the SearchBar text.
    /// </summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
                UpdateSearchStatus();
            }
        }
    }

    /// <summary>
    /// Gets or sets the Entry status message.
    /// </summary>
    public string EntryStatusMessage
    {
        get => entryStatusMessage;
        set => SetProperty(ref entryStatusMessage, value);
    }

    /// <summary>
    /// Gets or sets the Editor status message.
    /// </summary>
    public string EditorStatusMessage
    {
        get => editorStatusMessage;
        set => SetProperty(ref editorStatusMessage, value);
    }

    /// <summary>
    /// Gets or sets the Search status message.
    /// </summary>
    public string SearchStatusMessage
    {
        get => searchStatusMessage;
        set => SetProperty(ref searchStatusMessage, value);
    }

    /// <summary>
    /// Command to clear the Entry field.
    /// </summary>
    public ICommand ClearEntryCommand => new RelayCommand(ClearEntry);

    /// <summary>
    /// Command to clear the Editor field.
    /// </summary>
    public ICommand ClearEditorCommand => new RelayCommand(ClearEditor);

    /// <summary>
    /// Command to clear the SearchBar field.
    /// </summary>
    public ICommand ClearSearchCommand => new RelayCommand(ClearSearch);

    /// <summary>
    /// Command to reset all text fields.
    /// </summary>
    public ICommand ResetAllCommand => new RelayCommand(ResetAll);

    /// <summary>
    /// Search command executed when search is triggered.
    /// </summary>
    public ICommand SearchCommand => new RelayCommand(ExecuteSearch);

    /// <summary>
    /// Updates the Entry status message based on current text.
    /// </summary>
    private void UpdateEntryStatus()
    {
        if (string.IsNullOrEmpty(entryText))
        {
            EntryStatusMessage = "Entry is empty. Type to test.";
        }
        else
        {
            EntryStatusMessage = $"✓ Entry text: '{entryText}' ({entryText.Length} chars)";
        }
    }

    /// <summary>
    /// Updates the Editor status message based on current text.
    /// </summary>
    private void UpdateEditorStatus()
    {
        if (string.IsNullOrEmpty(editorText))
        {
            EditorStatusMessage = "Editor is empty. Type to test.";
        }
        else
        {
            // Count lines regardless of which newline the platform stored: WinUI's TextBox
            // keeps a bare CR, Android keeps the LF it was given.
            var lineCount = editorText
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .Length;
            EditorStatusMessage = $"✓ Editor text: {editorText.Length} chars, {lineCount} line{(lineCount != 1 ? "s" : "")}";
        }
    }

    /// <summary>
    /// Updates the Search status message based on current text.
    /// </summary>
    private void UpdateSearchStatus()
    {
        if (string.IsNullOrEmpty(searchText))
        {
            SearchStatusMessage = "Search is empty. Type to test.";
        }
        else
        {
            SearchStatusMessage = $"Search query: '{searchText}' ({searchText.Length} chars). Press search to execute.";
        }
    }

    /// <summary>
    /// Clears the Entry field.
    /// </summary>
    private void ClearEntry()
    {
        EntryText = string.Empty;
        EntryStatusMessage = "✓ Entry cleared.";
    }

    /// <summary>
    /// Clears the Editor field.
    /// </summary>
    private void ClearEditor()
    {
        EditorText = string.Empty;
        EditorStatusMessage = "✓ Editor cleared.";
    }

    /// <summary>
    /// Clears the SearchBar field.
    /// </summary>
    private void ClearSearch()
    {
        SearchText = string.Empty;
        SearchStatusMessage = "✓ Search cleared.";
    }

    /// <summary>
    /// Executes the search operation.
    /// </summary>
    private void ExecuteSearch()
    {
        if (string.IsNullOrEmpty(searchText))
        {
            SearchStatusMessage = "✗ Search query is empty.";
        }
        else
        {
            SearchStatusMessage = $"✓ Search executed for: '{searchText}'";
        }
    }

    /// <summary>
    /// Resets all text fields to initial state.
    /// </summary>
    private void ResetAll()
    {
        EntryText = string.Empty;
        EditorText = string.Empty;
        SearchText = string.Empty;
        EntryStatusMessage = "Ready. Type in the Entry field.";
        EditorStatusMessage = "Ready. Type in the Editor field.";
        SearchStatusMessage = "Ready. Type and search.";
    }
}

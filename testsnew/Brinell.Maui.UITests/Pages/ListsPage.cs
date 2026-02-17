using Brinell.Maui.Controls;
using Brinell.Maui.Controls.Display;
using Brinell.Maui.Pages;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the Lists tab demonstrating ListView and TreeView-like hierarchy.
/// Exposes controls from ListsView.xaml with their AutomationIds.
/// </summary>
public class ListsPage : PageObjectBase<ListsPage>
{
    public ListsPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "ListsPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return ListsTitle.IsExists();
    }

    #region Scrolling

    /// <summary>
    /// The page-level ScrollView wrapping all content.
    /// </summary>
    public ScrollView<ListsPage> ListsScrollView => ScrollView("ListsScrollView");

    #endregion

    #region Labels

    /// <summary>
    /// The main title label "Lists Demo".
    /// </summary>
    public Label<ListsPage> ListsTitle => Label("ListsTitle");

    /// <summary>
    /// The selected item label for the ListView.
    /// </summary>
    public Label<ListsPage> SelectedItemLabel => Label("SelectedItemLabel");

    #endregion

    #region Section Labels

    /// <summary>
    /// The section label for the ListView section.
    /// </summary>
    public Label<ListsPage> ListViewSectionLabel => Label("ListViewLabel");

    #endregion

    #region ListView

    /// <summary>
    /// The demo ListView section label.
    /// </summary>
    public Label<ListsPage> ListViewLabel => Label("ListViewLabel");

    #endregion
}

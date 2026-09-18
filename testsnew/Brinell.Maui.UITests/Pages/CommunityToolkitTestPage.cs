using Brinell.Maui.CommunityToolkit.Controls.Layouts;
using Brinell.Maui.CommunityToolkit.Controls.Views;
using Brinell.Maui.UITests.Containers;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the CommunityToolkit views sample page: Expander, AvatarView, RatingView,
/// DrawingView, StateContainer and DockLayout.
/// </summary>
public class CommunityToolkitTestPage : PageObjectBase<CommunityToolkitTestPage>
{
    public CommunityToolkitTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "CommunityToolkitPage";

    #region Expander

    public Expander<CommunityToolkitTestPage> TestExpander => new(this, "TestExpander");

    public Label<CommunityToolkitTestPage> ExpanderStatusLabel => new(this, "ExpanderStatusLabel");

    #endregion

    #region AvatarView

    public AvatarView<CommunityToolkitTestPage> TestAvatarView => new(this, "TestAvatarView");

    #endregion

    #region RatingView

    public RatingView<CommunityToolkitTestPage> TestRatingView => new(this, "TestRatingView");

    public Label<CommunityToolkitTestPage> RatingStatusLabel => new(this, "RatingStatusLabel");

    #endregion

    #region DrawingView

    public DrawingView<CommunityToolkitTestPage> TestDrawingView => new(this, "TestDrawingView");

    public Label<CommunityToolkitTestPage> DrawingStatusLabel => new(this, "DrawingStatusLabel");

    public Button<CommunityToolkitTestPage> ClearDrawingButton => new(this, "ClearDrawingButton");

    #endregion

    #region StateContainer

    public StateContainer<CommunityToolkitTestPage> TestStateContainer => new(this, "TestStateContainer");

    public Button<CommunityToolkitTestPage> StateLoadingButton => new(this, "StateLoadingButton");

    public Button<CommunityToolkitTestPage> StateErrorButton => new(this, "StateErrorButton");

    public Button<CommunityToolkitTestPage> StateContentButton => new(this, "StateContentButton");

    public Label<CommunityToolkitTestPage> StateStatusLabel => new(this, "StateStatusLabel");

    #endregion

    #region DockLayout

    public ToolkitDockContainer TestDockLayout => new(this, "TestDockLayout");

    #endregion
}

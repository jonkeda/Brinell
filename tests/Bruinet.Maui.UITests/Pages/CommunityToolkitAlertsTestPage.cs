using Bruinet.Maui.CommunityToolkit.Controls.Views;

namespace Bruinet.Maui.UITests.Pages;

/// <summary>
/// Page object for the CommunityToolkit alerts sample page: Popup, Snackbar and Toast.
/// </summary>
/// <remarks>
/// Snackbar and Toast have no control objects on Windows: both are OS notifications that an
/// unpackaged app cannot raise, and the page records the refusal in <see cref="AlertResult"/>.
/// See <c>docs/controls/community-toolkit.md</c>.
/// </remarks>
public class CommunityToolkitAlertsTestPage : PageObjectBase<CommunityToolkitAlertsTestPage>
{
    public CommunityToolkitAlertsTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "CommunityToolkitAlertsPage";

    public Label<CommunityToolkitAlertsTestPage> AlertResult => new(this, "AlertResultLabel");

    public Button<CommunityToolkitAlertsTestPage> ShowPopupButton => new(this, "ShowPopupButton");

    public Button<CommunityToolkitAlertsTestPage> ShowSnackbarButton => new(this, "ShowSnackbarButton");

    public Button<CommunityToolkitAlertsTestPage> ShowToastButton => new(this, "ShowToastButton");

    /// <summary>The popup <see cref="ShowPopupButton"/> raises.</summary>
    public Popup<CommunityToolkitAlertsTestPage> TestPopup => new(this, "TestPopup");
}

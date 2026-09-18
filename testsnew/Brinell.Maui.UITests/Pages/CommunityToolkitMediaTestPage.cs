using ToolkitMedia = Brinell.Maui.CommunityToolkit.Controls.Media;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the CommunityToolkit MediaElement sample page.
/// </summary>
public class CommunityToolkitMediaTestPage : PageObjectBase<CommunityToolkitMediaTestPage>
{
    public CommunityToolkitMediaTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "CommunityToolkitMediaPage";

    public ToolkitMedia.MediaElement<CommunityToolkitMediaTestPage> TestMediaElement => new(this, "TestMediaElement");
}

using Bruinell.Html.Controls.Buttons;
using Bruinell.Html.Controls.Container;
using Bruinell.Html.Controls.Display;

namespace Bruinell.Html.UITests.PageObjects;

/// <summary>The sample's /frames page: a status on the page, and an iframe with a status and a button of its own.</summary>
public sealed class FramesPage : HtmlPageObjectBase<FramesPage>
{
    public FramesPage(IHtmlTestContext context)
        : base(context)
    {
    }

    /// <summary>The page's own status, with the same test id as the frame's.</summary>
    public LabelControl<FramesPage> OuterStatus => new(this, "[data-testid='inner-status']");

    /// <summary>The document inside the iframe.</summary>
    public FrameScope<FramesPage> Inner => new(this, Locator.ByCss("[data-testid='inner-frame']"));

    /// <summary>The status inside the frame.</summary>
    public LabelControl<FrameScope<FramesPage>> InnerStatus => new(Inner, "[data-testid='inner-status']");

    /// <summary>The button inside the frame.</summary>
    public ButtonControl<FrameScope<FramesPage>> InnerButton => new(Inner, "[data-testid='inner-btn']");
}

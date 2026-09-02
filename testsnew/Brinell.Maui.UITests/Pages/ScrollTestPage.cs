namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the ScrollTestView: a page taller than any screen, used to test scrolling on
/// its own rather than through tests that happen to need it.
/// </summary>
/// <remarks>
/// No <c>IsLoaded</c> override. The root marker sits on the page's <c>ScrollView</c>, which is a
/// real rendered view on every platform and cannot scroll itself out of view — unlike a probe on
/// some child control, which is what the other page objects used and what broke on Android.
/// </remarks>
public class ScrollTestPage : PageObjectBase<ScrollTestPage>
{
    public ScrollTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "ScrollTestPage";

    /// <summary>Status label at the very top of the page; every button reports here.</summary>
    public Label<ScrollTestPage> StatusLabel => new(this, "ScrollStatusLabel");

    /// <summary>Button near the top, above the fold on any screen.</summary>
    public Button<ScrollTestPage> TopButton => new(this, "ScrollTopButton");

    /// <summary>Button roughly a screen down.</summary>
    public Button<ScrollTestPage> MiddleButton => new(this, "ScrollMiddleButton");

    /// <summary>Last control on the page, unreachable without scrolling.</summary>
    public Button<ScrollTestPage> BottomButton => new(this, "ScrollBottomButton");

    /// <summary>Label just above the bottom button, also unreachable without scrolling.</summary>
    public Label<ScrollTestPage> BottomLabel => new(this, "ScrollBottomLabel");

    /// <summary>Resets the status back to "none".</summary>
    public Button<ScrollTestPage> ResetButton => new(this, "ScrollResetButton");
}

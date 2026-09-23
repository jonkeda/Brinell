namespace Brinell.Wpf.UITests.PageObjects;

/// <summary>
/// Page object for the Home page.
/// </summary>
public class HomePage : PageObjectBase<HomePage>
{
    public Label<HomePage> WelcomeText => Label("WelcomeText");
    public Label<HomePage> DescriptionText => Label("DescriptionText");
    public Label<HomePage> FeaturesHeader => Label("FeaturesHeader");

    public HomePage(IWpfTestContext context) : base(context) { }

    /// <summary>Checks if home page content is visible.</summary>
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return WelcomeText.IsVisible() == true;
    }
}

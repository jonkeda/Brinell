using Brinell.Wpf.Controls;
using Brinell.Wpf.Controls.Base;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Samples.Wpf.UITests.PageObjects;

/// <summary>
/// Page object for the Home page.
/// </summary>
public class HomePage : PageBase
{
    /// <summary>
    /// The welcome message header.
    /// </summary>
    public LabelControl WelcomeText { get; }
    
    /// <summary>
    /// The description text.
    /// </summary>
    public LabelControl DescriptionText { get; }
    
    /// <summary>
    /// The features header.
    /// </summary>
    public LabelControl FeaturesHeader { get; }

    public HomePage(FlaUITestContext context)
        : base(context, "HomePage")
    {
        WelcomeText = new LabelControl(context, this, "WelcomeText");
        DescriptionText = new LabelControl(context, this, "DescriptionText");
        FeaturesHeader = new LabelControl(context, this, "FeaturesHeader");
    }

    /// <summary>
    /// Check if the home page is displayed.
    /// </summary>
    public override bool IsDisplayed()
    {
        return WelcomeText.IsVisible();
    }

    /// <summary>
    /// Get the welcome message text.
    /// </summary>
    public string GetWelcomeMessage()
    {
        return WelcomeText.GetText();
    }

    /// <summary>
    /// Get the description text.
    /// </summary>
    public string GetDescription()
    {
        return DescriptionText.GetText();
    }
}

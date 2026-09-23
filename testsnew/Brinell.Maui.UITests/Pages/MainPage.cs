using Brinell.Core.Composition;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the greeting demo: the page the Markdown UAT sample scenarios are written
/// against.
/// </summary>
/// <remarks>
/// Named "Main" for the scenarios, which read <c>Given I am on the Main page</c>. Control names
/// come from the property names with their suffix dropped, so <c>NameEntry</c> is "Name",
/// <c>GreetButton</c> is "Greet" and <c>GreetingLabel</c> is "Greeting" - exactly the words the
/// scenarios use.
/// </remarks>
[TestPage("Main")]
public class MainPage : PageObjectBase<MainPage>
{
    public MainPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "GreetingTestPage";

    /// <summary>The name to greet.</summary>
    public Entry<MainPage> NameEntry => new(this, "NameEntry");

    /// <summary>Produces the greeting.</summary>
    public Button<MainPage> GreetButton => new(this, "GreetButton");

    /// <summary>The greeting, or the validation message when no name was entered.</summary>
    public Label<MainPage> GreetingLabel => new(this, "GreetingLabel");
}

using Brinell.Core.Composition;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the profile form the Markdown UAT sample scenarios fill in.
/// </summary>
/// <remarks>
/// One control per kind - text, toggle, selection - so a single scenario can show all three
/// families of verb. The names the scenarios use are the property names without their suffix.
/// </remarks>
[TestPage("User Form")]
public class UserFormPage : PageObjectBase<UserFormPage>
{
    public UserFormPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "UserFormTestPage";

    /// <summary>The given name.</summary>
    public Entry<UserFormPage> FirstNameEntry => new(this, "FirstNameEntry");

    /// <summary>The family name.</summary>
    public Entry<UserFormPage> LastNameEntry => new(this, "LastNameEntry");

    /// <summary>The email address.</summary>
    public Entry<UserFormPage> EmailEntry => new(this, "EmailEntry");

    /// <summary>Whether the terms were accepted.</summary>
    public CheckBox<UserFormPage> TermsCheckBox => new(this, "TermsCheckBox");

    /// <summary>The selected country.</summary>
    public Picker<UserFormPage> CountryPicker => new(this, "CountryPicker");
}

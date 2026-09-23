using System.Collections.ObjectModel;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// A small profile form: the controls a UAT scenario fills in, and nothing else.
/// </summary>
public class UserFormViewModel : ParentViewModel
{
    private string firstName = string.Empty;
    private string lastName = string.Empty;
    private string email = string.Empty;
    private bool agreedToTerms;
    private string? country;

    /// <summary>The given name.</summary>
    public string FirstName
    {
        get => firstName;
        set => SetProperty(ref firstName, value);
    }

    /// <summary>The family name.</summary>
    public string LastName
    {
        get => lastName;
        set => SetProperty(ref lastName, value);
    }

    /// <summary>The email address.</summary>
    public string Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    /// <summary>Whether the terms were accepted.</summary>
    public bool AgreedToTerms
    {
        get => agreedToTerms;
        set => SetProperty(ref agreedToTerms, value);
    }

    /// <summary>The selected country.</summary>
    public string? Country
    {
        get => country;
        set => SetProperty(ref country, value);
    }

    /// <summary>The countries the picker offers.</summary>
    public ObservableCollection<string> Countries { get; } =
    [
        "Netherlands",
        "United Kingdom",
        "United States",
        "Germany"
    ];
}

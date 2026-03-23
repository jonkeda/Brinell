using System.Collections.ObjectModel;
using System.Windows.Input;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for the Forms tab - comprehensive form controls demo for UI testing.
/// </summary>
public class FormsViewModel : ParentViewModel
{
    // Personal Info
    private string _firstName = "";
    private string _lastName = "";
    private string _email = "";
    private string _phone = "";
    private string _bio = "";
    private string _searchText = "";

    // Preferences (toggles)
    private bool _newsletterSubscribed;
    private bool _agreeToTerms;
    private bool _agreeToPrivacy;
    private bool _indeterminateOption;

    // Subscription Tier (radio buttons)
    private bool _isBasicSelected = true;
    private bool _isProfessionalSelected;
    private bool _isEnterpriseSelected;

    // Selection (pickers)
    private string _selectedCountry = "";
    private string _selectedDepartment = "";

    // DateTime
    private DateTime _birthDate = DateTime.Today.AddYears(-25);
    private TimeSpan _preferredTime = new(9, 0, 0);

    // Range controls
    private double _fontSize = 14;
    private double _volume = 50;
    private double _quantity = 1;

    // Result
    private string _resultMessage = "";

    public FormsViewModel()
    {
        Countries = new ObservableCollection<string> 
        { 
            "United States", "Canada", "United Kingdom", "Germany", "France", 
            "Japan", "Australia", "Brazil", "India", "China" 
        };

        Departments = new ObservableCollection<string>
        {
            "Engineering", "Sales", "Marketing", "Finance", "HR", 
            "Operations", "Support", "Legal", "R&D"
        };

        SubmitCommand = new RelayCommand(Submit);
        SaveDraftCommand = new RelayCommand(SaveDraft);
        ClearCommand = new RelayCommand(Clear);
        SearchCommand = new RelayCommand(Search);
    }

    #region Personal Info Properties

    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string Bio
    {
        get => _bio;
        set => SetProperty(ref _bio, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    #endregion

    #region Preferences (Toggle Controls)

    public bool NewsletterSubscribed
    {
        get => _newsletterSubscribed;
        set => SetProperty(ref _newsletterSubscribed, value);
    }

    public bool AgreeToTerms
    {
        get => _agreeToTerms;
        set => SetProperty(ref _agreeToTerms, value);
    }

    public bool AgreeToPrivacy
    {
        get => _agreeToPrivacy;
        set => SetProperty(ref _agreeToPrivacy, value);
    }

    public bool IndeterminateOption
    {
        get => _indeterminateOption;
        set => SetProperty(ref _indeterminateOption, value);
    }

    #endregion

    #region Subscription Tier (Radio Buttons)

    public bool IsBasicSelected
    {
        get => _isBasicSelected;
        set => SetProperty(ref _isBasicSelected, value);
    }

    public bool IsProfessionalSelected
    {
        get => _isProfessionalSelected;
        set => SetProperty(ref _isProfessionalSelected, value);
    }

    public bool IsEnterpriseSelected
    {
        get => _isEnterpriseSelected;
        set => SetProperty(ref _isEnterpriseSelected, value);
    }

    #endregion

    #region Selection (Pickers)

    public ObservableCollection<string> Countries { get; }
    public ObservableCollection<string> Departments { get; }

    public string SelectedCountry
    {
        get => _selectedCountry;
        set => SetProperty(ref _selectedCountry, value);
    }

    public string SelectedDepartment
    {
        get => _selectedDepartment;
        set => SetProperty(ref _selectedDepartment, value);
    }

    #endregion

    #region DateTime Properties

    public DateTime BirthDate
    {
        get => _birthDate;
        set => SetProperty(ref _birthDate, value);
    }

    public TimeSpan PreferredTime
    {
        get => _preferredTime;
        set => SetProperty(ref _preferredTime, value);
    }

    #endregion

    #region Range Controls

    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (SetProperty(ref _fontSize, value))
            {
                OnPropertyChanged(nameof(FontSizeText));
            }
        }
    }

    public string FontSizeText => $"Size: {FontSize:F0}pt";

    public double Volume
    {
        get => _volume;
        set
        {
            if (SetProperty(ref _volume, value))
            {
                OnPropertyChanged(nameof(VolumeText));
            }
        }
    }

    public string VolumeText => $"Volume: {Volume:F0}%";

    public double Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(QuantityText));
            }
        }
    }

    public string QuantityText => $"Quantity: {Quantity:F0}";

    #endregion

    #region Result

    public string ResultMessage
    {
        get => _resultMessage;
        set => SetProperty(ref _resultMessage, value);
    }

    #endregion

    #region Commands

    public ICommand SubmitCommand { get; }
    public ICommand SaveDraftCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand SearchCommand { get; }

    private void Submit()
    {
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            ResultMessage = "Please enter first and last name";
            return;
        }

        if (!AgreeToTerms || !AgreeToPrivacy)
        {
            ResultMessage = "Please accept terms and privacy policy";
            return;
        }

        ResultMessage = $"Form submitted for {FirstName} {LastName}!";
    }

    private void SaveDraft()
    {
        ResultMessage = "Draft saved!";
    }

    private void Clear()
    {
        FirstName = "";
        LastName = "";
        Email = "";
        Phone = "";
        Bio = "";
        SearchText = "";
        NewsletterSubscribed = false;
        AgreeToTerms = false;
        AgreeToPrivacy = false;
        IndeterminateOption = false;
        IsBasicSelected = true;
        IsProfessionalSelected = false;
        IsEnterpriseSelected = false;
        SelectedCountry = "";
        SelectedDepartment = "";
        BirthDate = DateTime.Today.AddYears(-25);
        PreferredTime = new TimeSpan(9, 0, 0);
        FontSize = 14;
        Volume = 50;
        Quantity = 1;
        ResultMessage = "Form cleared";
    }

    private void Search()
    {
        ResultMessage = string.IsNullOrEmpty(SearchText) 
            ? "Enter search text" 
            : $"Searching for: {SearchText}";
    }

    #endregion
}

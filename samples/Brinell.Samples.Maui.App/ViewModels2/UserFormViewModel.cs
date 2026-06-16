using Brinell.Samples.Maui.App.Models2;

namespace Brinell.Samples.Maui.App.ViewModels2;

/// <summary>
/// ViewModel for the UserForm page demonstrating all input, toggle, selection, and range controls.
/// </summary>
public class UserFormViewModel : ParentViewModel
{
    private UserProfile _profile = new();
    private string _searchText = string.Empty;
    private string _resultMessage = string.Empty;
    private bool _showIndeterminate;
    private int _selectedCountryIndex = -1;
    private int _selectedDepartmentIndex = -1;

    public UserProfile Profile
    {
        get => _profile;
        set => SetProperty(ref _profile, value);
    }

    public string FirstName
    {
        get => _profile.FirstName;
        set { _profile.FirstName = value; OnPropertyChanged(); }
    }

    public string LastName
    {
        get => _profile.LastName;
        set { _profile.LastName = value; OnPropertyChanged(); }
    }

    public string Email
    {
        get => _profile.Email;
        set { _profile.Email = value; OnPropertyChanged(); }
    }

    public string Phone
    {
        get => _profile.Phone;
        set { _profile.Phone = value; OnPropertyChanged(); }
    }

    public string Bio
    {
        get => _profile.Bio;
        set { _profile.Bio = value; OnPropertyChanged(); }
    }

    public DateTime BirthDate
    {
        get => _profile.BirthDate;
        set { _profile.BirthDate = value; OnPropertyChanged(); }
    }

    public TimeSpan PreferredTime
    {
        get => _profile.PreferredTime;
        set { _profile.PreferredTime = value; OnPropertyChanged(); }
    }

    public bool SubscribeNewsletter
    {
        get => _profile.SubscribeNewsletter;
        set { _profile.SubscribeNewsletter = value; OnPropertyChanged(); }
    }

    public bool AcceptTerms
    {
        get => _profile.AcceptTerms;
        set { _profile.AcceptTerms = value; OnPropertyChanged(); }
    }

    public bool AcceptPrivacy
    {
        get => _profile.AcceptPrivacy;
        set { _profile.AcceptPrivacy = value; OnPropertyChanged(); }
    }

    public bool ShowIndeterminate
    {
        get => _showIndeterminate;
        set => SetProperty(ref _showIndeterminate, value);
    }

    public string SubscriptionTier
    {
        get => _profile.SubscriptionTier;
        set { _profile.SubscriptionTier = value; OnPropertyChanged(); }
    }

    public string ContactPreference
    {
        get => _profile.ContactPreference;
        set { _profile.ContactPreference = value; OnPropertyChanged(); }
    }

    public double FontSize
    {
        get => _profile.FontSize;
        set { _profile.FontSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(FontSizeDisplay)); }
    }

    public string FontSizeDisplay => $"{FontSize:F0} pt";

    public double Volume
    {
        get => _profile.Volume;
        set { _profile.Volume = value; OnPropertyChanged(); OnPropertyChanged(nameof(VolumeDisplay)); }
    }

    public string VolumeDisplay => $"{Volume:F0}%";

    public int Quantity
    {
        get => _profile.Quantity;
        set { _profile.Quantity = value; OnPropertyChanged(); }
    }

    public int SelectedCountryIndex
    {
        get => _selectedCountryIndex;
        set => SetProperty(ref _selectedCountryIndex, value);
    }

    public int SelectedDepartmentIndex
    {
        get => _selectedDepartmentIndex;
        set => SetProperty(ref _selectedDepartmentIndex, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string ResultMessage
    {
        get => _resultMessage;
        set => SetProperty(ref _resultMessage, value);
    }

    public List<string> Countries { get; } = new()
    {
        "United States", "Canada", "United Kingdom", "Germany", "France", "Australia", "Japan"
    };

    public List<string> Departments { get; } = new()
    {
        "Engineering", "Sales", "Marketing", "Human Resources", "Finance", "Operations"
    };

    public IAsyncRelayCommand SubmitCommand { get; }
    public IAsyncRelayCommand ClearCommand { get; }
    public IAsyncRelayCommand SaveDraftCommand { get; }
    public IAsyncRelayCommand SearchCommand { get; }

    public UserFormViewModel()
    {
        SubmitCommand = new AsyncRelayCommand(this, SubmitAsync);
        ClearCommand = new AsyncRelayCommand(this, ClearAsync);
        SaveDraftCommand = new AsyncRelayCommand(this, SaveDraftAsync);
        SearchCommand = new AsyncRelayCommand(this, SearchAsync);
    }

    private async Task SubmitAsync()
    {
        await Task.Delay(500);
        ResultMessage = $"Form submitted for {FirstName} {LastName}";
    }

    private async Task ClearAsync()
    {
        Profile = new UserProfile();
        OnPropertyChanged(nameof(FirstName));
        OnPropertyChanged(nameof(LastName));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(Phone));
        OnPropertyChanged(nameof(Bio));
        OnPropertyChanged(nameof(BirthDate));
        OnPropertyChanged(nameof(PreferredTime));
        OnPropertyChanged(nameof(SubscribeNewsletter));
        OnPropertyChanged(nameof(AcceptTerms));
        OnPropertyChanged(nameof(AcceptPrivacy));
        OnPropertyChanged(nameof(SubscriptionTier));
        OnPropertyChanged(nameof(ContactPreference));
        OnPropertyChanged(nameof(FontSize));
        OnPropertyChanged(nameof(Volume));
        OnPropertyChanged(nameof(Quantity));
        SelectedCountryIndex = -1;
        SelectedDepartmentIndex = -1;
        SearchText = string.Empty;
        ResultMessage = "Form cleared";
        await Task.CompletedTask;
    }

    private async Task SaveDraftAsync()
    {
        await Task.Delay(300);
        ResultMessage = "Draft saved";
    }

    private async Task SearchAsync()
    {
        await Task.Delay(200);
        ResultMessage = $"Searched for: {SearchText}";
    }
}

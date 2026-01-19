using System.Collections.ObjectModel;
using System.Windows.Input;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for the Forms tab - form validation, TableView, and settings demos.
/// </summary>
public class FormsViewModel : ViewModelBase
{
    private string _username = "";
    private string _email = "";
    private string _phone = "";
    private string _usernameError = "";
    private string _emailError = "";
    private string _phoneError = "";
    private string _formStatus = "";
    private bool _isDarkMode;
    private bool _notificationsEnabled = true;
    private bool _autoSaveEnabled = true;
    private string _displayName = "";
    private string _selectedLanguage = "English";

    public FormsViewModel()
    {
        Languages = new ObservableCollection<string> { "English", "Spanish", "French", "German", "Japanese" };
        SaveFormCommand = new RelayCommand(SaveForm);
        ClearFormCommand = new RelayCommand(ClearForm);
    }

    #region User Form

    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                ValidateUsername();
            }
        }
    }

    public string UsernameError
    {
        get => _usernameError;
        set => SetProperty(ref _usernameError, value);
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                ValidateEmail();
            }
        }
    }

    public string EmailError
    {
        get => _emailError;
        set => SetProperty(ref _emailError, value);
    }

    public string Phone
    {
        get => _phone;
        set
        {
            if (SetProperty(ref _phone, value))
            {
                ValidatePhone();
            }
        }
    }

    public string PhoneError
    {
        get => _phoneError;
        set => SetProperty(ref _phoneError, value);
    }

    public string FormStatus
    {
        get => _formStatus;
        set => SetProperty(ref _formStatus, value);
    }

    public ICommand SaveFormCommand { get; }
    public ICommand ClearFormCommand { get; }

    private void ValidateUsername()
    {
        if (string.IsNullOrWhiteSpace(_username))
        {
            UsernameError = "Username is required";
        }
        else if (_username.Length < 3)
        {
            UsernameError = "Username must be at least 3 characters";
        }
        else
        {
            UsernameError = "";
        }
    }

    private void ValidateEmail()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            EmailError = "Email is required";
        }
        else if (!_email.Contains('@') || !_email.Contains('.'))
        {
            EmailError = "Invalid email format";
        }
        else
        {
            EmailError = "";
        }
    }

    private void ValidatePhone()
    {
        if (string.IsNullOrWhiteSpace(_phone))
        {
            PhoneError = "";
        }
        else if (_phone.Length < 10)
        {
            PhoneError = "Phone must be at least 10 digits";
        }
        else
        {
            PhoneError = "";
        }
    }

    private void SaveForm()
    {
        ValidateUsername();
        ValidateEmail();
        ValidatePhone();

        if (string.IsNullOrEmpty(UsernameError) && string.IsNullOrEmpty(EmailError) && string.IsNullOrEmpty(PhoneError))
        {
            FormStatus = $"Form saved for {Username}!";
        }
        else
        {
            FormStatus = "Please fix validation errors";
        }
    }

    private void ClearForm()
    {
        Username = "";
        Email = "";
        Phone = "";
        UsernameError = "";
        EmailError = "";
        PhoneError = "";
        FormStatus = "";
    }

    #endregion

    #region TableView Settings

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => SetProperty(ref _isDarkMode, value);
    }

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }

    public bool AutoSaveEnabled
    {
        get => _autoSaveEnabled;
        set => SetProperty(ref _autoSaveEnabled, value);
    }

    public ObservableCollection<string> Languages { get; }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set => SetProperty(ref _selectedLanguage, value);
    }

    #endregion
}

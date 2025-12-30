using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.Navigation;
using Brinell.Samples.Shared.ViewModels;
using Brinell.Samples.Wpf.App.Infrastructure.Navigation;
using Brinell.Samples.Wpf.App.Models;

namespace Brinell.Samples.Wpf.App.Features.Login.ViewModels;

/// <summary>
/// ViewModel for the Login page with validation and async login support.
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string? _usernameError;
    private string? _passwordError;
    private string? _loginError;
    private bool _isLoginSuccessful;
    private User? _currentUser;

    public LoginViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        LoginCommand = new AsyncRelayCommand(this, ExecuteLoginAsync, CanExecuteLogin);
        CancelCommand = new RelayCommand(ExecuteCancel);
    }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                ValidateUsername();
                LoginCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ValidatePassword();
                LoginCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the username validation error.
    /// </summary>
    public string? UsernameError
    {
        get => _usernameError;
        private set => SetProperty(ref _usernameError, value);
    }

    /// <summary>
    /// Gets the password validation error.
    /// </summary>
    public string? PasswordError
    {
        get => _passwordError;
        private set => SetProperty(ref _passwordError, value);
    }

    /// <summary>
    /// Gets the login error message.
    /// </summary>
    public string? LoginError
    {
        get => _loginError;
        private set => SetProperty(ref _loginError, value);
    }

    /// <summary>
    /// Gets whether the login was successful.
    /// </summary>
    public bool IsLoginSuccessful
    {
        get => _isLoginSuccessful;
        private set => SetProperty(ref _isLoginSuccessful, value);
    }

    /// <summary>
    /// Gets the current logged-in user.
    /// </summary>
    public User? CurrentUser
    {
        get => _currentUser;
        private set => SetProperty(ref _currentUser, value);
    }

    /// <summary>
    /// Gets whether the form has any validation errors.
    /// </summary>
    public bool HasErrors => !string.IsNullOrEmpty(UsernameError) || !string.IsNullOrEmpty(PasswordError);

    /// <summary>
    /// Gets the login command.
    /// </summary>
    public AsyncRelayCommand LoginCommand { get; }

    /// <summary>
    /// Gets the cancel command.
    /// </summary>
    public RelayCommand CancelCommand { get; }

    private void ValidateUsername()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            UsernameError = "Username is required";
        }
        else if (Username.Length < 3)
        {
            UsernameError = "Username must be at least 3 characters";
        }
        else
        {
            UsernameError = null;
        }
    }

    private void ValidatePassword()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            PasswordError = "Password is required";
        }
        else if (Password.Length < 6)
        {
            PasswordError = "Password must be at least 6 characters";
        }
        else
        {
            PasswordError = null;
        }
    }

    private bool CanExecuteLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !HasErrors;
    }

    private async Task ExecuteLoginAsync()
    {
        // Clear previous errors
        LoginError = null;
        IsLoginSuccessful = false;

        // Validate all fields
        ValidateUsername();
        ValidatePassword();

        if (HasErrors)
            return;

        // Simulate async login operation
        await Task.Delay(1500); // Simulate network delay

        // Demo: Accept specific credentials
        if (Username.Equals("demo", StringComparison.OrdinalIgnoreCase) && Password == "password")
        {
            CurrentUser = new User
            {
                Id = 1,
                Username = Username,
                DisplayName = "Demo User",
                Email = "demo@example.com",
                IsActive = true
            };

            IsLoginSuccessful = true;

            // Navigate to home after successful login
            await _navigationService.NavigateToAsync(NavigationRoutes.Home);
        }
        else
        {
            LoginError = "Invalid username or password. Try demo/password.";
        }
    }

    private void ExecuteCancel()
    {
        Username = string.Empty;
        Password = string.Empty;
        UsernameError = null;
        PasswordError = null;
        LoginError = null;

        if (_navigationService.CanGoBack)
        {
            _ = _navigationService.GoBackAsync();
        }
    }
}

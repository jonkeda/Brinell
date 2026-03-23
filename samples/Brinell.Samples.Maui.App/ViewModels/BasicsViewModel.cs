using System.Windows.Input;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for the Basics tab - counter, text input, toggles, sliders, pickers.
/// </summary>
public class BasicsViewModel : ParentViewModel
{
    private int _counter;
    private string _name = "";
    private string _email = "";
    private string _message = "";
    private string _greeting = "";
    private bool _notificationsEnabled = true;
    private bool _agreeToTerms;
    private double _volume = 50;
    private string _selectedColor = "";
    private DateTime _birthDate = DateTime.Today;
    private TimeSpan _reminderTime = new(9, 0, 0);
    private bool _isLoading;

    public BasicsViewModel()
    {
        IncrementCommand = new RelayCommand(Increment);
        DecrementCommand = new RelayCommand(Decrement);
        ResetCommand = new RelayCommand(Reset);
        GreetCommand = new RelayCommand(Greet);
        ToggleLoadingCommand = new RelayCommand(ToggleLoading);
    }

    #region Counter

    public int Counter
    {
        get => _counter;
        set
        {
            if (SetProperty(ref _counter, value))
            {
                OnPropertyChanged(nameof(CounterText));
            }
        }
    }

    public string CounterText => $"Counter: {_counter}";

    public ICommand IncrementCommand { get; }
    public ICommand DecrementCommand { get; }
    public ICommand ResetCommand { get; }

    private void Increment() => Counter++;
    private void Decrement() => Counter--;
    private void Reset() => Counter = 0;

    #endregion

    #region Text Input

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public string Greeting
    {
        get => _greeting;
        set => SetProperty(ref _greeting, value);
    }

    public ICommand GreetCommand { get; }

    private void Greet()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Greeting = "Please enter your name";
        }
        else
        {
            Greeting = $"Hello, {Name}!";
        }
    }

    #endregion

    #region Toggle Controls

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set
        {
            if (SetProperty(ref _notificationsEnabled, value))
            {
                OnPropertyChanged(nameof(NotificationLabelText));
            }
        }
    }

    public string NotificationLabelText => NotificationsEnabled ? "Notifications enabled" : "Notifications disabled";

    public bool AgreeToTerms
    {
        get => _agreeToTerms;
        set => SetProperty(ref _agreeToTerms, value);
    }

    #endregion

    #region Volume/Slider

    public double Volume
    {
        get => _volume;
        set
        {
            if (SetProperty(ref _volume, value))
            {
                OnPropertyChanged(nameof(VolumeText));
                OnPropertyChanged(nameof(VolumeProgress));
            }
        }
    }

    public string VolumeText => $"Volume: {(int)_volume}%";
    public double VolumeProgress => _volume / 100.0;

    #endregion

    #region Picker/Selection

    public string[] Colors { get; } = { "Red", "Green", "Blue", "Yellow", "Purple" };

    public string SelectedColor
    {
        get => _selectedColor;
        set
        {
            if (SetProperty(ref _selectedColor, value))
            {
                OnPropertyChanged(nameof(SelectedColorText));
            }
        }
    }

    public string SelectedColorText => string.IsNullOrEmpty(_selectedColor) ? "No color selected" : $"Selected: {_selectedColor}";

    public DateTime BirthDate
    {
        get => _birthDate;
        set => SetProperty(ref _birthDate, value);
    }

    public TimeSpan ReminderTime
    {
        get => _reminderTime;
        set => SetProperty(ref _reminderTime, value);
    }

    #endregion

    #region Activity Indicator

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand ToggleLoadingCommand { get; }

    private void ToggleLoading() => IsLoading = !IsLoading;

    #endregion
}

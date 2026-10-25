using System.Windows.Input;
using Microsoft.Maui.Controls;
using MauiUnit.Samples.App.Services;

namespace MauiUnit.Samples.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly Command _save;
    private string _name = string.Empty;
    private bool _notifications;
    private string _status = string.Empty;
    private bool _isBusy;

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        _save = new Command(async () => await SaveAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(Name));
    }

    public string Name
    {
        get => _name;
        set
        {
            if (Set(ref _name, value))
                _save.ChangeCanExecute();
        }
    }

    public bool Notifications
    {
        get => _notifications;
        set => Set(ref _notifications, value);
    }

    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (Set(ref _isBusy, value))
                _save.ChangeCanExecute();
        }
    }

    public ICommand SaveCommand => _save;

    public void Load()
    {
        Name = _settings.LoadName() ?? string.Empty;
        Notifications = _settings.LoadNotifications();
        Status = string.IsNullOrEmpty(Name) ? "Tell us your name." : $"Welcome back, {Name}.";
    }

    private async Task SaveAsync()
    {
        IsBusy = true;
        Status = "Saving...";
        try
        {
            await _settings.SaveAsync(Name, Notifications);
            Status = "Saved";
        }
        catch (Exception e)
        {
            Status = $"Not saved: {e.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

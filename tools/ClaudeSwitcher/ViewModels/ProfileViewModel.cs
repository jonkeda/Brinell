using ClaudeSwitcher.Models;
using ClaudeSwitcher.Mvvm;

namespace ClaudeSwitcher.ViewModels;

public sealed class ProfileViewModel : ParentViewModel
{
    private DateTime? _lastActivatedUtc;

    public ProfileViewModel(ProfileEntry entry)
    {
        Name = entry.Name;
        _lastActivatedUtc = entry.LastActivatedUtc;
    }

    public string Name { get; }

    public DateTime? LastActivatedUtc
    {
        get => _lastActivatedUtc;
        set
        {
            if (SetProperty(ref _lastActivatedUtc, value))
                OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string DisplayName => _lastActivatedUtc is { } t
        ? $"{Name}    last activated: {t.ToLocalTime():yyyy-MM-dd HH:mm}"
        : Name;
}

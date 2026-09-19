using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClaudeSwitcher.Mvvm;

public class ParentViewModel : INotifyPropertyChanged, INotifyPropertyChanging, IViewVisible
{
    private bool _viewVisible = true;
    private int _busyCount;
    private readonly object _busyLock = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    public bool ViewVisible
    {
        get => _viewVisible;
        set => SetProperty(ref _viewVisible, value);
    }

    public bool IsBusy
    {
        get { lock (_busyLock) { return _busyCount > 0; } }
    }

    public void BeginBusy()
    {
        lock (_busyLock) { _busyCount++; }
        OnPropertyChanged(nameof(IsBusy));
    }

    public void EndBusy()
    {
        lock (_busyLock) { _busyCount = Math.Max(0, _busyCount - 1); }
        OnPropertyChanged(nameof(IsBusy));
    }

    public virtual void OnViewAppearing() => ViewVisible = true;
    public virtual void OnViewDisappearing() => ViewVisible = false;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        OnPropertyChanging(propertyName);
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected virtual void OnPropertyChanging([CallerMemberName] string? propertyName = null)
        => PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

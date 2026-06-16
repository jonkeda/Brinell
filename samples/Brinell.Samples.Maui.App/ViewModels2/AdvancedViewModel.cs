using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.ViewModels2;

/// <summary>
/// ViewModel for the Advanced page demonstrating gestures, swipe views, and containers.
/// </summary>
public class AdvancedViewModel : ParentViewModel
{
    private int _tapCount;
    private string _panStatus = "Not panning";
    private double _pinchScale = 1.0;
    private string _swipeResult = string.Empty;
    private string _gestureLog = string.Empty;

    public int TapCount
    {
        get => _tapCount;
        set => SetProperty(ref _tapCount, value);
    }

    public string TapCountDisplay => $"Tap count: {TapCount}";

    public string PanStatus
    {
        get => _panStatus;
        set => SetProperty(ref _panStatus, value);
    }

    public double PinchScale
    {
        get => _pinchScale;
        set => SetProperty(ref _pinchScale, value);
    }

    public string SwipeResult
    {
        get => _swipeResult;
        set => SetProperty(ref _swipeResult, value);
    }

    public string GestureLog
    {
        get => _gestureLog;
        set => SetProperty(ref _gestureLog, value);
    }

    public IAsyncRelayCommand TapCommand { get; }
    public IAsyncRelayCommand SwipeLeftCommand { get; }
    public IAsyncRelayCommand SwipeRightCommand { get; }
    public IAsyncRelayCommand ResetCommand { get; }

    public AdvancedViewModel()
    {
        TapCommand = new AsyncRelayCommand(this, TapAsync);
        SwipeLeftCommand = new AsyncRelayCommand(this, SwipeLeftAsync);
        SwipeRightCommand = new AsyncRelayCommand(this, SwipeRightAsync);
        ResetCommand = new AsyncRelayCommand(this, ResetAsync);
    }

    private async Task TapAsync()
    {
        TapCount++;
        OnPropertyChanged(nameof(TapCountDisplay));
        AppendLog($"Tap #{TapCount}");
        await Task.CompletedTask;
    }

    public void OnPanUpdated(double totalX, double totalY, bool completed)
    {
        if (completed)
        {
            PanStatus = $"Pan completed: ({totalX:F0}, {totalY:F0})";
            AppendLog($"Pan: ({totalX:F0}, {totalY:F0})");
        }
        else
        {
            PanStatus = $"Panning: ({totalX:F0}, {totalY:F0})";
        }
    }

    public void OnPinchUpdated(double scale)
    {
        PinchScale = Math.Clamp(scale, 0.5, 3.0);
        AppendLog($"Pinch scale: {PinchScale:F2}");
    }

    private async Task SwipeLeftAsync()
    {
        SwipeResult = "Swiped Left - Delete action";
        AppendLog("Swipe Left");
        await Task.CompletedTask;
    }

    private async Task SwipeRightAsync()
    {
        SwipeResult = "Swiped Right - Archive action";
        AppendLog("Swipe Right");
        await Task.CompletedTask;
    }

    private async Task ResetAsync()
    {
        TapCount = 0;
        PanStatus = "Not panning";
        PinchScale = 1.0;
        SwipeResult = string.Empty;
        GestureLog = string.Empty;
        OnPropertyChanged(nameof(TapCountDisplay));
        await Task.CompletedTask;
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        GestureLog = $"[{timestamp}] {message}\n{GestureLog}";
        if (GestureLog.Length > 500)
            GestureLog = GestureLog[..500];
    }
}

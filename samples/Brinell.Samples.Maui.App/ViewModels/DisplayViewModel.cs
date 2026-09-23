namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for Display controls testing (Label, Image, ActivityIndicator, ProgressBar, TitleBar).
/// Tests text rendering, image loading, progress visualization, and indicator animation.
/// </summary>
public class DisplayViewModel : ParentViewModel
{
    private string labelText = "This is a test label with wrapping";
    private double progressValue = 50;
    private bool isActivityRunning = true;
    private string statusMessage = "Ready. Interact with controls to test.";

    public DisplayViewModel()
    {
    }

    /// <summary>
    /// Gets or sets the text displayed by the Label control.
    /// </summary>
    public string LabelText
    {
        get => labelText;
        set => SetProperty(ref labelText, value);
    }

    /// <summary>
    /// Gets or sets the progress value for the ProgressBar (0-100).
    /// </summary>
    public double ProgressValue
    {
        get => progressValue;
        set
        {
            if (SetProperty(ref progressValue, value))
            {
                UpdateStatus();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the ActivityIndicator is running.
    /// </summary>
    public bool IsActivityRunning
    {
        get => isActivityRunning;
        set
        {
            if (SetProperty(ref isActivityRunning, value))
            {
                UpdateStatus();
            }
        }
    }

    /// <summary>
    /// Gets or sets the status message displayed to the user.
    /// </summary>
    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    /// <summary>
    /// Gets the formatted progress text for display.
    /// </summary>
    public string FormattedProgress => $"{ProgressValue:F0}%";

    /// <summary>
    /// Toggle ActivityIndicator running state command.
    /// </summary>
    public ICommand ToggleActivityCommand => new RelayCommand(ToggleActivity);

    /// <summary>
    /// Increase progress value by 10% command.
    /// </summary>
    public ICommand IncreaseProgressCommand => new RelayCommand(IncreaseProgress);

    /// <summary>
    /// Decrease progress value by 10% command.
    /// </summary>
    public ICommand DecreaseProgressCommand => new RelayCommand(DecreaseProgress);

    /// <summary>
    /// Reset command - clears all state and returns to initial values.
    /// </summary>
    public ICommand ResetCommand => new RelayCommand(Reset);

    /// <summary>
    /// Updates the status message based on current state.
    /// </summary>
    private void UpdateStatus()
    {
        var activityState = IsActivityRunning ? "Running" : "Stopped";
        StatusMessage = $"✓ Progress: {FormattedProgress} | Activity: {activityState}";
    }

    /// <summary>
    /// Toggles the ActivityIndicator running state.
    /// </summary>
    private void ToggleActivity()
    {
        IsActivityRunning = !IsActivityRunning;
    }

    /// <summary>
    /// Increases progress value by 10% (max 100%).
    /// </summary>
    private void IncreaseProgress()
    {
        ProgressValue = Math.Min(ProgressValue + 10, 100);
    }

    /// <summary>
    /// Decreases progress value by 10% (min 0%).
    /// </summary>
    private void DecreaseProgress()
    {
        ProgressValue = Math.Max(ProgressValue - 10, 0);
    }

    /// <summary>
    /// Resets all state to initial values.
    /// </summary>
    private void Reset()
    {
        LabelText = "This is a test label with wrapping";
        ProgressValue = 50;
        IsActivityRunning = true;

        // Report the state reset restored, the way every other change does. Overwriting the
        // status with a generic message left no way to observe the restored value — the
        // ProgressBar's value itself is not readable on Android, which exposes no range info.
        UpdateStatus();
    }
}

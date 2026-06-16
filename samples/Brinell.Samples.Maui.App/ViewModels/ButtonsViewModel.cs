namespace Brinell.Samples.Maui.App.ViewModels;

public class ButtonsViewModel : ParentViewModel
{
    private string statusMessage = "Ready. Click any button to test.";
    private int tapCount = 0;

    public string StatusMessage
    {
        get => statusMessage;
        set
        {
            if (statusMessage != value)
            {
                statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand TestButtonCommand => new RelayCommand(TestButton);
    public ICommand TestImageButtonCommand => new RelayCommand(TestImageButton);
    public ICommand ResetCommand => new RelayCommand(Reset);

    private void TestButton()
    {
        tapCount++;
        StatusMessage = $"✓ Button tapped {tapCount} time{(tapCount != 1 ? "s" : "")}. Command executed successfully.";
    }

    private void TestImageButton()
    {
        StatusMessage = "✓ ImageButton tapped! Image button is working.";
    }

    private void Reset()
    {
        StatusMessage = "Ready. Click any button to test.";
        tapCount = 0;
    }
}

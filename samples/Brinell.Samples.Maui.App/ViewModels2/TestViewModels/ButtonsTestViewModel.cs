namespace Brinell.Samples.Maui.App.ViewModels2.TestViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

public class ButtonsTestViewModel : INotifyPropertyChanged
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

    public ICommand TestButtonCommand => new Command(TestButton);
    public ICommand TestIconCommandButtonCommand => new Command(TestIconCommandButton);
    public ICommand TestImageButtonCommand => new Command(TestImageButton);
    public ICommand TestLinkCommand => new Command(TestLink);
    public ICommand TestRoundButtonCommand => new Command(TestRoundButton);
    public ICommand ResetCommand => new Command(Reset);

    private void TestButton()
    {
        tapCount++;
        StatusMessage = $"✓ Button tapped {tapCount} time{(tapCount != 1 ? "s" : "")}. Command executed successfully.";
    }

    private void TestIconCommandButton()
    {
        StatusMessage = "✓ IconCommandButton tapped! Command executed.";
    }

    private void TestImageButton()
    {
        StatusMessage = "✓ ImageButton tapped! Image button is working.";
    }

    private void TestLink()
    {
        StatusMessage = "✓ Link button tapped! Navigation command would execute here.";
    }

    private void TestRoundButton()
    {
        StatusMessage = "✓ RoundButton tapped! Confirmed action executed.";
    }

    private void Reset()
    {
        StatusMessage = "Ready. Click any button to test.";
        tapCount = 0;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

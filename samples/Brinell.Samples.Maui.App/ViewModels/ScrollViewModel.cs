namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// Backs the scroll test page: records which of the far-apart buttons was pressed last.
/// </summary>
/// <remarks>
/// The status label sits at the very top and the buttons are spread down a long page, so a test
/// that presses one and then reads the label must scroll in both directions. That is the whole
/// point of the page — see <c>.my/maui/finding-android-offscreen-elements-leave-the-tree.md</c>.
/// </remarks>
public class ScrollViewModel : ParentViewModel
{
    private const string Initial = "none";

    private string scrollStatusMessage = Initial;

    public string ScrollStatusMessage
    {
        get => scrollStatusMessage;
        set => SetProperty(ref scrollStatusMessage, value);
    }

    public ICommand TopCommand => new RelayCommand(() => ScrollStatusMessage = "top pressed");

    public ICommand MiddleCommand => new RelayCommand(() => ScrollStatusMessage = "middle pressed");

    public ICommand BottomCommand => new RelayCommand(() => ScrollStatusMessage = "bottom pressed");

    public ICommand ResetCommand => new RelayCommand(() => ScrollStatusMessage = Initial);
}

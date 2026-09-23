using System.Windows.Input;
using Brinell.Presenter.Commands;

namespace Brinell.Presenter.ViewModels;

public sealed class RecentFolderViewModel
{
    public RecentFolderViewModel(string path, int index, Action<string> openFolder)
    {
        Path = path;
        DisplayText = Directory.Exists(path)
            ? new DirectoryInfo(path).Name
            : path;
        AutomationText = $"{DisplayText} | {path}";
        AutomationId = $"RecentFolder_{index}";
        OpenCommand = new RelayCommand(() => openFolder(Path), () => Directory.Exists(Path));
    }

    public string Path { get; }

    public string DisplayText { get; }

    public string AutomationText { get; }

    public string AutomationId { get; }

    public ICommand OpenCommand { get; }
}

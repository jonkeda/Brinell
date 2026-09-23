using System.Windows;

namespace Brinell.Scraper.Services;

public interface IMessageDialogService
{
    bool ShowYesNo(string message, string title);
}

public sealed class WpfMessageDialogService : IMessageDialogService
{
    public bool ShowYesNo(string message, string title)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes;
    }
}

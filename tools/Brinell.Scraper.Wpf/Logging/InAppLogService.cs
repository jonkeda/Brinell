using System.Collections.ObjectModel;
using System.Windows;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.Logging;

public sealed class InAppLogService
{
    public ObservableCollection<LogEntry> Entries { get; } = [];

    public void Add(LogEntry entry)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
        {
            if (dispatcher.CheckAccess())
                Entries.Add(entry);
            else
                dispatcher.BeginInvoke(() => Entries.Add(entry));
        }
    }

    public void Clear()
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
        {
            if (dispatcher.CheckAccess())
                Entries.Clear();
            else
                dispatcher.BeginInvoke(() => Entries.Clear());
        }
    }
}

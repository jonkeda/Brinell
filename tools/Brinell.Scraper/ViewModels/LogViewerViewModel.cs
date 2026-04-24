using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Brinell.Scraper.Logging;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels;

public sealed class LogViewerViewModel : ViewModelBase
{
    private readonly InAppLogService _logService;
    private LogLevel _selectedLogLevel = LogLevel.Debug;
    private bool _isAutoScroll = true;

    public LogViewerViewModel(InAppLogService logService)
    {
        _logService = logService;
        FilteredLogEntries = CollectionViewSource.GetDefaultView(_logService.Entries);
        FilteredLogEntries.Filter = FilterByLevel;

        ClearLogsCommand = new RelayCommand(() => _logService.Clear());

        LogLevels = [LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error];
    }

    public ICollectionView FilteredLogEntries { get; }

    public LogLevel[] LogLevels { get; }

    public LogLevel SelectedLogLevel
    {
        get => _selectedLogLevel;
        set
        {
            if (SetProperty(ref _selectedLogLevel, value))
                FilteredLogEntries.Refresh();
        }
    }

    public bool IsAutoScroll
    {
        get => _isAutoScroll;
        set => SetProperty(ref _isAutoScroll, value);
    }

    public ICommand ClearLogsCommand { get; }

    public ObservableCollection<LogEntry> RawEntries => _logService.Entries;

    private bool FilterByLevel(object obj)
    {
        return obj is LogEntry entry && entry.Level >= _selectedLogLevel;
    }
}

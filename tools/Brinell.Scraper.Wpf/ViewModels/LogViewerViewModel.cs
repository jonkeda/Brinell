using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using Brinell.Scraper.Logging;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels;

public sealed class LogViewerViewModel : ViewModelBase
{
    private readonly InAppLogService _logService;
    private LogLevel _selectedLogLevel = LogLevel.Trace;
    private bool _isAutoScroll = true;
    private string _searchText = string.Empty;

    public LogViewerViewModel(InAppLogService logService)
    {
        _logService = logService;
        FilteredLogEntries = CollectionViewSource.GetDefaultView(_logService.Entries);
        FilteredLogEntries.Filter = FilterEntry;

        ClearLogsCommand = new RelayCommand(() =>
        {
            _logService.Clear();
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(FilteredCount));
        });
        ExportCommand = new RelayCommand(Export);

        LogLevels = [LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error];

        _logService.Entries.CollectionChanged += OnEntriesChanged;
    }

    public ICollectionView FilteredLogEntries { get; }

    public LogLevel[] LogLevels { get; }

    public LogLevel SelectedLogLevel
    {
        get => _selectedLogLevel;
        set
        {
            if (SetProperty(ref _selectedLogLevel, value))
            {
                FilteredLogEntries.Refresh();
                OnPropertyChanged(nameof(FilteredCount));
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? string.Empty))
            {
                FilteredLogEntries.Refresh();
                OnPropertyChanged(nameof(FilteredCount));
            }
        }
    }

    public bool IsAutoScroll
    {
        get => _isAutoScroll;
        set => SetProperty(ref _isAutoScroll, value);
    }

    public int TotalCount => _logService.Entries.Count;

    public int FilteredCount => FilteredLogEntries.Cast<object>().Count();

    public ICommand ClearLogsCommand { get; }
    public ICommand ExportCommand { get; }

    public ObservableCollection<LogEntry> RawEntries => _logService.Entries;

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(FilteredCount));
    }

    private bool FilterEntry(object obj)
    {
        if (obj is not LogEntry entry) return false;
        if (entry.Level < _selectedLogLevel) return false;
        if (!string.IsNullOrEmpty(_searchText) &&
            entry.Message.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0)
            return false;
        return true;
    }

    private void Export()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Logs",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = $"scraper-logs-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Level,Source,Message");
        foreach (var entry in FilteredLogEntries.Cast<LogEntry>())
        {
            sb.Append(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sb.Append(',');
            sb.Append(entry.Level);
            sb.Append(',');
            sb.Append(EscapeCsv(entry.Source));
            sb.Append(',');
            sb.Append(EscapeCsv(entry.Message));
            sb.AppendLine();
        }
        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var needsQuotes = value.IndexOfAny(['"', ',', '\r', '\n']) >= 0;
        var escaped = value.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }
}

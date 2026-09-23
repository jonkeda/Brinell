using System.Collections.ObjectModel;
using Brinell.Scraper.ViewModels;

namespace Brinell.Scraper.Models;

public sealed class PageObjectListItem : ViewModelBase
{
    private long _snapshotId;
    private string _pageName = "";
    private string _pageUrl = "";
    private int _elementCount;
    private string _className = "";
    private string _namespace = "";
    private PageObjectStatus _status = PageObjectStatus.NotGenerated;
    private DateTime? _generatedAt;
    private string _mainCode = "";
    private ValidationResult? _validation;

    public long SnapshotId
    {
        get => _snapshotId;
        set => SetProperty(ref _snapshotId, value);
    }

    public string PageName
    {
        get => _pageName;
        set => SetProperty(ref _pageName, value);
    }

    public string PageUrl
    {
        get => _pageUrl;
        set => SetProperty(ref _pageUrl, value);
    }

    public int ElementCount
    {
        get => _elementCount;
        set => SetProperty(ref _elementCount, value);
    }

    public string ClassName
    {
        get => _className;
        set => SetProperty(ref _className, value);
    }

    public string Namespace
    {
        get => _namespace;
        set => SetProperty(ref _namespace, value);
    }

    public PageObjectStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusLabel));
            }
        }
    }

    public DateTime? GeneratedAt
    {
        get => _generatedAt;
        set => SetProperty(ref _generatedAt, value);
    }

    public string MainCode
    {
        get => _mainCode;
        set => SetProperty(ref _mainCode, value);
    }

    public List<string> ContainerCodes { get; } = [];

    public ObservableCollection<PageObjectPropertyItem> Properties { get; } = [];

    public ObservableCollection<ControlObjectReference> UsedControlObjects { get; } = [];

    public ObservableCollection<ValidationEntry> ValidationEntries { get; } = [];

    public ValidationResult? Validation
    {
        get => _validation;
        set
        {
            if (SetProperty(ref _validation, value))
                RebuildValidationEntries();
        }
    }

    public string StatusIcon => _status switch
    {
        PageObjectStatus.Generated => "✓",
        PageObjectStatus.Error => "✗",
        _ => "⏳",
    };

    public string StatusLabel => _status.ToString();

    private void RebuildValidationEntries()
    {
        ValidationEntries.Clear();
        if (_validation is null) return;

        foreach (var err in _validation.Errors)
            ValidationEntries.Add(new ValidationEntry
            {
                Category = "Compilation",
                Severity = "Error",
                Message = err.Message,
            });

        foreach (var warn in _validation.Warnings)
            ValidationEntries.Add(new ValidationEntry
            {
                Category = "Compilation",
                Severity = "Warning",
                Message = warn.Message,
            });

        if (_validation.IsValid && _validation.Warnings.Count == 0)
            ValidationEntries.Add(new ValidationEntry
            {
                Category = "Compilation",
                Severity = "OK",
                Message = "No issues",
            });
    }
}

using System.Collections.ObjectModel;
using Brinell.Scraper.ViewModels;

namespace Brinell.Scraper.Models;

public sealed class ControlObjectListItem : ViewModelBase
{
    private string _name = "";
    private string _namespace = "";
    private int _confidence;
    private ControlObjectStatus _status = ControlObjectStatus.Pending;
    private string _domSignature = "";
    private DateTime _createdAt;
    private string _code = "";
    private string _exampleSnippet = "";
    private int _usedByPageCount;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Namespace
    {
        get => _namespace;
        set => SetProperty(ref _namespace, value);
    }

    public int Confidence
    {
        get => _confidence;
        set
        {
            if (SetProperty(ref _confidence, value))
                OnPropertyChanged(nameof(ConfidenceLabel));
        }
    }

    public ControlObjectStatus Status
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

    public string DomSignature
    {
        get => _domSignature;
        set => SetProperty(ref _domSignature, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    public string ExampleSnippet
    {
        get => _exampleSnippet;
        set => SetProperty(ref _exampleSnippet, value);
    }

    public int UsedByPageCount
    {
        get => _usedByPageCount;
        set => SetProperty(ref _usedByPageCount, value);
    }

    public ObservableCollection<ControlPropertyItem> Properties { get; } = [];

    public string ConfidenceLabel => $"{_confidence}%";

    public string StatusIcon => _status switch
    {
        ControlObjectStatus.Approved => "✓",
        ControlObjectStatus.Rejected => "✗",
        _ => "⏳",
    };

    public string StatusLabel => _status.ToString();
}

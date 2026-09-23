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
    private bool _isGenerated;
    private ControlProposal? _proposal;

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

    public bool IsGenerated
    {
        get => _isGenerated;
        set => SetProperty(ref _isGenerated, value);
    }

    /// <summary>The originating proposal, or null for registry-only controls with no analysis proposal.</summary>
    public ControlProposal? Proposal
    {
        get => _proposal;
        set => SetProperty(ref _proposal, value);
    }

    public ObservableCollection<ControlPropertyItem> Properties { get; } = [];

    public string ConfidenceLabel => $"{_confidence}%";

    public string StatusIcon => _status switch
    {
        ControlObjectStatus.Approved => "✓",
        ControlObjectStatus.Rejected => "✗",
        ControlObjectStatus.Generated => "⚡",
        _ => "⏳",
    };

    public string StatusLabel => _status.ToString();
}

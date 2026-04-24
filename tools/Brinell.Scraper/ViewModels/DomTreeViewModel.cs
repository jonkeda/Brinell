using System.Collections.ObjectModel;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.ViewModels;

public sealed class DomTreeViewModel : ViewModelBase
{
    private string _filterText = string.Empty;
    private DomSnapshot? _snapshot;
    private bool _isFilterActive;

    public ObservableCollection<DomElement> RootElements { get; } = [];

    public bool IsFilterActive
    {
        get => _isFilterActive;
        private set => SetProperty(ref _isFilterActive, value);
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
                ApplyFilter();
        }
    }

    public void LoadSnapshot(DomSnapshot snapshot)
    {
        _snapshot = snapshot;
        _filterText = string.Empty;
        OnPropertyChanged(nameof(FilterText));
        IsFilterActive = false;
        RootElements.Clear();
        RootElements.Add(snapshot.RootElement);
    }

    public void ShowFilteredByTags(string[] tags)
    {
        if (_snapshot is null) return;
        _filterText = string.Empty;
        OnPropertyChanged(nameof(FilterText));
        IsFilterActive = true;
        RootElements.Clear();

        var filtered = FilterElementByTags(_snapshot.RootElement, tags);
        if (filtered is not null)
            RootElements.Add(filtered);
    }

    private void ApplyFilter()
    {
        if (_snapshot is null) return;
        RootElements.Clear();

        if (string.IsNullOrWhiteSpace(_filterText))
        {
            IsFilterActive = false;
            RootElements.Add(_snapshot.RootElement);
            return;
        }

        IsFilterActive = true;
        var filtered = FilterElement(_snapshot.RootElement, _filterText);
        if (filtered is not null)
            RootElements.Add(filtered);
    }

    private static DomElement? FilterElement(DomElement element, string filter)
    {
        var matches = MatchesFilter(element, filter);
        var filteredChildren = new List<DomElement>();

        foreach (var child in element.Children)
        {
            var filtered = FilterElement(child, filter);
            if (filtered is not null)
                filteredChildren.Add(filtered);
        }

        if (!matches && filteredChildren.Count == 0)
            return null;

        return new DomElement
        {
            Tag = element.Tag,
            Id = element.Id,
            ClassName = element.ClassName,
            Name = element.Name,
            Type = element.Type,
            DataTestId = element.DataTestId,
            Role = element.Role,
            AriaLabel = element.AriaLabel,
            Placeholder = element.Placeholder,
            TextContent = element.TextContent,
            BoundingBox = element.BoundingBox,
            FrameSource = element.FrameSource,
            Children = filteredChildren
        };
    }

    private static DomElement? FilterElementByTags(DomElement element, string[] tags)
    {
        var matches = Array.Exists(tags, t => t.Equals(element.Tag, StringComparison.OrdinalIgnoreCase));
        var filteredChildren = new List<DomElement>();

        foreach (var child in element.Children)
        {
            var filtered = FilterElementByTags(child, tags);
            if (filtered is not null)
                filteredChildren.Add(filtered);
        }

        if (!matches && filteredChildren.Count == 0)
            return null;

        return new DomElement
        {
            Tag = element.Tag,
            Id = element.Id,
            ClassName = element.ClassName,
            Name = element.Name,
            Type = element.Type,
            DataTestId = element.DataTestId,
            Role = element.Role,
            AriaLabel = element.AriaLabel,
            Placeholder = element.Placeholder,
            TextContent = element.TextContent,
            BoundingBox = element.BoundingBox,
            FrameSource = element.FrameSource,
            Children = filteredChildren
        };
    }

    private static bool MatchesFilter(DomElement element, string filter)
    {
        return element.Tag.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || (element.Id?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (element.ClassName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (element.TextContent?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (element.DataTestId?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (element.AriaLabel?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}

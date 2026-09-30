using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.ViewModels;

public sealed class InspectorViewModel : ViewModelBase
{
    private DomSnapshot? _snapshot;
    private int _selectedCount;
    private int _totalElementCount;
    private bool _isInspecting;
    private string _controlGroupSummary = "";

    public InspectorViewModel()
    {
        SelectedElements = [];
        SelectedElements.CollectionChanged += OnSelectedElementsChanged;

        DomTree = new DomTreeViewModel();

        SelectAllFormsCommand = new RelayCommand(SelectAllFormElements, () => _snapshot is not null);
        SelectAllInputsCommand = new RelayCommand(SelectAllInputElements, () => _snapshot is not null);
        ClearSelectionCommand = new RelayCommand(ClearSelection, () => SelectedCount > 0);
        CaptureSnapshotCommand = new AsyncRelayCommand(OnCaptureSnapshotRequested, () => true);
        ToggleInspectCommand = new RelayCommand(() => IsInspecting = !IsInspecting);
        AcceptGroupCommand = new RelayCommand<ControlGroupSuggestion>(AcceptGroup);
        RejectGroupCommand = new RelayCommand<ControlGroupSuggestion>(RejectGroup);
        AcceptAllGroupsCommand = new RelayCommand(AcceptAllGroups, () => ControlGroups.Count > 0);
        DismissGroupsCommand = new RelayCommand(DismissGroups, () => ControlGroups.Count > 0);
    }

    public ObservableCollection<DomElement> SelectedElements { get; }

    public DomTreeViewModel DomTree { get; }

    public int SelectedCount
    {
        get => _selectedCount;
        private set => SetProperty(ref _selectedCount, value);
    }

    public int TotalElementCount
    {
        get => _totalElementCount;
        private set => SetProperty(ref _totalElementCount, value);
    }

    public bool IsInspecting
    {
        get => _isInspecting;
        set => SetProperty(ref _isInspecting, value);
    }

    public DomSnapshot? Snapshot => _snapshot;

    public string ControlGroupSummary
    {
        get => _controlGroupSummary;
        set => SetProperty(ref _controlGroupSummary, value);
    }

    public ObservableCollection<ControlGroupSuggestion> ControlGroups { get; } = [];

    public ICommand SelectAllFormsCommand { get; }
    public ICommand SelectAllInputsCommand { get; }
    public ICommand ClearSelectionCommand { get; }
    public ICommand CaptureSnapshotCommand { get; }
    public ICommand ToggleInspectCommand { get; }
    public ICommand AcceptGroupCommand { get; }
    public ICommand RejectGroupCommand { get; }
    public ICommand AcceptAllGroupsCommand { get; }
    public ICommand DismissGroupsCommand { get; }

    /// <summary>Fired when user requests a manual DOM capture.</summary>
    public event Func<Task>? CaptureSnapshotRequested;

    /// <summary>Fired when an element is selected/deselected (for browser overlay sync).</summary>
    public event Action<DomElement, bool>? ElementSelectionChanged;

    /// <summary>Fired when the entire selection is cleared.</summary>
    public event Action? SelectionCleared;

    public void LoadSnapshot(DomSnapshot snapshot)
    {
        _snapshot = snapshot;
        SelectedElements.Clear();
        DomTree.LoadSnapshot(snapshot);
        TotalElementCount = CountElements(snapshot.RootElement);
        ControlGroups.Clear();
        ControlGroupSummary = "";
        ((RelayCommand)SelectAllFormsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SelectAllInputsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AcceptAllGroupsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)DismissGroupsCommand).RaiseCanExecuteChanged();
    }

    public void LoadControlGroups(List<ControlGroupSuggestion> groups)
    {
        ControlGroups.Clear();
        foreach (var g in groups)
            ControlGroups.Add(g);

        if (groups.Count > 0)
        {
            foreach (var group in groups)
                group.IsAccepted = null;

            var parts = groups.GroupBy(g => g.ContainerType)
                .Select(g => $"{g.Count()} {g.Key.Replace("Container", "").ToLower()}(s)");
            ControlGroupSummary = $"Found {string.Join(", ", parts)}";
        }
        else
        {
            ControlGroupSummary = "";
        }

        ((RelayCommand)AcceptAllGroupsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)DismissGroupsCommand).RaiseCanExecuteChanged();
    }

    public void ToggleElement(DomElement element)
    {
        if (SelectedElements.Contains(element))
        {
            SelectedElements.Remove(element);
            ElementSelectionChanged?.Invoke(element, false);
        }
        else
        {
            SelectedElements.Add(element);
            ElementSelectionChanged?.Invoke(element, true);
        }
    }

    public void ClearSelection()
    {
        SelectedElements.Clear();
        if (_snapshot is not null)
            DomTree.LoadSnapshot(_snapshot);
        SelectionCleared?.Invoke();
    }

    private void SelectAllFormElements()
    {
        if (_snapshot is null) return;
        SelectByTags(_snapshot.RootElement, ["input", "select", "textarea", "button"]);
    }

    private void SelectAllInputElements()
    {
        if (_snapshot is null) return;
        SelectByTags(_snapshot.RootElement, ["input"]);
    }

    private void SelectByTags(DomElement root, string[] tags)
    {
        ClearSelection();
        var matches = new List<DomElement>();
        CollectByTags(root, tags, matches);
        foreach (var element in matches)
            SetElementSelected(element, true);
        DomTree.ShowFilteredByTags(tags);
    }

    private static void CollectByTags(DomElement element, string[] tags, List<DomElement> results)
    {
        if (Array.Exists(tags, t => t.Equals(element.Tag, StringComparison.OrdinalIgnoreCase)))
            results.Add(element);

        foreach (var child in element.Children)
            CollectByTags(child, tags, results);
    }

    private void OnSelectedElementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SelectedCount = SelectedElements.Count;
        ((RelayCommand)ClearSelectionCommand).RaiseCanExecuteChanged();
    }

    private void AcceptGroup(ControlGroupSuggestion? group)
    {
        if (group is null) return;

        group.IsAccepted = true;
        foreach (var child in group.ChildElements)
            SetElementSelected(child, true);
    }

    private void RejectGroup(ControlGroupSuggestion? group)
    {
        if (group is null) return;
        group.IsAccepted = false;
    }

    private void AcceptAllGroups()
    {
        foreach (var group in ControlGroups)
            AcceptGroup(group);
    }

    private void DismissGroups()
    {
        ControlGroups.Clear();
        ControlGroupSummary = "";
        ((RelayCommand)AcceptAllGroupsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)DismissGroupsCommand).RaiseCanExecuteChanged();
    }

    private void SetElementSelected(DomElement element, bool selected)
    {
        if (selected)
        {
            if (SelectedElements.Contains(element)) return;
            SelectedElements.Add(element);
            ElementSelectionChanged?.Invoke(element, true);
            return;
        }

        if (!SelectedElements.Remove(element)) return;
        ElementSelectionChanged?.Invoke(element, false);
    }

    private async Task OnCaptureSnapshotRequested(CancellationToken ct)
    {
        if (CaptureSnapshotRequested is not null)
            await CaptureSnapshotRequested.Invoke();
    }

    private static int CountElements(DomElement element)
    {
        var count = 1;
        foreach (var child in element.Children)
            count += CountElements(child);
        return count;
    }
}

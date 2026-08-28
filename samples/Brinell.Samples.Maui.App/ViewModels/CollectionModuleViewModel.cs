using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for the collection module test view.
/// </summary>
/// <remarks>
/// <see cref="CollectionItem"/> carries no Id or index and the commands do no reindexing.
/// Rows are addressed by scope, so the model stays a plain model — the same rule the
/// product demo follows.
/// </remarks>
public class CollectionModuleViewModel : ParentViewModel
{
    private const int SeedCount = 5;
    private const string NoSelection = "none";

    private static readonly string[] SeedNames =
        ["Alpha", "Bravo", "Charlie", "Delta", "Echo"];

    private string _status = "ready";
    private CollectionItem? _selectedItem;
    private int _carouselPosition;
    private bool _notificationsEnabled = true;
    private int _added;

    public CollectionModuleViewModel()
    {
        Items = [];
        Items.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ItemCount));

        AddCommand = new RelayCommand(Add);
        RemoveCommand = new RelayCommand(Remove);
        ResetCommand = new RelayCommand(Reset);
        NextCommand = new RelayCommand(Next);
        PreviousCommand = new RelayCommand(Previous);

        Reset();
    }

    /// <summary>The items shared by the ListView and the CarouselView.</summary>
    public ObservableCollection<CollectionItem> Items { get; }

    /// <summary>The logical item count, which tests wait on rather than counting rows.</summary>
    public int ItemCount => Items.Count;

    /// <summary>The most recent action.</summary>
    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    /// <summary>The ListView's selected item.</summary>
    public CollectionItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(SelectedName));
                Status = value == null ? "selection cleared" : $"selected {value.Name}";
            }
        }
    }

    /// <summary>The selected item's name, or "none".</summary>
    public string SelectedName => _selectedItem?.Name ?? NoSelection;

    /// <summary>The carousel's current position, which also drives the IndicatorView.</summary>
    public int CarouselPosition
    {
        get => _carouselPosition;
        set => SetProperty(ref _carouselPosition, value);
    }

    /// <summary>Backs the TableView's SwitchCell.</summary>
    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }

    public ICommand AddCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }

    private void Add()
    {
        _added++;
        Items.Add(new CollectionItem($"Added {_added}", $"{Items.Count + 1}"));
        Status = "added";
    }

    private void Remove()
    {
        if (Items.Count == 0)
        {
            Status = "nothing to remove";
            return;
        }

        Items.RemoveAt(Items.Count - 1);
        Status = "removed";

        // Removing the tail can leave the carousel pointing past the end.
        if (CarouselPosition >= Items.Count)
        {
            CarouselPosition = Math.Max(0, Items.Count - 1);
        }
    }

    private void Next()
    {
        if (CarouselPosition < Items.Count - 1)
        {
            CarouselPosition++;
        }
    }

    private void Previous()
    {
        if (CarouselPosition > 0)
        {
            CarouselPosition--;
        }
    }

    private void Reset()
    {
        Items.Clear();
        for (var i = 0; i < SeedCount; i++)
        {
            Items.Add(new CollectionItem(SeedNames[i], $"{i + 1}"));
        }

        _added = 0;
        SelectedItem = null;
        CarouselPosition = 0;
        NotificationsEnabled = true;
        Status = "ready";
    }
}

/// <summary>
/// One collection row. Deliberately has no Id — rows are addressed by scope.
/// </summary>
public class CollectionItem(string name, string value)
{
    public string Name { get; } = name;

    public string Value { get; } = value;
}

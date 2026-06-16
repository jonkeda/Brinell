using System.Collections.ObjectModel;
using Brinell.Samples.Maui.App.Models;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for the DataGrid page demonstrating collection, selection, and grouping controls.
/// </summary>
public class DataGridViewModel : ParentViewModel
{
    private string _searchText = string.Empty;
    private SampleDataItem? _selectedItem;
    private bool _isRefreshing;
    private int _selectedCount;

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public SampleDataItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public int SelectedCount
    {
        get => _selectedCount;
        set => SetProperty(ref _selectedCount, value);
    }

    public ObservableCollection<SampleDataItem> Items { get; } = new();
    public ObservableCollection<SampleDataItem> FilteredItems { get; } = new();
    public ObservableCollection<SampleDataItem> SelectedItems { get; } = new();
    public ObservableCollection<SampleDataGroup> GroupedItems { get; } = new();
    public ObservableCollection<SampleDataItem> CarouselItems { get; } = new();

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand ClearFilterCommand { get; }
    public IAsyncRelayCommand SelectAllCommand { get; }
    public IAsyncRelayCommand UnselectAllCommand { get; }
    public IAsyncRelayCommand DeleteSelectedCommand { get; }

    public DataGridViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(this, RefreshAsync);
        ClearFilterCommand = new AsyncRelayCommand(this, ClearFilterAsync);
        SelectAllCommand = new AsyncRelayCommand(this, SelectAllAsync);
        UnselectAllCommand = new AsyncRelayCommand(this, UnselectAllAsync);
        DeleteSelectedCommand = new AsyncRelayCommand(this, DeleteSelectedAsync);

        LoadSampleData();
    }

    private void LoadSampleData()
    {
        Items.Clear();
        var categories = new[] { "Alpha", "Beta", "Gamma" };

        for (int i = 1; i <= 30; i++)
        {
            Items.Add(new SampleDataItem
            {
                Id = i,
                Title = $"Item {i}",
                Subtitle = $"Description for item {i}",
                Description = $"This is a detailed description for sample item number {i}.",
                Status = i % 3 == 0 ? "Inactive" : "Active",
                CreatedAt = DateTime.Now.AddDays(-i),
                IsStarred = i % 5 == 0
            });
        }

        FilterItems();
        LoadGroupedData(categories);
        LoadCarouselData();
    }

    private void LoadGroupedData(string[] categories)
    {
        GroupedItems.Clear();
        foreach (var category in categories)
        {
            var items = Items.Where((_, i) => categories[i % categories.Length] == category).Take(5);
            GroupedItems.Add(new SampleDataGroup(category, category.ToLower(), items));
        }
    }

    private void LoadCarouselData()
    {
        CarouselItems.Clear();
        for (int i = 1; i <= 5; i++)
        {
            CarouselItems.Add(new SampleDataItem
            {
                Id = i,
                Title = $"Featured {i}",
                Subtitle = $"Featured item description",
                ImageUrl = $"https://picsum.photos/300/200?random={i}"
            });
        }
    }

    private void FilterItems()
    {
        /*FilteredItems.Clear();
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? Items
            : Items.Where(i => i.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                               i.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var item in filtered)
            FilteredItems.Add(item);*/
    }

    private async Task RefreshAsync()
    {
        /*IsRefreshing = true;
        await Task.Delay(1000);
        LoadSampleData();
        IsRefreshing = false;*/
        await Task.CompletedTask;
    }

    private async Task ClearFilterAsync()
    {
        SearchText = string.Empty;
        await Task.CompletedTask;
    }

    private async Task SelectAllAsync()
    {
        /*SelectedItems.Clear();
        foreach (var item in FilteredItems)
        {
            item.IsStarred = true;
            SelectedItems.Add(item);
        }
        SelectedCount = SelectedItems.Count;*/
        await Task.CompletedTask;
    }

    private async Task UnselectAllAsync()
    {
        /*foreach (var item in SelectedItems)
            item.IsStarred = false;
        SelectedItems.Clear();
        SelectedCount = 0;*/
        await Task.CompletedTask;
    }

    private async Task DeleteSelectedAsync()
    {
        /*foreach (var item in SelectedItems.ToList())
        {
            Items.Remove(item);
            FilteredItems.Remove(item);
        }
        SelectedItems.Clear();
        SelectedCount = 0;*/
        await Task.CompletedTask;
    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for the Lists tab - ListView and TreeView-like hierarchy demos.
/// </summary>
public class ListsViewModel : ViewModelBase
{
    private ListItem? _selectedItem;
    private bool _isRefreshing;

    public ListsViewModel()
    {
        Items = new ObservableCollection<ListItem>
        {
            new() { Id = "1", Name = "Apple", Description = "A red fruit" },
            new() { Id = "2", Name = "Banana", Description = "A yellow fruit" },
            new() { Id = "3", Name = "Cherry", Description = "A small red fruit" },
            new() { Id = "4", Name = "Date", Description = "A sweet dried fruit" },
            new() { Id = "5", Name = "Elderberry", Description = "A dark purple berry" },
        };

        CarouselItems = new ObservableCollection<CarouselItem>
        {
            new() { Id = "0", Title = "Welcome", Subtitle = "Swipe to explore", Color = "#6200EE" },
            new() { Id = "1", Title = "Features", Subtitle = "Discover what's new", Color = "#03DAC5" },
            new() { Id = "2", Title = "Getting Started", Subtitle = "Quick start guide", Color = "#FF6D00" },
            new() { Id = "3", Title = "Settings", Subtitle = "Customize your experience", Color = "#B00020" },
        };

        TreeNodes = new ObservableCollection<TreeNode>
        {
            new()
            {
                Id = "1",
                Name = "Documents",
                Children = new ObservableCollection<TreeNode>
                {
                    new()
                    {
                        Id = "1_1",
                        Name = "Work",
                        Children = new ObservableCollection<TreeNode>
                        {
                            new() { Id = "1_1_1", Name = "Report.docx" },
                            new() { Id = "1_1_2", Name = "Budget.xlsx" }
                        }
                    },
                    new()
                    {
                        Id = "1_2",
                        Name = "Personal",
                        Children = new ObservableCollection<TreeNode>
                        {
                            new() { Id = "1_2_1", Name = "Photos" },
                            new() { Id = "1_2_2", Name = "Notes.txt" }
                        }
                    }
                }
            },
            new()
            {
                Id = "2",
                Name = "Downloads",
                Children = new ObservableCollection<TreeNode>
                {
                    new() { Id = "2_1", Name = "setup.exe" },
                    new() { Id = "2_2", Name = "readme.md" }
                }
            },
            new()
            {
                Id = "3",
                Name = "Pictures",
                Children = new ObservableCollection<TreeNode>
                {
                    new()
                    {
                        Id = "3_1",
                        Name = "Vacation",
                        Children = new ObservableCollection<TreeNode>
                        {
                            new() { Id = "3_1_1", Name = "beach.jpg" },
                            new() { Id = "3_1_2", Name = "mountain.jpg" }
                        }
                    }
                }
            }
        };

        RefreshCommand = new AsyncRelayCommand(this, RefreshAsync);
        DeleteItemCommand = new RelayCommand<ListItem>(DeleteItem);

        // Initialize paginated list
        _allPagedItems = Enumerable.Range(1, 20)
            .Select(i => new PagedItem
            {
                Id = i.ToString(),
                Title = $"Record {i}",
                Detail = $"Detail for record {i}"
            })
            .ToList();
        PagedItems = new ObservableCollection<PagedItem>();
        PreviousPageCommand = new RelayCommand(GoToPreviousPage, () => CurrentPage > 1);
        NextPageCommand = new RelayCommand(GoToNextPage, () => CurrentPage < TotalPages);
        LoadPage(1);
    }

    #region ListView

    public ObservableCollection<ListItem> Items { get; }

    public ListItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }
    public ICommand DeleteItemCommand { get; }

    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await Task.Delay(1000); // Simulate network call
        
        // Add a new item on refresh
        var newId = (Items.Count + 1).ToString();
        Items.Add(new ListItem 
        { 
            Id = newId, 
            Name = $"NewItem{newId}", 
            Description = "Added on refresh" 
        });
        
        IsRefreshing = false;
    }

    private void DeleteItem(ListItem? item)
    {
        if (item != null)
        {
            Items.Remove(item);
        }
    }

    #endregion

    #region TreeView

    public ObservableCollection<TreeNode> TreeNodes { get; }

    #endregion

    #region CarouselView

    public ObservableCollection<CarouselItem> CarouselItems { get; }

    private int _carouselPosition;
    public int CarouselPosition
    {
        get => _carouselPosition;
        set => SetProperty(ref _carouselPosition, value);
    }

    #endregion

    #region TableView Settings

    private bool _notificationsEnabled = true;
    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }

    private bool _darkModeEnabled;
    public bool DarkModeEnabled
    {
        get => _darkModeEnabled;
        set => SetProperty(ref _darkModeEnabled, value);
    }

    private string _username = "JohnDoe";
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    #endregion

    #region PaginatedList

    private const int PageSize = 5;
    private readonly List<PagedItem> _allPagedItems;

    public ObservableCollection<PagedItem> PagedItems { get; }

    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(PageInfoText));
                PreviousPageCommand.NotifyCanExecuteChanged();
                NextPageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public int TotalPages => (int)Math.Ceiling(_allPagedItems.Count / (double)PageSize);

    public string PageInfoText => $"Page {CurrentPage} of {TotalPages}";

    public IRelayCommand PreviousPageCommand { get; }
    public IRelayCommand NextPageCommand { get; }

    private void LoadPage(int page)
    {
        CurrentPage = page;
        PagedItems.Clear();
        var items = _allPagedItems
            .Skip((page - 1) * PageSize)
            .Take(PageSize);
        foreach (var item in items)
        {
            PagedItems.Add(item);
        }
    }

    private void GoToNextPage()
    {
        if (CurrentPage < TotalPages)
        {
            LoadPage(CurrentPage + 1);
        }
    }

    private void GoToPreviousPage()
    {
        if (CurrentPage > 1)
        {
            LoadPage(CurrentPage - 1);
        }
    }

    #endregion
}

/// <summary>
/// Represents an item in the ListView.
/// </summary>
public class ListItem : INotifyPropertyChanged
{
    private string _id = "";
    private string _name = "";
    private string _description = "";

    public string Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); OnPropertyChanged(nameof(AutomationId)); }
    }

    public string AutomationId => $"ListItem_{_id}";

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents a node in the TreeView-like hierarchy.
/// </summary>
public class TreeNode : INotifyPropertyChanged
{
    private string _id = "";
    private string _name = "";
    private bool _isExpanded;
    private ObservableCollection<TreeNode>? _children;

    public string Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); OnPropertyChanged(nameof(AutomationId)); }
    }

    public string AutomationId => $"Node_{_id}";

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; OnPropertyChanged(); }
    }

    public ObservableCollection<TreeNode>? Children
    {
        get => _children;
        set { _children = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasChildren)); }
    }

    public bool HasChildren => Children != null && Children.Count > 0;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents an item in the CarouselView.
/// </summary>
public class CarouselItem : INotifyPropertyChanged
{
    private string _id = "";
    private string _title = "";
    private string _subtitle = "";
    private string _color = "#6200EE";

    public string Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); OnPropertyChanged(nameof(AutomationId)); }
    }

    public string AutomationId => $"CarouselItem_{_id}";

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public string Subtitle
    {
        get => _subtitle;
        set { _subtitle = value; OnPropertyChanged(); }
    }

    public string Color
    {
        get => _color;
        set { _color = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents an item in the PaginatedList.
/// </summary>
public class PagedItem : INotifyPropertyChanged
{
    private string _id = "";
    private string _title = "";
    private string _detail = "";

    public string Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); OnPropertyChanged(nameof(AutomationId)); }
    }

    public string AutomationId => $"PagedItem_{_id}";

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public string Detail
    {
        get => _detail;
        set { _detail = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

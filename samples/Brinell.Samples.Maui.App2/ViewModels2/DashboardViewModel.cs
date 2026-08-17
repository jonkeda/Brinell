using Brinell.Samples.Maui.App.Models2;

namespace Brinell.Samples.Maui.App.ViewModels2;

/// <summary>
/// ViewModel for the Dashboard page demonstrating tabs, progress, and status controls.
/// </summary>
public class DashboardViewModel : ParentViewModel
{
    private int _selectedTabIndex;
    private double _loadProgress;
    private bool _isLoading;
    private string _lastUpdated = string.Empty;
    private int _kpi1Value = 1234;
    private int _kpi2Value = 567;
    private int _kpi3Value = 89;
    private bool _isRefreshing;

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public double LoadProgress
    {
        get => _loadProgress;
        set => SetProperty(ref _loadProgress, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string LastUpdated
    {
        get => _lastUpdated;
        set => SetProperty(ref _lastUpdated, value);
    }

    public int Kpi1Value
    {
        get => _kpi1Value;
        set => SetProperty(ref _kpi1Value, value);
    }

    public int Kpi2Value
    {
        get => _kpi2Value;
        set => SetProperty(ref _kpi2Value, value);
    }

    public int Kpi3Value
    {
        get => _kpi3Value;
        set => SetProperty(ref _kpi3Value, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public ObservableCollection<SampleDataItem> StatusItems { get; } = new();

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IAsyncRelayCommand SettingsCommand { get; }

    public DashboardViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(this, RefreshAsync);
        ExportCommand = new AsyncRelayCommand(this, ExportAsync);
        SettingsCommand = new AsyncRelayCommand(this, OpenSettingsAsync);

        LastUpdated = DateTime.Now.ToString("g");
        LoadSampleData();
    }

    private void LoadSampleData()
    {
        StatusItems.Clear();
        for (int i = 1; i <= 5; i++)
        {
            StatusItems.Add(new SampleDataItem
            {
                Id = i,
                Title = $"Status Item {i}",
                Status = i % 2 == 0 ? "Active" : "Pending",
                CreatedAt = DateTime.Now.AddHours(-i)
            });
        }
    }

    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        IsLoading = true;
        LoadProgress = 0;

        for (int i = 0; i <= 10; i++)
        {
            LoadProgress = i / 10.0;
            await Task.Delay(100);
        }

        Kpi1Value = Random.Shared.Next(1000, 2000);
        Kpi2Value = Random.Shared.Next(500, 1000);
        Kpi3Value = Random.Shared.Next(50, 150);
        LastUpdated = DateTime.Now.ToString("g");

        IsLoading = false;
        IsRefreshing = false;
    }

    private async Task ExportAsync()
    {
        IsLoading = true;
        await Task.Delay(1000);
        IsLoading = false;
    }

    private async Task OpenSettingsAsync()
    {
        await Task.CompletedTask;
    }
}

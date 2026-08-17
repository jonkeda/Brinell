using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Brinell.Samples.Maui.App.ViewModels2;

/// <summary>
/// ViewModel for the Container Demo page.
/// </summary>
public class ContainerDemoViewModel : ParentViewModel
{
    private string _profileName = "";
    private string _profileEmail = "";
    private string _profileStatus = "";
    private string _innerText = "";
    private string _newTaskName = "";
    private TaskItem? _selectedTask;

    public ContainerDemoViewModel()
    {
        Tasks = new ObservableCollection<TaskItem>
        {
            new() { Name = "Buy groceries", IsCompleted = false },
            new() { Name = "Walk the dog", IsCompleted = true },
            new() { Name = "Finish report", IsCompleted = false }
        };

        ReindexTasks();

        SaveProfileCommand = new AsyncRelayCommand(this, SaveProfileAsync);
        InnerActionCommand = new AsyncRelayCommand(this, InnerActionAsync);
        OuterActionCommand = new AsyncRelayCommand(this, OuterActionAsync);
        AddTaskCommand = new AsyncRelayCommand(this, AddTaskAsync);
        DeleteTaskCommand = new RelayCommand<TaskItem>(DeleteTask);
    }

    #region Profile Section

    public string ProfileName
    {
        get => _profileName;
        set => SetProperty(ref _profileName, value);
    }

    public string ProfileEmail
    {
        get => _profileEmail;
        set => SetProperty(ref _profileEmail, value);
    }

    public string ProfileStatus
    {
        get => _profileStatus;
        set => SetProperty(ref _profileStatus, value);
    }

    public IAsyncRelayCommand SaveProfileCommand { get; }

    private async Task SaveProfileAsync()
    {
        await Task.Delay(100);
        ProfileStatus = $"Saved: {ProfileName}";
    }

    #endregion

    #region Nested Containers

    public string InnerText
    {
        get => _innerText;
        set => SetProperty(ref _innerText, value);
    }

    public IAsyncRelayCommand InnerActionCommand { get; }
    public IAsyncRelayCommand OuterActionCommand { get; }

    private async Task InnerActionAsync()
    {
        await Task.Delay(50);
        InnerText = "Inner clicked";
    }

    private async Task OuterActionAsync()
    {
        await Task.Delay(50);
        InnerText = "Outer clicked";
    }

    #endregion

    #region Task List

    public ObservableCollection<TaskItem> Tasks { get; }

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set => SetProperty(ref _selectedTask, value);
    }

    public string NewTaskName
    {
        get => _newTaskName;
        set => SetProperty(ref _newTaskName, value);
    }

    public int TaskCount => Tasks.Count;

    public IAsyncRelayCommand AddTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }

    private async Task AddTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTaskName)) return;
        
        await Task.Delay(50);
        Tasks.Add(new TaskItem { Name = NewTaskName, IsCompleted = false });
        ReindexTasks();
        NewTaskName = "";
        OnPropertyChanged(nameof(TaskCount));
    }

    private void DeleteTask(TaskItem? task)
    {
        if (task == null) return;
        Tasks.Remove(task);
        ReindexTasks();
        OnPropertyChanged(nameof(TaskCount));
    }

    private void ReindexTasks()
    {
        for (var index = 0; index < Tasks.Count; index++)
        {
            Tasks[index].Id = index;
        }
    }

    #endregion
}

/// <summary>
/// Represents a task item in the task list.
/// </summary>
public class TaskItem : INotifyPropertyChanged
{
    private int _id;
    private string _name = "";
    private bool _isCompleted;

    /// <summary>
    /// Unique identifier for this task, used for AutomationId.
    /// </summary>
    public int Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); OnPropertyChanged(nameof(AutomationId)); }
    }

    /// <summary>
    /// AutomationId for UI testing: Task_0, Task_1, etc.
    /// </summary>
    public string AutomationId => $"Task_{_id}";

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set { _isCompleted = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

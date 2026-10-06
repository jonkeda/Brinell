using System.Collections.ObjectModel;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;
using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Rules;
using Brinell.Samples.Todo.Core.Services;

namespace Brinell.Samples.Todo.Core.ViewModels;

/// <summary>One row of the users list, worked out once when the list loads.</summary>
public sealed class UserRowViewModel(User user, DateOnly today)
{
    /// <summary>The user's id.</summary>
    public Guid Id { get; } = user.Id;

    /// <summary>The name.</summary>
    public string Name { get; } = user.Name;

    /// <summary>"Member", "Lead" or "Admin".</summary>
    public string RoleText { get; } = UserRules.RoleNames[(int)user.Role];

    /// <summary>"Expired", "On leave", "Inactive", or empty.</summary>
    public string StateText { get; } = UserRules.StateText(user, today);

    /// <summary>Whether the row shows a state at all.</summary>
    public bool HasState => StateText.Length > 0;

    /// <summary>Whether the user is available today.</summary>
    public bool IsAvailable { get; } = UserRules.IsAvailable(user, today);
}

/// <summary>
/// The users list: available users first, then the rest; Add; and a notice while offline.
/// </summary>
public sealed class UserListViewModel : ParentViewModel
{
    private readonly UserService _users;
    private readonly IConnectivityState _connectivity;
    private readonly INavigator _navigator;

    private bool _isOnline;
    private UserRowViewModel? _selectedItem;

    /// <summary>Creates the view model.</summary>
    public UserListViewModel(UserService users, IConnectivityState connectivity, INavigator navigator)
    {
        _users = users;
        _connectivity = connectivity;
        _navigator = navigator;

        AddCommand = new TargetAsyncCommand(this, () => _navigator.GoToAsync(UserRoutes.Edit), () => IsOnline);
        OpenCommand = new TargetAsyncCommand<UserRowViewModel>(this, OpenAsync);

        _isOnline = _connectivity.IsOnline;
        _connectivity.Changed += (_, _) => IsOnline = _connectivity.IsOnline;
    }

    /// <summary>The rows, in list order.</summary>
    public ObservableCollection<UserRowViewModel> Items { get; } = [];

    /// <summary>Whether changes can be made; Add is disabled and the notice shows when not.</summary>
    public bool IsOnline
    {
        get => _isOnline;
        private set
        {
            if (SetProperty(ref _isOnline, value))
            {
                OnPropertyChanged(nameof(IsOffline));
                AddCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Whether the offline notice shows.</summary>
    public bool IsOffline => !IsOnline;

    /// <summary>The offline notice.</summary>
    public string OfflineNotice => UserService.OfflineMessage;

    /// <summary>
    /// The row the list has selected. Selecting a row opens it, and the selection is cleared so
    /// the same row can be opened again after coming back.
    /// </summary>
    public UserRowViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value) || value is null)
            {
                return;
            }

            _ = OpenCommand.ExecuteAsync(value);
            SetProperty(ref _selectedItem, null);
        }
    }

    /// <summary>Opens a new, empty user. Disabled offline.</summary>
    public IAsyncRelayCommand AddCommand { get; }

    /// <summary>Opens a user. Allowed offline, to read it.</summary>
    public IAsyncRelayCommand<UserRowViewModel> OpenCommand { get; }

    /// <summary>The last reload started by the page appearing. For tests.</summary>
    public Task Reloading { get; private set; } = Task.CompletedTask;

    /// <summary>Reloads the rows each time the page appears, so a saved user shows on return.</summary>
    public override void OnViewAppearing()
    {
        base.OnViewAppearing();
        Reloading = ReloadAsync();
    }

    /// <summary>Reads the cached users into <see cref="Items"/>.</summary>
    public async Task ReloadAsync()
    {
        var today = _users.Today;
        var users = UserRules.Arrange(await _users.GetAllAsync(), today);

        Items.Clear();
        foreach (var user in users)
        {
            Items.Add(new UserRowViewModel(user, today));
        }

        IsOnline = _connectivity.IsOnline;
    }

    private Task OpenAsync(UserRowViewModel? row)
        => row is null
            ? Task.CompletedTask
            : _navigator.GoToAsync(UserRoutes.Edit, TodoRoutes.ForTodo(row.Id));
}

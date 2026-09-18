using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;
using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Rules;
using Brinell.Samples.Todo.Core.Services;

namespace Brinell.Samples.Todo.Core.ViewModels;

/// <summary>
/// The edit page, for a new todo or an existing one.
/// </summary>
public sealed class TodoEditViewModel : ParentViewModel
{
    /// <summary>The discard confirmation's title.</summary>
    public const string DiscardTitle = "Discard changes?";

    /// <summary>The discard confirmation's accept button.</summary>
    public const string DiscardAccept = "Discard";

    /// <summary>The discard confirmation's cancel button.</summary>
    public const string DiscardCancel = "Keep editing";

    private readonly TodoService _todos;
    private readonly INavigator _navigator;
    private readonly IDialogs _dialogs;

    private Guid? _id;
    private Snapshot _original;
    private string _title = string.Empty;
    private string _notes = string.Empty;
    private bool _hasDueDate;
    private DateTime _dueDate;
    private TodoStatus _status;
    private string? _titleError;

    /// <summary>Creates the view model, as an empty new todo.</summary>
    public TodoEditViewModel(TodoService todos, INavigator navigator, IDialogs dialogs)
    {
        _todos = todos;
        _navigator = navigator;
        _dialogs = dialogs;

        _dueDate = _todos.Today.ToDateTime(TimeOnly.MinValue);
        _original = Current();

        SaveCommand = new TargetAsyncCommand(this, SaveAsync);
        CancelCommand = new TargetAsyncCommand(this, CancelAsync);
    }

    /// <summary>Whether this is a new todo.</summary>
    public bool IsNew => _id is null;

    /// <summary>"New todo" or "Edit todo".</summary>
    public string PageTitle => IsNew ? "New todo" : "Edit todo";

    /// <summary>The title as typed. Typing clears a shown error.</summary>
    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value ?? string.Empty))
            {
                TitleError = null;
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    /// <summary>The notes as typed.</summary>
    public string Notes
    {
        get => _notes;
        set
        {
            if (SetProperty(ref _notes, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    /// <summary>Whether the todo has a due date; off means <see cref="DueDate"/> is ignored (TOD.02.5).</summary>
    public bool HasDueDate
    {
        get => _hasDueDate;
        set
        {
            if (SetProperty(ref _hasDueDate, value))
            {
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(IsPastDue));
            }
        }
    }

    /// <summary>The picked due date. A <see cref="DateTime"/> because that is what DatePicker binds.</summary>
    public DateTime DueDate
    {
        get => _dueDate;
        set
        {
            if (SetProperty(ref _dueDate, value.Date))
            {
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(IsPastDue));
            }
        }
    }

    /// <summary>The status, set with the status control's Next.</summary>
    public TodoStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    /// <summary>Whether the picked due date has passed, for the status control.</summary>
    public bool IsPastDue => HasDueDate && DateOnly.FromDateTime(DueDate) < _todos.Today;

    /// <summary>The title's validation error, shown after a Save attempt; <c>null</c> when none.</summary>
    public string? TitleError
    {
        get => _titleError;
        private set
        {
            if (SetProperty(ref _titleError, value))
            {
                OnPropertyChanged(nameof(HasTitleError));
            }
        }
    }

    /// <summary>Whether <see cref="TitleError"/> is shown.</summary>
    public bool HasTitleError => _titleError is not null;

    /// <summary>Whether anything differs from what was loaded (TOD.04.3).</summary>
    public bool IsDirty => Current() != _original;

    /// <summary>Validates, saves and returns.</summary>
    public IAsyncRelayCommand SaveCommand { get; }

    /// <summary>Returns, asking first when there are unsaved changes.</summary>
    public IAsyncRelayCommand CancelCommand { get; }

    /// <summary>Loads a todo to edit, or resets to a new one when <paramref name="id"/> is <c>null</c>.</summary>
    public async Task LoadAsync(Guid? id)
    {
        _id = id;
        var item = id is { } existing ? await _todos.GetAsync(existing) : null;

        _title = item?.Title ?? string.Empty;
        _notes = item?.Notes ?? string.Empty;
        _hasDueDate = item?.DueDate is not null;
        _dueDate = (item?.DueDate ?? _todos.Today).ToDateTime(TimeOnly.MinValue);
        _status = item?.Status ?? TodoStatus.Open;
        _titleError = null;
        _original = Current();

        OnPropertyChanged(string.Empty);
    }

    private async Task SaveAsync()
    {
        if (TodoValidation.ValidateTitle(Title) is { } error)
        {
            TitleError = error;
            return;
        }

        DateOnly? due = HasDueDate ? DateOnly.FromDateTime(DueDate) : null;

        if (_id is { } id)
        {
            await _todos.UpdateAsync(id, Title, Notes, due, Status);
        }
        else
        {
            await _todos.CreateAsync(Title, Notes, due, Status);
        }

        _original = Current();
        await _navigator.GoBackAsync();
    }

    private async Task CancelAsync()
    {
        if (IsDirty)
        {
            var discard = await _dialogs.ConfirmAsync(
                DiscardTitle,
                "Your changes to this todo have not been saved.",
                DiscardAccept,
                DiscardCancel);

            if (!discard)
            {
                return;
            }
        }

        await _navigator.GoBackAsync();
    }

    private Snapshot Current() => new(
        _title.Trim(),
        _notes.Trim(),
        _hasDueDate,
        _hasDueDate ? DateOnly.FromDateTime(_dueDate) : null,
        _status);

    private readonly record struct Snapshot(string Title, string Notes, bool HasDueDate, DateOnly? DueDate, TodoStatus Status);
}

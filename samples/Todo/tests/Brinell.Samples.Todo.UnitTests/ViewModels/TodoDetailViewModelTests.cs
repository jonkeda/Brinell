namespace Brinell.Samples.Todo.UnitTests.ViewModels;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Unit")]
public sealed class TodoDetailViewModelTests
{
    private readonly InMemoryTodoRepository _repository = new();
    private readonly INavigator _navigator = Substitute.For<INavigator>();
    private readonly IDialogs _dialogs = Substitute.For<IDialogs>();

    [Fact]
    [Trait("Journey", "TOD.03.1")]
    public async Task Load_ShowsTheTodo()
    {
        var todo = Build.Todo("Book dentist", TodoStatus.InProgress) with { Notes = "Before May" };
        _repository.With(todo);
        var detail = Create();

        await detail.LoadAsync(todo.Id);

        Assert.Equal(todo.Id, detail.Id);
        Assert.Equal("Book dentist", detail.Title);
        Assert.Equal("Before May", detail.Notes);
        Assert.True(detail.HasNotes);
        Assert.Equal("No due date", detail.DueText);
        Assert.Equal(TodoStatus.InProgress, detail.Status);
    }

    [Theory]
    [Trait("Journey", "TOD.03.3")]
    [InlineData(SyncState.Synced, "Synced")]
    [InlineData(SyncState.Pending, "Waiting to sync")]
    [InlineData(SyncState.LocalOnly, "Local only")]
    public async Task SyncText_SaysWhetherTheServerHasIt(SyncState state, string expected)
    {
        var todo = Build.Todo(sync: state);
        _repository.With(todo);
        var detail = Create();

        await detail.LoadAsync(todo.Id);

        Assert.Equal(expected, detail.SyncText);
    }

    [Fact]
    [Trait("Journey", "TOD.05.3")]
    public async Task Overdue_IsShownForALateTodo()
    {
        var todo = Build.Todo(due: Build.Today.AddDays(-2));
        _repository.With(todo);
        var detail = Create();

        await detail.LoadAsync(todo.Id);

        Assert.True(detail.IsOverdue);
    }

    [Fact]
    [Trait("Journey", "TOD.05.5")]
    public async Task ChangingTheStatus_SavesItWithoutEditing()
    {
        var todo = Build.Todo();
        _repository.With(todo);
        var detail = Create();
        await detail.LoadAsync(todo.Id);

        detail.Status = TodoStatus.InProgress;
        await detail.StatusSave;

        var saved = _repository.Items.Single();
        Assert.Equal(TodoStatus.InProgress, saved.Status);
        Assert.Equal(SyncState.Pending, saved.SyncState);
        Assert.Equal("Waiting to sync", detail.SyncText);
    }

    [Fact]
    [Trait("Journey", "TOD.05.5")]
    public async Task Loading_DoesNotCountAsAStatusChange()
    {
        var todo = Build.Todo(status: TodoStatus.Done);
        _repository.With(todo);
        var detail = Create();

        await detail.LoadAsync(todo.Id);

        Assert.Equal(0, _repository.Saves);
        Assert.Equal(SyncState.Synced, _repository.Items.Single().SyncState);
    }

    [Fact]
    [Trait("Journey", "TOD.04.1")]
    public async Task Edit_OpensTheEditPageForThisTodo()
    {
        var todo = Build.Todo();
        _repository.With(todo);
        var detail = Create();
        await detail.LoadAsync(todo.Id);

        await detail.EditCommand.ExecuteAsync(null);

        await _navigator.Received(1).GoToAsync(
            TodoRoutes.Edit,
            Arg.Is<IReadOnlyDictionary<string, object>>(parameters => (string)parameters[TodoRoutes.IdParameter] == todo.Id.ToString("D")));
    }

    [Fact]
    [Trait("Journey", "TOD.06.1")]
    public async Task Delete_AsksFirst()
    {
        var todo = Build.Todo("Buy milk");
        _repository.With(todo);
        var detail = Create();
        await detail.LoadAsync(todo.Id);

        await detail.DeleteCommand.ExecuteAsync(null);

        await _dialogs.Received(1).ConfirmAsync(
            TodoDetailViewModel.DeleteTitle,
            Arg.Is<string>(message => message.Contains("Buy milk", StringComparison.Ordinal)),
            TodoDetailViewModel.DeleteAccept,
            TodoDetailViewModel.DeleteCancel);
    }

    [Fact]
    [Trait("Journey", "TOD.06.3")]
    public async Task Delete_Cancelled_KeepsTheTodo_AndStays()
    {
        var todo = Build.Todo();
        _repository.With(todo);
        _dialogs.ConfirmAsync(default!, default!, default!, default!).ReturnsForAnyArgs(false);
        var detail = Create();
        await detail.LoadAsync(todo.Id);

        await detail.DeleteCommand.ExecuteAsync(null);

        Assert.False(_repository.Items.Single().IsDeleted);
        await _navigator.DidNotReceive().GoBackAsync();
    }

    [Fact]
    [Trait("Journey", "TOD.06.2")]
    public async Task Delete_Confirmed_DeletesAndGoesBack()
    {
        var todo = Build.Todo();
        _repository.With(todo);
        _dialogs.ConfirmAsync(default!, default!, default!, default!).ReturnsForAnyArgs(true);
        var detail = Create();
        await detail.LoadAsync(todo.Id);

        await detail.DeleteCommand.ExecuteAsync(null);

        Assert.True(_repository.Items.Single().IsDeleted);
        await _navigator.Received(1).GoBackAsync();
    }

    [Fact]
    [Trait("Journey", "TOD.06.2")]
    public async Task EditAndDelete_AreDisabled_UntilATodoIsLoaded()
    {
        var detail = Create();

        await detail.LoadAsync(Guid.NewGuid());

        Assert.False(detail.EditCommand.CanExecute(null));
        Assert.False(detail.DeleteCommand.CanExecute(null));
    }

    private TodoDetailViewModel Create() => new(new TodoService(_repository, Build.Clock()), _navigator, _dialogs);
}

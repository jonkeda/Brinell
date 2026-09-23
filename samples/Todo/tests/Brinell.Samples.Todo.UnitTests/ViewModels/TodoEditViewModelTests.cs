namespace Brinell.Samples.Todo.UnitTests.ViewModels;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Unit")]
public sealed class TodoEditViewModelTests
{
    private readonly InMemoryTodoRepository _repository = new();
    private readonly INavigator _navigator = Substitute.For<INavigator>();
    private readonly IDialogs _dialogs = Substitute.For<IDialogs>();

    [Fact]
    [Trait("Journey", "TOD.02.1")]
    public async Task New_StartsEmpty_Open_WithoutADueDate()
    {
        var edit = Create();

        await edit.LoadAsync(null);

        Assert.True(edit.IsNew);
        Assert.Equal("New todo", edit.PageTitle);
        Assert.Equal(string.Empty, edit.Title);
        Assert.Equal(TodoStatus.Open, edit.Status);
        Assert.False(edit.HasDueDate);
        Assert.False(edit.IsDirty);
    }

    [Theory]
    [Trait("Journey", "TOD.02.3")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Save_WithoutATitle_ShowsTheError_AndStaysOnThePage(string title)
    {
        var edit = Create();
        await edit.LoadAsync(null);
        edit.Title = title;

        await edit.SaveCommand.ExecuteAsync(null);

        Assert.Equal(TodoValidation.TitleRequired, edit.TitleError);
        Assert.True(edit.HasTitleError);
        Assert.Empty(_repository.Items);
        await _navigator.DidNotReceive().GoBackAsync();
    }

    [Fact]
    [Trait("Journey", "TOD.02.3")]
    public async Task TypingAfterAnError_ClearsIt()
    {
        var edit = Create();
        await edit.LoadAsync(null);
        await edit.SaveCommand.ExecuteAsync(null);

        edit.Title = "B";

        Assert.Null(edit.TitleError);
    }

    [Fact]
    [Trait("Journey", "TOD.02.2")]
    public async Task Save_New_CreatesTheTodo_AndGoesBack()
    {
        var edit = Create();
        await edit.LoadAsync(null);
        edit.Title = "Buy milk";
        edit.Notes = "Oat";
        edit.Status = TodoStatus.InProgress;

        await edit.SaveCommand.ExecuteAsync(null);

        var created = Assert.Single(_repository.Items);
        Assert.Equal("Buy milk", created.Title);
        Assert.Equal("Oat", created.Notes);
        Assert.Equal(TodoStatus.InProgress, created.Status);
        await _navigator.Received(1).GoBackAsync();
    }

    [Fact]
    [Trait("Journey", "TOD.02.5")]
    public async Task Save_WithTheDueDateSwitchedOff_StoresNoDueDate()
    {
        var todo = Build.Todo(due: new DateOnly(2026, 3, 20));
        _repository.With(todo);
        var edit = Create();
        await edit.LoadAsync(todo.Id);

        Assert.True(edit.HasDueDate);
        Assert.Equal(new DateTime(2026, 3, 20), edit.DueDate);

        edit.HasDueDate = false;
        await edit.SaveCommand.ExecuteAsync(null);

        Assert.Null(_repository.Items.Single().DueDate);
    }

    [Fact]
    [Trait("Journey", "TOD.02.5")]
    public async Task Save_WithADueDate_StoresThePickedDate()
    {
        var edit = Create();
        await edit.LoadAsync(null);
        edit.Title = "Pay rent";
        edit.HasDueDate = true;
        edit.DueDate = new DateTime(2026, 4, 1, 15, 30, 0);

        await edit.SaveCommand.ExecuteAsync(null);

        Assert.Equal(new DateOnly(2026, 4, 1), _repository.Items.Single().DueDate);
    }

    [Fact]
    [Trait("Journey", "TOD.04.1")]
    public async Task Load_Existing_FillsTheForm()
    {
        var todo = Build.Todo("Book dentist", TodoStatus.InProgress) with { Notes = "Before May" };
        _repository.With(todo);
        var edit = Create();

        await edit.LoadAsync(todo.Id);

        Assert.False(edit.IsNew);
        Assert.Equal("Edit todo", edit.PageTitle);
        Assert.Equal("Book dentist", edit.Title);
        Assert.Equal("Before May", edit.Notes);
        Assert.Equal(TodoStatus.InProgress, edit.Status);
        Assert.False(edit.IsDirty);
    }

    [Fact]
    [Trait("Journey", "TOD.04.3")]
    public async Task Cancel_OnAnUnchangedForm_LeavesWithoutAsking()
    {
        var todo = Build.Todo();
        _repository.With(todo);
        var edit = Create();
        await edit.LoadAsync(todo.Id);

        await edit.CancelCommand.ExecuteAsync(null);

        await _dialogs.DidNotReceiveWithAnyArgs().ConfirmAsync(default!, default!, default!, default!);
        await _navigator.Received(1).GoBackAsync();
    }

    [Fact]
    [Trait("Journey", "TOD.04.3")]
    public async Task ChangingAFieldBack_IsNotDirty()
    {
        var todo = Build.Todo("Buy milk");
        _repository.With(todo);
        var edit = Create();
        await edit.LoadAsync(todo.Id);

        edit.Title = "Buy bread";
        Assert.True(edit.IsDirty);

        edit.Title = "Buy milk ";
        Assert.False(edit.IsDirty);
    }

    [Fact]
    [Trait("Journey", "TOD.04.4")]
    public async Task Cancel_OnAChangedForm_KeepEditing_StaysAndSavesNothing()
    {
        var todo = Build.Todo("Buy milk");
        _repository.With(todo);
        var edit = Create();
        await edit.LoadAsync(todo.Id);
        edit.Title = "Buy bread";
        _dialogs.ConfirmAsync(TodoEditViewModel.DiscardTitle, Arg.Any<string>(), TodoEditViewModel.DiscardAccept, TodoEditViewModel.DiscardCancel)
            .Returns(false);

        await edit.CancelCommand.ExecuteAsync(null);

        await _navigator.DidNotReceive().GoBackAsync();
        Assert.Equal("Buy bread", edit.Title);
        Assert.Equal("Buy milk", _repository.Items.Single().Title);
    }

    [Fact]
    [Trait("Journey", "TOD.04.4")]
    public async Task Cancel_OnAChangedForm_Discard_LeavesTheTodoUnchanged()
    {
        var todo = Build.Todo("Buy milk");
        _repository.With(todo);
        var edit = Create();
        await edit.LoadAsync(todo.Id);
        edit.Title = "Buy bread";
        _dialogs.ConfirmAsync(default!, default!, default!, default!).ReturnsForAnyArgs(true);

        await edit.CancelCommand.ExecuteAsync(null);

        await _navigator.Received(1).GoBackAsync();
        Assert.Equal("Buy milk", _repository.Items.Single().Title);
    }

    [Fact]
    [Trait("Journey", "TOD.04.2")]
    public async Task Save_Existing_UpdatesIt_AndMarksItPending()
    {
        var todo = Build.Todo("Buy milk");
        _repository.With(todo);
        var edit = Create();
        await edit.LoadAsync(todo.Id);
        edit.Title = "Buy oat milk";

        await edit.SaveCommand.ExecuteAsync(null);

        var saved = _repository.Items.Single();
        Assert.Equal("Buy oat milk", saved.Title);
        Assert.Equal(SyncState.Pending, saved.SyncState);
        await _navigator.Received(1).GoBackAsync();
    }

    private TodoEditViewModel Create() => new(new TodoService(_repository, Build.Clock()), _navigator, _dialogs);
}

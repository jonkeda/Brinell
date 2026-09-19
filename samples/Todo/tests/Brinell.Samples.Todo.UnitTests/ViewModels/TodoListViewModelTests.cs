namespace Brinell.Samples.Todo.UnitTests.ViewModels;

[Trait("Module", "Todo")]
[Trait("Pyramid", "Unit")]
public sealed class TodoListViewModelTests
{
    private readonly InMemoryTodoRepository _repository = new();
    private readonly ISyncService _sync = Substitute.For<ISyncService>();
    private readonly INavigator _navigator = Substitute.For<INavigator>();
    private IConnectivityState _connectivity = Build.Online();

    [Fact]
    [Trait("Journey", "TOD.01.1")]
    public async Task Appearing_WithTodos_ShowsTheRows()
    {
        _repository.With(Build.Todo("Buy milk"), Build.Todo("Book dentist"));
        var list = Create(syncOnStart: false);

        Assert.Equal(ListState.Loading, list.State);
        await list.AppearAsync();

        Assert.Equal(ListState.Content, list.State);
        Assert.Null(list.StateKey);
        Assert.Equal(2, list.Items.Count);
    }

    [Fact]
    [Trait("Journey", "TOD.01.3")]
    public async Task Appearing_WithNothing_ShowsEmpty()
    {
        var list = Create(syncOnStart: false);

        await list.AppearAsync();

        Assert.Equal(ListState.Empty, list.State);
        Assert.Equal("Empty", list.StateKey);
        Assert.True(list.AddCommand.CanExecute(null));
    }

    [Fact]
    [Trait("Journey", "TOD.08.3")]
    public async Task FirstAppearance_WithSyncOnStart_ShowsLoadingUntilTheSyncAnswers()
    {
        var answer = new TaskCompletionSource<SyncResult>();
        _sync.SyncAsync(Arg.Any<CancellationToken>()).Returns(answer.Task);
        var list = Create(syncOnStart: true);

        var appearing = list.AppearAsync();
        Assert.Equal(ListState.Loading, list.State);

        _repository.With(Build.Todo("From the server"));
        answer.SetResult(new SyncResult(SyncError.None, Build.Now, Pulled: 1));
        await appearing;

        Assert.Equal(ListState.Content, list.State);
        Assert.Single(list.Items);
    }

    [Fact]
    [Trait("Journey", "TOD.04.2")]
    public async Task AppearingAgain_ReloadsButDoesNotSyncAgain()
    {
        SyncSucceeds();
        var list = Create(syncOnStart: true);
        await list.AppearAsync();

        _repository.With(Build.Todo("Added on the edit page"));
        await list.AppearAsync();

        Assert.Single(list.Items);
        await _sync.Received(1).SyncAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Journey", "TOD.08.1")]
    public async Task ServerErrorOnSync_ShowsErrorWithMessage_AndKeepsTheLocalTodos()
    {
        _repository.With(Build.Todo("Kept"));
        _sync.SyncAsync(Arg.Any<CancellationToken>()).Returns(new SyncResult(SyncError.ServerError, Build.Now));
        var list = Create(syncOnStart: true);

        await list.AppearAsync();

        Assert.Equal(ListState.Error, list.State);
        Assert.Equal(TodoListRules.ErrorMessage(SyncError.ServerError), list.ErrorMessage);
        Assert.Single(list.Items);
    }

    [Fact]
    [Trait("Journey", "TOD.08.2")]
    public async Task Retry_AfterTheServerRecovers_ShowsTheContent()
    {
        _repository.With(Build.Todo());
        _sync.SyncAsync(Arg.Any<CancellationToken>()).Returns(
            new SyncResult(SyncError.ServerError, Build.Now),
            new SyncResult(SyncError.None, Build.Now));
        var list = Create(syncOnStart: true);
        await list.AppearAsync();

        await list.RetryCommand.ExecuteAsync(null);

        Assert.Equal(ListState.Content, list.State);
    }

    [Fact]
    [Trait("Journey", "TOD.09.4")]
    public async Task UnreachableServer_KeepsShowingLocalTodos()
    {
        _repository.With(Build.Todo());
        _sync.SyncAsync(Arg.Any<CancellationToken>()).Returns(new SyncResult(SyncError.Unreachable, Build.Now));
        var list = Create(syncOnStart: true);

        await list.AppearAsync();

        Assert.Equal(ListState.Content, list.State);
    }

    [Fact]
    [Trait("Journey", "TOD.08.8")]
    public async Task Offline_DisablesSync_AndSaysOffline_UntilBackOnline()
    {
        var online = false;
        _connectivity = Substitute.For<IConnectivityState>();
        _connectivity.IsOnline.Returns(_ => online);
        var list = Create(syncOnStart: true);

        await list.AppearAsync();

        Assert.False(list.SyncCommand.CanExecute(null));
        Assert.Equal("Offline", list.SyncLine);
        await _sync.DidNotReceive().SyncAsync(Arg.Any<CancellationToken>());

        online = true;
        _connectivity.Changed += Raise.Event();

        Assert.True(list.SyncCommand.CanExecute(null));
        Assert.Equal("Not synced yet", list.SyncLine);
    }

    [Fact]
    [Trait("Journey", "TOD.07.1")]
    public async Task Sync_CannotStartTwiceAtOnce()
    {
        var answer = new TaskCompletionSource<SyncResult>();
        _sync.SyncAsync(Arg.Any<CancellationToken>()).Returns(answer.Task);
        var list = Create(syncOnStart: false);
        await list.AppearAsync();

        var first = list.SyncCommand.ExecuteAsync(null);
        Assert.False(list.SyncCommand.CanExecute(null));
        await list.SyncCommand.ExecuteAsync(null);

        answer.SetResult(new SyncResult(SyncError.None, Build.Now));
        await first;

        await _sync.Received(1).SyncAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Journey", "TOD.07.1")]
    public async Task Sync_WhileRunning_TellsTheUiItIsDisabled()
    {
        var answer = new TaskCompletionSource<SyncResult>();
        _sync.SyncAsync(Arg.Any<CancellationToken>()).Returns(answer.Task);
        var list = Create(syncOnStart: false);
        await list.AppearAsync();
        var announced = new List<bool>();
        list.SyncCommand.CanExecuteChanged += (_, _) => announced.Add(list.SyncCommand.CanExecute(null));

        var running = list.SyncCommand.ExecuteAsync(null);
        answer.SetResult(new SyncResult(SyncError.None, Build.Now));
        await running;

        // A button bound to the command greys out while it runs, and comes back afterwards. Without
        // the first false, the button looked usable and a press during the run went nowhere.
        Assert.Equal([false, true], announced);
    }

    [Fact]
    [Trait("Journey", "TOD.01.4")]
    public async Task ChangingTheFilter_ReloadsTheRows()
    {
        _repository.With(Build.Todo("Open one"), Build.Todo("Done one", TodoStatus.Done));
        var list = Create(syncOnStart: false);
        await list.AppearAsync();

        list.FilterIndex = (int)TodoFilter.Done;
        await list.Reloading;

        Assert.Equal(["Done one"], list.Items.Select(row => row.Title));
    }

    [Fact]
    [Trait("Journey", "TOD.01.6")]
    public async Task Rows_ShowPendingAndOverdue()
    {
        _repository.With(
            Build.Todo("Late", due: Build.Today.AddDays(-1)),
            Build.Todo("Changed", sync: SyncState.Pending));
        var list = Create(syncOnStart: false);

        await list.AppearAsync();

        Assert.True(list.Items.Single(row => row.Title == "Late").IsOverdue);
        Assert.False(list.Items.Single(row => row.Title == "Late").IsPending);
        Assert.True(list.Items.Single(row => row.Title == "Changed").IsPending);
    }

    [Fact]
    [Trait("Journey", "TOD.03.1")]
    public async Task SelectingARow_OpensItsDetail_AndClearsTheSelection()
    {
        var todo = Build.Todo();
        _repository.With(todo);
        var list = Create(syncOnStart: false);
        await list.AppearAsync();

        list.SelectedItem = list.Items[0];

        await _navigator.Received(1).GoToAsync(
            TodoRoutes.Detail,
            Arg.Is<IReadOnlyDictionary<string, object>>(parameters => (string)parameters[TodoRoutes.IdParameter] == todo.Id.ToString("D")));
        Assert.Null(list.SelectedItem);
    }

    [Fact]
    [Trait("Journey", "TOD.02.1")]
    public async Task Add_OpensTheEditPageWithoutAnId()
    {
        var list = Create(syncOnStart: false);

        await list.AddCommand.ExecuteAsync(null);

        await _navigator.Received(1).GoToAsync(TodoRoutes.Edit, null);
    }

    private TodoListViewModel Create(bool syncOnStart)
        => new(new TodoService(_repository, Build.Clock()), _sync, _connectivity, _navigator, new TodoAppOptions(syncOnStart));

    private void SyncSucceeds()
        => _sync.SyncAsync(Arg.Any<CancellationToken>()).Returns(new SyncResult(SyncError.None, Build.Now));
}

using System.Runtime.CompilerServices;
using Bruinell.Samples.Todo.TestSupport.Apps;
using Bruinell.Samples.Todo.UITests;

namespace Bruinell.Samples.Todo.Mixed.UITests;

/// <summary>
/// A todo made or changed in one app reaches the others through the backend: Windows, web and
/// Android apps in one test (TOD.07).
/// </summary>
/// <remarks>
/// Each app waits for another on observed state, never on time: the backend's record of the PUT
/// the sending app made, then the row in the receiving app.
/// </remarks>
[Collection(TodoAppsCollection.Name)]
public sealed class SyncAcrossAppsTests(TodoAppsFixture apps) : IAsyncLifetime
{
    private const int RowTimeoutMs = TestConstants.PageTimeoutMs;

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs * 4)]
    [Trait("Journey", "TOD.07.11")]
    [Trait("Pyramid", "UiHermetic")]
    public Task ATodoCreatedOnWindows_ReachesTheBrowser_AfterItSyncs() => Run(() =>
    {
        var daan = apps.Maui("Daan");
        var finn = apps.Web("Finn");
        var title = TodoAppsFixture.UniqueTitle("Pack for the trip");

        daan.List.CreateTodo(title).Sync();
        apps.WaitForTodoFrom(daan, title);

        finn.List.Todos.AssertNoRow(title);
        finn.List.Sync().Todos.AssertRow(title, timeoutMs: RowTimeoutMs);
    });

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs * 4)]
    [Trait("Journey", "TOD.07.12")]
    [Trait("Pyramid", "UiHermetic")]
    public Task TwoWindowsApps_SideBySide_AnEditInOneReachesTheOther() => Run(() =>
    {
        var daan = apps.Maui("Daan");
        var gus = apps.Maui("Gus");
        var title = TodoAppsFixture.UniqueTitle("Water the plants");
        var edited = title + " (edited)";

        daan.List.CreateTodo(title).Sync();
        apps.WaitForTodoFrom(daan, title);
        gus.List.Sync().Todos.AssertRow(title, timeoutMs: RowTimeoutMs);

        gus.List.Open(title).Edit().EnterTitle(edited).SaveEdit().Back().Sync();
        apps.WaitForTodoFrom(gus, edited);

        daan.List.Sync().Todos.AssertRow(edited, timeoutMs: RowTimeoutMs);
        daan.List.Todos.AssertNoRow(title);
    });

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs * 4)]
    [Trait("Journey", "TOD.07.13")]
    [Trait("Pyramid", "UiHermetic")]
    public Task BothAppsEditOneTodo_TheNewerEditWinsOnBoth() => Run(() =>
    {
        // Finn's clock is ten minutes ahead of Daan's (apps.md), so Finn's edit is the newer one.
        var daan = apps.Maui("Daan");
        var finn = apps.Web("Finn");
        var title = TodoAppsFixture.UniqueTitle("Book the hotel");
        var daansEdit = title + " (Daan)";
        var finnsEdit = title + " (Finn)";

        daan.List.CreateTodo(title).Sync();
        apps.WaitForTodoFrom(daan, title);
        finn.List.Sync().Todos.AssertRow(title, timeoutMs: RowTimeoutMs);

        daan.Network.SetOffline();
        daan.List.Open(title).Edit().EnterTitle(daansEdit).SaveEdit().Back();
        finn.List.Open(title).Edit().EnterTitle(finnsEdit).SaveEdit().Back().Sync();
        apps.WaitForTodoFrom(finn, finnsEdit);

        daan.Network.SetOnline();
        daan.List.Sync().Todos.AssertRow(finnsEdit, timeoutMs: RowTimeoutMs);
        daan.List.Todos.AssertNoRow(daansEdit);
        finn.List.Sync().Todos.AssertRow(finnsEdit, timeoutMs: RowTimeoutMs);
    });

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs * 4)]
    [Trait("Journey", "TOD.07.14")]
    [Trait("Pyramid", "UiHermetic")]
    public Task AnOfflineBrowser_GetsTheTodo_WhenItIsBackOnline() => Run(() =>
    {
        var daan = apps.Maui("Daan");
        var finn = apps.Web("Finn");
        var title = TodoAppsFixture.UniqueTitle("Renew the insurance");

        finn.Network.SetOffline();
        daan.List.CreateTodo(title).Sync();
        apps.WaitForTodoFrom(daan, title);

        // Offline, the app does not offer a sync at all.
        finn.List.SyncButton.AssertEnabled(false, timeoutMs: RowTimeoutMs);
        finn.List.Todos.AssertNoRow(title);

        finn.Network.SetOnline();
        finn.List.SyncButton.AssertEnabled(true, timeoutMs: RowTimeoutMs);
        finn.List.Sync().Todos.AssertRow(title, timeoutMs: RowTimeoutMs);
    });

    [AndroidFact(Timeout = TestConstants.DefaultTestTimeoutMs * 8)]
    [Trait("Journey", "TOD.07.15")]
    [Trait("Pyramid", "UiHermetic")]
    public Task ATodoCreatedOnAndroid_ReachesWindows() => Run(() =>
    {
        var eva = apps.Maui("Eva");
        var daan = apps.Maui("Daan");
        var title = TodoAppsFixture.UniqueTitle("Call the plumber");

        eva.List.CreateTodo(title).Sync();
        apps.WaitForTodoFrom(eva, title);

        daan.List.Sync().Todos.AssertRow(title, timeoutMs: RowTimeoutMs);
    });

    /// <inheritdoc />
    public Task InitializeAsync() => apps.StartTestAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    // A failure captures every started app, not only the one whose step failed.
    private Task Run(Action body, [CallerMemberName] string test = "")
    {
        apps.Run(nameof(SyncAcrossAppsTests), test, body);
        return Task.CompletedTask;
    }
}

/// <summary>Binds the mixed tests to one <see cref="TodoAppsFixture"/>.</summary>
[CollectionDefinition(Name)]
public sealed class TodoAppsCollection : ICollectionFixture<TodoAppsFixture>
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Todo: several apps";
}

/// <summary>A test that needs the Android emulator: skipped, with the reason, unless <c>BRUINELL_ANDROID=1</c>.</summary>
public sealed class AndroidFactAttribute : FactAttribute
{
    /// <summary>The variable that turns the Android tests on.</summary>
    public const string Variable = "BRUINELL_ANDROID";

    /// <summary>A fact that runs only with the emulator.</summary>
    public AndroidFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(Variable), "1", StringComparison.Ordinal))
        {
            Skip = $"Needs the Android emulator and Appium: set {Variable}=1 to run it.";
        }
    }
}

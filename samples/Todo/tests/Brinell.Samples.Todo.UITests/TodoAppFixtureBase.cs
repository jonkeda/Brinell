using Brinell.Maui.Configuration;
using Brinell.Maui.Enums;
using Brinell.Maui.Testing;
using Brinell.Mocking;
using Brinell.Samples.Todo.Infrastructure;
using Brinell.Samples.Todo.TestSupport;
using Brinell.Samples.Todo.UITests.Containers;
using Brinell.Samples.Todo.UITests.Pages;

namespace Brinell.Samples.Todo.UITests;

/// <summary>
/// The Todo app on its own database, network-state file and pinned clock, against a backend a
/// subclass provides: the WireMock fake (<see cref="TodoAppFixture"/>, hermetic) or the real API
/// (<see cref="LiveTodoAppFixture"/>, live).
/// </summary>
/// <remarks>
/// <para>
/// <b>Everything is arranged before the app starts</b>, in <see cref="CreateTestContextOptions"/>,
/// which the base constructor calls just before it launches the app. The app's <c>LaunchSettings</c>
/// travel with that one launch (<c>MauiDriverOptions.LaunchSettings</c>, plan item B1): the launched
/// process's environment on Windows, launch-intent extras on Android.
/// </para>
/// <para>
/// <b>Because the base calls that override from its constructor</b>, it runs before any field of
/// a subclass is set. So a fixture describes itself with members that answer constants
/// (<see cref="TestDataName"/>, <see cref="AdjustSettings"/>), and what it builds for the run -
/// the backend included (<see cref="StartBackend"/>) - is created inside the override.
/// </para>
/// <para>
/// <b>One app per collection, shared by its tests.</b> Each test starts with
/// <see cref="StartTest"/>, which puts back what a previous test may have changed outside the app
/// and returns to the list. Tests work on todos of their own, so order does not matter.
/// </para>
/// </remarks>
public abstract class TodoAppFixtureBase : MauiTestFixtureBase
{
    private readonly List<TempDatabase> _retired = [];
    private Arrangement? _arrangement;
    private LaunchSettings? _settings;
    private LaunchSettings? _unchangedSettings;
    private IDictionary<string, string>? _launchValues;

    /// <summary>Launches the app for <see cref="TestDataName"/> and waits for the list to settle.</summary>
    protected TodoAppFixtureBase()
    {
        WaitForTheList();
    }

    /// <summary>The test set the app starts in. Answer with a constant: it is read during base construction.</summary>
    protected abstract string TestDataName { get; }

    /// <summary>The test set, as seeded.</summary>
    public TodoTestSet TestSet => Arranged.TestSet;

    /// <summary>Takes the app offline and back.</summary>
    public NetworkStateFile Network => Arranged.Network;

    /// <summary>The list page, which is where every test starts.</summary>
    public TodoListPage List => new(Context);

    private Arrangement Arranged => _arrangement
        ?? throw new InvalidOperationException("The fixture was used before the app was arranged.");

    /// <summary>
    /// Puts back what earlier tests in the collection may have changed, and returns to the list.
    /// </summary>
    /// <remarks>
    /// Online again, the backend reset as far as the subclass resets it
    /// (<see cref="ResetBackendForTest"/>), the list on screen with the filter on All.
    /// </remarks>
    public TodoListPage StartTest()
    {
        Network.SetOnline();
        ResetBackendForTest();

        var list = ReturnToList();

        // Windows only. On Android a Picker opens a native dialog the selection controls cannot
        // reach yet (the Picker tests are 0/8 there before any change), and a dialog left open
        // would take every later test down with it. Tests that use the filter are Windows-only.
        if (Context.Platform == MauiPlatform.Windows)
        {
            list.Filter.SelectByText("All");
        }

        return list;
    }

    /// <summary>
    /// Closes the app and starts it again with the same launch settings - the same database and
    /// backend on Windows - and waits for the list.
    /// </summary>
    /// <remarks>
    /// On Android a new session reinstalls the app (<c>enforceAppInstall</c>), which clears its
    /// data: a restart there is a fresh install, not a restart. TOD.09.3 is Windows-only for that
    /// reason.
    /// </remarks>
    public TodoListPage Restart()
    {
        RestartApp();
        OnAppRestarted();
        return WaitForTheList();
    }

    /// <summary>
    /// Restarts the app with changed launch settings, e.g. an unreachable backend. The change holds
    /// until <see cref="RestoreLaunchSettings"/>.
    /// </summary>
    /// <param name="change">Derives the new settings from the current ones.</param>
    /// <returns>The list, loaded.</returns>
    public TodoListPage RestartWith(Func<LaunchSettings, LaunchSettings> change)
    {
        _unchangedSettings ??= _settings;
        ApplySettings(change(_settings!));
        return Restart();
    }

    /// <summary>Undoes <see cref="RestartWith"/>, restarting the app. Nothing happens when nothing was changed.</summary>
    public void RestoreLaunchSettings()
    {
        if (_unchangedSettings is not { } unchanged)
        {
            return;
        }

        _unchangedSettings = null;
        ApplySettings(unchanged);
        Restart();
    }

    /// <summary>
    /// Saves a screenshot for the Reviews pass, into the run's artifacts
    /// (<c>TestResults/&lt;run-id&gt;/suites/TodoUITests/screenshots</c>, AD-007).
    /// </summary>
    /// <param name="screen">Which screen, e.g. <c>list</c>; it becomes part of the file name.</param>
    /// <returns>The file written.</returns>
    public string CaptureReview(string screen) => ScreenshotService.Capture("Review", screen, "review");

    /// <summary>
    /// Starts (or connects to) the backend for this run and returns the base URL the app should use.
    /// </summary>
    /// <remarks>
    /// Runs during base construction, after the database is seeded and before the app launches.
    /// Anything it starts is the subclass's to stop in <see cref="StopBackend"/>.
    /// </remarks>
    /// <param name="testSet">The test set: its server half is what the backend should hold.</param>
    /// <param name="runFolder">A folder of this run's own, for files the backend needs.</param>
    protected abstract string StartBackend(TodoTestSet testSet, string runFolder);

    /// <summary>Stops whatever <see cref="StartBackend"/> started. Called after the app has closed.</summary>
    protected abstract void StopBackend();

    /// <summary>Puts the backend back between tests. Nothing, unless the subclass can and should.</summary>
    protected virtual void ResetBackendForTest()
    {
    }

    /// <summary>
    /// Changes the launch settings a fixture needs beyond the test set. Answer from constants: this
    /// runs during base construction.
    /// </summary>
    protected virtual LaunchSettings AdjustSettings(LaunchSettings settings) => settings;

    /// <inheritdoc />
    protected override MauiTestContextOptions CreateTestContextOptions()
    {
        var testSet = TodoTestSet.Load(TestDataName);
        var database = new TempDatabase("ui-" + testSet.Name);
        testSet.SeedDatabaseAsync(database.Path).GetAwaiter().GetResult();

        var network = NetworkStateFile.Create(Path.Combine(database.Folder, "network-state.txt"));
        var apiBaseUrl = StartBackend(testSet, database.Folder);

        var settings = AdjustSettings(new LaunchSettings
        {
            DatabasePath = database.Path,
            ApiBaseUrl = apiBaseUrl,
            FixedNow = testSet.Now,
            NetworkStateFile = network.Path,
            ApiTimeout = TimeSpan.FromSeconds(5),
        });

        _arrangement = new Arrangement(testSet, database, network);
        _settings = settings;

        var options = base.CreateTestContextOptions();
        _launchValues = options.DriverOptions!.LaunchSettings;
        foreach (var (name, value) in LaunchValuesFor(settings, database.Folder))
        {
            _launchValues[name] = value;
        }

        return options;
    }

    /// <summary>
    /// Starts the app over on the test set called <paramref name="name"/>: a fresh database seeded
    /// from it, the backend reset to its server half, the clock at its now. Nothing happens when it
    /// is the test set already loaded.
    /// </summary>
    /// <remarks>
    /// The app restarts, so page objects built before are stale. The network state carries over.
    /// On Android only the server half and the clock change: the device's database is the app's own.
    /// </remarks>
    /// <param name="name">The test set, e.g. <c>one-todo</c>.</param>
    /// <returns>The list, loaded.</returns>
    public TodoListPage UseTestSet(string name)
    {
        if (name == Arranged.TestSet.Name)
        {
            return List;
        }

        if (!CanChangeTestSet)
        {
            throw new NotSupportedException($"{GetType().Name} cannot change its test set: its backend is seeded once, at start.");
        }

        // Drop a RestartWith change; the restart below applies every launch value again.
        _settings = _unchangedSettings ?? _settings;
        _unchangedSettings = null;

        var testSet = TodoTestSet.Load(name);
        var database = new TempDatabase("ui-" + testSet.Name);
        testSet.SeedDatabaseAsync(database.Path).GetAwaiter().GetResult();

        _settings = _settings! with { DatabasePath = database.Path, FixedNow = testSet.Now };
        foreach (var (key, value) in LaunchValuesFor(_settings, database.Folder))
        {
            _launchValues![key] = value;
        }

        // The first database's folder holds the network-state file, so earlier ones stay until Dispose.
        _retired.Add(Arranged.Database);
        _arrangement = new Arrangement(testSet, database, Arranged.Network);
        ResetBackendForTest();

        RestartApp();
        OnAppRestarted();
        return WaitForTheList();
    }

    private void ApplySettings(LaunchSettings settings)
    {
        _settings = settings;
        foreach (var (key, value) in LaunchValuesFor(settings, Arranged.Database.Folder))
        {
            _launchValues![key] = value;
        }
    }

    /// <summary>Whether <see cref="UseTestSet"/> can reseed the backend. A live backend is seeded once.</summary>
    protected virtual bool CanChangeTestSet => true;

    /// <summary>The app's database file on this machine, for read-only checks of what it stored.</summary>
    public string DatabasePath => Arranged.Database.Path;

    /// <summary>Called after the app has restarted, when <see cref="MauiTestFixtureBase.Context"/> is a new object.</summary>
    protected virtual void OnAppRestarted()
    {
    }

    /// <summary>
    /// The settings as the app on this platform receives them (<c>MauiDriverOptions.LaunchSettings</c>:
    /// the launched process's environment on Windows, launch-intent extras on Android).
    /// </summary>
    /// <remarks>
    /// <para>
    /// On Android the device cannot open files on this machine, so the database path, the
    /// network-state file and the crash log are left out: the app uses its own database, which
    /// the install at session start leaves empty. The test set's local half therefore does not
    /// reach an Android app; its server half does, through the first sync. The backend URL is
    /// rewritten to the emulator's address for this machine (<c>AndroidHost</c>, plan item B4).
    /// </para>
    /// <para>
    /// Per launch, not per process: two fixtures no longer share one environment.
    /// </para>
    /// </remarks>
    private Dictionary<string, string> LaunchValuesFor(LaunchSettings settings, string runFolder)
    {
        var values = new Dictionary<string, string>(settings.ToVariables());

        if (Platform == MauiPlatform.Android)
        {
            values.Remove(LaunchSettings.DatabasePathVariable);
            values.Remove(LaunchSettings.NetworkStateFileVariable);
            values[LaunchSettings.ApiBaseUrlVariable] = Brinell.Maui.Appium.AndroidHost.EmulatorUrl(settings.ApiBaseUrl);
        }
        else
        {
            values["BRINELL_APP_CRASH_LOG"] = Path.Combine(runFolder, "crash.log");
        }

        return values;
    }

    /// <inheritdoc />
    /// <remarks>The capabilities the Brinell sample fixture uses, for this app's package.</remarks>
    protected override void ConfigureAndroidOptions(MauiDriverOptions options)
    {
        base.ConfigureAndroidOptions(options);
        options.AdditionalCapabilities["autoGrantPermissions"] = true;
        options.AdditionalCapabilities["newCommandTimeout"] = 300;
        options.AdditionalCapabilities["appWaitDuration"] = 60000;
        options.AdditionalCapabilities["adbExecTimeout"] = 60000;

        // MAUI hashes the activity name, so wait on the package and accept whatever it launches.
        options.AdditionalCapabilities["appWaitPackage"] = AndroidPackage;

        // A MAUI app that animates may never report idle; don't pay 10 s per command for it.
        options.AdditionalCapabilities["settings[waitForIdleTimeout]"] = 100;

        // MAUI does not bump versionCode between builds: reinstall, or a run tests the old build.
        // The reinstall also clears the app's data, which is what gives each session an empty database.
        options.AdditionalCapabilities["enforceAppInstall"] = true;
    }

    /// <summary>The Todo app's Android package name (<c>ApplicationId</c>).</summary>
    public const string AndroidPackage = "com.brinell.samples.todo";

    /// <inheritdoc />
    /// <remarks>
    /// The app built by the Todo solution, Debug preferred: the Windows exe, or the signed Android
    /// APK. Found from the test output by walking up to the folder holding
    /// <c>Brinell.Samples.Todo.slnx</c>: the base's own search looks for a <c>.sln</c>.
    /// </remarks>
    protected override string GetDefaultAppPath(MauiPlatform platform)
    {
        var (fileName, framework, buildCommand) = platform switch
        {
            MauiPlatform.Windows => ("Brinell.Samples.Todo.App.exe", "net10.0-windows", "-f net10.0-windows10.0.19041.0"),
            MauiPlatform.Android => ($"{AndroidPackage}-Signed.apk", "net10.0-android", "-f net10.0-android"),
            _ => throw new NotSupportedException($"The Todo UI tests run on Windows and Android, not {platform}."),
        };

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Brinell.Samples.Todo.slnx")))
        {
            root = root.Parent;
        }

        if (root is null)
        {
            throw new DirectoryNotFoundException(
                $"No folder above '{AppContext.BaseDirectory}' holds Brinell.Samples.Todo.slnx.");
        }

        var bin = Path.Combine(root.FullName, "src", "Brinell.Samples.Todo.App", "bin");
        foreach (var configuration in new[] { "Debug", "Release" })
        {
            var folder = Path.Combine(bin, configuration);
            if (!Directory.Exists(folder))
            {
                continue;
            }

            var app = Directory
                .EnumerateFiles(folder, fileName, SearchOption.AllDirectories)
                .FirstOrDefault(path => path.Contains(framework, StringComparison.OrdinalIgnoreCase));

            if (app is not null)
            {
                return app;
            }
        }

        throw new FileNotFoundException(
            $"The Todo app is not built for {platform}. From samples\\Todo: dotnet build "
            + $"src\\Brinell.Samples.Todo.App\\Brinell.Samples.Todo.App.csproj {buildCommand}",
            bin);
    }

    /// <inheritdoc />
    /// <remarks>The app first, then the backend, then the files the app had open.</remarks>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && _arrangement is { } arranged)
        {
            StopBackend();
            arranged.Network.Dispose();
            arranged.Database.Dispose();
            _retired.ForEach(database => database.Dispose());
        }
    }

    private TodoListPage WaitForTheList()
    {
        var list = TodoListPage.Arrived(Context);
        list.State.WaitShowing(TodoListState.Loading, false, TestConstants.PageTimeoutMs);
        return list;
    }

    /// <summary>
    /// Leaves whatever page or dialog an earlier test ended on, through the pages' own controls.
    /// </summary>
    /// <remarks>
    /// A dialog is answered with its leaving button (Discard for unsaved changes, Cancel for a
    /// delete), an edit page is cancelled, a detail page is left with the back arrow. Four rounds
    /// cover the deepest case: a dirty edit page over a detail page, with its dialog open.
    /// </remarks>
    private TodoListPage ReturnToList()
    {
        var appRoot = new AppRoot(Context);

        for (var round = 0; round < 4; round++)
        {
            var dialog = new ContentDialog<AppRoot>(appRoot);
            var list = new TodoListPage(Context);

            if (dialog.IsExists())
            {
                var buttons = dialog.GetButtonTexts() ?? [];
                dialog.DialogButton(buttons.Contains("Discard") ? "Discard" : "Cancel").Click();
            }
            else if (list.IsLoaded())
            {
                return list;
            }
            else if (new TodoEditPage(Context).IsLoaded())
            {
                new TodoEditPage(Context).CancelButton.Click();
            }
            else if (new TodoDetailPage(Context).IsLoaded())
            {
                new TodoDetailPage(Context).BackButton.Click();
            }

            list.WaitLoaded(true, TestConstants.PageTimeoutMs / 4);
        }

        return TodoListPage.Arrived(Context);
    }

    private sealed class Arrangement(TodoTestSet testSet, TempDatabase database, NetworkStateFile network)
    {
        public TodoTestSet TestSet { get; } = testSet;

        public TempDatabase Database { get; } = database;

        public NetworkStateFile Network { get; } = network;
    }
}

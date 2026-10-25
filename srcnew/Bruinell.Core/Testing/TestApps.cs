using Bruinell.Core.Interfaces;
using Bruinell.Core.Utilities;

namespace Bruinell.Core.Testing;

/// <summary>
/// One app a test or scenario can use: a row of an <c>Apps</c> table. The UAT engine and UI tests
/// read the same rows (<c>.docs/contracts/uat-apps-host.md</c>).
/// </summary>
/// <param name="Name">What tests and steps call the app, such as <c>Daan's PC</c>.</param>
/// <param name="User">Who uses it, such as <c>Daan</c>; several apps may have one user.</param>
/// <param name="Head">The head whose pages drive it (<c>maui</c>, <c>web</c>), or null for the suite's.</param>
/// <param name="Values">Every column of the row, by column name: <c>Platform</c>, <c>ClientId</c> and so on.</param>
public sealed record TestAppDefinition(
    string Name,
    string User,
    string? Head,
    IReadOnlyDictionary<string, string> Values)
{
    /// <summary>The value of <paramref name="column"/>, or null when it is missing or empty.</summary>
    public string? this[string column]
        => Values.TryGetValue(column, out var value) && value.Length > 0 ? value : null;

    /// <summary>A definition from a row's cells: <c>Name</c> is required, and <c>User</c> defaults to it.</summary>
    /// <param name="cells">The row, by column name.</param>
    public static TestAppDefinition FromCells(IReadOnlyDictionary<string, string> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        var values = new Dictionary<string, string>(cells, StringComparer.OrdinalIgnoreCase);
        var name = values.TryGetValue("Name", out var found) && found.Length > 0
            ? found
            : throw new ArgumentException("An app needs a Name.", nameof(cells));
        var user = values.TryGetValue("User", out var named) && named.Length > 0 ? named : name;
        var head = values.TryGetValue("Head", out var declared) && declared.Length > 0 ? declared : null;
        return new TestAppDefinition(name, user, head, values);
    }

    /// <summary>The apps of the <c>Apps</c> table in <paramref name="markdown"/>; none when it has no such table.</summary>
    /// <param name="markdown">A markdown document such as <c>apps.md</c> or <c>uat.config.md</c>.</param>
    public static IReadOnlyList<TestAppDefinition> ReadMarkdown(string markdown)
        => MarkdownTables.ReadSections(markdown).TryGetValue("Apps", out var table)
            ? table.Rows
                .Where(row => row.Cells.TryGetValue("Name", out var name) && name.Length > 0)
                .Select(row => FromCells(row.Cells))
                .ToArray()
            : [];

    /// <summary>The apps of the <c>Apps</c> table in the file at <paramref name="path"/>.</summary>
    /// <param name="path">The file, such as <c>apps.md</c>.</param>
    public static IReadOnlyList<TestAppDefinition> ReadMarkdownFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var apps = ReadMarkdown(File.ReadAllText(path));
        return apps.Count > 0
            ? apps
            : throw new InvalidOperationException($"'{path}' has no Apps table, or the table has no rows with a Name.");
    }
}

/// <summary>A running app of a test: a Windows app, an emulator, a browser context.</summary>
public interface ITestApp : IAsyncDisposable
{
    /// <summary>What the app is.</summary>
    TestAppDefinition Definition { get; }

    /// <summary>Screenshots of this app, into a folder of its own.</summary>
    IScreenshotService Screenshots { get; }

    /// <summary>Back to where every test starts, such as online and on its first page.</summary>
    /// <param name="cancellationToken">Cancels the reset.</param>
    ValueTask ResetAsync(CancellationToken cancellationToken = default);
}

/// <summary>A screenshot <see cref="TestAppSet.CaptureAll"/> took, or why it could not.</summary>
/// <param name="App">The app's name.</param>
/// <param name="Path">The file written, or null.</param>
/// <param name="Error">Why nothing was written, or null.</param>
public sealed record TestAppCapture(string App, string? Path, string? Error);

/// <summary>
/// The apps of one fixture: each started on first use, the started ones reset between tests, and
/// all of them stopped together. A test finds an app by its name or by its user's.
/// </summary>
/// <remarks>
/// Apps start one at a time, in the order tests ask for them, and stop in reverse order.
/// </remarks>
public sealed class TestAppSet : IAsyncDisposable
{
    private readonly Func<TestAppDefinition, CancellationToken, Task<ITestApp>> _start;
    private readonly List<ITestApp> _started = [];

    /// <summary>Creates the set; nothing starts until it is asked for.</summary>
    /// <param name="definitions">The apps, as in the <c>Apps</c> table.</param>
    /// <param name="start">Starts one app.</param>
    public TestAppSet(
        IEnumerable<TestAppDefinition> definitions,
        Func<TestAppDefinition, CancellationToken, Task<ITestApp>> start)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(start);

        Definitions = definitions.ToArray();
        _start = start;

        var duplicate = Definitions.GroupBy(app => app.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Two apps are called '{duplicate.Key}'.", nameof(definitions));
        }
    }

    /// <summary>Every app the set can start.</summary>
    public IReadOnlyList<TestAppDefinition> Definitions { get; }

    /// <summary>The apps running now, in the order they started.</summary>
    public IReadOnlyList<ITestApp> Started => _started;

    /// <summary>The app called <paramref name="nameOrUser"/>, started when it is not running yet.</summary>
    /// <param name="nameOrUser">An app's name, or a user with one app.</param>
    public ITestApp Get(string nameOrUser) => GetAsync(nameOrUser).GetAwaiter().GetResult();

    /// <summary>The app called <paramref name="nameOrUser"/>, of the kind a test expects.</summary>
    /// <typeparam name="TApp">The kind, such as a MAUI or a web device.</typeparam>
    /// <param name="nameOrUser">An app's name, or a user with one app.</param>
    public TApp Get<TApp>(string nameOrUser)
        where TApp : ITestApp
    {
        var app = Get(nameOrUser);
        return app is TApp typed
            ? typed
            : throw new InvalidOperationException(
                $"{app.Definition.Name} is a {app.Definition.Head ?? "default"}-head app ({app.GetType().Name}), not a {typeof(TApp).Name}.");
    }

    /// <summary>The app called <paramref name="nameOrUser"/>, started when it is not running yet.</summary>
    /// <param name="nameOrUser">An app's name, or a user with one app.</param>
    /// <param name="cancellationToken">Cancels a start.</param>
    public async Task<ITestApp> GetAsync(string nameOrUser, CancellationToken cancellationToken = default)
    {
        var definition = Resolve(nameOrUser);
        var running = _started.FirstOrDefault(app => app.Definition.Name == definition.Name);
        if (running is not null)
        {
            return running;
        }

        var started = await _start(definition, cancellationToken).ConfigureAwait(false);
        _started.Add(started);
        return started;
    }

    /// <summary>
    /// The app <paramref name="nameOrUser"/> means: an app's name, or a user with one app. For a
    /// user with several apps, the one of them in <paramref name="lastUsed"/>, when there is one.
    /// </summary>
    /// <param name="nameOrUser">An app's name or a user's.</param>
    /// <param name="lastUsed">App names used so far, most recent last; the UAT engine's memory of a scenario.</param>
    public TestAppDefinition Resolve(string nameOrUser, IEnumerable<string>? lastUsed = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameOrUser);

        var byName = Definitions.FirstOrDefault(app => string.Equals(app.Name, nameOrUser, StringComparison.OrdinalIgnoreCase));
        if (byName is not null)
        {
            return byName;
        }

        var owned = Definitions.Where(app => string.Equals(app.User, nameOrUser, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (owned.Length == 1)
        {
            return owned[0];
        }

        if (owned.Length > 1)
        {
            var last = lastUsed?.LastOrDefault(used => owned.Any(app => string.Equals(app.Name, used, StringComparison.OrdinalIgnoreCase)));
            return last is not null
                ? owned.First(app => string.Equals(app.Name, last, StringComparison.OrdinalIgnoreCase))
                : throw new InvalidOperationException(
                    $"{nameOrUser} has several apps ({string.Join(", ", owned.Select(app => app.Name))}). Name the app.");
        }

        throw new InvalidOperationException($"No app or user '{nameOrUser}'. Apps: {Describe()}.");
    }

    /// <summary>Resets every started app. Apps no test has asked for stay stopped.</summary>
    /// <param name="cancellationToken">Cancels the resets.</param>
    public async Task ResetStartedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var app in _started)
        {
            await app.ResetAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>A screenshot of every started app, each into its own folder (AD-007).</summary>
    /// <param name="testClass">The test class, for the file name.</param>
    /// <param name="testName">The test, for the file name.</param>
    /// <param name="description">What the capture is for, such as <c>failure</c>.</param>
    /// <remarks>One app that cannot be captured does not stop the others: its entry says why.</remarks>
    public IReadOnlyList<TestAppCapture> CaptureAll(string testClass, string testName, string description)
        => _started.Select(app => TryCapture(app, testClass, testName, description)).ToArray();

    /// <summary>Stops every started app, the last started first. The set can start them again.</summary>
    public async Task StopAllAsync()
    {
        for (var i = _started.Count - 1; i >= 0; i--)
        {
            var app = _started[i];
            _started.RemoveAt(i);
            await app.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAllAsync().ConfigureAwait(false);

    /// <summary>The apps, for messages: <c>Daan's PC (Daan), Finn</c>.</summary>
    public string Describe()
        => string.Join(", ", Definitions.Select(app => string.Equals(app.User, app.Name, StringComparison.Ordinal) ? app.Name : $"{app.Name} ({app.User})"));

    // A capture is evidence for a failure already being reported: its own failure goes into the
    // result, not over the test's.
    private static TestAppCapture TryCapture(ITestApp app, string testClass, string testName, string description)
    {
        try
        {
            var path = app.Screenshots.Capture(testClass, testName, description);
            return new TestAppCapture(app.Definition.Name, string.IsNullOrEmpty(path) ? null : path, string.IsNullOrEmpty(path) ? "nothing was written" : null);
        }
        catch (Exception exception)
        {
            return new TestAppCapture(app.Definition.Name, null, exception.Message);
        }
    }
}

using Brinell.Mocking;
using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.TestSupport;

namespace Brinell.Samples.Todo.UITests;

/// <summary>
/// The Todo app, hermetic: its backend is a WireMock fake of the API that the test controls.
/// </summary>
/// <remarks>
/// The fake is the stateful <see cref="TodoServerStub"/>, which the integration tier's contract
/// tests hold to the real API's behaviour. Between tests it is reset to the scenario's server
/// state, with any per-test override (a 500, a delay) removed.
/// </remarks>
public abstract class TodoAppFixture : TodoAppFixtureBase
{
    private MockApiServer? _backend;
    private TodoServerStub? _server;

    /// <summary>The WireMock server the app talks to: for request assertions and one-test overrides.</summary>
    public MockApiServer Backend => _backend
        ?? throw new InvalidOperationException("The backend was used before it was started.");

    /// <summary>The fake API's state: what the app has sent it, and changes "from another device".</summary>
    public TodoServerStub Server => _server
        ?? throw new InvalidOperationException("The backend was used before it was started.");

    /// <summary>
    /// Adds a todo to the fake server, as another device would, with a unique title.
    /// </summary>
    /// <remarks>
    /// Arranging through the backend rather than the database: the app picks it up with its own
    /// sync, which is production code, and it works the same on every platform.
    /// </remarks>
    /// <param name="title">The title's start; a short unique suffix is added.</param>
    /// <param name="status">Its status.</param>
    /// <param name="dueDate">Its due date, or none.</param>
    /// <returns>The full, unique title.</returns>
    public string ServerHasTodo(string title, TodoStatusDto status = TodoStatusDto.Open, DateOnly? dueDate = null)
    {
        var unique = $"{title} {Guid.NewGuid().ToString("N")[..6]}";
        var yesterday = Scenario.Now.AddDays(-1);
        Server.Store(new TodoDto(Guid.NewGuid(), unique, null, dueDate, status, yesterday, yesterday));
        return unique;
    }

    /// <summary>Removes every per-test backend override and restores the scenario's server state.</summary>
    public void ResetBackend()
    {
        Backend.Reset();
        _server = Scenario.StubServer(Backend);
    }

    /// <inheritdoc />
    protected override string StartBackend(TodoScenario scenario, string runFolder)
    {
        _backend = MockApiServer.Start();
        _server = scenario.StubServer(_backend);
        return _backend.BaseUrl;
    }

    /// <inheritdoc />
    protected override void StopBackend() => _backend?.Dispose();

    /// <inheritdoc />
    protected override void ResetBackendForTest() => ResetBackend();
}

using Brinell.Uat;

namespace Brinell.Samples.Todo.TestSupport.Uat;

/// <summary>
/// What a hermetic Todo fixture arranges, whatever head it drives: the test set, the fake server
/// and the app's database. The MAUI and web UAT fixtures both are one, so steps that reach past the
/// screen (<see cref="TodoDataPhrases"/>) are written once.
/// </summary>
public interface ITodoArrangement : IUatClock
{
    /// <summary>The test set the app runs on.</summary>
    TodoTestSet TestSet { get; }

    /// <summary>Relative dates in steps (<c>+2d</c>) resolve against the test set's now.</summary>
    DateTimeOffset IUatClock.Now => TestSet.Now;

    /// <summary>The fake server's todos.</summary>
    TodoServerStub Server { get; }

    /// <summary>The app's SQLite file.</summary>
    string DatabasePath { get; }
}

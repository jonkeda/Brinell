namespace Brinell.Samples.Todo.TestSupport.Uat;

/// <summary>
/// What a hermetic Todo fixture arranges, whatever head it drives: the test set, the fake server
/// and the app's database. The MAUI and web UAT fixtures both are one, so steps that reach past the
/// screen (<see cref="TodoDataPhrases"/>) are written once.
/// </summary>
public interface ITodoArrangement
{
    /// <summary>The test set the app runs on.</summary>
    TodoTestSet TestSet { get; }

    /// <summary>The fake server's todos.</summary>
    TodoServerStub Server { get; }

    /// <summary>The app's SQLite file.</summary>
    string DatabasePath { get; }
}

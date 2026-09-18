using Brinell.Samples.Todo.Infrastructure.Data;

namespace Brinell.Samples.Todo.TestSupport;

/// <summary>
/// A database file of its own for one test or fixture, deleted afterwards.
/// </summary>
/// <remarks>
/// Tests never share a database file: a file per owner is what lets them run in any order.
/// </remarks>
public sealed class TempDatabase : IDisposable
{
    /// <summary>Reserves a unique path; the file is created by whoever opens it first.</summary>
    public TempDatabase(string purpose = "test")
    {
        Folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Brinell", "TodoTests", $"{purpose}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Folder);
        Path = System.IO.Path.Combine(Folder, "todo.db");
    }

    /// <summary>The folder holding the file, for anything else the test needs next to it.</summary>
    public string Folder { get; }

    /// <summary>The database file.</summary>
    public string Path { get; }

    /// <summary>Releases pooled connections and deletes the folder.</summary>
    public void Dispose()
    {
        TodoDatabase.ReleaseFileHandles();

        if (Directory.Exists(Folder))
        {
            Directory.Delete(Folder, recursive: true);
        }
    }
}

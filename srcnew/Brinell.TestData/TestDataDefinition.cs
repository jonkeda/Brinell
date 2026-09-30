using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Brinell.TestData;

/// <summary>
/// What an app's test data is: where its test-set files are, which type a file is read into, and
/// the rules the data must follow.
/// </summary>
/// <remarks>
/// Implement it by deriving from <see cref="TestDataDefinition{TSet}"/>. The <c>brinell-data</c>
/// tool finds every public implementation in the assemblies it loads.
/// </remarks>
public interface ITestDataDefinition
{
    /// <summary>A short name for reports, e.g. <c>Todo</c>.</summary>
    string Name { get; }

    /// <summary>The type one test-set file is read into.</summary>
    Type SetType { get; }

    /// <summary>The folder holding the test-set files, as an absolute path.</summary>
    string Folder { get; }

    /// <summary>The folder holding the definition's project file, when the sources are on this machine.</summary>
    string? ProjectDirectory { get; }

    /// <summary>The serializer settings files are read with. They should be strict.</summary>
    JsonSerializerOptions JsonOptions { get; }

    /// <summary>The record types a person or model writes rows of, for <c>describe</c> and step tables.</summary>
    IReadOnlyList<Type> TableTypes { get; }

    /// <summary>The test sets in <see cref="Folder"/>, by name (file name without <c>.json</c>).</summary>
    IReadOnlyList<string> TestSetNames { get; }

    /// <summary>The file of the test set called <paramref name="name"/>.</summary>
    string FileOf(string name);

    /// <summary>Reads <paramref name="path"/> and runs the rules on it.</summary>
    IReadOnlyList<TestDataDiagnostic> ValidateFile(string path);
}

/// <summary>
/// The base of an app's test-data definition.
/// </summary>
/// <typeparam name="TSet">The type one test-set file is read into.</typeparam>
public abstract class TestDataDefinition<TSet> : ITestDataDefinition
    where TSet : class
{
    private const string FileExtension = ".json";
    private const string SchemaSuffix = ".schema.json";

    private readonly string _sourceDirectory;

    /// <summary>Creates the definition.</summary>
    /// <param name="sourceFile">Filled in by the compiler with the derived class's file. Do not pass it.</param>
    protected TestDataDefinition([CallerFilePath] string sourceFile = "")
    {
        _sourceDirectory = Path.GetDirectoryName(sourceFile) ?? string.Empty;
    }

    /// <inheritdoc />
    public virtual string Name => GetType().Name.Replace("TestDataDefinition", string.Empty, StringComparison.Ordinal);

    /// <inheritdoc />
    public Type SetType => typeof(TSet);

    /// <inheritdoc />
    /// <remarks>
    /// The source folder, where the files can be edited: next to the definition's source file, or
    /// in the project folder of its assembly. Only when neither exists (a machine without the
    /// sources) is it the copy next to the assembly.
    /// </remarks>
    public string Folder
    {
        get
        {
            if (_sourceDirectory.Length > 0 && Path.Combine(_sourceDirectory, RelativeFolder) is var nextToSource && Directory.Exists(nextToSource))
            {
                return Path.GetFullPath(nextToSource);
            }

            var assemblyDirectory = AssemblyDirectory;
            if (ProjectDirectoryOf(GetType().Assembly.GetName().Name!, assemblyDirectory) is { } project &&
                Path.Combine(project, RelativeFolder) is var inProject && Directory.Exists(inProject))
            {
                return Path.GetFullPath(inProject);
            }

            return Path.GetFullPath(Path.Combine(assemblyDirectory, RelativeFolder));
        }
    }

    /// <inheritdoc />
    public string? ProjectDirectory
        => ProjectDirectoryOf(GetType().Assembly.GetName().Name!, AssemblyDirectory);

    // The folder this definition's assembly was built into.
    private string AssemblyDirectory
        => Path.GetDirectoryName(GetType().Assembly.Location) is { Length: > 0 } folder ? folder : AppContext.BaseDirectory;

    /// <inheritdoc />
    public virtual JsonSerializerOptions JsonOptions => TestDataJson.StrictOptions;

    /// <inheritdoc />
    public virtual IReadOnlyList<Type> TableTypes => [];

    /// <inheritdoc />
    public IReadOnlyList<string> TestSetNames => Directory.Exists(Folder)
        ? Directory.EnumerateFiles(Folder, "*" + FileExtension)
            .Where(path => !path.EndsWith(SchemaSuffix, StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetFileName(path)[..^FileExtension.Length])
            .Order(StringComparer.Ordinal)
            .ToList()
        : [];

    /// <summary>The test-set folder, relative to the derived class's source file, e.g. <c>TestData</c>.</summary>
    protected abstract string RelativeFolder { get; }

    /// <inheritdoc />
    public string FileOf(string name) => Path.Combine(Folder, name + FileExtension);

    /// <inheritdoc />
    public IReadOnlyList<TestDataDiagnostic> ValidateFile(string path)
    {
        var rules = new TestDataRules<TSet>();
        DefineRules(rules);
        return TestDataValidator.ValidateFile(path, JsonOptions, rules, TestDataTypes.Reachable(JsonOptions, SetType, TableTypes));
    }

    /// <summary>Declares the rules a loaded test set must follow.</summary>
    protected abstract void DefineRules(TestDataRules<TSet> rules);

    // [CallerFilePath] is empty when the derived class calls base() implicitly, so the project is
    // also found by walking up from the assembly: <name>.csproj itself, or <name>/<name>.csproj
    // beside a folder on the way up (a test project's bin holds copies of the libraries it uses).
    private static string? ProjectDirectoryOf(string assemblyName, string start)
    {
        var projectFile = assemblyName + ".csproj";
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, projectFile)))
            {
                return directory.FullName;
            }

            var sibling = Path.Combine(directory.FullName, assemblyName);
            if (File.Exists(Path.Combine(sibling, projectFile)))
            {
                return sibling;
            }
        }

        return null;
    }
}

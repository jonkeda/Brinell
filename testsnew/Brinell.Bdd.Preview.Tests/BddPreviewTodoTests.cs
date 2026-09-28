using System.Net;
using Brinell.Bdd.Workspace;
using Xunit;

namespace Brinell.Bdd.Preview.Tests;

/// <summary>
/// The Todo MAUI suite's files, bound against its catalog as the language server loads it. Needs
/// the Todo sample built (<c>dotnet build samples/Todo/Brinell.Samples.Todo.slnx</c>).
/// </summary>
public sealed class BddPreviewTodoTests : IClassFixture<BddPreviewTodoTests.TodoSuite>
{
    private readonly TodoSuite _todo;

    public BddPreviewTodoTests(TodoSuite todo) => _todo = todo;

    /// <summary>The MAUI suite's own feature and step files, relative to its project.</summary>
    public static IEnumerable<object[]> Files => Directory
        .EnumerateFiles(TodoSuite.Project, "*.md", SearchOption.AllDirectories)
        .Where(file => file.EndsWith(BddFileFormat.FeatureExtension, StringComparison.OrdinalIgnoreCase) ||
                       file.EndsWith(BddFileFormat.StepsExtension, StringComparison.OrdinalIgnoreCase))
        .Select(file => Path.GetRelativePath(TodoSuite.Project, file))
        .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
        .Order(StringComparer.Ordinal)
        .Select(file => new object[] { file.Replace('\\', '/') });

    [Theory]
    [MemberData(nameof(Files))]
    public void TodoFile_Bound_RendersAsItsSnapshot(string file)
    {
        var path = Path.Combine(TodoSuite.Project, file);

        var result = BddPreview.Render(File.ReadAllText(path), path, new BddPreviewOptions { Catalogs = _todo.Catalogs });

        Snapshot.Match(result.Html, "bound." + file.Replace('/', '.'));
    }

    [Fact]
    public void TheWithoutUsingFile_ShowsUatb010_WordForWordAsTheGateReportsIt()
    {
        var path = Path.Combine(TodoSuite.Project, "ExpectedFailures", "TOD.11-without-using.feature.md");
        var gate = UatScenarioCheck.CheckFile(path, _todo.Catalogs).Where(d => d.Code == "UATB010").ToList();

        var html = WebUtility.HtmlDecode(BddPreview.Render(File.ReadAllText(path), path, new BddPreviewOptions { Catalogs = _todo.Catalogs }).Html);

        Assert.NotEmpty(gate);
        Assert.All(gate, diagnostic => Assert.Contains($"data-line=\"{diagnostic.Location.LineNumber}\"><span class=\"uat-diag-code\">UATB010</span> {diagnostic.Message}", html));
    }

    /// <summary>The MAUI suite, loaded once for the class.</summary>
    public sealed class TodoSuite : IDisposable
    {
        public static readonly string Project = Path.Combine(FindTodo(), "tests", "Brinell.Samples.Todo.Uat");

        private readonly BddSuite _suite = BddSuite.Open(Path.Combine(Project, BddFileFormat.UiConfigFileName));

        public BddCatalogSet Catalogs => _suite.TryGetCatalogs(out var reason)
            ?? throw new InvalidOperationException($"The Todo MAUI suite cannot be bound: {reason} Build samples/Todo first.");

        public void Dispose() => _suite.Dispose();

        private static string FindTodo()
        {
            for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
            {
                var todo = Path.Combine(folder.FullName, "samples", "Todo");
                if (Directory.Exists(todo))
                {
                    return todo;
                }
            }

            throw new DirectoryNotFoundException("samples/Todo is not above the test's folder.");
        }
    }
}

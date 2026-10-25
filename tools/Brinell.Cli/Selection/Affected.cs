using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Brinell.Cli.Selection;

/// <summary>
/// The tests of one suite a change reaches (.my/test-selection/next-plan.md, item 3): each changed file is followed to the
/// pages and page objects it is, and through the <see cref="TouchMap"/> to the tests that used them. Code the whole suite
/// stands on (the libraries it references, its test base, the app's layout and start) runs the suite whole; files outside
/// the suite, its app and what they reference run nothing.
/// </summary>
/// <param name="Whole">Why the whole suite runs; null when a filter does.</param>
/// <param name="Picked">The tests picked, each with why; empty when nothing is reached.</param>
/// <param name="Notes">Changed files that reach no test, and why.</param>
internal sealed record Affected(string? Whole, IReadOnlyList<PickedTest> Picked, IReadOnlyList<string> Notes)
{
    /// <summary>The one <c>dotnet test --filter</c> for the picked tests; null when the suite runs whole or nothing does.</summary>
    public string? Filter => Whole is null && Picked.Count > 0 ? string.Join('|', Picked.Select(test => test.Filter).Distinct()) : null;

    private static readonly string[] RootFiles = ["Directory.Packages.props", "Directory.Build.props", "Directory.Build.targets", "global.json", "NuGet.config"];

    /// <summary>What the changed files reach in the suite.</summary>
    /// <param name="suiteFolder">The suite's project folder.</param>
    /// <param name="appFolders">The apps the suite tests; null: the web projects the suite references.</param>
    /// <param name="changed">The changed files, as full paths.</param>
    /// <param name="map">The suite's touch map.</param>
    /// <param name="always">Filter terms run whatever changed (a smoke set).</param>
    public static Affected Of(string suiteFolder, IReadOnlyList<string>? appFolders, IEnumerable<string> changed, TouchMap map, IEnumerable<string>? always = null)
    {
        suiteFolder = Folder(suiteFolder);
        var references = References(suiteFolder);
        var apps = appFolders?.Select(Folder).ToList() ?? references.Where(IsWebProject).ToList();
        var scenarios = SuiteScenarios.Of(suiteFolder);
        var picked = new List<PickedTest>();
        var notes = new List<string>();

        foreach (var file in changed.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Under(file, suiteFolder))
            {
                if (InSuite(file, suiteFolder, map, scenarios, picked, notes) is { } whole)
                    return new Affected(whole, [], notes);
            }
            else if (apps.FirstOrDefault(app => Under(file, app)) is { } app)
            {
                if (InApp(file, app, map, picked, notes) is { } whole)
                    return new Affected(whole, [], notes);
            }
            else if (references.FirstOrDefault(reference => Under(file, reference)) is { } reference)
            {
                return new Affected($"{Name(file, reference)} is in {Path.GetFileName(reference)}, which the suite builds on", [], notes);
            }
            else if (RootFiles.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase))
            {
                return new Affected($"{Path.GetFileName(file)} sets every project's build", [], notes);
            }
        }

        var ordered = picked.DistinctBy(test => test.Filter).OrderBy(test => test.Name, StringComparer.Ordinal).ToList();
        if (ordered.Count > 0)
            ordered.AddRange((always ?? []).Select(term => new PickedTest(term, term, "always run")));
        return new Affected(null, ordered, notes);
    }

    // A file of the suite itself: a feature file, a page object, a test class; anything else is the suite's base.
    private static string? InSuite(string file, string suiteFolder, TouchMap map, SuiteScenarios scenarios, List<PickedTest> picked, List<string> notes)
    {
        var name = Name(file, suiteFolder);
        if (file.EndsWith(".feature.md", StringComparison.OrdinalIgnoreCase))
        {
            var inFile = scenarios.InFile(name).ToList();
            picked.AddRange(inFile.Select(scenario => new PickedTest(scenario.Name, $"{Brinell.Bdd.BddScenarioAddress.Trait}={scenario.Address}", $"{name} changed")));
            if (inFile.Count == 0)
                notes.Add($"{name}: no scenario of the suite's");
            return null;
        }

        if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || !File.Exists(file))
            return $"{name} is part of the suite's set-up";

        var source = File.ReadAllText(file);
        var types = Regex.Matches(source, @"\b(?:class|record|struct)\s+(\w+)").Select(match => match.Groups[1].Value).ToList();
        var pages = types.Where(type => map.Tests.Any(test => test.Pages.Any(page => PageIs(page, type)))).ToList();
        if (pages.Count > 0)
        {
            foreach (var test in map.Tests)
            {
                if (pages.FirstOrDefault(page => test.Pages.Any(used => PageIs(used, page))) is { } page)
                    picked.Add(new PickedTest(test.Name, test.Filter, $"uses {page} ({name})"));
            }

            return null;
        }

        if (Regex.IsMatch(source, @"\[(?:Fact|Theory|UiFact|BrowserFact|UatScenarioTheory)\b"))
        {
            var space = Regex.Match(source, @"\bnamespace\s+([\w.]+)").Groups[1].Value;
            foreach (var type in types)
                picked.Add(new PickedTest($"{space}.{type}", $"FullyQualifiedName~{space}.{type}.", $"its tests changed ({name})"));
            return null;
        }

        if (Regex.IsMatch(name, @"(^|/)(PageObjects|Pages)/"))
        {
            notes.Add($"{name}: no test of the map uses {string.Join(", ", types)}");
            return null;
        }

        return $"{name} is the suite's shared test code";
    }

    // A file of the app: a page reaches the tests that used its test ids; the rest of the app (layout, start, styles) all.
    private static string? InApp(string file, string app, TouchMap map, List<PickedTest> picked, List<string> notes)
    {
        var name = Name(file, app);
        var page = Regex.Replace(file, @"\.razor\.(cs|css)$", ".razor", RegexOptions.IgnoreCase);
        if (!page.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) || !Regex.IsMatch(Name(page, app), @"(^|/)Pages/"))
            return $"{name} is shared by the app's pages";
        if (!File.Exists(page))
        {
            notes.Add($"{name}: the page is gone; the tests that used it fail to find it");
            return $"{name} was removed";
        }

        var ids = TestIds(File.ReadAllText(page)).ToList();
        var reached = 0;
        foreach (var test in map.Tests)
        {
            if (ids.FirstOrDefault(id => test.Controls.Any(control => Uses(control, id))) is { } id)
            {
                picked.Add(new PickedTest(test.Name, test.Filter, $"uses {id.Text} of {name}"));
                reached++;
            }
        }

        if (reached == 0)
            notes.Add(ids.Count == 0 ? $"{name}: no test ids" : $"{name}: no test of the map uses its test ids");
        return null;
    }

    /// <summary>A page's test ids and ids, as written: a value with <c>@</c> in it counts by what comes before.</summary>
    public static IEnumerable<TestId> TestIds(string razor)
        => Regex.Matches(razor, @"\b(data-testid|id)\s*=\s*""([^""]*)""")
            .Select(match => (Attribute: match.Groups[1].Value, Value: match.Groups[2].Value))
            .Select(found => found.Value.IndexOf('@') is var at and >= 0
                ? new TestId(found.Attribute, found.Value[..at], Prefix: true)
                : new TestId(found.Attribute, found.Value, Prefix: false))
            .Where(id => id.Text.Length > 0)
            .Distinct();

    // A control's locator names the id: [data-testid='x'], [data-testid^='x-'], #x, or the id alone.
    private static bool Uses(string control, TestId id)
    {
        if (control == id.Text)
            return true;
        var quoted = id.Prefix ? new[] { $"'{id.Text}", $"\"{id.Text}" } : [$"'{id.Text}'", $"\"{id.Text}\""];
        if (id.Attribute == "data-testid")
            return control.Contains("testid", StringComparison.OrdinalIgnoreCase) && quoted.Any(quote => control.Contains(quote, StringComparison.Ordinal));
        return Regex.IsMatch(control, $@"#{Regex.Escape(id.Text)}{(id.Prefix ? string.Empty : @"(?![\w-])")}") || (control.Contains("id=", StringComparison.Ordinal) && quoted.Any(quote => control.Contains(quote, StringComparison.Ordinal)));
    }

    // The Page column names a page object, or a scope inside one: "TodoRows('Css:...')".
    private static bool PageIs(string page, string type) => page == type || page.StartsWith(type + "(", StringComparison.Ordinal);

    /// <summary>The project folders the suite's project references, followed through their references.</summary>
    public static IReadOnlyList<string> References(string suiteFolder)
    {
        var found = new List<string>();
        var queue = new Queue<string>(Directory.EnumerateFiles(suiteFolder, "*.csproj"));
        var seen = new HashSet<string>(queue, StringComparer.OrdinalIgnoreCase);
        while (queue.Count > 0)
        {
            var project = queue.Dequeue();
            foreach (var include in XDocument.Load(project).Descendants("ProjectReference").Select(element => element.Attribute("Include")?.Value).OfType<string>())
            {
                var path = Path.GetFullPath(include.Replace('\\', Path.DirectorySeparatorChar), Path.GetDirectoryName(project)!);
                if (File.Exists(path) && seen.Add(path))
                {
                    queue.Enqueue(path);
                    found.Add(Folder(Path.GetDirectoryName(path)!));
                }
            }
        }

        return found;
    }

    private static bool IsWebProject(string folder)
        => Directory.EnumerateFiles(folder, "*.csproj").Any(project => XDocument.Load(project).Root?.Attribute("Sdk")?.Value == "Microsoft.NET.Sdk.Web");

    private static string Folder(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static bool Under(string file, string folder) => file.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static string Name(string file, string folder) => Path.GetRelativePath(folder, file).Replace('\\', '/');
}

/// <summary>A test the change reaches.</summary>
/// <param name="Name">The test's full name, or the scenario's.</param>
/// <param name="Filter">The filter term that runs it.</param>
/// <param name="Why">The changed file and what of it the test used.</param>
internal sealed record PickedTest(string Name, string Filter, string Why);

/// <summary>An id on a page: a <c>data-testid</c> or an <c>id</c>, or the start of one built at run time.</summary>
internal sealed record TestId(string Attribute, string Text, bool Prefix);

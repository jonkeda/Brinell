namespace Bruinet.Narrator;

/// <summary>A scene as it will run: its scenario, found, and what is said at which of its steps.</summary>
/// <param name="Scene">The scene, as written.</param>
/// <param name="FeatureFile">The scenario's feature file, full path.</param>
/// <param name="Scenario">The scenario, as parsed.</param>
/// <param name="Steps">The steps it runs, as written: the Background's, then its own.</param>
/// <param name="Says">What is said before a step, by the step's line in the feature file.</param>
public sealed record BddVideoScenePlan(BddVideoScene Scene, string FeatureFile, BddScenario Scenario, IReadOnlyList<BddStep> Steps, IReadOnlyDictionary<int, BddVideoLine> Says)
{
    /// <summary>The scenario's address.</summary>
    public string Address => Scene.Scenario!;
}

/// <summary>
/// A video script checked like a feature file (AD-022's one checker): its form, and each scene
/// against the feature files. The editor, the preview and the gate show these diagnostics.
/// </summary>
/// <param name="Script">The script; null when it could not be read.</param>
/// <param name="Diagnostics">What is wrong, at its lines in the script.</param>
/// <param name="Scenes">The scenes whose scenario was found, in order.</param>
/// <param name="ConfigDirectory">The suite's folder, which addresses are relative to; null when none was found.</param>
public sealed record BddVideoScriptCheck(
    BddVideoScript? Script,
    IReadOnlyList<BddDiagnostic> Diagnostics,
    IReadOnlyList<BddVideoScenePlan> Scenes,
    string? ConfigDirectory)
{
    /// <summary>No suite config above the script, so no address can be found.</summary>
    public const string NoConfigCode = "VID010";

    /// <summary>A scene's address names no feature file.</summary>
    public const string NoFileCode = "VID011";

    /// <summary>A scene's address names no scenario of its file.</summary>
    public const string NoScenarioCode = "VID012";

    /// <summary>A Say row names no step of the scene's scenario.</summary>
    public const string NoStepCode = "VID013";

    /// <summary>A Say row is out of step order.</summary>
    public const string OrderCode = "VID014";

    /// <summary>Whether nothing is wrong enough to stop a recording.</summary>
    public bool Success => Script is not null && Diagnostics.All(diagnostic => diagnostic.Severity != BddDiagnosticSeverity.Error);

    /// <summary>Reads and checks a video script.</summary>
    /// <param name="text">The script's text, which may be unsaved.</param>
    /// <param name="filePath">The script's path.</param>
    /// <param name="configDirectory">The suite's folder; null to look for it above the script.</param>
    public static BddVideoScriptCheck Check(string text, string filePath, string? configDirectory = null)
    {
        var parse = BddVideoScriptParser.Parse(text, filePath);
        var diagnostics = new List<BddDiagnostic>(parse.Diagnostics);
        var scenes = new List<BddVideoScenePlan>();
        configDirectory ??= FindConfigDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
        if (parse.Script is not { } script)
        {
            return new BddVideoScriptCheck(null, diagnostics, scenes, configDirectory);
        }

        void Report(BddDiagnosticSeverity severity, string code, string message, int line)
            => diagnostics.Add(new BddDiagnostic(severity, code, message, new BddSourceLocation(filePath, line)));

        if (configDirectory is null && script.Scenes.Any(scene => scene.Scenario is not null))
        {
            Report(BddDiagnosticSeverity.Error, NoConfigCode,
                $"No {BddFileFormat.UiConfigFileName} or {BddFileFormat.ServiceConfigFileName} above the script, so its scenario addresses cannot be found.", 1);
            return new BddVideoScriptCheck(script, diagnostics, scenes, null);
        }

        var files = new Dictionary<string, BddParseResult?>(StringComparer.OrdinalIgnoreCase);
        foreach (var scene in script.Scenes.Where(scene => scene.Scenario is not null))
        {
            if (Find(scene, configDirectory!, files, Report) is { } found)
            {
                // The binder runs the Background's steps first, so a scene can narrate them too.
                var steps = found.Background.Concat(found.Scenario.Steps).ToList();
                scenes.Add(new BddVideoScenePlan(scene, found.File, found.Scenario, steps, Says(scene, found.Scenario.Name, steps, Report)));
            }
        }

        diagnostics.Sort((a, b) => a.Location.LineNumber.CompareTo(b.Location.LineNumber));
        return new BddVideoScriptCheck(script, diagnostics, scenes, configDirectory);
    }

    /// <summary>The folder with the suite's config at or above <paramref name="directory"/>; null when none.</summary>
    /// <param name="directory">Where to start looking.</param>
    public static string? FindConfigDirectory(string? directory)
    {
        for (var folder = directory is null ? null : new DirectoryInfo(directory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, BddFileFormat.UiConfigFileName)) ||
                File.Exists(Path.Combine(folder.FullName, BddFileFormat.ServiceConfigFileName)))
            {
                return folder.FullName;
            }
        }

        return null;
    }

    /// <summary>A step as the scenario writes it: its keyword and text.</summary>
    /// <param name="step">A scenario's step.</param>
    public static string Written(BddStep step) => $"{step.Keyword} {step.Text}";

    private static (string File, BddScenario Scenario, IReadOnlyList<BddStep> Background)? Find(
        BddVideoScene scene,
        string configDirectory,
        Dictionary<string, BddParseResult?> files,
        Action<BddDiagnosticSeverity, string, string, int> report)
    {
        var address = scene.Scenario!;
        var hash = address.IndexOf('#', StringComparison.Ordinal);
        var relative = hash < 0 ? address : address[..hash];
        var path = Path.GetFullPath(Path.Combine(configDirectory, relative));
        if (!files.TryGetValue(path, out var parse))
        {
            parse = File.Exists(path) ? BddMarkdownParser.ParseFile(path) : null;
            files[path] = parse;
        }

        if (parse?.Document is not { } document)
        {
            report(BddDiagnosticSeverity.Error, NoFileCode, $"No feature file '{relative}' in {configDirectory}.", scene.ScenarioLine);
            return null;
        }

        var scenario = document.Scenarios.FirstOrDefault(candidate => Matches(relative, candidate.Name, address))
            ?? document.Scenarios.FirstOrDefault(candidate => candidate.OutlineName is { } outline && Matches(relative, outline, address));
        if (scenario is null)
        {
            var names = document.Scenarios.Select(candidate => BddScenarioAddress.Slug(candidate.OutlineName ?? candidate.Name)).Distinct().Take(6);
            report(BddDiagnosticSeverity.Error, NoScenarioCode,
                $"'{relative}' has no scenario '{(hash < 0 ? string.Empty : address[(hash + 1)..])}'. Its scenarios: {string.Join(", ", names)}.", scene.ScenarioLine);
            return null;
        }

        return (path, scenario, document.Background);
    }

    private static bool Matches(string relative, string name, string address)
        => string.Equals(BddScenarioAddress.Of(relative, name), address.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);

    // Rows go in step order: each matches the first step with its text after the previous row's.
    private static Dictionary<int, BddVideoLine> Says(BddVideoScene scene, string scenario, List<BddStep> steps, Action<BddDiagnosticSeverity, string, string, int> report)
    {
        var says = new Dictionary<int, BddVideoLine>();
        var after = -1;
        foreach (var line in scene.Lines)
        {
            var wanted = Normalize(line.Step);
            var index = IndexOf(after + 1);
            if (index < 0 && IndexOf(0) is var earlier and >= 0)
            {
                report(BddDiagnosticSeverity.Warning, OrderCode, $"'{line.Step}' comes before the previous row's step: rows go in step order.", line.Line);
                index = earlier;
            }

            if (index < 0)
            {
                report(BddDiagnosticSeverity.Error, NoStepCode,
                    $"'{line.Step}' is not a step of {scenario}. Write the step as the scenario does, keyword and text.", line.Line);
                continue;
            }

            says[steps[index].Source.LineNumber] = line;
            after = Math.Max(after, index);

            int IndexOf(int from)
            {
                for (var candidate = from; candidate < steps.Count; candidate++)
                {
                    if (string.Equals(Normalize(Written(steps[candidate])), wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        return candidate;
                    }
                }

                return -1;
            }
        }

        return says;
    }

    private static string Normalize(string text) => string.Join(' ', text.Split((char[])[' ', '\t'], StringSplitOptions.RemoveEmptyEntries));
}

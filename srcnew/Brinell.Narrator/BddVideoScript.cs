namespace Brinell.Narrator;

/// <summary>
/// A video script (<c>*.video.md</c>): scenes that each run one scenario, with what is said before
/// it, at its steps and after it.
/// </summary>
/// <param name="FilePath">The script's file.</param>
/// <param name="Title">The video's title, from <c># Video: …</c>.</param>
/// <param name="Voice">The voice to speak with; null for the settings' voice.</param>
/// <param name="Beat">How long to wait after a step, for its result to be seen; null for the settings' beat.</param>
/// <param name="Scenes">The scenes, in the order they play.</param>
public sealed record BddVideoScript(string FilePath, string Title, string? Voice, TimeSpan? Beat, IReadOnlyList<BddVideoScene> Scenes)
{
    /// <summary>The extension of a video script.</summary>
    public const string Extension = ".video.md";

    /// <summary>Whether a path is a video script.</summary>
    /// <param name="path">A file's path.</param>
    public static bool IsVideoScript(string path) => path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);
}

/// <summary>One scene: a scenario, run and recorded, with its narration.</summary>
/// <param name="Name">The scene's name, from <c>## Scene: …</c>; a chapter of the video.</param>
/// <param name="Scenario">The scenario's address (<c>Features/X.feature.md#slug</c>); null when the scene has none.</param>
/// <param name="Intro">Said before the first step; null for nothing.</param>
/// <param name="Outro">Said after the last step; null for nothing.</param>
/// <param name="Lines">What is said at a step, in step order.</param>
/// <param name="Line">The scene heading's line.</param>
/// <param name="ScenarioLine">The <c>Scenario:</c> line; the heading's when there is none.</param>
public sealed record BddVideoScene(
    string Name,
    string? Scenario,
    string? Intro,
    string? Outro,
    IReadOnlyList<BddVideoLine> Lines,
    int Line,
    int ScenarioLine);

/// <summary>What is said at one step.</summary>
/// <param name="Step">The step as written in the scenario: keyword and text, a call by its call line.</param>
/// <param name="Say">What is said before the step runs.</param>
/// <param name="Line">The row's line in the script.</param>
public sealed record BddVideoLine(string Step, string Say, int Line);

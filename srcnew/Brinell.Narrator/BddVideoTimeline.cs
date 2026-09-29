using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brinell.Narrator;

/// <summary>
/// What happened when, on the video's clock (seconds from its first frame): each step's start, end
/// and result, and each spoken line. The captions, the chapters and the review page are made from it.
/// </summary>
/// <param name="Title">The video's title.</param>
/// <param name="Scenes">The scenes, in the order they play.</param>
public sealed record BddVideoTimeline(string Title, IReadOnlyList<BddSceneTimeline> Scenes)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Writes the timeline as JSON.</summary>
    /// <param name="path">The file.</param>
    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Json));

    /// <summary>Reads a timeline written by <see cref="Save"/>.</summary>
    /// <param name="path">The file.</param>
    public static BddVideoTimeline Load(string path)
        => JsonSerializer.Deserialize<BddVideoTimeline>(File.ReadAllText(path), Json) ?? throw new InvalidDataException($"{path} is not a timeline.");
}

/// <summary>One scene: its video, and its steps and lines on that video's clock.</summary>
/// <param name="Number">The scene's number in the script, from 1.</param>
/// <param name="Name">The scene's name; its chapter.</param>
/// <param name="Scenario">The scenario's address.</param>
/// <param name="Video">The scene's video file; null when it could not be recorded.</param>
/// <param name="Duration">How long the scene lasts, in seconds.</param>
/// <param name="Status">How the scenario ended.</param>
/// <param name="Steps">The steps as written, in the order they ran.</param>
/// <param name="Clips">The spoken lines.</param>
/// <param name="Warnings">What went less well than asked.</param>
/// <param name="Width">The video's width in pixels; 0 when there is no video.</param>
/// <param name="Height">The video's height in pixels; 0 when there is no video.</param>
public sealed record BddSceneTimeline(
    int Number,
    string Name,
    string? Scenario,
    string? Video,
    double Duration,
    BddStepResultStatus Status,
    IReadOnlyList<BddTimedStep> Steps,
    IReadOnlyList<BddTimedClip> Clips,
    IReadOnlyList<string> Warnings,
    int Width = 0,
    int Height = 0)
{
    /// <summary>The file a scene's timeline is kept in beside its video.</summary>
    public const string FileName = "timeline.json";

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Writes the scene's timeline as JSON.</summary>
    /// <param name="path">The file.</param>
    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Json));

    /// <summary>Reads a scene timeline written by <see cref="Save"/>.</summary>
    /// <param name="path">The file.</param>
    public static BddSceneTimeline Load(string path)
        => JsonSerializer.Deserialize<BddSceneTimeline>(File.ReadAllText(path), Json) ?? throw new InvalidDataException($"{path} is not a scene timeline.");
}

/// <summary>A step as written, on the video's clock.</summary>
/// <param name="Line">Its line in the feature file.</param>
/// <param name="Text">Its keyword and text.</param>
/// <param name="Start">When it started, in seconds.</param>
/// <param name="End">When it ended, in seconds.</param>
/// <param name="Status">Its result; a call's is its worst step's.</param>
/// <param name="Message">A failed step's message.</param>
public sealed record BddTimedStep(int Line, string Text, double Start, double End, BddStepResultStatus Status, string? Message = null);

/// <summary>A spoken line, on the video's clock.</summary>
/// <param name="Text">What is said.</param>
/// <param name="Wav">The speech; null when there was no voice.</param>
/// <param name="Start">When it starts, in seconds.</param>
/// <param name="End">When it ends, in seconds.</param>
public sealed record BddTimedClip(string Text, string? Wav, double Start, double End);

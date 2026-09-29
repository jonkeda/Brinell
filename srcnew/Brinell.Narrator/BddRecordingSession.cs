using System.Globalization;
using Brinell.Core.Artifacts;
using Brinell.Recording;

namespace Brinell.Narrator;

/// <summary>
/// A test run that records (<c>BRINELL_RECORD</c>): the video script whose scenes it records, or
/// every scenario, and where the scenes go. One per test process; each scenario asks it for its scene.
/// </summary>
/// <remarks>
/// <c>BRINELL_RECORD</c>: a <c>*.video.md</c> (full, or relative to the suite's folder) records its
/// scenes; <c>1</c> records every scenario, silently. <c>BRINELL_RECORD_DIR</c>: the Recordings
/// folder, the run's results folder when not set. <c>BRINELL_RECORD_OUT</c>: the scenes folder, for a
/// host that composes the video after the run. <c>BRINELL_VOICE</c> and <c>BRINELL_BEAT</c> (ms):
/// the settings, when the script does not say.
/// </remarks>
public sealed class BddRecordingSession
{
    /// <summary>What to record.</summary>
    public const string Variable = "BRINELL_RECORD";

    /// <summary>The Recordings folder.</summary>
    public const string FolderVariable = "BRINELL_RECORD_DIR";

    /// <summary>The scenes folder of this run.</summary>
    public const string OutputVariable = "BRINELL_RECORD_OUT";

    /// <summary>The voice, when the script does not name one.</summary>
    public const string VoiceVariable = "BRINELL_VOICE";

    /// <summary>The beat in milliseconds, when the script does not say.</summary>
    public const string BeatVariable = "BRINELL_BEAT";

    /// <summary>The beat when nothing says: long enough to see a step's result.</summary>
    public static readonly TimeSpan DefaultBeat = TimeSpan.FromMilliseconds(600);

    private readonly string _configDirectory;
    private readonly ISpeechSynthesizer? _synthesizer;
    private int _unscripted;

    /// <summary>Creates a session.</summary>
    /// <param name="configDirectory">The suite's folder, which scenario addresses are relative to.</param>
    /// <param name="script">The video script's check; null to record every scenario.</param>
    /// <param name="scenesFolder">Where the scenes go.</param>
    /// <param name="synthesizer">The voice; null to estimate the lines' lengths only.</param>
    /// <param name="voice">The voice's name, when the script names none.</param>
    /// <param name="beat">The beat, when the script does not say.</param>
    public BddRecordingSession(string configDirectory, BddVideoScriptCheck? script, string scenesFolder, ISpeechSynthesizer? synthesizer, string? voice, TimeSpan beat)
    {
        _configDirectory = configDirectory ?? throw new ArgumentNullException(nameof(configDirectory));
        Script = script;
        ScenesFolder = scenesFolder ?? throw new ArgumentNullException(nameof(scenesFolder));
        _synthesizer = synthesizer;
        Voice = script?.Script?.Voice ?? voice;
        Beat = script?.Script?.Beat ?? beat;
    }

    /// <summary>The video script's check; null when every scenario is recorded.</summary>
    public BddVideoScriptCheck? Script { get; }

    /// <summary>Where the scenes go: a folder per scene.</summary>
    public string ScenesFolder { get; }

    /// <summary>The voice's name; null for the default voice.</summary>
    public string? Voice { get; }

    /// <summary>The wait after a step.</summary>
    public TimeSpan Beat { get; }

    /// <summary>
    /// The run's session from the environment; null when <c>BRINELL_RECORD</c> is not set. Throws
    /// when the script has errors, or ffmpeg is missing: a recording that cannot be made should
    /// not look like a test run that passed.
    /// </summary>
    /// <param name="configDirectory">The suite's folder.</param>
    /// <param name="read">Reads a process variable; <see cref="Environment.GetEnvironmentVariable(string)"/> by default.</param>
    public static BddRecordingSession? FromEnvironment(string configDirectory, Func<string, string?>? read = null)
    {
        read ??= Environment.GetEnvironmentVariable;
        var record = read(Variable)?.Trim();
        if (string.IsNullOrEmpty(record) || record is "0" || record.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (Ffmpeg.Find(read) is null)
        {
            throw new FfmpegNotFoundException();
        }

        BddVideoScriptCheck? script = null;
        var name = "scenarios";
        if (record is not ("1" or "true" or "TRUE" or "True"))
        {
            var path = Path.IsPathRooted(record) ? record : Path.GetFullPath(Path.Combine(configDirectory, record));
            script = BddVideoScriptCheck.Check(File.ReadAllText(path), path);
            if (!script.Success)
            {
                throw new InvalidOperationException(
                    $"The video script {path} has errors:{Environment.NewLine}{string.Join(Environment.NewLine, script.Diagnostics)}");
            }

            name = Path.GetFileName(path)[..^BddVideoScript.Extension.Length];
        }

        var folder = read(FolderVariable) is { Length: > 0 } named ? named : DefaultTestArtifactPathProvider.Create().RunDirectory;
        var scenes = read(OutputVariable) is { Length: > 0 } output
            ? output
            : Path.Combine(folder, $"{name}-{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.scenes");
        var beat = int.TryParse(read(BeatVariable), NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds)
            ? TimeSpan.FromMilliseconds(milliseconds)
            : DefaultBeat;
        return new BddRecordingSession(configDirectory, script, scenes, Speech.Discover(), read(VoiceVariable), beat);
    }

    /// <summary>
    /// The scene a scenario records, with its lines spoken into its folder; null when the script has
    /// no scene for it (it runs unrecorded).
    /// </summary>
    /// <param name="featureFile">The scenario's feature file, as the run reads it.</param>
    /// <param name="scenarioName">The scenario's name.</param>
    /// <param name="cancellationToken">Cancels the speech.</param>
    public async Task<BddSceneRequest?> SceneForAsync(string featureFile, string scenarioName, CancellationToken cancellationToken = default)
    {
        var address = BddScenarioAddress.Of(Path.GetRelativePath(_configDirectory, featureFile), scenarioName);
        if (Script is null)
        {
            var number = Interlocked.Increment(ref _unscripted);
            return new BddSceneRequest(number, scenarioName, address, SceneFolder(number), BddSceneClips.Silent, Beat);
        }

        var scenes = Script.Script!.Scenes;
        foreach (var plan in Script.Scenes)
        {
            var planned = BddScenarioAddress.Of(Path.GetRelativePath(Script.ConfigDirectory!, plan.FeatureFile), plan.Scenario.Name);
            if (string.Equals(planned, address, StringComparison.OrdinalIgnoreCase))
            {
                var number = IndexOf(scenes, plan.Scene) + 1;
                var folder = SceneFolder(number);
                var clips = await BddSceneClips.SpeakAsync(plan, _synthesizer, Voice, folder, cancellationToken).ConfigureAwait(false);
                return new BddSceneRequest(number, plan.Scene.Name, address, folder, clips, Beat);
            }
        }

        return null;
    }

    private string SceneFolder(int number) => Path.Combine(ScenesFolder, $"scene-{number:00}");

    private static int IndexOf(IReadOnlyList<BddVideoScene> scenes, BddVideoScene scene)
    {
        for (var index = 0; index < scenes.Count; index++)
        {
            if (ReferenceEquals(scenes[index], scene))
            {
                return index;
            }
        }

        return -1;
    }
}

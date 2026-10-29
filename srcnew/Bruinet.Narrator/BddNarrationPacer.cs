using System.Diagnostics;

namespace Bruinet.Narrator;

/// <summary>
/// Paces one scene: a written step's spoken line plays while the step runs (or, with
/// <see cref="BddSayMode.Before"/>, before it), the run only taking as long as the speech; the voice
/// is laid in afterwards. After the step and its line, a beat for its result to be seen. It writes
/// down when each step and line happened, on the <see cref="Stopwatch"/> clock the recorder uses.
/// </summary>
public sealed class BddNarrationPacer : IBddStepPacer
{
    private readonly BddSceneClips _clips;
    private readonly TimeSpan _beat;
    private readonly Func<SpeechClip, CancellationToken, Task>? _play;
    private readonly BddSayMode _say;
    private readonly List<StepTimes> _steps = [];
    private readonly List<(SpeechClip Clip, long Start)> _spoken = [];
    private StepTimes? _current;
    private Task _speaking = Task.CompletedTask;
    private bool _started;

    /// <summary>Creates the pacer for one scene.</summary>
    /// <param name="clips">The scene's spoken lines, made before the run.</param>
    /// <param name="beat">The wait after a step.</param>
    /// <param name="play">Plays a line aloud while it is waited for; null to wait in silence.</param>
    /// <param name="say">Whether a step's line plays while the step runs, or before it.</param>
    public BddNarrationPacer(BddSceneClips clips, TimeSpan beat, Func<SpeechClip, CancellationToken, Task>? play = null, BddSayMode say = BddSayMode.While)
    {
        _clips = clips ?? throw new ArgumentNullException(nameof(clips));
        _beat = beat;
        _play = play;
        _say = say;
    }

    /// <inheritdoc />
    public async Task BeforeAsync(BddStepInvocation invocation, BddExecutionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        var line = BddWrittenStep.Line(invocation);
        if (_current?.Line == line)
        {
            // The next step of the same call: it is part of the written step already said.
            return;
        }

        await EndCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!_started)
        {
            _started = true;
            await SayAsync(_clips.Intro, cancellationToken).ConfigureAwait(false);
        }

        var saying = SayAsync(_clips.Says.GetValueOrDefault(line), cancellationToken);
        if (_say == BddSayMode.Before)
        {
            await saying.ConfigureAwait(false);
        }
        else
        {
            // The step runs while its line plays; the line is waited for before the beat.
            _speaking = saying;
        }

        _current = new StepTimes(line, Written(invocation), Stopwatch.GetTimestamp());
        _steps.Add(_current);
    }

    /// <inheritdoc />
    public Task AfterAsync(BddStepResult result, BddExecutionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (_current is { } step)
        {
            step.End = Stopwatch.GetTimestamp();
            if (Worse(result.Status, step.Status))
            {
                step.Status = result.Status;
                step.Message = result.Message;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>After the last step: its beat, then the outro.</summary>
    /// <param name="cancellationToken">Cancels the wait.</param>
    public async Task FinishAsync(CancellationToken cancellationToken)
    {
        await EndCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!_started)
        {
            _started = true;
            await SayAsync(_clips.Intro, cancellationToken).ConfigureAwait(false);
        }

        await SayAsync(_clips.Outro, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The scene's steps and lines, on the clock of a video whose first frame was at <paramref name="videoStart"/>.</summary>
    /// <param name="number">The scene's number.</param>
    /// <param name="name">The scene's name.</param>
    /// <param name="scenario">The scenario's address.</param>
    /// <param name="video">The scene's video; null when there is none.</param>
    /// <param name="videoStart">When the video's first frame was taken, in <see cref="Stopwatch"/> ticks.</param>
    /// <param name="videoEnd">When the video ends, in <see cref="Stopwatch"/> ticks.</param>
    /// <param name="warnings">What went less well than asked.</param>
    public BddSceneTimeline Timeline(int number, string name, string? scenario, string? video, long videoStart, long videoEnd, IReadOnlyList<string> warnings)
    {
        double At(long time) => Math.Max(0, Stopwatch.GetElapsedTime(videoStart, time).TotalSeconds);
        var steps = _steps.Select(step => new BddTimedStep(step.Line, step.Text, At(step.Start), At(step.End ?? step.Start), step.Status, step.Message)).ToList();
        var clips = _spoken.Select(spoken => new BddTimedClip(spoken.Clip.Text, spoken.Clip.WavPath is { } wav ? Path.GetFileName(wav) : null, At(spoken.Start), At(spoken.Start) + spoken.Clip.Duration.TotalSeconds)).ToList();
        var status = steps.Count == 0 ? BddStepResultStatus.Skipped : steps.Select(step => step.Status).Aggregate((worst, next) => Worse(next, worst) ? next : worst);
        return new BddSceneTimeline(number, name, scenario, video, At(videoEnd), status, steps, clips, warnings);
    }

    private async Task EndCurrentAsync(CancellationToken cancellationToken)
    {
        if (_current is null)
        {
            return;
        }

        _current.End ??= Stopwatch.GetTimestamp();
        _current = null;
        var speaking = _speaking;
        _speaking = Task.CompletedTask;
        await speaking.ConfigureAwait(false);
        await Task.Delay(_beat, cancellationToken).ConfigureAwait(false);
    }

    private async Task SayAsync(SpeechClip? clip, CancellationToken cancellationToken)
    {
        if (clip is null)
        {
            return;
        }

        _spoken.Add((clip, Stopwatch.GetTimestamp()));
        var wait = Task.Delay(clip.Duration, cancellationToken);
        if (_play is not null && clip.WavPath is not null)
        {
            await Task.WhenAll(wait, _play(clip, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            await wait.ConfigureAwait(false);
        }
    }

    // Failed over canceled over skipped over passed: a call shows its worst step.
    private static bool Worse(BddStepResultStatus candidate, BddStepResultStatus current)
        => Rank(candidate) > Rank(current);

    private static int Rank(BddStepResultStatus status) => status switch
    {
        BddStepResultStatus.Failed => 4,
        BddStepResultStatus.Canceled => 3,
        BddStepResultStatus.Skipped => 2,
        BddStepResultStatus.Passed => 1,
        _ => 0,
    };

    private static string Written(BddStepInvocation invocation)
    {
        var frame = invocation.Call;
        while (frame?.Parent is { } parent)
        {
            frame = parent;
        }

        return BddVideoScriptCheck.Written(frame?.Call ?? invocation.Step);
    }

    private sealed class StepTimes(int line, string text, long start)
    {
        public int Line { get; } = line;

        public string Text { get; } = text;

        public long Start { get; } = start;

        public long? End { get; set; }

        public BddStepResultStatus Status { get; set; } = BddStepResultStatus.Passed;

        public string? Message { get; set; }
    }
}

/// <summary>When a step's line is said (a video script's <c>Say</c> field).</summary>
public enum BddSayMode
{
    /// <summary>While the step runs: the voice over what it does.</summary>
    While,

    /// <summary>Before the step runs: the step waits for its line.</summary>
    Before,
}

/// <summary>A scene's spoken lines, made before its run.</summary>
/// <param name="Intro">Said before the first step.</param>
/// <param name="Says">Said before a step, by the step's line in the feature file.</param>
/// <param name="Outro">Said after the last step.</param>
public sealed record BddSceneClips(SpeechClip? Intro, IReadOnlyDictionary<int, SpeechClip> Says, SpeechClip? Outro)
{
    /// <summary>No lines: a scene run silently, with its beats.</summary>
    public static BddSceneClips Silent { get; } = new(null, new Dictionary<int, SpeechClip>(), null);

    /// <summary>Speaks a scene's lines into <paramref name="folder"/>: <c>intro.wav</c>, <c>line-&lt;n&gt;.wav</c>, <c>outro.wav</c>.</summary>
    /// <param name="plan">The scene.</param>
    /// <param name="synthesizer">The voice; null to estimate the lengths only.</param>
    /// <param name="voice">The voice's name; null for the default.</param>
    /// <param name="folder">Where the WAV files go.</param>
    /// <param name="cancellationToken">Cancels the synthesis.</param>
    public static async Task<BddSceneClips> SpeakAsync(BddVideoScenePlan plan, ISpeechSynthesizer? synthesizer, string? voice, string folder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Directory.CreateDirectory(folder);
        Task<SpeechClip> Clip(string text, string file) => Speech.ClipAsync(synthesizer, plan.Pronunciations?.Apply(text) ?? text, voice, Path.Combine(folder, file), cancellationToken);

        var intro = plan.Scene.Intro is { } introText ? await Clip(introText, "intro.wav").ConfigureAwait(false) : null;
        var says = new Dictionary<int, SpeechClip>();
        foreach (var (line, say) in plan.Says.OrderBy(pair => pair.Key))
        {
            says[line] = await Clip(say.Say, $"line-{line}.wav").ConfigureAwait(false);
        }

        var outro = plan.Scene.Outro is { } outroText ? await Clip(outroText, "outro.wav").ConfigureAwait(false) : null;
        return new BddSceneClips(intro, says, outro);
    }
}

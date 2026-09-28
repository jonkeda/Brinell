using System.Diagnostics;
using System.Text.Json;

namespace Brinell.Bdd;

/// <summary>
/// Where a step session stops for the debugger: before a step whose line is listed, and after a
/// step that fails when asked to. The seam a test replaces; the running default is
/// <see cref="BddBreakpoints"/>, which only exists while a debugger is attached.
/// </summary>
public interface IBddBreakHook
{
    /// <summary>The break before a step, or null to run on.</summary>
    /// <param name="invocation">The step about to run.</param>
    /// <param name="context">The scenario's context, for the app and the page.</param>
    BddStepBreak? Before(BddStepInvocation invocation, BddExecutionContext context);

    /// <summary>Whether to stop after a step that failed, with its result in view.</summary>
    /// <param name="result">The failed step's result.</param>
    bool AfterFailure(BddStepResult result);

    /// <summary>Stops: <see cref="Debugger.Break"/> for the running default.</summary>
    /// <param name="stepBreak">What the developer sees in Locals.</param>
    void Break(BddStepBreak stepBreak);
}

/// <summary>What a break before a step shows: the step, its arguments as read, where it runs, and how it got there.</summary>
/// <param name="Text">The step's text, with a callable's values filled in.</param>
/// <param name="Arguments">Its arguments, as the step definition receives them.</param>
/// <param name="App">The app it runs on; null for the fixture's own.</param>
/// <param name="Page">The page the scenario is on; null when not known.</param>
/// <param name="Source">Where the step is written: the feature file, or a step file for a callable's step.</param>
/// <param name="Calls">The calls that put it there, outermost first: <c>Step: the synced list has todos (Todo.steps.md:3) at TOD.01.feature.md:34</c>.</param>
/// <param name="Result">For a break after a failure, the failed step's result; null before a step.</param>
public sealed record BddStepBreak(
    string Text,
    IReadOnlyDictionary<string, string> Arguments,
    string? App,
    string? Page,
    BddSourceLocation Source,
    IReadOnlyList<string> Calls,
    BddStepResult? Result = null);

/// <summary>
/// The step breakpoints an IDE sets (R2 of the IDE plan): a per-user file,
/// <c>%LOCALAPPDATA%\Brinell\uat-breakpoints.json</c>, with a list of lines per file. It is a file,
/// not an environment variable, so a Debug started from Test Explorer honours it too. It is only
/// read while a debugger is attached, and read again when it changes.
/// </summary>
/// <remarks>
/// <code>{ "files": { "C:\\src\\Suite\\Features\\TOD.01.feature.md": [19, 25] }, "breakOnFailure": false }</code>
/// A file matches a step's file when the paths are equal, or when both are under a
/// <c>Features</c> folder with the same path below it: a test runs the build output's copy of a
/// file the IDE lists by its source path.
/// </remarks>
public sealed class BddBreakpoints : IBddBreakHook
{
    private readonly string _path;
    private readonly Func<string, string?> _read;
    private DateTime _readAt = DateTime.MinValue;
    private IReadOnlyList<(string File, HashSet<int> Lines)> _files = [];
    private bool _breakOnFailure;
    private BddCallFrame? _lastCall;

    /// <summary>Reads breakpoints from <paramref name="path"/>.</summary>
    /// <param name="path">The breakpoints file.</param>
    /// <param name="read">Reads the file; null when it does not exist.</param>
    public BddBreakpoints(string path, Func<string, string?>? read = null)
    {
        _path = path;
        _read = read ?? (file => File.Exists(file) ? File.ReadAllText(file) : null);
    }

    /// <summary>The per-user breakpoints file.</summary>
    public static string DefaultPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brinell", "uat-breakpoints.json");

    /// <summary>The breakpoints of <see cref="DefaultPath"/> while a debugger is attached; null otherwise, so nothing is read.</summary>
    /// <param name="attached">Whether a debugger is attached; <see cref="Debugger.IsAttached"/> by default.</param>
    /// <param name="path">The breakpoints file; <see cref="DefaultPath"/> by default.</param>
    public static IBddBreakHook? ForDebugger(Func<bool>? attached = null, string? path = null)
        => (attached ?? (() => Debugger.IsAttached))() ? new BddBreakpoints(path ?? DefaultPath) : null;

    /// <inheritdoc />
    public BddStepBreak? Before(BddStepInvocation invocation, BddExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        ArgumentNullException.ThrowIfNull(context);
        Refresh();

        // The step's own line (in a step file: each call of it), or the call's line, once, before its first step.
        var outermost = invocation.Call is { } frame ? Outermost(frame) : null;
        var atCall = outermost is not null && !ReferenceEquals(outermost, _lastCall) && IsListed(outermost.Call.Source);
        _lastCall = outermost;
        if (!IsListed(invocation.Step.Source) && !atCall)
        {
            return null;
        }

        return Describe(invocation, context);
    }

    /// <inheritdoc />
    public bool AfterFailure(BddStepResult result)
    {
        Refresh();
        return _breakOnFailure;
    }

    /// <inheritdoc />
    [DebuggerHidden]
    public void Break(BddStepBreak stepBreak) => Debugger.Break();

    /// <summary>What a break shows for a step.</summary>
    /// <param name="invocation">The step.</param>
    /// <param name="context">Its scenario's context.</param>
    /// <param name="result">The failed result, for a break after a failure.</param>
    public static BddStepBreak Describe(BddStepInvocation invocation, BddExecutionContext context, BddStepResult? result = null)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        ArgumentNullException.ThrowIfNull(context);
        var calls = invocation.Call?.Chain.Select(frame => $"{frame.Callable} ({Where(frame.Callable.Source)}) at {Where(frame.Location)}").ToArray() ?? [];
        return new BddStepBreak(
            invocation.Step.Text,
            invocation.Arguments,
            invocation.App ?? context.CurrentSession,
            context.CurrentPageName,
            invocation.Step.Source,
            calls,
            result);
    }

    private bool IsListed(BddSourceLocation location)
        => location.FilePath is { } file && _files.Any(listed => listed.Lines.Contains(location.LineNumber) && SameFile(listed.File, file));

    // Equal, or the same below a Features folder: the build output's copy of a listed source file.
    private static bool SameFile(string listed, string actual)
    {
        var a = Path.GetFullPath(listed);
        var b = Path.GetFullPath(actual);
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return BelowFeatures(a) is { } below && string.Equals(below, BelowFeatures(b), StringComparison.OrdinalIgnoreCase);
    }

    private static string? BelowFeatures(string path)
    {
        var root = BddScenarioFiles.ScenarioRoot(path);
        return Path.GetFileName(root).Equals(BddFileFormat.FeatureFolder, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(root, path)
            : null;
    }

    private void Refresh()
    {
        var written = File.Exists(_path) ? File.GetLastWriteTimeUtc(_path) : DateTime.MinValue;
        if (written == _readAt && _files.Count > 0)
        {
            return;
        }

        _readAt = written;
        _files = [];
        _breakOnFailure = false;
        if (_read(_path) is not { } json)
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Object)
            {
                _files = files.EnumerateObject()
                    .Select(file => (file.Name, file.Value.ValueKind == JsonValueKind.Array
                        ? file.Value.EnumerateArray().Where(line => line.ValueKind == JsonValueKind.Number).Select(line => line.GetInt32()).ToHashSet()
                        : []))
                    .ToList();
            }

            _breakOnFailure = root.TryGetProperty("breakOnFailure", out var onFailure) && onFailure.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            // A file the IDE is writing just now: no breakpoints until it is whole.
            _files = [];
        }
    }

    private static BddCallFrame Outermost(BddCallFrame frame)
    {
        while (frame.Parent is { } parent)
        {
            frame = parent;
        }

        return frame;
    }

    private static string Where(BddSourceLocation location)
        => location.FilePath is { } file ? $"{Path.GetFileName(file)}:{location.LineNumber}" : $"line {location.LineNumber}";
}

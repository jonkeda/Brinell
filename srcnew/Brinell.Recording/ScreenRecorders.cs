using System.Reflection;

namespace Brinell.Recording;

/// <summary>
/// The recorders a run can choose from, in order: the first that can record a target records it.
/// A platform package offers its own with <see cref="ScreenRecorderAttribute"/>.
/// </summary>
public sealed class ScreenRecorders
{
    // The platform packages a run may have in its output folder without referencing them itself.
    private static readonly string[] KnownPackages = ["Brinell.Recording.Windows", "Brinell.Html.Playwright", "Brinell.Maui.Appium"];

    private readonly List<IScreenRecorder> _recorders;

    /// <summary>Creates a registry of the given recorders, in the order they are tried.</summary>
    /// <param name="recorders">The recorders.</param>
    public ScreenRecorders(IEnumerable<IScreenRecorder> recorders)
    {
        ArgumentNullException.ThrowIfNull(recorders);
        _recorders = [.. recorders];
    }

    /// <summary>The recorders, in the order they are tried.</summary>
    public IReadOnlyList<IScreenRecorder> Recorders => _recorders;

    /// <summary>
    /// The recorders on this machine: those the platform packages declare (loaded from the output
    /// folder when present), then <see cref="FfmpegRegionRecorder"/> as the last resort.
    /// </summary>
    public static ScreenRecorders Discover()
    {
        foreach (var name in KnownPackages)
        {
            TryLoad(name);
        }

        var declared = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetCustomAttributes<ScreenRecorderAttribute>())
            .OrderBy(attribute => attribute.Order)
            .Select(attribute => (IScreenRecorder)Activator.CreateInstance(attribute.RecorderType)!);
        return new ScreenRecorders([.. declared, new FfmpegRegionRecorder()]);
    }

    /// <summary>The first recorder that can record the target; null when none can.</summary>
    /// <param name="target">What to record.</param>
    public IScreenRecorder? Find(RecordingTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return _recorders.FirstOrDefault(recorder => recorder.CanRecord(target));
    }

    // A platform package is optional: not being there means its recorders are not offered.
    private static void TryLoad(string name)
    {
        try
        {
            Assembly.Load(new AssemblyName(name));
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            Debug.WriteLine($"Recorders: {name} is not available ({exception.GetType().Name}).");
        }
    }
}

/// <summary>Declares a recorder that <see cref="ScreenRecorders.Discover"/> offers; it needs a public parameterless constructor.</summary>
/// <param name="recorderType">The recorder's type, an <see cref="IScreenRecorder"/>.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ScreenRecorderAttribute(Type recorderType) : Attribute
{
    /// <summary>The recorder's type.</summary>
    public Type RecorderType { get; } = recorderType;

    /// <summary>Where it is tried among the declared recorders: lower first.</summary>
    public int Order { get; init; }
}

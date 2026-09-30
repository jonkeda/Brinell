using System.Reflection;

namespace Brinell.Recording;

/// <summary>
/// The recorders a run can choose from, in order: the first that can record a target records it.
/// A platform package offers its own with <see cref="ScreenRecorderAttribute"/>.
/// </summary>
public sealed class ScreenRecorders
{
    // The platform packages that may bring recorders, loaded when the run has them (RecordingPackages).
    private static readonly string[] KnownPackages = ["Brinell.Narrator.Windows", "Brinell.Html.Playwright", "Brinell.Maui.Appium"];

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
    /// The recorders on this machine: those the platform packages declare (see
    /// <see cref="RecordingPackages"/>), then <see cref="FfmpegRegionRecorder"/> as the last resort.
    /// </summary>
    public static ScreenRecorders Discover()
    {
        RecordingPackages.Load(KnownPackages);
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

namespace Bruinet.Core.Recording;

/// <summary>A test context that can say what it shows, so a recorder can record it.</summary>
public interface IRecordable
{
    /// <summary>What the context shows now; null when it cannot say (the app has not started, or is gone).</summary>
    RecordingTarget? RecordingTarget { get; }
}

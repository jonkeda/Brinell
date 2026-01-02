namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for label/text display controls.
/// Labels are read-only, so they only inherit from IContentControl (no editing methods).
/// </summary>
public interface ILabel : IContentControl{
    // Label-specific methods if needed
}
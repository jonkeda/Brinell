namespace Brinell.Core.Abstractions;

/// <summary>
/// Represents an element found by the driver.
/// </summary>
public interface IElementAdapter
{
    /// <summary>
    /// The AutomationId of this element.
    /// </summary>
    string AutomationId { get; }
    
    /// <summary>
    /// The native element object (for platform-specific operations).
    /// </summary>
    object NativeElement { get; }
}

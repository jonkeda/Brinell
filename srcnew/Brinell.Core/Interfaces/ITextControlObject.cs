namespace Brinell.Core.Interfaces;

/// <summary>
/// Text display capability for labels, spans, and other text elements.
/// </summary>
public interface ITextControlObject : IControlObject
{
    /// <summary>
    /// Wait until text equals expected value.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitTextEquals(string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until text contains expected substring.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitTextContains(string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert text matches pattern (regex).
    /// If pattern is null, returns immediately (skip).
    /// </summary>
    void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null);
}

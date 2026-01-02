using Brinell.Core.Abstractions.Controls;
using System.Numerics;

namespace Brinell.Stride.Abstractions.Controls;

/// <summary>
/// Platform-specific interface for Stride/game engine controls.
/// Extends core control functionality with game-specific operations.
/// </summary>
public interface IGameControl : IControlObject
{
    /// <summary>
    /// Check if the control is interactable (can be clicked/used).
    /// </summary>
    bool IsInteractable();
    
    /// <summary>
    /// Check if the control has input focus.
    /// </summary>
    bool IsFocused();
    
    /// <summary>
    /// Hover over the control.
    /// </summary>
    void Hover();
    
    /// <summary>
    /// Try to click the control with a timeout.
    /// Returns true if click succeeded, false if timeout occurred.
    /// </summary>
    bool TryClick(int timeoutMs = 5000);
    
    /// <summary>
    /// Get the world position of the control.
    /// </summary>
    Vector3 GetWorldPosition();
    
    /// <summary>
    /// Get the screen position of the control.
    /// </summary>
    Vector2 GetScreenPosition();
}

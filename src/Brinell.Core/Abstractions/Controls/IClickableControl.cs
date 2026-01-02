namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for controls that can be clicked/interacted with.
/// </summary>
public interface IClickableControl : IControlObject
{
    /// <summary>
    /// Click the control.
    /// </summary>
    void Click();
    
    /// <summary>
    /// Double-click the control.
    /// </summary>
    void DoubleClick();
    
    /// <summary>
    /// Right-click the control.
    /// </summary>
    void RightClick();
    
    /// <summary>
    /// Hover over the control.
    /// </summary>
    void Hover();
}

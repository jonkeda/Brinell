namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for controls that display content and can be clicked.
/// </summary>
public interface IContentControl : IControlObject
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
}

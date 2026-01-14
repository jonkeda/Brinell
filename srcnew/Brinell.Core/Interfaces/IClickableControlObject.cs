namespace Brinell.Core.Interfaces;

/// <summary>
/// Click capability for buttons, links, images.
/// Action methods return TPage for fluent method chaining.
/// </summary>
/// <typeparam name="TPage">The parent page type for fluent chaining.</typeparam>
public interface IClickableControlObject<TPage> : IControlObject
    where TPage : IPageObject
{
    /// <summary>
    /// Check if the control is clickable (visible and enabled).
    /// Returns null if element not found.
    /// </summary>
    bool? IsClickable();
    
    /// <summary>
    /// Perform a single click on the control.
    /// </summary>
    /// <returns>The parent page for fluent chaining.</returns>
    TPage Click(int? timeoutMs = null);
    
    /// <summary>
    /// Perform a double-click on the control.
    /// </summary>
    /// <returns>The parent page for fluent chaining.</returns>
    TPage DoubleClick(int? timeoutMs = null);
    
    /// <summary>
    /// Perform a right-click (context click) on the control.
    /// </summary>
    /// <returns>The parent page for fluent chaining.</returns>
    TPage RightClick(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until control is clickable (visible and enabled).
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitClickable(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert control clickable state matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertClickable(bool? expected, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// Non-generic click capability for backward compatibility.
/// </summary>
public interface IClickableControlObject : IControlObject
{
    /// <summary>
    /// Check if the control is clickable (visible and enabled).
    /// Returns null if element not found.
    /// </summary>
    bool? IsClickable();
    
    /// <summary>
    /// Perform a single click on the control.
    /// </summary>
    void Click(int? timeoutMs = null);
    
    /// <summary>
    /// Perform a double-click on the control.
    /// </summary>
    void DoubleClick(int? timeoutMs = null);
    
    /// <summary>
    /// Perform a right-click (context click) on the control.
    /// </summary>
    void RightClick(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until control is clickable (visible and enabled).
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitClickable(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert control clickable state matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertClickable(bool? expected, string? message = null, int? timeoutMs = null);
}

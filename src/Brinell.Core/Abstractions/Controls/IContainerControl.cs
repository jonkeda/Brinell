namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for controls that contain child controls.
/// </summary>
public interface IContainerControl : IControlObject
{
    /// <summary>
    /// Get the count of child controls.
    /// </summary>
    int GetChildCount();
    
    /// <summary>
    /// Get the names/ids of all child controls.
    /// </summary>
    IReadOnlyList<string> GetChildNames();
    
    /// <summary>
    /// Check if a child control exists by name.
    /// </summary>
    bool ChildExists(string childName);
    
    /// <summary>
    /// Get a child control by name and cast to specified type.
    /// </summary>
    T GetChild<T>(string childName) where T : IControlObject;
}

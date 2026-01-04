using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for container controls that can hold child controls.
/// </summary>
public interface IContainerControlObject : IControlObject
{
    /// <summary>
    /// Gets the count of child elements.
    /// </summary>
    int GetChildCount(int? timeoutMs = null);

    /// <summary>
    /// Asserts the child count matches the expected value.
    /// </summary>
    void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Finds a child control by locator.
    /// </summary>
    T FindChild<T>(ControlLocator locator) where T : IControlObject;

    /// <summary>
    /// Finds a child control by automation ID.
    /// </summary>
    T FindChild<T>(string automationId) where T : IControlObject;

    /// <summary>
    /// Finds all child controls matching the locator.
    /// </summary>
    IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject;
}

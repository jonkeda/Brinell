namespace Brinell.Core.ControlObject6.Locators;

/// <summary>
/// Represents a locator for finding UI elements.
/// Supports multiple strategies and chaining.
/// </summary>
public class ControlLocator
{
    /// <summary>
    /// The strategy used to locate the element.
    /// </summary>
    public LocatorStrategy Strategy { get; }

    /// <summary>
    /// The value used with the strategy (e.g., the automation ID, CSS selector, etc.).
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Optional parent locator for chained lookups.
    /// </summary>
    public ControlLocator? Parent { get; }

    /// <summary>
    /// Optional index for selecting nth matching element.
    /// </summary>
    public int? Index { get; private set; }

    /// <summary>
    /// Optional data attribute name (used with DataAttribute strategy).
    /// </summary>
    public string? DataAttributeName { get; }

    /// <summary>
    /// Creates a new locator with the specified strategy and value.
    /// </summary>
    public ControlLocator(LocatorStrategy strategy, string value, ControlLocator? parent = null, string? dataAttributeName = null)
    {
        Strategy = strategy;
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Parent = parent;
        DataAttributeName = dataAttributeName;
    }

    /// <summary>
    /// Creates a chained locator that searches within this element.
    /// </summary>
    /// <param name="child">The child locator to chain.</param>
    /// <returns>A new chained locator.</returns>
    public ControlLocator Then(ControlLocator child)
    {
        ArgumentNullException.ThrowIfNull(child);
        return new ControlLocator(LocatorStrategy.Chained, child.Value, this)
        {
            Index = child.Index
        };
    }

    /// <summary>
    /// Selects a specific index when multiple elements match.
    /// </summary>
    /// <param name="index">Zero-based index of the element to select.</param>
    /// <returns>A new locator with the index set.</returns>
    public ControlLocator WithIndex(int index)
    {
        return new ControlLocator(Strategy, Value, Parent, DataAttributeName)
        {
            Index = index
        };
    }

    /// <summary>
    /// Selects the first matching element (index 0).
    /// </summary>
    public ControlLocator First() => WithIndex(0);

    /// <summary>
    /// Selects the last matching element (index -1).
    /// </summary>
    public ControlLocator Last() => WithIndex(-1);

    /// <summary>
    /// Selects the nth matching element (1-based for readability).
    /// </summary>
    /// <param name="n">1-based position (Nth(1) = first element).</param>
    public ControlLocator Nth(int n) => WithIndex(n - 1);

    /// <summary>
    /// Implicit conversion from string to ControlLocator using AutomationId strategy.
    /// </summary>
    public static implicit operator ControlLocator(string automationId)
    {
        return new ControlLocator(LocatorStrategy.AutomationId, automationId);
    }

    /// <summary>
    /// Returns a string representation of this locator.
    /// </summary>
    public override string ToString()
    {
        var result = $"{Strategy}={Value}";
        if (Index.HasValue)
            result += $"[{Index}]";
        if (Parent != null)
            result = $"{Parent} -> {result}";
        return result;
    }
}

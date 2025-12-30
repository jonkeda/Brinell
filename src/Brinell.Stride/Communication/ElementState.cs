using System.Text.Json.Serialization;

namespace Brinell.Stride.Communication;

/// <summary>
/// Serializable state information for a UI element.
/// </summary>
public class ElementState
{
    /// <summary>
    /// Whether the element exists.
    /// </summary>
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    /// <summary>
    /// Whether the element is visible.
    /// </summary>
    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; }

    /// <summary>
    /// Whether the element is enabled.
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Whether the element is hit-test visible (can receive input).
    /// </summary>
    [JsonPropertyName("isHitTestVisible")]
    public bool IsHitTestVisible { get; set; }

    /// <summary>
    /// Whether the element is focused.
    /// </summary>
    [JsonPropertyName("isFocused")]
    public bool IsFocused { get; set; }

    /// <summary>
    /// Text content of the element.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Name of the element.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Automation ID of the element.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; set; }

    /// <summary>
    /// Control type name.
    /// </summary>
    [JsonPropertyName("controlType")]
    public string? ControlType { get; set; }

    /// <summary>
    /// Element bounds in screen coordinates.
    /// </summary>
    [JsonPropertyName("bounds")]
    public ElementBounds Bounds { get; set; } = new();

    /// <summary>
    /// Element opacity (0-1).
    /// </summary>
    [JsonPropertyName("opacity")]
    public float Opacity { get; set; } = 1f;

    /// <summary>
    /// Toggle/checkbox checked state.
    /// </summary>
    [JsonPropertyName("isChecked")]
    public bool? IsChecked { get; set; }

    /// <summary>
    /// Selected index for selector controls.
    /// </summary>
    [JsonPropertyName("selectedIndex")]
    public int SelectedIndex { get; set; } = -1;

    /// <summary>
    /// Selected text for selector controls.
    /// </summary>
    [JsonPropertyName("selectedText")]
    public string? SelectedText { get; set; }

    /// <summary>
    /// Items for list controls.
    /// </summary>
    [JsonPropertyName("items")]
    public List<string>? Items { get; set; }

    /// <summary>
    /// Current value for range controls.
    /// </summary>
    [JsonPropertyName("value")]
    public double? Value { get; set; }

    /// <summary>
    /// Minimum value for range controls.
    /// </summary>
    [JsonPropertyName("minimum")]
    public double? Minimum { get; set; }

    /// <summary>
    /// Maximum value for range controls.
    /// </summary>
    [JsonPropertyName("maximum")]
    public double? Maximum { get; set; }

    /// <summary>
    /// Whether the control is open (for dropdowns, popups, etc.).
    /// </summary>
    [JsonPropertyName("isOpen")]
    public bool? IsOpen { get; set; }
}

/// <summary>
/// Screen rectangle for UI element bounds.
/// </summary>
public class ElementBounds
{
    /// <summary>
    /// X coordinate (left edge).
    /// </summary>
    [JsonPropertyName("x")]
    public int X { get; set; }

    /// <summary>
    /// Y coordinate (top edge).
    /// </summary>
    [JsonPropertyName("y")]
    public int Y { get; set; }

    /// <summary>
    /// Width of the element.
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>
    /// Height of the element.
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }

    /// <summary>
    /// Whether the bounds are empty (no size).
    /// </summary>
    [JsonIgnore]
    public bool IsEmpty => Width == 0 || Height == 0;

    /// <summary>
    /// Get center X coordinate.
    /// </summary>
    [JsonIgnore]
    public int CenterX => X + Width / 2;

    /// <summary>
    /// Get center Y coordinate.
    /// </summary>
    [JsonIgnore]
    public int CenterY => Y + Height / 2;

    /// <summary>
    /// Check if a point is within the bounds.
    /// </summary>
    public bool Contains(int x, int y)
        => x >= X && x < X + Width && y >= Y && y < Y + Height;
}

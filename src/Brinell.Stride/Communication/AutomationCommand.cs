using System.Text.Json.Serialization;

namespace Brinell.Stride.Communication;

/// <summary>
/// Command sent from test process to game for automation.
/// </summary>
public class AutomationCommand
{
    /// <summary>
    /// Command type: "Query", "Action", "Wait".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Target element automation ID (null for global commands).
    /// </summary>
    [JsonPropertyName("target")]
    public string? Target { get; set; }

    /// <summary>
    /// Method to invoke.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Method arguments.
    /// </summary>
    [JsonPropertyName("args")]
    public object[]? Args { get; set; }

    /// <summary>
    /// Timeout for wait commands (milliseconds).
    /// </summary>
    [JsonPropertyName("timeoutMs")]
    public int TimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Create a query command.
    /// </summary>
    public static AutomationCommand Query(string method, string? target = null, params object[] args) => new()
    {
        Type = "Query",
        Method = method,
        Target = target,
        Args = args.Length > 0 ? args : null
    };

    /// <summary>
    /// Create an action command.
    /// </summary>
    public static AutomationCommand Action(string method, string? target = null, params object[] args) => new()
    {
        Type = "Action",
        Method = method,
        Target = target,
        Args = args.Length > 0 ? args : null
    };

    /// <summary>
    /// Create a wait command.
    /// </summary>
    public static AutomationCommand Wait(string method, string? target = null, int timeoutMs = 10000, params object[] args) => new()
    {
        Type = "Wait",
        Method = method,
        Target = target,
        TimeoutMs = timeoutMs,
        Args = args.Length > 0 ? args : null
    };
}

using System.Text.Json.Serialization;

namespace Brinell.Stride.Communication;

/// <summary>
/// Response from game to test process.
/// </summary>
public class AutomationResponse
{
    /// <summary>
    /// Whether the command executed successfully.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Result data (type depends on command).
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    /// <summary>
    /// Error message if Success is false.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Stack trace for debugging (optional).
    /// </summary>
    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; set; }

    /// <summary>
    /// Create a successful response.
    /// </summary>
    public static AutomationResponse Ok(object? result = null) => new()
    {
        Success = true,
        Result = result
    };

    /// <summary>
    /// Create a failure response.
    /// </summary>
    public static AutomationResponse Fail(string error, string? stackTrace = null) => new()
    {
        Success = false,
        Error = error,
        StackTrace = stackTrace
    };
}

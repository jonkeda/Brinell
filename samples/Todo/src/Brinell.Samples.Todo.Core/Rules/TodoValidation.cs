namespace Brinell.Samples.Todo.Core.Rules;

/// <summary>
/// What a todo must satisfy before it can be saved.
/// </summary>
public static class TodoValidation
{
    /// <summary>The longest title allowed.</summary>
    public const int MaxTitleLength = 100;

    /// <summary>Shown when the title is empty or only whitespace.</summary>
    public const string TitleRequired = "Title is required";

    /// <summary>Shown when the title is too long.</summary>
    public static readonly string TitleTooLong = $"Title can be at most {MaxTitleLength} characters";

    /// <summary>
    /// The error for <paramref name="title"/>, or <c>null</c> when it is valid. The length is
    /// measured after trimming, which is also how the title is stored.
    /// </summary>
    public static string? ValidateTitle(string? title)
    {
        var trimmed = title?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return TitleRequired;
        }

        return trimmed.Length > MaxTitleLength ? TitleTooLong : null;
    }
}

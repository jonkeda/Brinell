namespace Brinell.Core.Scraping;

/// <summary>
/// Something the person did in the app while a source records actions (<see cref="IScrapeSource.StartActionsAsync"/>):
/// a click, a value, a check. Its element is a node of a capture, so it can be resolved later from files.
/// </summary>
public sealed record ScrapeAction
{
    /// <summary>What it is: one of <see cref="ScrapeActionKind"/>.</summary>
    public required string Kind { get; init; }

    /// <summary>When it happened.</summary>
    public DateTimeOffset At { get; init; }

    /// <summary>Where the app was: the page's address, not a frame's.</summary>
    public string Location { get; init; } = "";

    /// <summary>The element's number in the capture taken at <see cref="CapturedAt"/>; null when no capture holds it.</summary>
    public int? Node { get; init; }

    /// <summary>When the capture <see cref="Node"/> is in was taken: its <see cref="ScrapeSnapshot.CapturedAt"/>.</summary>
    public DateTimeOffset? CapturedAt { get; init; }

    /// <summary>
    /// A capture the source took for this action because its element came after the last one: the
    /// recording keeps it before the action.
    /// </summary>
    public ScrapeSnapshot? Capture { get; init; }

    /// <summary>The element from the root down, as it was when the person acted.</summary>
    public IReadOnlyList<ScrapePathStep>? Path { get; init; }

    /// <summary>What was typed, chosen or pressed, or a dialog's text.</summary>
    public string? Value { get; init; }

    /// <summary>A checkbox's or radio's new state.</summary>
    public bool? Checked { get; init; }

    /// <summary>Whether a dialog was accepted.</summary>
    public bool? Accepted { get; init; }
}

/// <summary>The kinds of <see cref="ScrapeAction"/>.</summary>
public static class ScrapeActionKind
{
    /// <summary>A click on an element, or on its nearest clickable ancestor.</summary>
    public const string Click = "click";

    /// <summary>A field's final value after one visit.</summary>
    public const string Input = "input";

    /// <summary>An option chosen: the element is the select (or the list box's owner), the value the option's text.</summary>
    public const string Select = "select";

    /// <summary>A checkbox, switch or radio set to a state.</summary>
    public const string Check = "check";

    /// <summary>Enter, Escape or Tab.</summary>
    public const string Key = "key";

    /// <summary>The page moved to another address.</summary>
    public const string Navigate = "navigate";

    /// <summary>A browser dialog was shown and answered.</summary>
    public const string Dialog = "dialog";
}

/// <summary>One level of an element's path from the root: what identifies it among its siblings.</summary>
public sealed record ScrapePathStep
{
    /// <summary>The tag name or control type.</summary>
    public string Type { get; init; } = "";

    /// <summary>Its test id, in one of the project's test id attributes.</summary>
    public string? TestId { get; init; }

    /// <summary>Its accessibility role.</summary>
    public string? Role { get; init; }

    /// <summary>Its accessible name.</summary>
    public string? Name { get; init; }

    /// <summary>Its classes, space-separated.</summary>
    public string? Classes { get; init; }
}

using Brinell.Samples.Todo.Core.Models;

namespace Brinell.Samples.Todo.Core.Rules;

/// <summary>
/// The status cycle, and what the status control shows for a todo.
/// </summary>
public static class TodoStatusRules
{
    /// <summary>The status after <paramref name="status"/>: Open, In progress, Done, and round again.</summary>
    public static TodoStatus Next(TodoStatus status) => status switch
    {
        TodoStatus.Open => TodoStatus.InProgress,
        TodoStatus.InProgress => TodoStatus.Done,
        TodoStatus.Done => TodoStatus.Open,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    /// <summary>
    /// Overdue when the todo is not done and its due date is before <paramref name="today"/>;
    /// otherwise the stored status. Due today is not overdue.
    /// </summary>
    public static DisplayStatus Display(TodoStatus status, DateOnly? dueDate, DateOnly today)
        => Display(status, isPastDue: dueDate < today);

    /// <summary>
    /// Overdue when <paramref name="isPastDue"/> and the todo is not done; otherwise the stored status.
    /// What the status control uses, since it is told whether the date has passed rather than the dates.
    /// </summary>
    public static DisplayStatus Display(TodoStatus status, bool isPastDue)
    {
        if (isPastDue && status != TodoStatus.Done)
        {
            return DisplayStatus.Overdue;
        }

        return status switch
        {
            TodoStatus.Open => DisplayStatus.Open,
            TodoStatus.InProgress => DisplayStatus.InProgress,
            TodoStatus.Done => DisplayStatus.Done,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
    }

    /// <summary>Whether <see cref="Display"/> would say Overdue.</summary>
    public static bool IsOverdue(TodoStatus status, DateOnly? dueDate, DateOnly today)
        => Display(status, dueDate, today) == DisplayStatus.Overdue;

    /// <summary>The status control's glyph.</summary>
    public static string Glyph(DisplayStatus status) => status switch
    {
        DisplayStatus.Open => "○",
        DisplayStatus.InProgress => "◐",
        DisplayStatus.Done => "●",
        DisplayStatus.Overdue => "⚠",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    /// <summary>The status control's text.</summary>
    public static string Text(DisplayStatus status) => status switch
    {
        DisplayStatus.Open => "Open",
        DisplayStatus.InProgress => "In progress",
        DisplayStatus.Done => "Done",
        DisplayStatus.Overdue => "Overdue",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}

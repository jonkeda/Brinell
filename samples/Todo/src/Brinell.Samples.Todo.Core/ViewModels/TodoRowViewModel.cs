using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Rules;

namespace Brinell.Samples.Todo.Core.ViewModels;

/// <summary>
/// One row of the list: what the row shows, worked out once when the list loads.
/// </summary>
public sealed class TodoRowViewModel(TodoItem item, DateOnly today, IFormatProvider? culture = null)
{
    /// <summary>The todo's id.</summary>
    public Guid Id { get; } = item.Id;

    /// <summary>The title.</summary>
    public string Title { get; } = item.Title;

    /// <summary>"Due …", or empty.</summary>
    public string DueText { get; } = TodoFormatting.Due(item.DueDate, culture);

    /// <summary>Whether the row shows a due date at all.</summary>
    public bool HasDueDate { get; } = item.DueDate is not null;

    /// <summary>The stored status, for the read-only status control.</summary>
    public TodoStatus Status { get; } = item.Status;

    /// <summary>Whether the status control shows Overdue.</summary>
    public bool IsOverdue { get; } = TodoStatusRules.IsOverdue(item.Status, item.DueDate, today);

    /// <summary>Whether the due date has passed; the status control shows Overdue for it unless Done.</summary>
    public bool IsPastDue { get; } = item.DueDate < today;

    /// <summary>Whether the row has changes the server has not seen (TOD.01.6).</summary>
    public bool IsPending { get; } = item.NeedsPush;
}

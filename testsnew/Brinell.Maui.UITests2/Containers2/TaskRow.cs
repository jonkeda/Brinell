using Brinell.Maui.Containers;

namespace Brinell.Maui.UITests.Containers2;

/// <summary>
/// One task row, scoped to its own root element.
/// </summary>
/// <remarks>
/// Every row uses the same automation ids; the row's supplied root is what makes them
/// resolve independently.
/// </remarks>
public class TaskRow : ItemContainerBase<TaskCollection, TaskRow>
{
    public TaskRow(TaskCollection collection, IMauiElement itemRoot, int index)
        : base(collection, itemRoot, index)
    {
    }

    /// <summary>The task completion checkbox.</summary>
    public CheckBox<TaskRow> TaskCheckBox => new(this, "TaskCheckBox");

    /// <summary>The task name label.</summary>
    public Label<TaskRow> NameLabel => new(this, "TaskNameLabel");

    /// <summary>The delete button.</summary>
    public Button<TaskRow> DeleteButton => new(this, "TaskDeleteButton");
}

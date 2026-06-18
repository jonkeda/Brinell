using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Containers2;

/// <summary>
/// Container for a task item in the TaskList.
/// Uses unique AutomationId per task (Task_0, Task_1, Task_2, etc.).
/// </summary>
public class TaskItemContainer : ContainerBase<ContainerDemoPage, TaskItemContainer>
{
    private readonly int _index;

    public TaskItemContainer(IMauiScope<ContainerDemoPage> parentScope, int index)
        : base(parentScope, new Locator(LocatorStrategy.AutomationId, $"Task_{index}"))
    {
        _index = index;
    }

    /// <summary>
    /// Gets the 0-based index of this task item.
    /// </summary>
    public int Index => _index;

    /// <summary>
    /// The task checkbox.
    /// </summary>
    public CheckBox<TaskItemContainer> TaskCheckBox => new(this,"TaskCheckBox");

    /// <summary>
    /// The task name label.
    /// </summary>
    public Label<TaskItemContainer> NameLabel => new(this,"TaskNameLabel");

    /// <summary>
    /// The delete task button.
    /// </summary>
    public Button<TaskItemContainer> DeleteButton => new(this,"TaskDeleteButton");
}

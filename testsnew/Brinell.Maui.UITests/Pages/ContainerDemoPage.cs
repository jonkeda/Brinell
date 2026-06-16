using Brinell.Maui.UITests.Containers;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the ContainerDemoPage demonstrating container testing patterns.
/// Containers are initialized in constructor per SPEC-017b design principles.
/// </summary>
public class ContainerDemoPage : PageObjectBase<ContainerDemoPage>
{
    public ContainerDemoPage(IMauiTestContext context)
        : base(context)
    {
        // Containers initialized in constructor, NOT as lazy properties with => new()
        PageTitle = new Label<ContainerDemoPage>(this, "PageTitle");
        UserProfile = new UserProfileContainer(this, "UserProfileBorder");
        Outer = new OuterContainer(this, "OuterBorder");
        // TaskList uses TaskListBorder as container with static items (Task_0, Task_1, Task_2)
        // Item count is determined by iterating Task_0, Task_1, ... until not found
        TaskList = new List<ContainerDemoPage, TaskItemContainer>(
            this,
            "TaskListBorder",  // Use TaskListBorder as the list container
            "Task_",  // Prefix for Task_0, Task_1, Task_2
            (scope, index) => new TaskItemContainer(this, index));
    }

    /// <inheritdoc />
    public override string Name => "ContainerDemoPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Use the control's IsExists, not Control("X").IsExists()
        return PageTitle.IsExists();
    }

    #region Simple Controls
    
    /// <summary>
    /// The page title label.
    /// </summary>
    public Label<ContainerDemoPage> PageTitle { get; }
    
    /// <summary>
    /// The new task entry field.
    /// </summary>
    public Entry<ContainerDemoPage> NewTaskEntry => Entry("NewTaskEntry");
    
    /// <summary>
    /// The add task button.
    /// </summary>
    public Button<ContainerDemoPage> AddTaskButton => Button("AddTaskButton");
    
    /// <summary>
    /// The task count label.
    /// </summary>
    public Label<ContainerDemoPage> TaskCountLabel => Label("TaskCountLabel");
    
    #endregion

    #region Containers (initialized in constructor)

    /// <summary>
    /// The user profile container.
    /// </summary>
    public UserProfileContainer UserProfile { get; }

    /// <summary>
    /// The outer container for nested container testing.
    /// </summary>
    public OuterContainer Outer { get; }

    #endregion

    #region Task List

    /// <summary>
    /// The task list containing task items.
    /// </summary>
    public List<ContainerDemoPage, TaskItemContainer> TaskList { get; }
    
    /// <summary>
    /// Gets a task item by index (convenience method).
    /// </summary>
    public TaskItemContainer TaskItem(int index) => TaskList.Item(index);

    #endregion

    #region Contact Cards (indexed by AutomationId)

    /// <summary>
    /// Gets a contact card by index (0-based).
    /// Demonstrates direct indexed container access without MauiListControl.
    /// </summary>
    public ContactContainer Contact(int index) => new(this, index);

    #endregion
}

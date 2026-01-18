using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests;

/// <summary>
/// Tests for list container patterns using MauiListControl.
/// Demonstrates typed list access and item enumeration.
/// Uses xUnit Assert per SPEC-017b design principles (never FluentAssertions).
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Pattern", "ListContainer")]
public class ListContainerTests
{
    private readonly AppiumFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public ListContainerTests(AppiumFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToContainerDemo();
    }

    #region List Count Tests

    /// <summary>
    /// Verifies initial task count.
    /// </summary>
    [Fact]
    [Trait("Method", "GetItemCount")]
    public void TaskList_GetItemCount_ReturnsExpectedCount()
    {
        // Act
        var count = Page.TaskList.GetItemCount();

        // Assert - should have at least 3 initial tasks
        Assert.True(count >= 3);
    }

    /// <summary>
    /// Verifies AssertItemCount works correctly.
    /// </summary>
    [Fact]
    [Trait("Method", "AssertItemCount")]
    public void TaskList_AssertItemCount_DoesNotThrow()
    {
        // Arrange
        var count = Page.TaskList.GetItemCount();

        // Act & Assert - should not throw
        Page.TaskList.AssertItemCount(count);
    }

    #endregion

    #region Item Access Tests

    /// <summary>
    /// Verifies task items by index exist.
    /// </summary>
    [Fact]
    [Trait("Method", "Item")]
    public void TaskItem_ByIndex_Exists()
    {
        // Assert
        Page.TaskItem(0).AssertExists();
        Page.TaskItem(1).AssertExists();
        Page.TaskItem(2).AssertExists();
    }

    /// <summary>
    /// Verifies accessing individual items by index.
    /// </summary>
    [Fact]
    [Trait("Method", "Item")]
    public void TaskList_Item_ReturnsTypedContainer()
    {
        // Act
        var firstTask = Page.TaskList.Item(0);

        // Assert
        Assert.NotNull(firstTask);
        firstTask.NameLabel.AssertExists();
        firstTask.CheckBox.AssertExists();
    }

    /// <summary>
    /// Verifies item has correct controls.
    /// </summary>
    [Fact]
    [Trait("Pattern", "ItemControls")]
    public void TaskItem_FindsChildren()
    {
        // Arrange
        var task = Page.TaskItem(0);

        // Assert - all expected controls exist
        task.NameLabel.AssertExists();
        task.CheckBox.AssertExists();
        task.DeleteButton.AssertExists();
    }

    /// <summary>
    /// Verifies item has readable text.
    /// </summary>
    [Fact]
    [Trait("Method", "GetText")]
    public void TaskItem_GetName()
    {
        // Arrange
        var task = Page.TaskItem(0);

        // Act
        var name = task.NameLabel.GetText();

        // Assert
        Assert.False(string.IsNullOrEmpty(name));
    }

    /// <summary>
    /// Verifies different task items have different content.
    /// </summary>
    [Fact]
    [Trait("Pattern", "ItemScoping")]
    public void TaskItems_HaveDifferentContent()
    {
        // Act
        var task1 = Page.TaskItem(0).NameLabel.GetText();
        var task2 = Page.TaskItem(1).NameLabel.GetText();

        // Assert
        Assert.NotEqual(task1, task2);
    }

    #endregion

    #region GetAllItems Tests

    /// <summary>
    /// Verifies GetAllItems returns all task containers.
    /// </summary>
    [Fact]
    [Trait("Method", "GetAllItems")]
    public void TaskList_GetAllItems_ReturnsAllTasks()
    {
        // Act
        var tasks = Page.TaskList.GetAllItems();

        // Assert
        Assert.NotEmpty(tasks);
        Assert.Equal(Page.TaskList.GetItemCount(), tasks.Count);
    }

    /// <summary>
    /// Verifies all items have expected structure.
    /// </summary>
    [Fact]
    [Trait("Pattern", "ItemEnumeration")]
    public void TaskList_AllItems_HaveExpectedStructure()
    {
        // Act
        var tasks = Page.TaskList.GetAllItems();

        // Assert - each task has the expected controls
        foreach (var task in tasks)
        {
            task.NameLabel.AssertExists();
            task.DeleteButton.AssertExists();
        }
    }

    #endregion

    #region Add Task Tests

    /// <summary>
    /// Verifies adding a new task increases the count.
    /// </summary>
    [Fact]
    [Trait("Method", "AddTask")]
    public void TaskList_AddTask()
    {
        // Arrange
        var initialCount = Page.TaskList.GetItemCount();

        // Act
        Page.NewTaskEntry.Enter("New test task");
        Page.AddTaskButton.Click();

        // Wait for item to be added
        Page.TaskList.WaitItemCount(initialCount + 1, 2000);

        // Assert
        var newCount = Page.TaskList.GetItemCount();
        Assert.Equal(initialCount + 1, newCount);
    }

    #endregion

    #region Button Click Tests

    /// <summary>
    /// Verifies delete button on task item is clickable.
    /// </summary>
    [Fact]
    [Trait("Method", "Click")]
    public void TaskItem_DeleteButton_IsClickable()
    {
        // Arrange
        var firstTask = Page.TaskList.Item(0);

        // Assert
        firstTask.DeleteButton.AssertClickable();
    }

    /// <summary>
    /// Verifies clicking button returns correct container scope.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentReturn")]
    public void TaskItem_DeleteButton_Click_ReturnsTaskContainer()
    {
        // Arrange - use last item to avoid affecting other tests
        var count = Page.TaskList.GetItemCount();
        var lastTask = Page.TaskList.Item(count - 1);

        // Act - click returns the task item container
        var container = lastTask.DeleteButton.Click();

        // Assert
        Assert.NotNull(container);
    }

    #endregion

    #region Index Property Tests

    /// <summary>
    /// Verifies task items track their index.
    /// </summary>
    [Fact]
    [Trait("Property", "Index")]
    public void TaskItem_Index_IsCorrect()
    {
        // Act & Assert
        Assert.Equal(0, Page.TaskList.Item(0).Index);
        Assert.Equal(1, Page.TaskList.Item(1).Index);
        Assert.Equal(2, Page.TaskList.Item(2).Index);
    }

    #endregion
}

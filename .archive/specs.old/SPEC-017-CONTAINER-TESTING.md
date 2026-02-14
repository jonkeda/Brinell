# SPEC-017: Container Control Testing

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Author:** Copilot

---

## 1. Overview

### 1.1 Purpose

This specification defines the test cases and page objects required to validate the container control implementation in Brinell.Maui. Container controls are scopes that nest child controls and enable scoped element finding.

### 1.2 Scope

- Single container (Frame, Border) with child controls
- CollectionView/ListView with repeating item containers
- Nested containers (containers within containers)
- Container factory methods for page objects

### 1.3 Goals

1. Verify containers correctly scope child element searches
2. Verify containers can access child controls via factory methods
3. Verify list/collection controls can enumerate item containers
4. Verify fluent chaining works across container boundaries
5. Verify container navigation (Parent, Page)

---

## 2. Sample App Requirements

### 2.1 Existing Controls to Test

The MainPage already has containers that can be used for testing:

| AutomationId | Type | Child Controls |
|-------------|------|----------------|
| `CounterFrame` | Frame | CounterLabel, DecrementButton, IncrementButton, ResetButton |
| `TextInputFrame` | Frame | NameEntry, EmailEntry, MessageEditor, GreetingLabel, GreetButton |
| `ToggleFrame` | Frame | NotificationSwitch, NotificationLabel, AgreeCheckBox |
| `SliderFrame` | Frame | VolumeSlider, VolumeLabel, VolumeProgress |
| `PickerFrame` | Frame | ColorPicker, SelectedColorLabel, DatePicker, TimePicker |
| `ActivityFrame` | Frame | LoadingIndicator, ToggleLoadingButton |

### 2.2 New Sample App Page: ContainerDemoPage

A new page is needed to test collection scenarios:

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="Brinell.Samples.Maui.App.Pages.ContainerDemoPage"
             AutomationId="ContainerDemoPage"
             Title="Container Demo">

    <ScrollView AutomationId="ContainerScrollView">
        <VerticalStackLayout Padding="20" Spacing="20">
            
            <!-- Section 1: Single Container -->
            <Frame AutomationId="UserProfileFrame" Padding="15" CornerRadius="10">
                <VerticalStackLayout Spacing="10">
                    <Label AutomationId="ProfileTitle" Text="User Profile" FontSize="18" FontAttributes="Bold" />
                    <Entry AutomationId="ProfileNameEntry" Placeholder="Name" />
                    <Entry AutomationId="ProfileEmailEntry" Placeholder="Email" Keyboard="Email" />
                    <Button AutomationId="ProfileSaveButton" Text="Save Profile" />
                    <Label AutomationId="ProfileStatusLabel" Text="" />
                </VerticalStackLayout>
            </Frame>
            
            <!-- Section 2: Nested Containers -->
            <Frame AutomationId="OuterFrame" Padding="15" CornerRadius="10">
                <VerticalStackLayout Spacing="10">
                    <Label AutomationId="OuterTitle" Text="Outer Container" FontSize="16" FontAttributes="Bold" />
                    
                    <Frame AutomationId="InnerFrame" Padding="10" CornerRadius="5" BackgroundColor="#F0F0F0">
                        <VerticalStackLayout Spacing="5">
                            <Label AutomationId="InnerTitle" Text="Inner Container" FontSize="14" />
                            <Entry AutomationId="InnerEntry" Placeholder="Nested input" />
                            <Button AutomationId="InnerButton" Text="Inner Action" />
                        </VerticalStackLayout>
                    </Frame>
                    
                    <Button AutomationId="OuterButton" Text="Outer Action" />
                </VerticalStackLayout>
            </Frame>
            
            <!-- Section 3: Task List (CollectionView with item containers) -->
            <Frame AutomationId="TaskListFrame" Padding="15" CornerRadius="10">
                <VerticalStackLayout Spacing="10">
                    <Label AutomationId="TaskListTitle" Text="Tasks" FontSize="18" FontAttributes="Bold" />
                    
                    <CollectionView AutomationId="TaskList" ItemsSource="{Binding Tasks}">
                        <CollectionView.ItemTemplate>
                            <DataTemplate>
                                <Frame AutomationId="TaskItem" Padding="10" Margin="0,5" CornerRadius="5">
                                    <HorizontalStackLayout Spacing="10">
                                        <CheckBox AutomationId="TaskCheckBox" IsChecked="{Binding IsCompleted}" />
                                        <Label AutomationId="TaskNameLabel" Text="{Binding Name}" VerticalOptions="Center" />
                                        <Button AutomationId="TaskDeleteButton" Text="X" WidthRequest="40" />
                                    </HorizontalStackLayout>
                                </Frame>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>
                    
                    <HorizontalStackLayout Spacing="10">
                        <Entry AutomationId="NewTaskEntry" Placeholder="New task" HorizontalOptions="FillAndExpand" />
                        <Button AutomationId="AddTaskButton" Text="Add" />
                    </HorizontalStackLayout>
                </VerticalStackLayout>
            </Frame>
            
            <!-- Section 4: Contact Cards (Multiple similar containers) -->
            <Frame AutomationId="ContactsFrame" Padding="15" CornerRadius="10">
                <VerticalStackLayout Spacing="10">
                    <Label AutomationId="ContactsTitle" Text="Contacts" FontSize="18" FontAttributes="Bold" />
                    
                    <Frame AutomationId="Contact_1" Padding="10" Margin="0,5" CornerRadius="5">
                        <VerticalStackLayout>
                            <Label AutomationId="ContactName" Text="Alice Johnson" FontAttributes="Bold" />
                            <Label AutomationId="ContactEmail" Text="alice@example.com" />
                            <Button AutomationId="ContactCallButton" Text="Call" />
                        </VerticalStackLayout>
                    </Frame>
                    
                    <Frame AutomationId="Contact_2" Padding="10" Margin="0,5" CornerRadius="5">
                        <VerticalStackLayout>
                            <Label AutomationId="ContactName" Text="Bob Smith" FontAttributes="Bold" />
                            <Label AutomationId="ContactEmail" Text="bob@example.com" />
                            <Button AutomationId="ContactCallButton" Text="Call" />
                        </VerticalStackLayout>
                    </Frame>
                    
                    <Frame AutomationId="Contact_3" Padding="10" Margin="0,5" CornerRadius="5">
                        <VerticalStackLayout>
                            <Label AutomationId="ContactName" Text="Carol White" FontAttributes="Bold" />
                            <Label AutomationId="ContactEmail" Text="carol@example.com" />
                            <Button AutomationId="ContactCallButton" Text="Call" />
                        </VerticalStackLayout>
                    </Frame>
                </VerticalStackLayout>
            </Frame>
            
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### 2.3 ViewModel for ContainerDemoPage

```csharp
public class ContainerDemoViewModel : INotifyPropertyChanged
{
    public ObservableCollection<TaskItem> Tasks { get; } = new()
    {
        new TaskItem { Name = "Buy groceries", IsCompleted = false },
        new TaskItem { Name = "Walk the dog", IsCompleted = true },
        new TaskItem { Name = "Finish report", IsCompleted = false }
    };
    
    public ICommand AddTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    
    public string NewTaskName { get; set; } = "";
}

public class TaskItem : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public bool IsCompleted { get; set; }
}
```

---

## 3. Page Object Design

### 3.1 Container Page Object: UserProfileContainer

```csharp
/// <summary>
/// Container for the User Profile section.
/// Demonstrates a simple container with child controls.
/// </summary>
public class UserProfileContainer : MauiContainerBase<ContainerDemoPage, UserProfileContainer>
{
    public UserProfileContainer(ContainerDemoPage page)
        : base(page, "UserProfileFrame")
    {
    }
    
    // Child controls scoped within this container
    public MauiControlBase<UserProfileContainer> ProfileTitle => Control("ProfileTitle");
    public MauiEntryControl<UserProfileContainer> NameEntry => Entry("ProfileNameEntry");
    public MauiEntryControl<UserProfileContainer> EmailEntry => Entry("ProfileEmailEntry");
    public MauiButtonControl<UserProfileContainer> SaveButton => Button("ProfileSaveButton");
    public MauiControlBase<UserProfileContainer> StatusLabel => Control("ProfileStatusLabel");
}
```

### 3.2 Nested Container: OuterContainer with InnerContainer

```csharp
/// <summary>
/// Outer container demonstrating nested containers.
/// </summary>
public class OuterContainer : MauiContainerBase<ContainerDemoPage, OuterContainer>
{
    public OuterContainer(ContainerDemoPage page)
        : base(page, "OuterFrame")
    {
    }
    
    public MauiControlBase<OuterContainer> OuterTitle => Control("OuterTitle");
    public MauiButtonControl<OuterContainer> OuterButton => Button("OuterButton");
    
    // Nested container
    public InnerContainer Inner => new(this);
}

/// <summary>
/// Inner container nested within OuterContainer.
/// Parent is OuterContainer, not the page.
/// </summary>
public class InnerContainer : MauiContainerBase<OuterContainer, InnerContainer>
{
    public InnerContainer(OuterContainer parent)
        : base(parent, "InnerFrame")
    {
    }
    
    public MauiControlBase<InnerContainer> InnerTitle => Control("InnerTitle");
    public MauiEntryControl<InnerContainer> InnerEntry => Entry("InnerEntry");
    public MauiButtonControl<InnerContainer> InnerButton => Button("InnerButton");
}
```

### 3.3 Item Container: TaskItemContainer

```csharp
/// <summary>
/// Container representing a single task item in the task list.
/// Can be instantiated for each item in the list.
/// </summary>
public class TaskItemContainer : MauiContainerBase<ContainerDemoPage, TaskItemContainer>
{
    private readonly int _index;
    
    public TaskItemContainer(ContainerDemoPage page, int index)
        : base(page, new Locator(LocatorStrategy.XPath, $"(//Frame[@AutomationId='TaskItem'])[{index + 1}]"))
    {
        _index = index;
    }
    
    public int Index => _index;
    
    // Child controls within this task item
    public MauiControlBase<TaskItemContainer> CheckBox => Control("TaskCheckBox");
    public MauiControlBase<TaskItemContainer> NameLabel => Control("TaskNameLabel");
    public MauiButtonControl<TaskItemContainer> DeleteButton => Button("TaskDeleteButton");
}
```

### 3.4 Contact Container

```csharp
/// <summary>
/// Container representing a single contact card.
/// Uses indexed AutomationId (Contact_1, Contact_2, etc.)
/// </summary>
public class ContactContainer : MauiContainerBase<ContainerDemoPage, ContactContainer>
{
    private readonly int _index;
    
    public ContactContainer(ContainerDemoPage page, int index)
        : base(page, $"Contact_{index}")
    {
        _index = index;
    }
    
    public int Index => _index;
    
    public MauiControlBase<ContactContainer> NameLabel => Control("ContactName");
    public MauiControlBase<ContactContainer> EmailLabel => Control("ContactEmail");
    public MauiButtonControl<ContactContainer> CallButton => Button("ContactCallButton");
}
```

### 3.5 ContainerDemoPage

```csharp
/// <summary>
/// Page object for the Container Demo page.
/// </summary>
public class ContainerDemoPage : MauiPageObjectBase<ContainerDemoPage>
{
    public ContainerDemoPage(IMauiTestContext context)
        : base(context)
    {
    }

    public override string Name => "ContainerDemoPage";

    public override bool IsLoaded(int? timeoutMs = null)
    {
        return Control("ContainerDemoPage").IsExists();
    }

    #region Single Container
    
    /// <summary>
    /// User profile container with form fields.
    /// </summary>
    public UserProfileContainer UserProfile => new(this);
    
    #endregion
    
    #region Nested Containers
    
    /// <summary>
    /// Outer container with nested inner container.
    /// </summary>
    public OuterContainer Outer => new(this);
    
    #endregion
    
    #region Task List
    
    /// <summary>
    /// Entry for adding new tasks.
    /// </summary>
    public MauiEntryControl<ContainerDemoPage> NewTaskEntry => Entry("NewTaskEntry");
    
    /// <summary>
    /// Button to add a new task.
    /// </summary>
    public MauiButtonControl<ContainerDemoPage> AddTaskButton => Button("AddTaskButton");
    
    /// <summary>
    /// Gets a task item container by index (0-based).
    /// </summary>
    public TaskItemContainer TaskItem(int index) => new(this, index);
    
    /// <summary>
    /// Gets the count of task items in the list.
    /// </summary>
    public int GetTaskCount()
    {
        var elements = FindElements(new Locator(LocatorStrategy.AutomationId, "TaskItem"));
        return elements.Count;
    }
    
    #endregion
    
    #region Contacts
    
    /// <summary>
    /// Gets a contact container by index (1-based, matches AutomationId).
    /// </summary>
    public ContactContainer Contact(int index) => new(this, index);
    
    #endregion
}
```

---

## 4. Test Cases

### 4.1 Single Container Tests

```csharp
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Container")]
public class SingleContainerTests
{
    private readonly AppiumFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public SingleContainerTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Container exists and is findable.
    /// </summary>
    [Fact]
    public void Container_IsExists_ReturnsTrue()
    {
        Page.UserProfile.IsExists().Should().BeTrue();
    }

    /// <summary>
    /// Container can find child controls within its scope.
    /// </summary>
    [Fact]
    public void Container_FindsChildControls()
    {
        Page.UserProfile.NameEntry.IsExists().Should().BeTrue();
        Page.UserProfile.EmailEntry.IsExists().Should().BeTrue();
        Page.UserProfile.SaveButton.IsExists().Should().BeTrue();
    }

    /// <summary>
    /// Child controls can be interacted with.
    /// </summary>
    [Fact]
    public void Container_ChildControlInteraction()
    {
        Page.UserProfile.NameEntry
            .Clear()
            .Enter("John Doe");
        
        Page.UserProfile.NameEntry.GetText().Should().Be("John Doe");
    }

    /// <summary>
    /// Fluent chaining works within container.
    /// </summary>
    [Fact]
    public void Container_FluentChaining_WithinContainer()
    {
        Page.UserProfile.NameEntry
            .Clear()
            .Enter("Test User")
            .EmailEntry  // Returns to UserProfileContainer, accesses EmailEntry
            .Clear()
            .Enter("test@example.com")
            .SaveButton
            .Click();
    }

    /// <summary>
    /// Parent navigation returns to page.
    /// </summary>
    [Fact]
    public void Container_Parent_ReturnsToPage()
    {
        var container = Page.UserProfile;
        container.Parent.Should().Be(Page);
    }

    /// <summary>
    /// Container correctly reports Page reference.
    /// </summary>
    [Fact]
    public void Container_Page_ReturnsPageObject()
    {
        Page.UserProfile.Page.Should().Be(Page);
    }
}
```

### 4.2 Nested Container Tests

```csharp
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Container")]
public class NestedContainerTests
{
    private readonly AppiumFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public NestedContainerTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Outer container exists.
    /// </summary>
    [Fact]
    public void OuterContainer_IsExists()
    {
        Page.Outer.IsExists().Should().BeTrue();
    }

    /// <summary>
    /// Inner container exists within outer.
    /// </summary>
    [Fact]
    public void InnerContainer_IsExists()
    {
        Page.Outer.Inner.IsExists().Should().BeTrue();
    }

    /// <summary>
    /// Inner container finds its child controls.
    /// </summary>
    [Fact]
    public void InnerContainer_FindsChildren()
    {
        Page.Outer.Inner.InnerEntry.IsExists().Should().BeTrue();
        Page.Outer.Inner.InnerButton.IsExists().Should().BeTrue();
    }

    /// <summary>
    /// Outer container finds its direct children (not inner's children at root).
    /// </summary>
    [Fact]
    public void OuterContainer_FindsOwnChildren()
    {
        Page.Outer.OuterButton.IsExists().Should().BeTrue();
        Page.Outer.OuterTitle.IsExists().Should().BeTrue();
    }

    /// <summary>
    /// Parent navigation from inner returns outer container.
    /// </summary>
    [Fact]
    public void InnerContainer_Parent_ReturnsOuterContainer()
    {
        var inner = Page.Outer.Inner;
        inner.Parent.Should().Be(Page.Outer);
    }

    /// <summary>
    /// Page reference from nested container returns page.
    /// </summary>
    [Fact]
    public void NestedContainer_Page_ReturnsPageObject()
    {
        Page.Outer.Inner.Page.Should().Be(Page);
    }

    /// <summary>
    /// Deep fluent chaining through nested containers.
    /// </summary>
    [Fact]
    public void NestedContainer_DeepFluentChaining()
    {
        Page.Outer.Inner.InnerEntry
            .Clear()
            .Enter("Nested value")
            .InnerButton
            .Click()
            .Parent  // Returns to OuterContainer
            .OuterButton
            .Click();
    }
}
```

### 4.3 List Item Container Tests

```csharp
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Container")]
public class ListContainerTests
{
    private readonly AppiumFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public ListContainerTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Task list has expected item count.
    /// </summary>
    [Fact]
    public void TaskList_GetCount()
    {
        var count = Page.GetTaskCount();
        count.Should().BeGreaterOrEqualTo(3);
    }

    /// <summary>
    /// Task item container at index exists.
    /// </summary>
    [Fact]
    public void TaskItem_ByIndex_Exists()
    {
        Page.TaskItem(0).IsExists().Should().BeTrue();
        Page.TaskItem(1).IsExists().Should().BeTrue();
        Page.TaskItem(2).IsExists().Should().BeTrue();
    }

    /// <summary>
    /// Task item container finds child controls.
    /// </summary>
    [Fact]
    public void TaskItem_FindsChildren()
    {
        var firstTask = Page.TaskItem(0);
        
        firstTask.NameLabel.IsExists().Should().BeTrue();
        firstTask.CheckBox.IsExists().Should().BeTrue();
        firstTask.DeleteButton.IsExists().Should().BeTrue();
    }

    /// <summary>
    /// Task item can read its name.
    /// </summary>
    [Fact]
    public void TaskItem_GetName()
    {
        var firstTask = Page.TaskItem(0);
        var name = firstTask.NameLabel.GetText();
        name.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Different task items have different content.
    /// </summary>
    [Fact]
    public void TaskItems_HaveDifferentContent()
    {
        var task1 = Page.TaskItem(0).NameLabel.GetText();
        var task2 = Page.TaskItem(1).NameLabel.GetText();
        
        task1.Should().NotBe(task2);
    }

    /// <summary>
    /// Task delete button works.
    /// </summary>
    [Fact]
    public void TaskItem_Delete()
    {
        var initialCount = Page.GetTaskCount();
        
        Page.TaskItem(0).DeleteButton.Click();
        
        // Wait for item to be removed
        Page.WaitReady(2000);
        
        var newCount = Page.GetTaskCount();
        newCount.Should().Be(initialCount - 1);
    }

    /// <summary>
    /// Adding a new task increases count.
    /// </summary>
    [Fact]
    public void TaskList_AddTask()
    {
        var initialCount = Page.GetTaskCount();
        
        Page.NewTaskEntry.Enter("New test task");
        Page.AddTaskButton.Click();
        
        Page.WaitReady(2000);
        
        var newCount = Page.GetTaskCount();
        newCount.Should().Be(initialCount + 1);
    }
}
```

### 4.4 Contact Container Tests (Indexed Containers)

```csharp
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "Container")]
public class IndexedContainerTests
{
    private readonly AppiumFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public IndexedContainerTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Contact containers exist with indexed IDs.
    /// </summary>
    [Fact]
    public void Contact_ByIndex_Exists()
    {
        Page.Contact(1).IsExists().Should().BeTrue();
        Page.Contact(2).IsExists().Should().BeTrue();
        Page.Contact(3).IsExists().Should().BeTrue();
    }

    /// <summary>
    /// Contact container finds child controls.
    /// </summary>
    [Fact]
    public void Contact_FindsChildren()
    {
        var alice = Page.Contact(1);
        
        alice.NameLabel.IsExists().Should().BeTrue();
        alice.EmailLabel.IsExists().Should().BeTrue();
        alice.CallButton.IsExists().Should().BeTrue();
    }

    /// <summary>
    /// Contact has expected name.
    /// </summary>
    [Fact]
    public void Contact_GetName()
    {
        Page.Contact(1).NameLabel.GetText().Should().Be("Alice Johnson");
        Page.Contact(2).NameLabel.GetText().Should().Be("Bob Smith");
        Page.Contact(3).NameLabel.GetText().Should().Be("Carol White");
    }

    /// <summary>
    /// Contact has expected email.
    /// </summary>
    [Fact]
    public void Contact_GetEmail()
    {
        Page.Contact(1).EmailLabel.GetText().Should().Be("alice@example.com");
    }

    /// <summary>
    /// Contact call button is clickable.
    /// </summary>
    [Fact]
    public void Contact_CallButton_IsClickable()
    {
        Page.Contact(1).CallButton.IsClickable().Should().BeTrue();
    }
}
```

---

## 5. Container Scope Verification Tests

### 5.1 Element Scoping Tests

These tests verify that containers correctly scope element searches.

```csharp
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Feature", "Scoping")]
public class ContainerScopingTests
{
    private readonly AppiumFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public ContainerScopingTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Each contact container finds only its own ContactName label,
    /// not the ContactName from other containers.
    /// </summary>
    [Fact]
    public void Container_ScopesSearchToItsRoot()
    {
        // All contacts have a "ContactName" child, but each container
        // should find only its own
        var contact1Name = Page.Contact(1).NameLabel.GetText();
        var contact2Name = Page.Contact(2).NameLabel.GetText();
        var contact3Name = Page.Contact(3).NameLabel.GetText();
        
        // Each should get different text because search is scoped
        contact1Name.Should().Be("Alice Johnson");
        contact2Name.Should().Be("Bob Smith");
        contact3Name.Should().Be("Carol White");
    }

    /// <summary>
    /// Inner container doesn't find outer container's controls.
    /// </summary>
    [Fact]
    public void InnerContainer_DoesNotFindOuterControls()
    {
        // InnerContainer should not find OuterButton
        var innerScope = Page.Outer.Inner;
        
        // This should return null or false because OuterButton 
        // is outside InnerContainer's scope
        var control = new MauiControlBase<InnerContainer>(innerScope, "OuterButton");
        control.IsExists().Should().BeFalse();
    }

    /// <summary>
    /// Outer container doesn't find inner container's controls at root level.
    /// Only finds them through the Inner container.
    /// </summary>
    [Fact]
    public void OuterContainer_FindsNestedControlsViaInner()
    {
        // Accessing InnerEntry through proper nesting works
        Page.Outer.Inner.InnerEntry.IsExists().Should().BeTrue();
    }
}
```

---

## 6. Implementation Tasks

### 6.1 Sample App Changes

| Task | Priority | Effort |
|------|----------|--------|
| Create ContainerDemoPage.xaml | P1 | Medium |
| Create ContainerDemoPage.xaml.cs | P1 | Low |
| Create ContainerDemoViewModel.cs | P1 | Medium |
| Add navigation to ContainerDemoPage | P1 | Low |
| Register in AppShell | P1 | Low |

### 6.2 Test Project Changes

| Task | Priority | Effort |
|------|----------|--------|
| Create ContainerDemoPage.cs (page object) | P1 | Medium |
| Create UserProfileContainer.cs | P1 | Low |
| Create OuterContainer.cs + InnerContainer.cs | P1 | Low |
| Create TaskItemContainer.cs | P1 | Low |
| Create ContactContainer.cs | P1 | Low |
| Create SingleContainerTests.cs | P1 | Medium |
| Create NestedContainerTests.cs | P1 | Medium |
| Create ListContainerTests.cs | P1 | Medium |
| Create IndexedContainerTests.cs | P1 | Low |
| Create ContainerScopingTests.cs | P1 | Medium |
| Update AppiumFixture with ContainerDemoPage | P1 | Low |

---

## 7. Verification Checklist

- [ ] Container.IsExists() returns true for existing containers
- [ ] Container.TryFindElement() scopes search to container root
- [ ] Container.FindElement() throws for elements outside scope
- [ ] Container.FindElements() returns only elements within scope
- [ ] Container.Parent returns parent scope
- [ ] Container.Page returns page object
- [ ] Nested containers scope correctly (inner doesn't find outer's controls)
- [ ] List item containers can be accessed by index
- [ ] Different list items have isolated child controls
- [ ] Fluent chaining works within containers
- [ ] Fluent chaining works across container boundaries

---

## 8. File Locations

### Sample App Files
- `samples/Brinell.Samples.Maui.App/Pages/ContainerDemoPage.xaml`
- `samples/Brinell.Samples.Maui.App/Pages/ContainerDemoPage.xaml.cs`
- `samples/Brinell.Samples.Maui.App/ViewModels/ContainerDemoViewModel.cs`

### Test Project Files
- `testsnew/Brinell.Maui.UITests/Pages/ContainerDemoPage.cs`
- `testsnew/Brinell.Maui.UITests/Containers/UserProfileContainer.cs`
- `testsnew/Brinell.Maui.UITests/Containers/OuterContainer.cs`
- `testsnew/Brinell.Maui.UITests/Containers/InnerContainer.cs`
- `testsnew/Brinell.Maui.UITests/Containers/TaskItemContainer.cs`
- `testsnew/Brinell.Maui.UITests/Containers/ContactContainer.cs`
- `testsnew/Brinell.Maui.UITests/Tests/SingleContainerTests.cs`
- `testsnew/Brinell.Maui.UITests/Tests/NestedContainerTests.cs`
- `testsnew/Brinell.Maui.UITests/Tests/ListContainerTests.cs`
- `testsnew/Brinell.Maui.UITests/Tests/IndexedContainerTests.cs`
- `testsnew/Brinell.Maui.UITests/Tests/ContainerScopingTests.cs`

---

**Status:** Ready for Implementation  
**Estimated Effort:** 6-8 hours  
**Dependencies:** Existing MauiContainerBase implementation


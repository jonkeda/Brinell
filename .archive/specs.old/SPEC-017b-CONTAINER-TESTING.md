# SPEC-017b: Container Control Testing (v2)

**Version:** 2.0  
**Status:** Draft  
**Date:** January 2026  
**Author:** Copilot

---

## 1. Overview

### 1.1 Purpose

This specification defines the test cases and page objects required to validate the container control implementation in srcnew Brinell.Maui. Container controls are scopes that nest child controls and enable scoped element finding.

### 1.2 Scope

- Single container (Frame, Border) with child controls
- Generic `MauiListControl<TItem>` for collection/list scenarios
- Nested containers (containers within containers)
- Container factory methods for page objects

### 1.3 Goals

1. Verify containers correctly scope child element searches
2. Verify containers can access child controls via factory methods
3. Verify generic list controls can enumerate and interact with items
4. Verify fluent chaining works across container boundaries
5. Verify container navigation (Parent, Page)

### 1.4 Design Principles (from src patterns)

1. **Controls are properties, not methods** - Use `=> new()` for simple controls
2. **Containers initialized in constructor** - NOT as lazy properties with `=> new(this)`
3. **Use xUnit Assert** - Never FluentAssertions
4. **PageLocator control defined properly** - Never `Control("X").IsExists()` for IsLoaded
5. **Follow src/Brinell.Maui patterns** - See `ItemsControlBase`, `CollectionViewControl`

---

## 2. Sample App Requirements

### 2.1 New Sample App Page: ContainerDemoPage

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:viewmodels="clr-namespace:Brinell.Samples.Maui.App.ViewModels"
             x:Class="Brinell.Samples.Maui.App.Pages.ContainerDemoPage"
             AutomationId="ContainerDemoPage"
             Title="Container Demo">

    <ContentPage.BindingContext>
        <viewmodels:ContainerDemoViewModel />
    </ContentPage.BindingContext>

    <ScrollView AutomationId="ContainerScrollView">
        <VerticalStackLayout Padding="20" Spacing="20">
            
            <!-- Page Title -->
            <Label AutomationId="PageTitle" 
                   Text="Container Demo" 
                   FontSize="28" 
                   FontAttributes="Bold" />
            
            <!-- Section 1: Single Container (UserProfile) -->
            <Frame AutomationId="UserProfileFrame" Padding="15" CornerRadius="10">
                <VerticalStackLayout Spacing="10">
                    <Label AutomationId="ProfileTitle" Text="User Profile" FontSize="18" FontAttributes="Bold" />
                    <Entry AutomationId="ProfileNameEntry" Placeholder="Name" Text="{Binding ProfileName}" />
                    <Entry AutomationId="ProfileEmailEntry" Placeholder="Email" Keyboard="Email" Text="{Binding ProfileEmail}" />
                    <Button AutomationId="ProfileSaveButton" Text="Save Profile" Command="{Binding SaveProfileCommand}" />
                    <Label AutomationId="ProfileStatusLabel" Text="{Binding ProfileStatus}" />
                </VerticalStackLayout>
            </Frame>
            
            <!-- Section 2: Nested Containers -->
            <Frame AutomationId="OuterFrame" Padding="15" CornerRadius="10">
                <VerticalStackLayout Spacing="10">
                    <Label AutomationId="OuterTitle" Text="Outer Container" FontSize="16" FontAttributes="Bold" />
                    
                    <Frame AutomationId="InnerFrame" Padding="10" CornerRadius="5" BackgroundColor="#F0F0F0">
                        <VerticalStackLayout Spacing="5">
                            <Label AutomationId="InnerTitle" Text="Inner Container" FontSize="14" />
                            <Entry AutomationId="InnerEntry" Placeholder="Nested input" Text="{Binding InnerText}" />
                            <Button AutomationId="InnerButton" Text="Inner Action" Command="{Binding InnerActionCommand}" />
                        </VerticalStackLayout>
                    </Frame>
                    
                    <Button AutomationId="OuterButton" Text="Outer Action" Command="{Binding OuterActionCommand}" />
                </VerticalStackLayout>
            </Frame>
            
            <!-- Section 3: Task List (CollectionView) -->
            <Frame AutomationId="TaskListFrame" Padding="15" CornerRadius="10">
                <VerticalStackLayout Spacing="10">
                    <Label AutomationId="TaskListTitle" Text="Tasks" FontSize="18" FontAttributes="Bold" />
                    
                    <CollectionView AutomationId="TaskList" 
                                    ItemsSource="{Binding Tasks}"
                                    SelectionMode="Single"
                                    SelectedItem="{Binding SelectedTask}">
                        <CollectionView.ItemTemplate>
                            <DataTemplate>
                                <Frame AutomationId="TaskItem" Padding="10" Margin="0,5" CornerRadius="5">
                                    <HorizontalStackLayout Spacing="10">
                                        <CheckBox AutomationId="TaskCheckBox" IsChecked="{Binding IsCompleted}" />
                                        <Label AutomationId="TaskNameLabel" Text="{Binding Name}" VerticalOptions="Center" HorizontalOptions="FillAndExpand" />
                                        <Button AutomationId="TaskDeleteButton" Text="X" WidthRequest="40" Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:ContainerDemoViewModel}}, Path=DeleteTaskCommand}" CommandParameter="{Binding}" />
                                    </HorizontalStackLayout>
                                </Frame>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>
                    
                    <HorizontalStackLayout Spacing="10">
                        <Entry AutomationId="NewTaskEntry" Placeholder="New task" HorizontalOptions="FillAndExpand" Text="{Binding NewTaskName}" />
                        <Button AutomationId="AddTaskButton" Text="Add" Command="{Binding AddTaskCommand}" />
                    </HorizontalStackLayout>
                    
                    <Label AutomationId="TaskCountLabel" Text="{Binding TaskCount, StringFormat='Total: {0} tasks'}" />
                </VerticalStackLayout>
            </Frame>
            
            <!-- Section 4: Contact Cards (Static list with indexed IDs) -->
            <Frame AutomationId="ContactsFrame" Padding="15" CornerRadius="10">
                <VerticalStackLayout Spacing="10">
                    <Label AutomationId="ContactsTitle" Text="Contacts" FontSize="18" FontAttributes="Bold" />
                    
                    <Frame AutomationId="Contact_0" Padding="10" Margin="0,5" CornerRadius="5">
                        <VerticalStackLayout>
                            <Label AutomationId="ContactName" Text="Alice Johnson" FontAttributes="Bold" />
                            <Label AutomationId="ContactEmail" Text="alice@example.com" />
                            <Button AutomationId="ContactCallButton" Text="Call" />
                        </VerticalStackLayout>
                    </Frame>
                    
                    <Frame AutomationId="Contact_1" Padding="10" Margin="0,5" CornerRadius="5">
                        <VerticalStackLayout>
                            <Label AutomationId="ContactName" Text="Bob Smith" FontAttributes="Bold" />
                            <Label AutomationId="ContactEmail" Text="bob@example.com" />
                            <Button AutomationId="ContactCallButton" Text="Call" />
                        </VerticalStackLayout>
                    </Frame>
                    
                    <Frame AutomationId="Contact_2" Padding="10" Margin="0,5" CornerRadius="5">
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

### 2.2 ViewModel for ContainerDemoPage

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.ViewModels;

public class ContainerDemoViewModel : ViewModelBase
{
    private string _profileName = "";
    private string _profileEmail = "";
    private string _profileStatus = "";
    private string _innerText = "";
    private string _newTaskName = "";
    private TaskItem? _selectedTask;

    public ContainerDemoViewModel()
    {
        Tasks = new ObservableCollection<TaskItem>
        {
            new() { Name = "Buy groceries", IsCompleted = false },
            new() { Name = "Walk the dog", IsCompleted = true },
            new() { Name = "Finish report", IsCompleted = false }
        };

        SaveProfileCommand = new AsyncRelayCommand(this, SaveProfileAsync);
        InnerActionCommand = new AsyncRelayCommand(this, InnerActionAsync);
        OuterActionCommand = new AsyncRelayCommand(this, OuterActionAsync);
        AddTaskCommand = new AsyncRelayCommand(this, AddTaskAsync);
        DeleteTaskCommand = new RelayCommand<TaskItem>(this, DeleteTask);
    }

    #region Profile Section

    public string ProfileName
    {
        get => _profileName;
        set => SetProperty(ref _profileName, value);
    }

    public string ProfileEmail
    {
        get => _profileEmail;
        set => SetProperty(ref _profileEmail, value);
    }

    public string ProfileStatus
    {
        get => _profileStatus;
        set => SetProperty(ref _profileStatus, value);
    }

    public IAsyncRelayCommand SaveProfileCommand { get; }

    private async Task SaveProfileAsync()
    {
        await Task.Delay(100);
        ProfileStatus = $"Saved: {ProfileName}";
    }

    #endregion

    #region Nested Containers

    public string InnerText
    {
        get => _innerText;
        set => SetProperty(ref _innerText, value);
    }

    public IAsyncRelayCommand InnerActionCommand { get; }
    public IAsyncRelayCommand OuterActionCommand { get; }

    private async Task InnerActionAsync()
    {
        await Task.Delay(50);
        InnerText = "Inner clicked";
    }

    private async Task OuterActionAsync()
    {
        await Task.Delay(50);
        InnerText = "Outer clicked";
    }

    #endregion

    #region Task List

    public ObservableCollection<TaskItem> Tasks { get; }

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set => SetProperty(ref _selectedTask, value);
    }

    public string NewTaskName
    {
        get => _newTaskName;
        set => SetProperty(ref _newTaskName, value);
    }

    public int TaskCount => Tasks.Count;

    public IAsyncRelayCommand AddTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }

    private async Task AddTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTaskName)) return;
        
        await Task.Delay(50);
        Tasks.Add(new TaskItem { Name = NewTaskName, IsCompleted = false });
        NewTaskName = "";
        OnPropertyChanged(nameof(TaskCount));
    }

    private void DeleteTask(TaskItem? task)
    {
        if (task == null) return;
        Tasks.Remove(task);
        OnPropertyChanged(nameof(TaskCount));
    }

    #endregion
}

public class TaskItem : INotifyPropertyChanged
{
    private string _name = "";
    private bool _isCompleted;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set { _isCompleted = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

---

## 3. srcnew Implementation: MauiListControl

### 3.1 MauiListControl<TItem> - Generic List Control

A generic list control that can enumerate items and access them as typed containers.

```csharp
// srcnew/Brinell.Maui/Controls/MauiListControl.cs

namespace Brinell.Maui.Controls;

/// <summary>
/// Generic list control that finds items and provides typed access to each item container.
/// TItem is a container type that represents each item in the list.
/// </summary>
/// <typeparam name="TScope">The containing scope (page or container).</typeparam>
/// <typeparam name="TItem">The item container type.</typeparam>
public class MauiListControl<TScope, TItem> : MauiControlBase<TScope>
    where TScope : IMauiScope<TScope>
    where TItem : class
{
    private readonly Func<IMauiScope<TScope>, int, TItem> _itemFactory;
    private readonly string _itemLocatorPattern;
    
    /// <summary>
    /// Creates a list control.
    /// </summary>
    /// <param name="scope">The containing scope.</param>
    /// <param name="listLocator">Locator for the list container itself.</param>
    /// <param name="itemLocatorPattern">XPath pattern for finding items (e.g., ".//Frame[@AutomationId='TaskItem']").</param>
    /// <param name="itemFactory">Factory to create item containers. Receives scope and 0-based index.</param>
    public MauiListControl(
        IMauiScope<TScope> scope, 
        Locator listLocator,
        string itemLocatorPattern,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, listLocator)
    {
        _itemLocatorPattern = itemLocatorPattern;
        _itemFactory = itemFactory;
    }
    
    /// <summary>
    /// Creates a list control using automation ID.
    /// </summary>
    public MauiListControl(
        IMauiScope<TScope> scope, 
        string automationId,
        string itemLocatorPattern,
        Func<IMauiScope<TScope>, int, TItem> itemFactory)
        : base(scope, automationId)
    {
        _itemLocatorPattern = itemLocatorPattern;
        _itemFactory = itemFactory;
    }
    
    /// <summary>
    /// Gets the count of items in the list.
    /// </summary>
    public int GetItemCount()
    {
        var listElement = TryFindElement();
        if (listElement == null) return 0;
        
        var items = listElement.FindElements(OpenQA.Selenium.By.XPath(_itemLocatorPattern));
        return items.Count;
    }
    
    /// <summary>
    /// Gets an item container by index (0-based).
    /// </summary>
    public TItem Item(int index)
    {
        return _itemFactory(ContainingScope as IMauiScope<TScope> 
            ?? throw new InvalidOperationException("Scope is not IMauiScope"), index);
    }
    
    /// <summary>
    /// Gets all item containers.
    /// </summary>
    public IReadOnlyList<TItem> GetAllItems()
    {
        var count = GetItemCount();
        var items = new List<TItem>(count);
        for (int i = 0; i < count; i++)
        {
            items.Add(Item(i));
        }
        return items;
    }
    
    /// <summary>
    /// Waits for a specific item count.
    /// </summary>
    public bool WaitItemCount(int expected, int? timeoutMs = null)
    {
        return Poll(() => GetItemCount() == expected, timeoutMs ?? DefaultTimeoutMs);
    }
    
    /// <summary>
    /// Asserts item count matches expected.
    /// </summary>
    public TScope AssertItemCount(int expected, string? message = null, int? timeoutMs = null)
    {
        if (!WaitItemCount(expected, timeoutMs))
        {
            var actual = GetItemCount();
            throw new AssertionException(
                message ?? $"Expected {expected} items but found {actual}. Locator: {Locator}");
        }
        return ContainingScope;
    }
}
```

### 3.2 Usage Example: TaskItemContainer

```csharp
// testsnew/Brinell.Maui.UITests/Containers/TaskItemContainer.cs

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Container representing a single task item in the task list.
/// Uses XPath index to find the specific item.
/// </summary>
public class TaskItemContainer : MauiContainerBase<ContainerDemoPage, TaskItemContainer>
{
    public TaskItemContainer(ContainerDemoPage page, int index)
        : base(page, new Locator(LocatorStrategy.XPath, $"(//Frame[@AutomationId='TaskItem'])[{index + 1}]"))
    {
        Index = index;
        CheckBox = new MauiControlBase<TaskItemContainer>(this, "TaskCheckBox");
        NameLabel = new MauiControlBase<TaskItemContainer>(this, "TaskNameLabel");
        DeleteButton = new MauiButtonControl<TaskItemContainer>(this, "TaskDeleteButton");
    }
    
    public int Index { get; }
    public MauiControlBase<TaskItemContainer> CheckBox { get; }
    public MauiControlBase<TaskItemContainer> NameLabel { get; }
    public MauiButtonControl<TaskItemContainer> DeleteButton { get; }
}
```

---

## 4. Page Object Design

### 4.1 ContainerDemoPage

```csharp
// testsnew/Brinell.Maui.UITests/Pages/ContainerDemoPage.cs

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the Container Demo page.
/// Containers are initialized in constructor, not as lazy properties.
/// </summary>
public class ContainerDemoPage : MauiPageObjectBase<ContainerDemoPage>
{
    public ContainerDemoPage(IMauiTestContext context) : base(context)
    {
        PageTitle = new MauiControlBase<ContainerDemoPage>(this, "PageTitle");
        UserProfile = new UserProfileContainer(this);
        Outer = new OuterContainer(this);
        TaskList = new MauiListControl<ContainerDemoPage, TaskItemContainer>(
            this,
            "TaskList",
            ".//Frame[@AutomationId='TaskItem']",
            (scope, index) => new TaskItemContainer(this, index));
    }

    public override string Name => "ContainerDemoPage";

    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Use the control's IsExists, not Control("X").IsExists()
        return PageTitle.IsExists();
    }

    #region Simple Controls
    
    public MauiControlBase<ContainerDemoPage> PageTitle { get; }
    public MauiEntryControl<ContainerDemoPage> NewTaskEntry => Entry("NewTaskEntry");
    public MauiButtonControl<ContainerDemoPage> AddTaskButton => Button("AddTaskButton");
    public MauiControlBase<ContainerDemoPage> TaskCountLabel => Control("TaskCountLabel");
    
    #endregion
    
    #region Containers (initialized in constructor)
    
    public UserProfileContainer UserProfile { get; }
    public OuterContainer Outer { get; }
    
    #endregion
    
    #region Task List
    
    public MauiListControl<ContainerDemoPage, TaskItemContainer> TaskList { get; }
    
    /// <summary>
    /// Gets a task item by index (convenience method).
    /// </summary>
    public TaskItemContainer TaskItem(int index) => TaskList.Item(index);
    
    #endregion
    
    #region Contact Cards (indexed by AutomationId)
    
    public ContactContainer Contact(int index) => new(this, index);
    
    #endregion
}
```

### 4.2 UserProfileContainer

```csharp
// testsnew/Brinell.Maui.UITests/Containers/UserProfileContainer.cs

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Container for the User Profile section.
/// Child controls initialized in constructor.
/// </summary>
public class UserProfileContainer : MauiContainerBase<ContainerDemoPage, UserProfileContainer>
{
    public UserProfileContainer(ContainerDemoPage page)
        : base(page, "UserProfileFrame")
    {
        ProfileTitle = new MauiControlBase<UserProfileContainer>(this, "ProfileTitle");
        NameEntry = new MauiEntryControl<UserProfileContainer>(this, "ProfileNameEntry");
        EmailEntry = new MauiEntryControl<UserProfileContainer>(this, "ProfileEmailEntry");
        SaveButton = new MauiButtonControl<UserProfileContainer>(this, "ProfileSaveButton");
        StatusLabel = new MauiControlBase<UserProfileContainer>(this, "ProfileStatusLabel");
    }
    
    public MauiControlBase<UserProfileContainer> ProfileTitle { get; }
    public MauiEntryControl<UserProfileContainer> NameEntry { get; }
    public MauiEntryControl<UserProfileContainer> EmailEntry { get; }
    public MauiButtonControl<UserProfileContainer> SaveButton { get; }
    public MauiControlBase<UserProfileContainer> StatusLabel { get; }
}
```

### 4.3 Nested Containers

```csharp
// testsnew/Brinell.Maui.UITests/Containers/OuterContainer.cs

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Outer container with nested inner container.
/// </summary>
public class OuterContainer : MauiContainerBase<ContainerDemoPage, OuterContainer>
{
    public OuterContainer(ContainerDemoPage page)
        : base(page, "OuterFrame")
    {
        OuterTitle = new MauiControlBase<OuterContainer>(this, "OuterTitle");
        OuterButton = new MauiButtonControl<OuterContainer>(this, "OuterButton");
        Inner = new InnerContainer(this);
    }
    
    public MauiControlBase<OuterContainer> OuterTitle { get; }
    public MauiButtonControl<OuterContainer> OuterButton { get; }
    public InnerContainer Inner { get; }
}

// testsnew/Brinell.Maui.UITests/Containers/InnerContainer.cs

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Inner container nested within OuterContainer.
/// Parent is OuterContainer, not the page.
/// </summary>
public class InnerContainer : MauiContainerBase<OuterContainer, InnerContainer>
{
    public InnerContainer(OuterContainer parent)
        : base(parent, "InnerFrame")
    {
        InnerTitle = new MauiControlBase<InnerContainer>(this, "InnerTitle");
        InnerEntry = new MauiEntryControl<InnerContainer>(this, "InnerEntry");
        InnerButton = new MauiButtonControl<InnerContainer>(this, "InnerButton");
    }
    
    public MauiControlBase<InnerContainer> InnerTitle { get; }
    public MauiEntryControl<InnerContainer> InnerEntry { get; }
    public MauiButtonControl<InnerContainer> InnerButton { get; }
}
```

### 4.4 ContactContainer

```csharp
// testsnew/Brinell.Maui.UITests/Containers/ContactContainer.cs

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// Container for a contact card. Uses indexed AutomationId (Contact_0, Contact_1, etc.)
/// </summary>
public class ContactContainer : MauiContainerBase<ContainerDemoPage, ContactContainer>
{
    public ContactContainer(ContainerDemoPage page, int index)
        : base(page, $"Contact_{index}")
    {
        Index = index;
        NameLabel = new MauiControlBase<ContactContainer>(this, "ContactName");
        EmailLabel = new MauiControlBase<ContactContainer>(this, "ContactEmail");
        CallButton = new MauiButtonControl<ContactContainer>(this, "ContactCallButton");
    }
    
    public int Index { get; }
    public MauiControlBase<ContactContainer> NameLabel { get; }
    public MauiControlBase<ContactContainer> EmailLabel { get; }
    public MauiButtonControl<ContactContainer> CallButton { get; }
}
```

---

## 5. Test Cases (using xUnit Assert)

### 5.1 Single Container Tests

```csharp
// testsnew/Brinell.Maui.UITests/Tests/SingleContainerTests.cs

namespace Brinell.Maui.UITests.Tests;

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

    [Fact]
    public void Container_IsExists_ReturnsTrue()
    {
        Page.UserProfile.AssertExists();
    }

    [Fact]
    public void Container_FindsChildControls()
    {
        Page.UserProfile.NameEntry.AssertExists();
        Page.UserProfile.EmailEntry.AssertExists();
        Page.UserProfile.SaveButton.AssertExists();
    }

    [Fact]
    public void Container_ChildControlInteraction()
    {
        Page.UserProfile.NameEntry.Clear().Enter("John Doe");
        
        var text = Page.UserProfile.NameEntry.GetText();
        Assert.Equal("John Doe", text);
    }

    [Fact]
    public void Container_FluentChaining_WithinContainer()
    {
        Page.UserProfile.NameEntry
            .Clear()
            .Enter("Test User")
            .EmailEntry
            .Clear()
            .Enter("test@example.com")
            .SaveButton
            .Click();
        
        // Verify action completed
        Page.UserProfile.StatusLabel.AssertExists();
    }

    [Fact]
    public void Container_Parent_ReturnsPage()
    {
        var parent = Page.UserProfile.Parent;
        Assert.Same(Page, parent);
    }

    [Fact]
    public void Container_Page_ReturnsPageObject()
    {
        var page = Page.UserProfile.Page;
        Assert.Same(Page, page);
    }
}
```

### 5.2 Nested Container Tests

```csharp
// testsnew/Brinell.Maui.UITests/Tests/NestedContainerTests.cs

namespace Brinell.Maui.UITests.Tests;

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

    [Fact]
    public void OuterContainer_IsExists()
    {
        Page.Outer.AssertExists();
    }

    [Fact]
    public void InnerContainer_IsExists()
    {
        Page.Outer.Inner.AssertExists();
    }

    [Fact]
    public void InnerContainer_FindsChildren()
    {
        Page.Outer.Inner.InnerEntry.AssertExists();
        Page.Outer.Inner.InnerButton.AssertExists();
    }

    [Fact]
    public void OuterContainer_FindsOwnChildren()
    {
        Page.Outer.OuterButton.AssertExists();
        Page.Outer.OuterTitle.AssertExists();
    }

    [Fact]
    public void InnerContainer_Parent_ReturnsOuterContainer()
    {
        var parent = Page.Outer.Inner.Parent;
        Assert.Same(Page.Outer, parent);
    }

    [Fact]
    public void NestedContainer_Page_ReturnsPageObject()
    {
        var page = Page.Outer.Inner.Page;
        Assert.Same(Page, page);
    }

    [Fact]
    public void NestedContainer_DeepFluentChaining()
    {
        Page.Outer.Inner.InnerEntry
            .Clear()
            .Enter("Nested value")
            .InnerButton
            .Click();
        
        // Then access outer
        Page.Outer.OuterButton.Click();
        
        Page.Outer.AssertExists();
    }
}
```

### 5.3 List Container Tests

```csharp
// testsnew/Brinell.Maui.UITests/Tests/ListContainerTests.cs

namespace Brinell.Maui.UITests.Tests;

[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "List")]
public class ListContainerTests
{
    private readonly AppiumFixture _fixture;
    private ContainerDemoPage Page => _fixture.ContainerDemoPage;

    public ListContainerTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void TaskList_GetCount()
    {
        var count = Page.TaskList.GetItemCount();
        Assert.True(count >= 3);
    }

    [Fact]
    public void TaskItem_ByIndex_Exists()
    {
        Page.TaskItem(0).AssertExists();
        Page.TaskItem(1).AssertExists();
        Page.TaskItem(2).AssertExists();
    }

    [Fact]
    public void TaskItem_FindsChildren()
    {
        var firstTask = Page.TaskItem(0);
        
        firstTask.NameLabel.AssertExists();
        firstTask.CheckBox.AssertExists();
        firstTask.DeleteButton.AssertExists();
    }

    [Fact]
    public void TaskItem_GetName()
    {
        var firstTask = Page.TaskItem(0);
        var name = firstTask.NameLabel.GetText();
        Assert.False(string.IsNullOrEmpty(name));
    }

    [Fact]
    public void TaskItems_HaveDifferentContent()
    {
        var task1 = Page.TaskItem(0).NameLabel.GetText();
        var task2 = Page.TaskItem(1).NameLabel.GetText();
        
        Assert.NotEqual(task1, task2);
    }

    [Fact]
    public void TaskList_AddTask()
    {
        var initialCount = Page.TaskList.GetItemCount();
        
        Page.NewTaskEntry.Enter("New test task");
        Page.AddTaskButton.Click();
        
        // Wait for item to be added
        Page.TaskList.WaitItemCount(initialCount + 1, 2000);
        
        var newCount = Page.TaskList.GetItemCount();
        Assert.Equal(initialCount + 1, newCount);
    }
}
```

### 5.4 Indexed Container Tests

```csharp
// testsnew/Brinell.Maui.UITests/Tests/IndexedContainerTests.cs

namespace Brinell.Maui.UITests.Tests;

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

    [Fact]
    public void Contact_ByIndex_Exists()
    {
        Page.Contact(0).AssertExists();
        Page.Contact(1).AssertExists();
        Page.Contact(2).AssertExists();
    }

    [Fact]
    public void Contact_FindsChildren()
    {
        var alice = Page.Contact(0);
        
        alice.NameLabel.AssertExists();
        alice.EmailLabel.AssertExists();
        alice.CallButton.AssertExists();
    }

    [Fact]
    public void Contact_GetName()
    {
        Assert.Equal("Alice Johnson", Page.Contact(0).NameLabel.GetText());
        Assert.Equal("Bob Smith", Page.Contact(1).NameLabel.GetText());
        Assert.Equal("Carol White", Page.Contact(2).NameLabel.GetText());
    }

    [Fact]
    public void Contact_GetEmail()
    {
        Assert.Equal("alice@example.com", Page.Contact(0).EmailLabel.GetText());
    }

    [Fact]
    public void Contact_CallButton_IsClickable()
    {
        Page.Contact(0).CallButton.AssertClickable();
    }
}
```

### 5.5 Container Scoping Tests

```csharp
// testsnew/Brinell.Maui.UITests/Tests/ContainerScopingTests.cs

namespace Brinell.Maui.UITests.Tests;

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
    /// Each contact container finds only its own ContactName label.
    /// </summary>
    [Fact]
    public void Container_ScopesSearchToItsRoot()
    {
        var contact0Name = Page.Contact(0).NameLabel.GetText();
        var contact1Name = Page.Contact(1).NameLabel.GetText();
        var contact2Name = Page.Contact(2).NameLabel.GetText();
        
        Assert.Equal("Alice Johnson", contact0Name);
        Assert.Equal("Bob Smith", contact1Name);
        Assert.Equal("Carol White", contact2Name);
    }

    /// <summary>
    /// Inner container doesn't find outer container's controls.
    /// </summary>
    [Fact]
    public void InnerContainer_DoesNotFindOuterControls()
    {
        // Create a control looking for OuterButton within InnerContainer scope
        var innerScope = Page.Outer.Inner;
        var outerButtonInInner = new MauiControlBase<InnerContainer>(innerScope, "OuterButton");
        
        outerButtonInInner.AssertExists(false);
    }

    /// <summary>
    /// Outer container finds nested controls via Inner.
    /// </summary>
    [Fact]
    public void OuterContainer_FindsNestedControlsViaInner()
    {
        Page.Outer.Inner.InnerEntry.AssertExists();
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
| Add TaskItem model class | P1 | Low |
| Register in AppShell | P1 | Low |

### 6.2 srcnew Framework Changes

| Task | Priority | Effort |
|------|----------|--------|
| Create MauiListControl.cs | P1 | Medium |

### 6.3 Test Project Changes

| Task | Priority | Effort |
|------|----------|--------|
| Create ContainerDemoPage.cs (page object) | P1 | Medium |
| Create UserProfileContainer.cs | P1 | Low |
| Create OuterContainer.cs | P1 | Low |
| Create InnerContainer.cs | P1 | Low |
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
- [ ] Container.Parent returns parent scope
- [ ] Container.Page returns page object
- [ ] Nested containers scope correctly (inner doesn't find outer's controls)
- [ ] MauiListControl.GetItemCount() returns correct count
- [ ] MauiListControl.Item(index) returns typed item container
- [ ] List item containers have isolated child controls
- [ ] Fluent chaining works within containers
- [ ] Containers initialized in constructor, not as lazy `=> new()` properties
- [ ] Tests use xUnit Assert, never FluentAssertions

---

## 8. File Locations

### Sample App Files
- `samples/Brinell.Samples.Maui.App/Pages/ContainerDemoPage.xaml`
- `samples/Brinell.Samples.Maui.App/Pages/ContainerDemoPage.xaml.cs`
- `samples/Brinell.Samples.Maui.App/ViewModels/ContainerDemoViewModel.cs`

### srcnew Framework Files
- `srcnew/Brinell.Maui/Controls/MauiListControl.cs`

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

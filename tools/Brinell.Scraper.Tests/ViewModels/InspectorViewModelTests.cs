using Brinell.Scraper.Models;
using Brinell.Scraper.ViewModels;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public sealed class InspectorViewModelTests
{
    [Fact]
    public void LoadSnapshot_PopulatesSnapshotAndElementCount()
    {
        var vm = new InspectorViewModel();
        var snapshot = CreateSnapshot();

        vm.LoadSnapshot(snapshot);

        Assert.Same(snapshot, vm.Snapshot);
        Assert.Equal(5, vm.TotalElementCount);
        Assert.Single(vm.DomTree.RootElements);
        Assert.Same(snapshot.RootElement, vm.DomTree.RootElements[0]);
    }

    [Fact]
    public void ToggleElement_AddsAndRemovesSelection()
    {
        var vm = new InspectorViewModel();
        var element = new DomElement { Tag = "input", Id = "email" };

        vm.ToggleElement(element);
        Assert.Single(vm.SelectedElements);
        Assert.Equal(1, vm.SelectedCount);

        vm.ToggleElement(element);
        Assert.Empty(vm.SelectedElements);
        Assert.Equal(0, vm.SelectedCount);
    }

    [Fact]
    public void ToggleElement_RaisesSelectionChangedEvents()
    {
        var vm = new InspectorViewModel();
        var element = new DomElement { Tag = "button", Id = "save" };
        var events = new List<bool>();
        vm.ElementSelectionChanged += (_, selected) => events.Add(selected);

        vm.ToggleElement(element);
        vm.ToggleElement(element);

        Assert.Equal([true, false], events);
    }

    [Fact]
    public void ClearSelection_ClearsItemsAndRaisesSelectionCleared()
    {
        var vm = new InspectorViewModel();
        vm.LoadSnapshot(CreateSnapshot());
        vm.ToggleElement(vm.Snapshot!.RootElement.Children[0]);
        var fired = 0;
        vm.SelectionCleared += () => fired++;

        vm.ClearSelection();

        Assert.Empty(vm.SelectedElements);
        Assert.Equal(0, vm.SelectedCount);
        Assert.Equal(1, fired);
        Assert.Single(vm.DomTree.RootElements);
        Assert.Same(vm.Snapshot.RootElement, vm.DomTree.RootElements[0]);
    }

    [Fact]
    public void SelectAllForms_SelectsOnlyFormControlTags()
    {
        var vm = new InspectorViewModel();
        vm.LoadSnapshot(CreateSnapshot());

        vm.SelectAllFormsCommand.Execute(null);

        Assert.Equal(3, vm.SelectedCount);
        Assert.All(vm.SelectedElements, element =>
            Assert.Contains(element.Tag, new[] { "input", "select", "button", "textarea" }));
        Assert.True(vm.DomTree.IsFilterActive);
    }

    [Fact]
    public void SelectAllInputs_SelectsOnlyInputs()
    {
        var vm = new InspectorViewModel();
        vm.LoadSnapshot(CreateSnapshot());

        vm.SelectAllInputsCommand.Execute(null);

        Assert.Single(vm.SelectedElements);
        Assert.All(vm.SelectedElements, element => Assert.Equal("input", element.Tag));
        Assert.True(vm.DomTree.IsFilterActive);
    }

    [Fact]
    public void LoadControlGroups_SetsSummaryAndResetsAcceptanceState()
    {
        var vm = new InspectorViewModel();
        var groups = new List<ControlGroupSuggestion>
        {
            new()
            {
                ContainerType = "FormContainer",
                DisplayName = "Form",
                Element = new DomElement { Tag = "form" },
                ChildElements = [new DomElement { Tag = "input", Id = "a" }],
                IsAccepted = true
            },
            new()
            {
                ContainerType = "ListContainer",
                DisplayName = "List",
                Element = new DomElement { Tag = "ul" },
                ChildElements = [new DomElement { Tag = "li", Id = "b" }],
                IsAccepted = false
            }
        };

        vm.LoadControlGroups(groups);

        Assert.Equal("Found 1 form(s), 1 list(s)", vm.ControlGroupSummary);
        Assert.Equal(2, vm.ControlGroups.Count);
        Assert.All(vm.ControlGroups, group => Assert.Null(group.IsAccepted));
    }

    [Fact]
    public void AcceptGroup_SelectsChildrenAndMarksAccepted()
    {
        var vm = new InspectorViewModel();
        var child1 = new DomElement { Tag = "input", Id = "email" };
        var child2 = new DomElement { Tag = "button", Id = "submit" };
        var group = new ControlGroupSuggestion
        {
            ContainerType = "FormContainer",
            DisplayName = "Login Form",
            Element = new DomElement { Tag = "form" },
            ChildElements = [child1, child2]
        };
        vm.LoadControlGroups([group]);

        vm.AcceptGroupCommand.Execute(group);

        Assert.True(group.IsAccepted);
        Assert.Equal(2, vm.SelectedCount);
        Assert.Contains(child1, vm.SelectedElements);
        Assert.Contains(child2, vm.SelectedElements);
    }

    [Fact]
    public void RejectGroup_MarksRejectedWithoutSelectingChildren()
    {
        var vm = new InspectorViewModel();
        var group = new ControlGroupSuggestion
        {
            ContainerType = "FormContainer",
            DisplayName = "Login Form",
            Element = new DomElement { Tag = "form" },
            ChildElements = [new DomElement { Tag = "input", Id = "email" }]
        };
        vm.LoadControlGroups([group]);

        vm.RejectGroupCommand.Execute(group);

        Assert.False(group.IsAccepted);
        Assert.Empty(vm.SelectedElements);
    }

    [Fact]
    public void AcceptAllGroups_AcceptsAllAndDeDuplicatesSelections()
    {
        var vm = new InspectorViewModel();
        var shared = new DomElement { Tag = "button", Id = "submit" };
        var groups = new List<ControlGroupSuggestion>
        {
            new()
            {
                ContainerType = "FormContainer",
                DisplayName = "Form",
                Element = new DomElement { Tag = "form" },
                ChildElements = [shared, new DomElement { Tag = "input", Id = "email" }]
            },
            new()
            {
                ContainerType = "RoleContainer",
                DisplayName = "Dialog",
                Element = new DomElement { Tag = "div", Role = "dialog" },
                ChildElements = [shared]
            }
        };
        vm.LoadControlGroups(groups);

        vm.AcceptAllGroupsCommand.Execute(null);

        Assert.All(vm.ControlGroups, group => Assert.True(group.IsAccepted));
        Assert.Equal(2, vm.SelectedCount);
    }

    [Fact]
    public void DismissGroups_ClearsGroupsAndSummary()
    {
        var vm = new InspectorViewModel();
        vm.LoadControlGroups([
            new ControlGroupSuggestion
            {
                ContainerType = "NavigationContainer",
                DisplayName = "Navigation",
                Element = new DomElement { Tag = "nav" }
            }
        ]);

        vm.DismissGroupsCommand.Execute(null);

        Assert.Empty(vm.ControlGroups);
        Assert.Equal(string.Empty, vm.ControlGroupSummary);
    }

    private static DomSnapshot CreateSnapshot()
    {
        var input = new DomElement { Tag = "input", Id = "email", TextContent = "Email" };
        var select = new DomElement { Tag = "select", Id = "country" };
        var button = new DomElement { Tag = "button", Id = "submit", TextContent = "Save" };
        var div = new DomElement { Tag = "div", Id = "container", TextContent = "Wrapper" };

        return new DomSnapshot
        {
            PageUrl = "https://example.com",
            PageTitle = "Example",
            CapturedAt = DateTimeOffset.UtcNow,
            RootElement = new DomElement
            {
                Tag = "html",
                Children = [input, select, button, div]
            }
        };
    }
}

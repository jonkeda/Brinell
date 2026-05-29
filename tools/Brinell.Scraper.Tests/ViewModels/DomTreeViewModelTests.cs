using Brinell.Scraper.Models;
using Brinell.Scraper.ViewModels;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public sealed class DomTreeViewModelTests
{
    [Fact]
    public void LoadSnapshot_LoadsRootAndClearsFilterState()
    {
        var vm = new DomTreeViewModel();
        var snapshot = CreateSnapshot();
        vm.FilterText = "email";

        vm.LoadSnapshot(snapshot);

        Assert.False(vm.IsFilterActive);
        Assert.Equal(string.Empty, vm.FilterText);
        Assert.Single(vm.RootElements);
        Assert.Same(snapshot.RootElement, vm.RootElements[0]);
    }

    [Fact]
    public void FilterText_MatchingValue_ReducesTreeToMatchingBranches()
    {
        var vm = new DomTreeViewModel();
        vm.LoadSnapshot(CreateSnapshot());

        vm.FilterText = "email";

        Assert.True(vm.IsFilterActive);
        Assert.Single(vm.RootElements);
        var root = vm.RootElements[0];
        Assert.Single(root.Children);
        Assert.Equal("email", root.Children[0].Id);
    }

    [Fact]
    public void ClearingFilter_RestoresFullTree()
    {
        var vm = new DomTreeViewModel();
        var snapshot = CreateSnapshot();
        vm.LoadSnapshot(snapshot);
        vm.FilterText = "email";

        vm.FilterText = string.Empty;

        Assert.False(vm.IsFilterActive);
        Assert.Single(vm.RootElements);
        Assert.Same(snapshot.RootElement, vm.RootElements[0]);
        Assert.Equal(3, vm.RootElements[0].Children.Count);
    }

    [Fact]
    public void ShowFilteredByTags_RetainsOnlyMatchingSubtree()
    {
        var vm = new DomTreeViewModel();
        vm.LoadSnapshot(CreateSnapshot());

        vm.ShowFilteredByTags(["button"]);

        Assert.True(vm.IsFilterActive);
        Assert.Single(vm.RootElements);
        var root = vm.RootElements[0];
        Assert.Single(root.Children);
        Assert.Equal("button", root.Children[0].Tag);
    }

    [Fact]
    public void ElementEvents_AreRaised()
    {
        var vm = new DomTreeViewModel();
        var element = new DomElement { Tag = "button", Id = "save" };
        DomElement? hovered = null;
        DomElement? clicked = null;
        var unhovered = false;

        vm.ElementHovered += el => hovered = el;
        vm.ElementClicked += el => clicked = el;
        vm.ElementUnhovered += () => unhovered = true;

        vm.OnElementHover(element);
        vm.OnElementClick(element);
        vm.OnElementUnhover();

        Assert.Same(element, hovered);
        Assert.Same(element, clicked);
        Assert.True(unhovered);
    }

    private static DomSnapshot CreateSnapshot()
    {
        return new DomSnapshot
        {
            PageUrl = "https://example.com",
            PageTitle = "Example",
            CapturedAt = DateTimeOffset.UtcNow,
            RootElement = new DomElement
            {
                Tag = "html",
                Children =
                [
                    new DomElement { Tag = "input", Id = "email", Placeholder = "Email" },
                    new DomElement { Tag = "button", Id = "save", TextContent = "Save" },
                    new DomElement
                    {
                        Tag = "div",
                        Id = "container",
                        Children = [new DomElement { Tag = "span", TextContent = "Nested content" }]
                    }
                ]
            }
        };
    }
}

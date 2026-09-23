using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Xunit;

namespace Brinell.Scraper.Tests.Services;

public sealed class ControlGroupDetectorTests
{
    private readonly ControlGroupDetector _sut = new();

    [Fact]
    public void Detect_FindsFormContainer()
    {
        var results = _sut.Detect(new DomElement
        {
            Tag = "html",
            Children =
            [
                new DomElement
                {
                    Tag = "form",
                    Id = "loginForm",
                    Children = [new DomElement { Tag = "input", Id = "email" }]
                }
            ]
        });

        var form = Assert.Single(results);
        Assert.Equal("FormContainer", form.ContainerType);
        Assert.Equal("Form: loginForm", form.DisplayName);
        Assert.Single(form.ChildElements);
    }

    [Fact]
    public void Detect_FindsTableOnlyWhenTheadAndTbodyExist()
    {
        var results = _sut.Detect(new DomElement
        {
            Tag = "html",
            Children =
            [
                new DomElement
                {
                    Tag = "table",
                    Id = "fullTable",
                    Children = [new DomElement { Tag = "thead" }, new DomElement { Tag = "tbody" }]
                },
                new DomElement
                {
                    Tag = "table",
                    Id = "partialTable",
                    Children = [new DomElement { Tag = "tbody" }]
                }
            ]
        });

        var table = Assert.Single(results);
        Assert.Equal("TableContainer", table.ContainerType);
        Assert.Equal("Table: fullTable", table.DisplayName);
    }

    [Fact]
    public void Detect_FindsListContainerOnlyForTwoOrMoreItems()
    {
        var results = _sut.Detect(new DomElement
        {
            Tag = "html",
            Children =
            [
                new DomElement
                {
                    Tag = "ul",
                    Id = "single",
                    Children = [new DomElement { Tag = "li", TextContent = "One" }]
                },
                new DomElement
                {
                    Tag = "ol",
                    Id = "multi",
                    Children = [new DomElement { Tag = "li", TextContent = "One" }, new DomElement { Tag = "li", TextContent = "Two" }]
                }
            ]
        });

        var list = Assert.Single(results);
        Assert.Equal("ListContainer", list.ContainerType);
        Assert.Equal("List: multi", list.DisplayName);
        Assert.Equal(2, list.ChildElements.Count);
    }

    [Fact]
    public void Detect_FindsNavigationContainer()
    {
        var results = _sut.Detect(new DomElement
        {
            Tag = "html",
            Children = [new DomElement { Tag = "nav", AriaLabel = "Primary" }]
        });

        var nav = Assert.Single(results);
        Assert.Equal("NavigationContainer", nav.ContainerType);
        Assert.Equal("Primary", nav.DisplayName);
    }

    [Fact]
    public void Detect_FindsFieldsetContainerWhenLegendExists()
    {
        var results = _sut.Detect(new DomElement
        {
            Tag = "html",
            Children =
            [
                new DomElement
                {
                    Tag = "fieldset",
                    Children =
                    [
                        new DomElement { Tag = "legend", TextContent = "Address" },
                        new DomElement { Tag = "input", Id = "street" },
                        new DomElement { Tag = "button", Id = "save" }
                    ]
                }
            ]
        });

        var fieldset = Assert.Single(results);
        Assert.Equal("FieldsetContainer", fieldset.ContainerType);
        Assert.Equal("Address", fieldset.DisplayName);
        Assert.Equal(2, fieldset.ChildElements.Count);
    }

    [Fact]
    public void Detect_FindsRoleContainerForSupportedDivRoles()
    {
        var results = _sut.Detect(new DomElement
        {
            Tag = "html",
            Children = [new DomElement { Tag = "div", Role = "dialog", AriaLabel = "Edit user" }]
        });

        var role = Assert.Single(results);
        Assert.Equal("RoleContainer", role.ContainerType);
        Assert.Equal("dialog: Edit user", role.DisplayName);
    }

    [Fact]
    public void Detect_ReturnsEmptyForPlainDom()
    {
        var results = _sut.Detect(new DomElement
        {
            Tag = "html",
            Children = [new DomElement { Tag = "div", Id = "plain" }]
        });

        Assert.Empty(results);
    }
}

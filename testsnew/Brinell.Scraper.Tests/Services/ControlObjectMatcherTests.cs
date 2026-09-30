using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Xunit;

namespace Brinell.Scraper.Tests.Services;

public sealed class ControlObjectMatcherTests
{
    private static ControlObjectMatcher CreateMatcher() => new(new CssSignatureParser());

    private static DomSnapshot SnapshotOf(DomElement root) =>
        new() { RootElement = root };

    [Fact]
    public void MatchAll_TagOnlySignature_MatchesActionableTagElement()
    {
        var matcher = CreateMatcher();
        var root = new DomElement
        {
            Tag = "body",
            Children =
            {
                new DomElement { Tag = "form" },
            },
        };

        var controls = new List<GeneratedControl>
        {
            new() { Name = "FormCtrl", DomSignature = "form" },
        };

        var matches = matcher.MatchAll(SnapshotOf(root), controls);

        Assert.Single(matches);
        Assert.Equal("FormCtrl", matches[0].Control.Name);
        // Tag-only match: 0.4 (tag) + 0.3 (no classes -> full) + 0.3 (no children chain -> full) = 1.0
        Assert.Equal(1.0, matches[0].Score, precision: 3);
    }

    [Fact]
    public void MatchAll_ClassOverlap_BoostsScoreAboveThreshold()
    {
        var matcher = CreateMatcher();
        var element = new DomElement
        {
            Tag = "div",
            Role = "region",
            ClassName = "card primary highlighted",
        };
        var root = new DomElement
        {
            Tag = "body",
            Children = { element },
        };

        var controls = new List<GeneratedControl>
        {
            new() { Name = "CardCtrl", DomSignature = "div.card.primary" },
        };

        var matches = matcher.MatchAll(SnapshotOf(root), controls);

        Assert.Single(matches);
        // tag(0.4) + class jaccard 2/3 * 0.3 + child(0.3 since no sig children) = 0.4 + 0.2 + 0.3 = 0.9
        Assert.True(matches[0].Score >= 0.75);
    }

    [Fact]
    public void MatchAll_ChildChain_MatchesOrderedChildTags()
    {
        var matcher = CreateMatcher();
        var element = new DomElement
        {
            Tag = "form",
            Children =
            {
                new DomElement { Tag = "label" },
                new DomElement { Tag = "input" },
                new DomElement { Tag = "button" },
            },
        };
        var root = new DomElement { Tag = "body", Children = { element } };

        var controls = new List<GeneratedControl>
        {
            new() { Name = "LoginForm", DomSignature = "form > label > input > button" },
        };

        var matches = matcher.MatchAll(SnapshotOf(root), controls);

        Assert.Single(matches);
        // tag(0.4) + classes(0.3 default) + children 3/3 * 0.3 = 1.0
        Assert.Equal(1.0, matches[0].Score, precision: 3);
    }

    [Fact]
    public void MatchAll_NoMatch_WhenTagDiffersAndNoActionable()
    {
        var matcher = CreateMatcher();
        var root = new DomElement
        {
            Tag = "body",
            Children =
            {
                new DomElement { Tag = "section", ClassName = "card" },
            },
        };

        var controls = new List<GeneratedControl>
        {
            new() { Name = "FormCtrl", DomSignature = "form.login" },
        };

        var matches = matcher.MatchAll(SnapshotOf(root), controls);

        Assert.Empty(matches);
    }

    [Fact]
    public void MatchAll_DeduplicatesPerElement_KeepsHighestScore()
    {
        var matcher = CreateMatcher();
        var element = new DomElement
        {
            Tag = "form",
            ClassName = "login primary",
        };
        var root = new DomElement { Tag = "body", Children = { element } };

        var controls = new List<GeneratedControl>
        {
            new() { Name = "Generic", DomSignature = "form.login" },
            new() { Name = "Specific", DomSignature = "form.login.primary" },
        };

        var matches = matcher.MatchAll(SnapshotOf(root), controls);

        Assert.Single(matches);
        Assert.Equal("Specific", matches[0].Control.Name);
    }

    [Fact]
    public void MatchAll_DeterministicOrdering_OrderedByXPath()
    {
        var matcher = CreateMatcher();
        var root = new DomElement
        {
            Tag = "body",
            Children =
            {
                new DomElement { Tag = "form" },
                new DomElement { Tag = "table" },
            },
        };

        var controls = new List<GeneratedControl>
        {
            new() { Name = "FormCtrl", DomSignature = "form" },
            new() { Name = "TableCtrl", DomSignature = "table" },
        };

        var first = matcher.MatchAll(SnapshotOf(root), controls);
        var second = matcher.MatchAll(SnapshotOf(root), controls);

        Assert.Equal(2, first.Count);
        Assert.Equal(
            first.Select(m => m.XPath).ToList(),
            second.Select(m => m.XPath).ToList());
    }
}

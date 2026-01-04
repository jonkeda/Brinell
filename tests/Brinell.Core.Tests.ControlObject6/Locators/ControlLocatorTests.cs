using Brinell.Core.ControlObject6.Locators;
using FluentAssertions;

namespace Brinell.Core.Tests.ControlObject6.Locators;

/// <summary>
/// Tests for ControlLocator class (CL-001 to CL-010).
/// </summary>
public class ControlLocatorTests
{
    [Fact]
    public void CL001_Constructor_SetsStrategyAndValue()
    {
        // Arrange & Act
        var locator = new ControlLocator(LocatorStrategy.Id, "myButton");

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.Id);
        locator.Value.Should().Be("myButton");
    }

    [Fact]
    public void CL002_Then_CreatesChainedLocator()
    {
        // Arrange
        var parent = new ControlLocator(LocatorStrategy.Id, "container");
        var child = new ControlLocator(LocatorStrategy.ClassName, "button");

        // Act
        var chained = parent.Then(child);

        // Assert
        chained.Parent.Should().Be(parent);
        chained.Strategy.Should().Be(LocatorStrategy.Chained); // Then creates a Chained strategy
        chained.Value.Should().Be("button");
    }

    [Fact]
    public void CL003_WithIndex_SetsIndex()
    {
        // Arrange
        var locator = new ControlLocator(LocatorStrategy.ClassName, "item");

        // Act
        var indexed = locator.WithIndex(3);

        // Assert
        indexed.Index.Should().Be(3);
        indexed.Value.Should().Be("item"); // Original properties preserved
    }

    [Fact]
    public void CL004_First_SetsIndexToZero()
    {
        // Arrange
        var locator = new ControlLocator(LocatorStrategy.ClassName, "item");

        // Act
        var first = locator.First();

        // Assert
        first.Index.Should().Be(0);
    }

    [Fact]
    public void CL005_Last_SetsIndexToMinusOne()
    {
        // Arrange
        var locator = new ControlLocator(LocatorStrategy.ClassName, "item");

        // Act
        var last = locator.Last();

        // Assert
        last.Index.Should().Be(-1);
    }

    [Fact]
    public void CL006_Nth_SetsIndexToN()
    {
        // Arrange
        var locator = new ControlLocator(LocatorStrategy.ClassName, "item");

        // Act - Nth is 1-based, so Nth(5) means the 5th element, which is index 4 (0-based)
        var nth = locator.Nth(5);

        // Assert - Index is 0-based, so Nth(5) sets Index to 4
        nth.Index.Should().Be(4);
    }

    [Fact]
    public void CL007_ImplicitStringConversion_UsesAutomationId()
    {
        // Arrange & Act
        ControlLocator locator = "myControlId";

        // Assert
        locator.Strategy.Should().Be(LocatorStrategy.AutomationId);
        locator.Value.Should().Be("myControlId");
    }

    [Fact]
    public void CL008_ToString_ReturnsReadableFormat()
    {
        // Arrange
        var locator = new ControlLocator(LocatorStrategy.Id, "testButton");

        // Act
        var result = locator.ToString();

        // Assert
        result.Should().Contain("Id");
        result.Should().Contain("testButton");
    }

    [Fact]
    public void CL009_ToString_IncludesParentChain()
    {
        // Arrange
        var parent = new ControlLocator(LocatorStrategy.Id, "container");
        var child = new ControlLocator(LocatorStrategy.ClassName, "button");
        var chained = parent.Then(child);

        // Act
        var result = chained.ToString();

        // Assert
        result.Should().Contain("container");
        result.Should().Contain("button");
    }

    [Fact]
    public void CL010_NullValue_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new ControlLocator(LocatorStrategy.Id, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_PreservesDataAttributeName()
    {
        // Arrange & Act
        var locator = new ControlLocator(LocatorStrategy.DataAttribute, "value", dataAttributeName: "custom-attr");

        // Assert
        locator.DataAttributeName.Should().Be("custom-attr");
    }
}

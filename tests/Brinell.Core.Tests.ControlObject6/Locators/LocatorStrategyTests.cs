using Brinell.Core.ControlObject6.Locators;
using FluentAssertions;

namespace Brinell.Core.Tests.ControlObject6.Locators;

/// <summary>
/// Tests for LocatorStrategy enum (LS-001 to LS-003).
/// </summary>
public class LocatorStrategyTests
{
    [Fact]
    public void LS001_AllEnumValuesAreDefined_Returns17Strategies()
    {
        // Arrange & Act
        var values = Enum.GetValues<LocatorStrategy>();

        // Assert
        values.Should().HaveCount(17);
    }

    [Fact]
    public void LS002_AutomationId_IsDefaultValue()
    {
        // Arrange & Act
        var defaultValue = (int)LocatorStrategy.AutomationId;

        // Assert
        defaultValue.Should().Be(0);
    }

    [Fact]
    public void LS003_EnumValues_AreUnique()
    {
        // Arrange
        var values = Enum.GetValues<LocatorStrategy>();

        // Act
        var uniqueValues = values.Cast<int>().Distinct();

        // Assert
        uniqueValues.Should().HaveCount(values.Length, "all enum values should be unique");
    }

    [Theory]
    [InlineData(LocatorStrategy.AutomationId)]
    [InlineData(LocatorStrategy.Id)]
    [InlineData(LocatorStrategy.Name)]
    [InlineData(LocatorStrategy.ClassName)]
    [InlineData(LocatorStrategy.XPath)]
    [InlineData(LocatorStrategy.Css)]
    [InlineData(LocatorStrategy.Text)]
    [InlineData(LocatorStrategy.PartialText)]
    [InlineData(LocatorStrategy.AccessibilityId)]
    [InlineData(LocatorStrategy.TagName)]
    [InlineData(LocatorStrategy.Label)]
    [InlineData(LocatorStrategy.Placeholder)]
    [InlineData(LocatorStrategy.Title)]
    [InlineData(LocatorStrategy.Role)]
    [InlineData(LocatorStrategy.TestId)]
    [InlineData(LocatorStrategy.DataAttribute)]
    [InlineData(LocatorStrategy.Chained)]
    public void AllStrategies_ExistInEnum(LocatorStrategy strategy)
    {
        // Assert - if this compiles and runs, the strategy exists
        strategy.Should().BeDefined();
    }
}

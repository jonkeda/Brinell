using System.Reflection;
using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using FluentAssertions;

namespace Brinell.Core.Tests.ControlObject6.Interfaces;

/// <summary>
/// Tests for IControlObject interface contract (ICO-001 to ICO-008).
/// </summary>
public class IControlObjectTests
{
    private readonly Type _interfaceType = typeof(IControlObject);

    [Fact]
    public void ICO001_Interface_DefinesLocatorProperty()
    {
        // Arrange & Act
        var property = _interfaceType.GetProperty("Locator");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<ControlLocator>();
        property.CanRead.Should().BeTrue();
    }

    [Fact]
    public void ICO002_Interface_DefinesPageProperty()
    {
        // Arrange & Act
        var property = _interfaceType.GetProperty("Page");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<IPageObject>();
        property.CanRead.Should().BeTrue();
    }

    [Fact]
    public void ICO003_Interface_DefinesIsExistsMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("IsExists");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<bool>();
    }

    [Fact]
    public void ICO004_Interface_DefinesWaitExistsMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("WaitExists");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<bool>();
        method.GetParameters().Should().HaveCount(2);
        method.GetParameters()[0].ParameterType.Should().Be<bool?>();
        method.GetParameters()[1].ParameterType.Should().Be<int?>();
    }

    [Fact]
    public void ICO005_Interface_DefinesCheckExistsMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("CheckExists");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void ICO006_Interface_DefinesAssertExistsMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("AssertExists");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
        method.GetParameters().Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public void ICO007_Interface_DefinesAllVisibilityMethods()
    {
        // Assert
        _interfaceType.GetMethod("IsVisible").Should().NotBeNull();
        _interfaceType.GetMethod("WaitVisible").Should().NotBeNull();
        _interfaceType.GetMethod("CheckVisible").Should().NotBeNull();
        _interfaceType.GetMethod("AssertVisible").Should().NotBeNull();
    }

    [Fact]
    public void ICO008_Interface_DefinesAllTextMethods()
    {
        // Assert
        _interfaceType.GetMethod("GetText").Should().NotBeNull();
        _interfaceType.GetMethod("AssertText").Should().NotBeNull();
        _interfaceType.GetMethod("AssertTextContains").Should().NotBeNull();
        _interfaceType.GetMethod("AssertTextStartsWith").Should().NotBeNull();
        _interfaceType.GetMethod("AssertTextEndsWith").Should().NotBeNull();
        _interfaceType.GetMethod("AssertTextMatches").Should().NotBeNull();
        _interfaceType.GetMethod("AssertTextEmpty").Should().NotBeNull();
    }
}

using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using FluentAssertions;

namespace Brinell.Core.Tests.ControlObject6.Interfaces;

/// <summary>
/// Tests for IPageObject interface contract (IPO-001 to IPO-007).
/// </summary>
public class IPageObjectTests
{
    private readonly Type _interfaceType = typeof(IPageObject);

    [Fact]
    public void IPO001_Interface_DefinesNameProperty()
    {
        // Arrange & Act
        var property = _interfaceType.GetProperty("Name");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
        property.CanRead.Should().BeTrue();
    }

    [Fact]
    public void IPO002_Interface_DefinesIsLoadedMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("IsLoaded");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<bool>();
    }

    [Fact]
    public void IPO003_Interface_ControlExistsMethod_Defined()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("ControlExists");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<bool>();
    }

    [Fact]
    public void IPO004_Interface_DefinesWaitControlExistsMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("WaitControlExists");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<bool>();
    }

    [Fact]
    public void IPO005_Interface_DefinesAssertControlExistsMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("AssertControlExists");

        // Assert
        method.Should().NotBeNull();
    }

    [Fact]
    public void IPO006_Interface_DefinesTakeScreenshotMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("TakeScreenshot");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void IPO007_Interface_DefinesScrollToControlMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("ScrollToControl");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }
}

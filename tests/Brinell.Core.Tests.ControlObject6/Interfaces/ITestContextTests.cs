using Brinell.Core.ControlObject6.Interfaces;
using FluentAssertions;

namespace Brinell.Core.Tests.ControlObject6.Interfaces;

/// <summary>
/// Tests for ITestContext interface contract (ITC-001 to ITC-009).
/// Note: Using "ITestContext" prefix to match test case IDs in documentation.
/// </summary>
public class ITestContextTests
{
    private readonly Type _interfaceType = typeof(ITestContext);

    [Fact]
    public void ITC_001_Interface_DefinesDefaultTimeoutMsProperty()
    {
        // Arrange & Act
        var property = _interfaceType.GetProperty("DefaultTimeoutMs");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<int>();
        property.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeTrue();
    }

    [Fact]
    public void ITC_002_Interface_DefinesDefaultPollingIntervalMsProperty()
    {
        // Arrange & Act
        var property = _interfaceType.GetProperty("DefaultPollingIntervalMs");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<int>();
        property.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeTrue();
    }

    [Fact]
    public void ITC_003_Interface_DefinesCurrentPageProperty()
    {
        // Arrange & Act
        var property = _interfaceType.GetProperty("CurrentPage");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<IPageObject>();
        property.CanRead.Should().BeTrue();
    }

    [Fact]
    public void ITC_004_Interface_DefinesNavigateToStringMethod()
    {
        // Arrange & Act
        var methods = _interfaceType.GetMethods().Where(m => m.Name == "NavigateTo" && !m.IsGenericMethod);

        // Assert
        methods.Should().NotBeEmpty();
    }

    [Fact]
    public void ITC_005_Interface_DefinesNavigateToGenericMethod()
    {
        // Arrange & Act
        var methods = _interfaceType.GetMethods().Where(m => m.Name == "NavigateTo" && m.IsGenericMethod);

        // Assert
        methods.Should().NotBeEmpty();
    }

    [Fact]
    public void ITC_007_Interface_DefinesTakeScreenshotMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("TakeScreenshot");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void ITC_008_Interface_DefinesLogMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("Log");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void ITC_009_Interface_DefinesLogErrorMethod()
    {
        // Arrange & Act
        var method = _interfaceType.GetMethod("LogError");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }
}

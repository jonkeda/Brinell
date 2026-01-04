using Brinell.Maui.Tests.ControlObject6.Mocks;
using OpenQA.Selenium;

namespace Brinell.Maui.Tests.ControlObject6.Context;

/// <summary>
/// Tests for TestableMauiTestContext (MTC-001 to MTC-034).
/// Uses the testable wrapper approach to avoid Moq issues with non-virtual AppiumDriver members.
/// </summary>
public class MauiTestContextTests
{
    #region Constructor and Properties (MTC-001 to MTC-007)

    [Fact]
    public void MTC001_Constructor_WithNullDriver_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new TestableMauiTestContext(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MTC002_Constructor_SetsDriverProperty()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();

        // Act
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Assert
        context.DriverWrapper.Should().NotBeNull();
        context.DriverWrapper.Should().BeSameAs(mockDriverWrapper.Object);
    }

    [Fact]
    public void MTC003_DefaultTimeoutMs_DefaultIs30000()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act & Assert
        context.DefaultTimeoutMs.Should().Be(30000);
    }

    [Fact]
    public void MTC004_DefaultPollingIntervalMs_DefaultIs100()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act & Assert
        context.DefaultPollingIntervalMs.Should().Be(100);
    }

    [Fact]
    public void MTC005_CurrentPage_IsNullInitially()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act & Assert
        context.CurrentPage.Should().BeNull();
    }

    [Fact]
    public void MTC006_DefaultTimeoutMs_CanBeChanged()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        context.DefaultTimeoutMs = 60000;

        // Assert
        context.DefaultTimeoutMs.Should().Be(60000);
    }

    [Fact]
    public void MTC007_DefaultPollingIntervalMs_CanBeChanged()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        context.DefaultPollingIntervalMs = 200;

        // Assert
        context.DefaultPollingIntervalMs.Should().Be(200);
    }

    #endregion

    #region Navigation (MTC-010 to MTC-014)

    [Fact]
    public void MTC010_NavigateTo_WithNull_DoesNothing()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act & Assert - should not throw
        Action act = () => context.NavigateTo(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void MTC011_NavigateTo_WithRoute_Navigates()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var mockNavigation = new Mock<INavigation>();
        mockDriverWrapper.Setup(d => d.Navigate()).Returns(mockNavigation.Object);

        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act
        context.NavigateTo("/home");

        // Assert
        mockNavigation.Verify(n => n.GoToUrl(It.Is<string>(s => s == "/home")), Times.Once);
    }

    #endregion

    #region Screenshot and Logging (MTC-030 to MTC-034)

    [Fact]
    public void MTC030_TakeScreenshot_WithNull_DoesNothing()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act & Assert - should not throw
        Action act = () => context.TakeScreenshot(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void MTC032_Log_WithNull_DoesNothing()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act & Assert - should not throw
        Action act = () => context.Log(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void MTC033_Log_WithMessage_DoesNotThrow()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act & Assert - should not throw
        Action act = () => context.Log("Test message");
        act.Should().NotThrow();
    }

    [Fact]
    public void MTC034_LogError_WithMessage_DoesNotThrow()
    {
        // Arrange
        var mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriverWrapper.Object);

        // Act & Assert - should not throw
        Action act = () => context.LogError("Error message");
        act.Should().NotThrow();
    }

    #endregion
}

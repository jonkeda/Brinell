using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Controls;

/// <summary>
/// Unit tests for TestableImageControl.
/// Tests cover source, dimensions, loading state, and assertions.
/// Test IDs: IM-001 to IM-012
/// </summary>
public class ImageControlTests
{
    #region Source Operations (IM-001 to IM-004)

    [Fact]
    public void IM001_GetSource_ReturnsImageSource()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "profileImage");

        // Act
        var source = control.GetSource();

        // Assert
        source.Should().Be("image.png"); // Default
    }

    [Fact]
    public void IM002_GetSource_AfterSetting_ReturnsNewSource()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "profileImage");
        control.SetSource("avatar.jpg");

        // Act
        var source = control.GetSource();

        // Assert
        source.Should().Be("avatar.jpg");
    }

    [Fact]
    public void IM003_HasSource_WhenSourceSet_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "image");
        control.SetSource("photo.png");

        // Act
        var hasSource = control.HasSource();

        // Assert
        hasSource.Should().BeTrue();
    }

    [Fact]
    public void IM004_HasSource_WhenSourceNull_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "image");
        control.SetSource(null);

        // Act
        var hasSource = control.HasSource();

        // Assert
        hasSource.Should().BeFalse();
    }

    #endregion

    #region Dimensions (IM-005 to IM-007)

    [Fact]
    public void IM005_GetDimensions_ReturnsWidthAndHeight()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "image");
        control.SetDimensions(200, 150);

        // Act
        var (width, height) = control.GetDimensions();

        // Assert
        width.Should().Be(200);
        height.Should().Be(150);
    }

    [Fact]
    public void IM006_AssertDimensions_WhenMatch_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "image");
        control.SetDimensions(100, 100);

        // Act & Assert - Should not throw
        control.AssertDimensions(100, 100);
    }

    [Fact]
    public void IM007_AssertDimensions_WhenMismatch_ThrowsAssertionException()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "image");
        control.SetDimensions(100, 100);

        // Act & Assert
        var action = () => control.AssertDimensions(200, null);
        action.Should().Throw<AssertionException>()
            .WithMessage("*Expected width*");
    }

    #endregion

    #region Loading State (IM-008 to IM-010)

    [Fact]
    public void IM008_IsLoading_WhenNotLoading_ReturnsFalse()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "image");
        control.SetLoading(false);

        // Act
        var isLoading = control.IsLoading();

        // Assert
        isLoading.Should().BeFalse();
    }

    [Fact]
    public void IM009_IsLoading_WhenLoading_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "image");
        control.SetLoading(true);

        // Act
        var isLoading = control.IsLoading();

        // Assert
        isLoading.Should().BeTrue();
    }

    [Fact]
    public void IM010_WaitLoaded_WhenNotLoading_ReturnsTrue()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "image");
        control.SetLoading(false);

        // Act
        var result = control.WaitLoaded();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Assertions (IM-011 to IM-012)

    [Fact]
    public void IM011_AssertSource_WhenMatches_Passes()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "image");
        control.SetSource("expected.png");

        // Act & Assert - Should not throw
        control.AssertSource("expected.png");
    }

    [Fact]
    public void IM012_AssertLoaded_WhenStillLoading_ThrowsAssertionException()
    {
        // Arrange
        var mockDriver = MockAppiumFactory.CreateMockDriverWrapper();
        var context = new TestableMauiTestContext(mockDriver.Object);

        var control = new TestableImageControl(context, "image");
        control.SetLoading(true);

        // Act & Assert
        var action = () => control.AssertLoaded();
        action.Should().Throw<AssertionException>()
            .WithMessage("*loading*");
    }

    #endregion
}

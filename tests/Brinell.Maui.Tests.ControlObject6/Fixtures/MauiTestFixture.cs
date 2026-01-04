using Brinell.Maui.Tests.ControlObject6.Mocks;

namespace Brinell.Maui.Tests.ControlObject6.Fixtures;

/// <summary>
/// Shared fixture for MAUI tests providing mock driver and context.
/// Uses testable wrappers to avoid Moq issues with non-virtual members.
/// </summary>
public class MauiTestFixture : IDisposable
{
    private readonly Mock<IAppiumDriverWrapper> _mockDriverWrapper;
    private readonly TestableMauiTestContext _context;

    /// <summary>
    /// Gets the mock driver wrapper for test configuration.
    /// </summary>
    public Mock<IAppiumDriverWrapper> MockDriverWrapper => _mockDriverWrapper;

    /// <summary>
    /// Gets the testable context for control creation.
    /// </summary>
    public TestableMauiTestContext Context => _context;

    public MauiTestFixture()
    {
        _mockDriverWrapper = MockAppiumFactory.CreateMockDriverWrapper();
        _context = new TestableMauiTestContext(_mockDriverWrapper.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
        GC.SuppressFinalize(this);
    }
}

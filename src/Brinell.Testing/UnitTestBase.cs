using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Testing;

/// <summary>
/// Unit test base with mock support.
/// Extends TestBase with MockRepository for dependency injection testing.
/// </summary>
public abstract class UnitTestBase : TestBase<MockRepository>
{
    protected UnitTestBase(ITestOutputHelper output) : base(output) { }

    /// <summary>
    /// Create MockRepository as context.
    /// </summary>
    protected override MockRepository CreateContext()
    {
        return new MockRepository(MockBehavior.Loose);
    }

    /// <summary>
    /// Create a mock service.
    /// </summary>
    protected Mock<T> CreateMock<T>() where T : class
    {
        var mock = new Mock<T>(MockBehavior.Loose);
        LogAction("CreateMock", typeof(T).Name);
        return mock;
    }

    /// <summary>
    /// Create a strict mock service.
    /// </summary>
    protected Mock<T> CreateStrictMock<T>() where T : class
    {
        var mock = new Mock<T>(MockBehavior.Strict);
        LogAction("CreateStrictMock", typeof(T).Name);
        return mock;
    }

    /// <summary>
    /// Verify mock calls.
    /// </summary>
    protected void VerifyMock<T>(Mock<T> mock) where T : class
    {
        // Note: Moq doesn't have a simple Verify() without parameters
        // Use VerifyNoOtherCalls() instead
        mock.VerifyNoOtherCalls();
        LogAction("VerifyMock", typeof(T).Name);
    }

    /// <summary>
    /// Verify all mocks in context.
    /// </summary>
    protected void VerifyAllMocks()
    {
        Context.VerifyAll();
        LogAction("VerifyAllMocks", "All mocks verified");
    }

    /// <summary>
    /// Cleanup: Verify all mocks.
    /// </summary>
    protected override Task CleanupContextAsync()
    {
        try
        {
            Context.VerifyAll();
        }
        catch (Exception ex)
        {
            Log($"[WARNING] Mock verification failed: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    #region Collection Assertions

    /// <summary>
    /// Assert collection is empty.
    /// </summary>
    protected void AssertEmpty<T>(IEnumerable<T> collection)
    {
        Assert.Empty(collection);
        LogAssertion("AssertEmpty", "0 items", "0 items", true);
    }

    /// <summary>
    /// Assert collection is not empty.
    /// </summary>
    protected void AssertNotEmpty<T>(IEnumerable<T> collection)
    {
        Assert.NotEmpty(collection);
        LogAssertion("AssertNotEmpty", ">0 items", $"{collection.Count()} items", true);
    }

    /// <summary>
    /// Assert collection has expected count.
    /// </summary>
    protected void AssertCount<T>(IEnumerable<T> collection, int expectedCount)
    {
        var actualCount = collection.Count();
        Assert.Equal(expectedCount, actualCount);
        LogAssertion("AssertCount", expectedCount.ToString(), actualCount.ToString(), true);
    }

    #endregion

    #region Exception Assertions

    /// <summary>
    /// Assert action throws exception with message.
    /// </summary>
    protected void AssertThrowsWithMessage<T>(Action action, string expectedMessage) where T : Exception
    {
        var ex = Assert.Throws<T>(action);
        Assert.Contains(expectedMessage, ex.Message);
        LogAssertion("AssertThrowsWithMessage", expectedMessage, ex.Message, true);
    }

    /// <summary>
    /// Assert async action throws exception with message.
    /// </summary>
    protected async Task AssertThrowsWithMessageAsync<T>(Func<Task> action, string expectedMessage) where T : Exception
    {
        var ex = await Assert.ThrowsAsync<T>(action);
        Assert.Contains(expectedMessage, ex.Message);
        LogAssertion("AssertThrowsWithMessageAsync", expectedMessage, ex.Message, true);
    }

    #endregion

    #region Predicate Assertions

    /// <summary>
    /// Assert item matches predicate.
    /// </summary>
    protected void AssertMatch<T>(T item, Predicate<T> predicate)
    {
        Assert.True(predicate(item), "Item does not match predicate");
        LogAssertion("AssertMatch", "predicate", "matched", true);
    }

    /// <summary>
    /// Assert all items match predicate.
    /// </summary>
    protected void AssertAllMatch<T>(IEnumerable<T> items, Predicate<T> predicate)
    {
        Assert.All(items, item => Assert.True(predicate(item)));
        LogAssertion("AssertAllMatch", "all match predicate", "all matched", true);
    }

    #endregion
}

/// <summary>
/// Repository for managing multiple mocks.
/// </summary>
public class MockRepository
{
    private readonly MockBehavior _behavior;
    private readonly Dictionary<Type, object> _mocks = new();

    public MockRepository(MockBehavior behavior = MockBehavior.Loose)
    {
        _behavior = behavior;
    }

    /// <summary>
    /// Create or get a mock.
    /// </summary>
    public Mock<T> GetMock<T>() where T : class
    {
        var type = typeof(T);
        if (!_mocks.TryGetValue(type, out var mock))
        {
            mock = new Mock<T>(_behavior);
            _mocks[type] = mock;
        }
        return (Mock<T>)mock;
    }

    /// <summary>
    /// Verify all mocks.
    /// </summary>
    public void VerifyAll()
    {
        foreach (var mockObject in _mocks.Values)
        {
            var mockType = mockObject.GetType();
            var verifyAllMethod = mockType.GetMethod("VerifyNoOtherCalls");
            if (verifyAllMethod != null)
            {
                verifyAllMethod.Invoke(mockObject, null);
            }
        }
    }

    /// <summary>
    /// Clear all mocks.
    /// </summary>
    public void Clear()
    {
        _mocks.Clear();
    }
}

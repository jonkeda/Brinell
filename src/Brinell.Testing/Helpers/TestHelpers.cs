using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Linq.Expressions;
using System.Reflection;

namespace Brinell.Testing.Helpers;

/// <summary>
/// Fluent builder for creating mocked services with dependencies.
/// Simplifies service registration in tests.
/// </summary>
/// <example>
/// <code>
/// var builder = new MockServiceBuilder()
///     .AddMock<IUserService>()
///     .AddMock<IEmailService>()
///     .Configure<IUserService>((mock) =>
///     {
///         mock.Setup(s => s.GetUserAsync(It.IsAny<int>()))
///             .ReturnsAsync(new User { Id = 1, Name = "John" });
///     });
/// 
/// var services = builder.Build();
/// var userService = services.GetRequiredService<IUserService>();
/// </code>
/// </example>
public class MockServiceBuilder
{
    private readonly IServiceCollection _services;
    private readonly Dictionary<Type, Mock> _mocks = new();

    public MockServiceBuilder()
    {
        _services = new ServiceCollection();
    }

    /// <summary>
    /// Register a mocked service.
    /// </summary>
    public MockServiceBuilder AddMock<TService>() where TService : class
    {
        var mock = new Mock<TService>();
        _mocks[typeof(TService)] = mock;
        _services.AddSingleton(mock.Object);
        return this;
    }

    /// <summary>
    /// Register a mocked service with strict behavior.
    /// </summary>
    public MockServiceBuilder AddStrictMock<TService>() where TService : class
    {
        var mock = new Mock<TService>(MockBehavior.Strict);
        _mocks[typeof(TService)] = mock;
        _services.AddSingleton(mock.Object);
        return this;
    }

    /// <summary>
    /// Configure a registered mock.
    /// </summary>
    public MockServiceBuilder Configure<TService>(Action<Mock<TService>> configurator) where TService : class
    {
        if (_mocks.TryGetValue(typeof(TService), out var mock))
        {
            configurator((Mock<TService>)mock);
        }
        return this;
    }

    /// <summary>
    /// Register a real service.
    /// </summary>
    public MockServiceBuilder AddService<TService>(TService implementation) where TService : class
    {
        _services.AddSingleton(implementation);
        return this;
    }

    /// <summary>
    /// Add service factory registration.
    /// </summary>
    public MockServiceBuilder AddService<TService>(Func<IServiceProvider, TService> factory) where TService : class
    {
        _services.AddSingleton(factory);
        return this;
    }

    /// <summary>
    /// Get a registered mock for assertion.
    /// </summary>
    public Mock<TService> GetMock<TService>() where TService : class
    {
        if (_mocks.TryGetValue(typeof(TService), out var mock))
        {
            return (Mock<TService>)mock;
        }
        throw new InvalidOperationException($"Mock for {typeof(TService).Name} not registered");
    }

    /// <summary>
    /// Verify all mocks.
    /// </summary>
    public void VerifyAll()
    {
        foreach (var mock in _mocks.Values)
        {
            mock.Verify();
        }
    }

    /// <summary>
    /// Build service provider.
    /// </summary>
    public IServiceProvider Build()
    {
        return _services.BuildServiceProvider();
    }
}

/// <summary>
/// Container for managing multiple mocks with auto-mocking support.
/// Automatically creates mocks for requested types.
/// </summary>
/// <example>
/// <code>
/// var container = new AutoMockContainer();
/// var userService = container.Create<IUserService>();
/// var emailService = container.Create<IEmailService>();
/// 
/// container.GetMock<IUserService>()
///     .Setup(s => s.GetUserAsync(1))
///     .ReturnsAsync(new User { Id = 1 });
/// 
/// container.VerifyAll();
/// </code>
/// </example>
public class AutoMockContainer
{
    private readonly Dictionary<Type, Mock> _mocks = new();
    private readonly MockBehavior _behavior;

    public AutoMockContainer(MockBehavior behavior = MockBehavior.Loose)
    {
        _behavior = behavior;
    }

    /// <summary>
    /// Create or get mock instance.
    /// </summary>
    public T Create<T>() where T : class
    {
        var type = typeof(T);
        if (!_mocks.TryGetValue(type, out var mock))
        {
            mock = new Mock<T>(_behavior);
            _mocks[type] = mock;
        }
        return ((Mock<T>)mock).Object;
    }

    /// <summary>
    /// Get mock for configuration.
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
        foreach (var mock in _mocks.Values)
        {
            mock.Verify();
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

/// <summary>
/// Fluent builder for creating test data.
/// Simplifies creation of complex domain objects.
/// </summary>
/// <typeparam name="T">The type to build.</typeparam>
/// <example>
/// <code>
/// var user = new TestDataBuilder<User>()
///     .With(u => u.Id, 1)
///     .With(u => u.Name, "John")
///     .With(u => u.Email, "john@example.com")
///     .Build();
/// </code>
/// </example>
public class TestDataBuilder<T> where T : class, new()
{
    private readonly T _instance = new();

    /// <summary>
    /// Set property value.
    /// </summary>
    public TestDataBuilder<T> With<TProperty>(Expression<Func<T, TProperty>> propertySelector, TProperty value)
    {
        if (propertySelector.Body is MemberExpression memberExpr &&
            memberExpr.Member is PropertyInfo property)
        {
            property.SetValue(_instance, value);
        }
        return this;
    }

    /// <summary>
    /// Configure object with action.
    /// </summary>
    public TestDataBuilder<T> Configure(Action<T> configurator)
    {
        configurator(_instance);
        return this;
    }

    /// <summary>
    /// Build instance.
    /// </summary>
    public T Build()
    {
        return _instance;
    }

    /// <summary>
    /// Build multiple instances.
    /// </summary>
    public List<T> Build(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => new TestDataBuilder<T>()
                .Configure(obj =>
                {
                    foreach (var property in typeof(T).GetProperties())
                    {
                        var value = property.GetValue(_instance);
                        property.SetValue(obj, value);
                    }
                })
                .Build())
            .ToList();
    }
}

/// <summary>
/// Test data generator for common patterns.
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// Generate unique email addresses.
    /// </summary>
    private static int _emailCounter = 0;
    public static string GenerateEmail()
    {
        return $"test{Interlocked.Increment(ref _emailCounter)}@example.com";
    }

    /// <summary>
    /// Generate unique usernames.
    /// </summary>
    private static int _usernameCounter = 0;
    public static string GenerateUsername()
    {
        return $"testuser{Interlocked.Increment(ref _usernameCounter)}";
    }

    /// <summary>
    /// Generate random string.
    /// </summary>
    public static string RandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }

    /// <summary>
    /// Generate random integer.
    /// </summary>
    public static int RandomInt(int min = 1, int max = int.MaxValue)
    {
        return new Random().Next(min, max);
    }

    /// <summary>
    /// Generate random boolean.
    /// </summary>
    public static bool RandomBool()
    {
        return new Random().Next(2) == 0;
    }

    /// <summary>
    /// Generate random date in past.
    /// </summary>
    public static DateTime RandomPastDate(int daysBack = 365)
    {
        var random = new Random();
        return DateTime.UtcNow.AddDays(-random.Next(1, daysBack));
    }

    /// <summary>
    /// Generate random future date.
    /// </summary>
    public static DateTime RandomFutureDate(int daysAhead = 365)
    {
        var random = new Random();
        return DateTime.UtcNow.AddDays(random.Next(1, daysAhead));
    }
}

# 10. WireMock API Mocking - Code Examples

**Parent:** [WireMock API Mocking](21d10_WireMockApiMocking.md)

---

## 10.1 MockApiServer Implementation

```csharp
namespace Oravey.UITestFramework.Mocking;

using WireMock.Server;
using WireMock.Settings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Logging;
using Oravey.UITestFramework.Core.Logging;

/// <summary>
/// Manages WireMock server lifecycle for API mocking in UI tests.
/// </summary>
public class MockApiServer : IDisposable
{
    private WireMockServer? _server;
    private readonly ITestLogger _logger;
    private readonly string _testName;
    private bool _disposed;
    
    public string? BaseUrl => _server?.Url;
    public int Port => _server?.Port ?? 0;
    public bool IsRunning => _server?.IsStarted ?? false;
    
    public MockApiServer(ITestLogger logger, string testName)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _testName = testName ?? throw new ArgumentNullException(nameof(testName));
    }
    
    #region Server Lifecycle
    
    /// <summary>
    /// Start the mock server on a dynamic port.
    /// </summary>
    public void Start(int? port = null)
    {
        if (_server != null)
        {
            throw new InvalidOperationException("Mock server already started");
        }
        
        var settings = new WireMockServerSettings
        {
            Port = port,  // null = dynamic port
            StartAdminInterface = true,
            Logger = new WireMockConsoleLogger()
        };
        
        _server = WireMockServer.Start(settings);
        
        _logger.LogInfo(
            _testName,
            "MockApiServer",
            $"Started on port {Port} ({BaseUrl})");
    }
    
    /// <summary>
    /// Stop the mock server.
    /// </summary>
    public void Stop()
    {
        if (_server != null)
        {
            _logger.LogInfo(
                _testName,
                "MockApiServer",
                "Stopping mock server");
            
            _server.Stop();
            _server.Dispose();
            _server = null;
        }
    }
    
    /// <summary>
    /// Clear all registered stubs.
    /// </summary>
    public void Reset()
    {
        EnsureRunning();
        _server!.Reset();
        
        _logger.LogInfo(
            _testName,
            "MockApiServer",
            "Stubs cleared");
    }
    
    #endregion
    
    #region Stub Registration
    
    /// <summary>
    /// Register a stub using a builder.
    /// </summary>
    public void Stub(Action<ApiStubBuilder> configure)
    {
        EnsureRunning();
        
        var builder = new ApiStubBuilder();
        configure(builder);
        
        var (request, response) = builder.Build();
        _server!.Given(request).RespondWith(response);
        
        _logger.Log(
            _testName,
            "MockApi",
            builder.Path ?? "/",
            "Stub",
            builder.Method,
            builder.StatusCode.ToString(),
            true,
            null);
    }
    
    /// <summary>
    /// Register multiple stubs.
    /// </summary>
    public void StubAll(params Action<ApiStubBuilder>[] configurations)
    {
        foreach (var configure in configurations)
        {
            Stub(configure);
        }
    }
    
    #endregion
    
    #region Verification
    
    /// <summary>
    /// Verify that a call was made to the specified path.
    /// </summary>
    public void VerifyCallMade(string path, int expectedCalls = 1)
    {
        EnsureRunning();
        
        var requests = _server!.LogEntries
            .Where(e => e.RequestMessage.Path == path)
            .ToList();
        
        var passed = requests.Count >= expectedCalls;
        
        _logger.Log(
            _testName,
            "MockApi",
            path,
            "Verify",
            requests.Count.ToString(),
            expectedCalls.ToString(),
            passed,
            passed ? null : $"Expected {expectedCalls} calls, got {requests.Count}");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Expected at least {expectedCalls} call(s) to '{path}', but got {requests.Count}");
        }
    }
    
    /// <summary>
    /// Verify exactly the specified number of calls.
    /// </summary>
    public void VerifyCallMadeExactly(string path, int expectedCalls)
    {
        EnsureRunning();
        
        var requests = _server!.LogEntries
            .Where(e => e.RequestMessage.Path == path)
            .ToList();
        
        var passed = requests.Count == expectedCalls;
        
        _logger.Log(
            _testName,
            "MockApi",
            path,
            "VerifyExact",
            requests.Count.ToString(),
            expectedCalls.ToString(),
            passed,
            passed ? null : $"Expected exactly {expectedCalls} calls, got {requests.Count}");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Expected exactly {expectedCalls} call(s) to '{path}', but got {requests.Count}");
        }
    }
    
    /// <summary>
    /// Verify no calls were made to the path.
    /// </summary>
    public void VerifyNoCallsMade(string path)
    {
        VerifyCallMadeExactly(path, 0);
    }
    
    #endregion
    
    #region Helpers
    
    private void EnsureRunning()
    {
        if (_server == null || !_server.IsStarted)
        {
            throw new InvalidOperationException(
                "Mock server not started. Call Start() first.");
        }
    }
    
    #endregion
    
    #region IDisposable
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            Stop();
            _disposed = true;
        }
    }
    
    #endregion
}
```

---

## 10.2 ApiStubBuilder Implementation

```csharp
namespace Oravey.UITestFramework.Mocking;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Matchers;
using System.Text.Json;

/// <summary>
/// Fluent builder for configuring API stubs.
/// </summary>
public class ApiStubBuilder
{
    private IRequestBuilder _request;
    private IResponseBuilder _response;
    
    public string? Path { get; private set; }
    public string Method { get; private set; } = "GET";
    public int StatusCode { get; private set; } = 200;
    
    public ApiStubBuilder()
    {
        _request = Request.Create();
        _response = Response.Create().WithStatusCode(200);
    }
    
    #region Request Configuration
    
    /// <summary>
    /// Match exact path.
    /// </summary>
    public ApiStubBuilder WithPath(string path)
    {
        Path = path;
        _request = _request.WithPath(path);
        return this;
    }
    
    /// <summary>
    /// Match path pattern (regex).
    /// </summary>
    public ApiStubBuilder WithPathPattern(string pattern)
    {
        Path = pattern;
        _request = _request.WithPath(new RegexMatcher(pattern));
        return this;
    }
    
    /// <summary>
    /// Match HTTP method.
    /// </summary>
    public ApiStubBuilder WithMethod(string method)
    {
        Method = method;
        _request = method.ToUpperInvariant() switch
        {
            "GET" => _request.UsingGet(),
            "POST" => _request.UsingPost(),
            "PUT" => _request.UsingPut(),
            "DELETE" => _request.UsingDelete(),
            "PATCH" => _request.UsingPatch(),
            _ => _request.UsingMethod(method)
        };
        return this;
    }
    
    /// <summary>
    /// Match request header.
    /// </summary>
    public ApiStubBuilder WithHeader(string name, string value)
    {
        _request = _request.WithHeader(name, value);
        return this;
    }
    
    /// <summary>
    /// Match authorization header.
    /// </summary>
    public ApiStubBuilder WithAuthorization(string scheme = "Bearer")
    {
        _request = _request.WithHeader("Authorization", new RegexMatcher($"^{scheme} .+$"));
        return this;
    }
    
    /// <summary>
    /// Match query parameter.
    /// </summary>
    public ApiStubBuilder WithQueryParam(string name, string value)
    {
        _request = _request.WithParam(name, value);
        return this;
    }
    
    /// <summary>
    /// Match request body exactly.
    /// </summary>
    public ApiStubBuilder WithBody(string body)
    {
        _request = _request.WithBody(body);
        return this;
    }
    
    /// <summary>
    /// Match JSON body.
    /// </summary>
    public ApiStubBuilder WithJsonBody(object body)
    {
        var json = JsonSerializer.Serialize(body);
        _request = _request.WithBody(new JsonMatcher(json));
        return this;
    }
    
    #endregion
    
    #region Response Configuration
    
    /// <summary>
    /// Set response status code.
    /// </summary>
    public ApiStubBuilder ReturnsStatus(int statusCode)
    {
        StatusCode = statusCode;
        _response = _response.WithStatusCode(statusCode);
        return this;
    }
    
    /// <summary>
    /// Set response body.
    /// </summary>
    public ApiStubBuilder ReturnsBody(string body)
    {
        _response = _response.WithBody(body);
        return this;
    }
    
    /// <summary>
    /// Set JSON response body.
    /// </summary>
    public ApiStubBuilder ReturnsJson(object body)
    {
        var json = JsonSerializer.Serialize(body);
        _response = _response
            .WithHeader("Content-Type", "application/json")
            .WithBody(json);
        return this;
    }
    
    /// <summary>
    /// Set response header.
    /// </summary>
    public ApiStubBuilder ReturnsHeader(string name, string value)
    {
        _response = _response.WithHeader(name, value);
        return this;
    }
    
    /// <summary>
    /// Add response delay (simulate slow API).
    /// </summary>
    public ApiStubBuilder WithDelay(int milliseconds)
    {
        _response = _response.WithDelay(TimeSpan.FromMilliseconds(milliseconds));
        return this;
    }
    
    /// <summary>
    /// Simulate network fault.
    /// </summary>
    public ApiStubBuilder WithFault(FaultType fault)
    {
        _response = fault switch
        {
            FaultType.ConnectionReset => _response.WithFault(WireMock.Handlers.FaultType.CONNECTION_RESET_BY_PEER),
            FaultType.EmptyResponse => _response.WithFault(WireMock.Handlers.FaultType.EMPTY_RESPONSE),
            FaultType.MalformedResponse => _response.WithFault(WireMock.Handlers.FaultType.MALFORMED_RESPONSE_CHUNK),
            _ => _response
        };
        return this;
    }
    
    #endregion
    
    #region Build
    
    internal (IRequestBuilder Request, IResponseBuilder Response) Build()
    {
        return (_request, _response);
    }
    
    #endregion
}

public enum FaultType
{
    ConnectionReset,
    EmptyResponse,
    MalformedResponse
}
```

---

## 10.3 Predefined Mock Scenarios

```csharp
namespace Oravey.UITestFramework.Mocking.Scenarios;

using System.Text.Json;

/// <summary>
/// Pre-built scenarios for common API mocking patterns.
/// </summary>
public static class MockScenarios
{
    #region User API
    
    public static Action<ApiStubBuilder> GetUser(int userId, UserDto user) =>
        stub => stub
            .WithPath($"/api/users/{userId}")
            .WithMethod("GET")
            .ReturnsStatus(200)
            .ReturnsJson(user);
    
    public static Action<ApiStubBuilder> GetUserNotFound(int userId) =>
        stub => stub
            .WithPath($"/api/users/{userId}")
            .WithMethod("GET")
            .ReturnsStatus(404)
            .ReturnsJson(new { error = "User not found", userId });
    
    public static Action<ApiStubBuilder> GetUserList(IEnumerable<UserDto> users) =>
        stub => stub
            .WithPath("/api/users")
            .WithMethod("GET")
            .ReturnsStatus(200)
            .ReturnsJson(new { data = users, total = users.Count() });
    
    public static Action<ApiStubBuilder> CreateUser(UserDto createdUser) =>
        stub => stub
            .WithPath("/api/users")
            .WithMethod("POST")
            .ReturnsStatus(201)
            .ReturnsHeader("Location", $"/api/users/{createdUser.Id}")
            .ReturnsJson(createdUser);
    
    public static Action<ApiStubBuilder> UpdateUser(int userId, UserDto updatedUser) =>
        stub => stub
            .WithPath($"/api/users/{userId}")
            .WithMethod("PUT")
            .ReturnsStatus(200)
            .ReturnsJson(updatedUser);
    
    public static Action<ApiStubBuilder> DeleteUser(int userId) =>
        stub => stub
            .WithPath($"/api/users/{userId}")
            .WithMethod("DELETE")
            .ReturnsStatus(204);
    
    #endregion
    
    #region Authentication API
    
    public static Action<ApiStubBuilder> LoginSuccess(string token) =>
        stub => stub
            .WithPath("/api/auth/login")
            .WithMethod("POST")
            .ReturnsStatus(200)
            .ReturnsJson(new { token, expiresIn = 3600 });
    
    public static Action<ApiStubBuilder> LoginFailure() =>
        stub => stub
            .WithPath("/api/auth/login")
            .WithMethod("POST")
            .ReturnsStatus(401)
            .ReturnsJson(new { error = "Invalid credentials" });
    
    public static Action<ApiStubBuilder> RefreshToken(string newToken) =>
        stub => stub
            .WithPath("/api/auth/refresh")
            .WithMethod("POST")
            .WithAuthorization()
            .ReturnsStatus(200)
            .ReturnsJson(new { token = newToken, expiresIn = 3600 });
    
    #endregion
    
    #region Error Scenarios
    
    public static Action<ApiStubBuilder> ServerError(string path) =>
        stub => stub
            .WithPath(path)
            .ReturnsStatus(500)
            .ReturnsJson(new { error = "Internal server error" });
    
    public static Action<ApiStubBuilder> ServiceUnavailable(string path) =>
        stub => stub
            .WithPath(path)
            .ReturnsStatus(503)
            .ReturnsJson(new { error = "Service temporarily unavailable" });
    
    public static Action<ApiStubBuilder> Unauthorized(string path) =>
        stub => stub
            .WithPath(path)
            .ReturnsStatus(401)
            .ReturnsJson(new { error = "Unauthorized" });
    
    public static Action<ApiStubBuilder> BadRequest(string path, object validationErrors) =>
        stub => stub
            .WithPath(path)
            .ReturnsStatus(400)
            .ReturnsJson(new { error = "Validation failed", errors = validationErrors });
    
    public static Action<ApiStubBuilder> SlowResponse(string path, int delayMs) =>
        stub => stub
            .WithPath(path)
            .WithDelay(delayMs)
            .ReturnsStatus(200);
    
    public static Action<ApiStubBuilder> ConnectionError(string path) =>
        stub => stub
            .WithPath(path)
            .WithFault(FaultType.ConnectionReset);
    
    #endregion
}

/// <summary>
/// DTO for user responses.
/// </summary>
public record UserDto(
    int Id,
    string Username,
    string Email,
    string? DisplayName = null,
    DateTime CreatedAt = default);
```

---

## 10.4 Test Base with Mock Server

```csharp
namespace Oravey.Tools.Wpf.UITests.Infrastructure;

using Oravey.UITestFramework.Mocking;
using Oravey.UITestFramework.Mocking.Scenarios;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Base class for UI tests that use mocked APIs.
/// </summary>
public abstract class MockedUITestBase : UITestBase
{
    protected MockApiServer MockServer { get; }
    
    protected MockedUITestBase(ITestOutputHelper output) : base(output)
    {
        MockServer = new MockApiServer(Logger, TestName);
    }
    
    protected override void SetupTest()
    {
        base.SetupTest();
        
        // Start mock server before app launches
        MockServer.Start();
        
        // Configure app to use mock server URL
        // This is typically done via environment variable or config
        ConfigureAppForMocking();
        
        // Register default stubs
        SetupDefaultStubs();
    }
    
    protected override void CleanupTest()
    {
        try
        {
            MockServer.Stop();
        }
        finally
        {
            base.CleanupTest();
        }
    }
    
    /// <summary>
    /// Configure the app to use the mock server URL.
    /// Override in derived class for custom configuration.
    /// </summary>
    protected virtual void ConfigureAppForMocking()
    {
        // Set environment variable for the app to read
        Environment.SetEnvironmentVariable(
            "API_BASE_URL",
            MockServer.BaseUrl);
    }
    
    /// <summary>
    /// Register default stubs that apply to all tests.
    /// Override to customize.
    /// </summary>
    protected virtual void SetupDefaultStubs()
    {
        // Default: Health check always succeeds
        MockServer.Stub(stub => stub
            .WithPath("/api/health")
            .WithMethod("GET")
            .ReturnsStatus(200)
            .ReturnsJson(new { status = "healthy" }));
    }
    
    /// <summary>
    /// Helper to stub multiple scenarios at once.
    /// </summary>
    protected void SetupScenarios(params Action<ApiStubBuilder>[] scenarios)
    {
        MockServer.StubAll(scenarios);
    }
}
```

---

## 10.5 Complete Mocked Test Example

```csharp
namespace Oravey.Tools.Wpf.UITests.Tests;

using FluentAssertions;
using Oravey.Tools.Wpf.UITests.Infrastructure;
using Oravey.Tools.Wpf.UITests.PageObjects;
using Oravey.UITestFramework.Mocking;
using Oravey.UITestFramework.Mocking.Scenarios;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "MockedAPITest")]
[Trait("MockApi", "true")]
[Collection("UITests")]
public class UserManagementTests : MockedUITestBase
{
    public UserManagementTests(ITestOutputHelper output) : base(output) { }
    
    protected override void SetupDefaultStubs()
    {
        base.SetupDefaultStubs();
        
        // Setup default user list
        var users = new[]
        {
            new UserDto(1, "john.doe", "john@example.com", "John Doe"),
            new UserDto(2, "jane.smith", "jane@example.com", "Jane Smith")
        };
        
        MockServer.Stub(MockScenarios.GetUserList(users));
    }
    
    [Fact]
    public void UserList_Displays_Users_From_Api()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        
        // Act
        var usersPage = shell.NavigateToUsers();
        
        // Assert - Users from mock are displayed
        usersPage.UserGrid.AssertRowCount(2);
        usersPage.UserGrid.AssertCellText(0, "Username", "john.doe");
        usersPage.UserGrid.AssertCellText(1, "Username", "jane.smith");
        
        // Verify API was called
        MockServer.VerifyCallMade("/api/users");
    }
    
    [Fact]
    public void UserDetails_Shows_User_Information()
    {
        // Arrange - Setup specific user response
        var user = new UserDto(1, "john.doe", "john@example.com", "John Doe", DateTime.Parse("2024-01-01"));
        MockServer.Stub(MockScenarios.GetUser(1, user));
        
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var usersPage = shell.NavigateToUsers();
        
        // Act - View user details
        var detailsPage = usersPage.ViewUserDetails(1);
        
        // Assert
        detailsPage.UsernameLabel.AssertText("john.doe");
        detailsPage.EmailLabel.AssertText("john@example.com");
        detailsPage.DisplayNameLabel.AssertText("John Doe");
        
        MockServer.VerifyCallMade("/api/users/1");
    }
    
    [Fact]
    public void UserNotFound_Shows_Error_Message()
    {
        // Arrange - Setup 404 response
        MockServer.Stub(MockScenarios.GetUserNotFound(999));
        
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var usersPage = shell.NavigateToUsers();
        
        // Act - Try to view non-existent user
        usersPage.NavigateToUserDirectly(999);
        
        // Assert - Error is shown
        var errorDialog = new ErrorDialog(Context);
        errorDialog.WaitForDisplayed();
        errorDialog.GetMessage().Should().Contain("User not found");
    }
    
    [Fact]
    public void CreateUser_Success_Shows_Confirmation()
    {
        // Arrange
        var newUser = new UserDto(3, "new.user", "new@example.com", "New User");
        MockServer.Stub(MockScenarios.CreateUser(newUser));
        
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        var usersPage = shell.NavigateToUsers();
        var createPage = usersPage.NavigateToCreateUser();
        
        // Act
        createPage.SetUsername("new.user");
        createPage.SetEmail("new@example.com");
        createPage.SetDisplayName("New User");
        createPage.SubmitAndWait();
        
        // Assert
        var successDialog = new SuccessDialog(Context);
        successDialog.WaitForDisplayed();
        successDialog.GetMessage().Should().Contain("created successfully");
        
        MockServer.VerifyCallMade("/api/users", 1);
    }
    
    [Fact]
    public void ServerError_Shows_Error_Dialog()
    {
        // Arrange - API returns 500
        MockServer.Reset();
        MockServer.Stub(MockScenarios.ServerError("/api/users"));
        
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        
        // Act
        shell.NavigateToUsersButton.Click();
        
        // Assert - Error dialog appears
        var errorDialog = new ErrorDialog(Context);
        errorDialog.WaitForDisplayed();
        errorDialog.GetMessage().Should().Contain("error");
    }
    
    [Fact]
    public void SlowApi_Shows_Loading_Indicator()
    {
        // Arrange - API takes 3 seconds
        MockServer.Reset();
        MockServer.Stub(MockScenarios.GetUserList(new[] { new UserDto(1, "test", "test@test.com") }));
        MockServer.Stub(stub => stub
            .WithPath("/api/users")
            .WithMethod("GET")
            .WithDelay(3000)
            .ReturnsStatus(200)
            .ReturnsJson(new { data = Array.Empty<UserDto>(), total = 0 }));
        
        var shell = new ShellPage(Context);
        shell.WaitForShellReady();
        
        // Act
        shell.NavigateToUsersButton.Click();
        var usersPage = new UsersPage(Context);
        
        // Assert - Loading indicator should be visible during slow load
        usersPage.LoadingIndicator.WaitForVisible();
        
        // Eventually page loads
        usersPage.WaitForPageReady(timeout: 5000);
    }
}
```

---

## 10.6 Running Mocked Tests

```powershell
# Run all mocked API tests
dotnet test --filter "MockApi=true"

# Run mocked tests with detailed output
dotnet test --filter "Category=MockedAPITest" --logger "console;verbosity=detailed"

# Run specific mocked test class
dotnet test --filter "FullyQualifiedName~UserManagementTests"

# Run mocked tests in isolation (single thread)
dotnet test --filter "MockApi=true" -- xunit.parallelizeTestCollections=false
```

---

*Related: [Cloud Provider Support Code Examples](21d11_CloudProviderSupport_CodeExamples.md)*

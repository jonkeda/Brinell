# 10. WireMock API Mocking

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d10_WireMockApiMocking_CodeExamples.md](21d10_WireMockApiMocking_CodeExamples.md)  
**Previous:** [Page Object Pattern](21d9_PageObjectPattern.md)

---

## 10.1 Overview

WireMock.Net enables API mocking for isolated UI tests. The application can be tested without depending on external services by mocking HTTP responses.

### 10.1.1 Benefits

| Benefit | Description |
|---------|-------------|
| **Isolation** | Tests run without external dependencies |
| **Speed** | Local mock responses are instant |
| **Control** | Simulate any API state or error condition |
| **Reliability** | No network issues or service downtime |
| **Edge Cases** | Test error handling, timeouts, edge scenarios |

---

## 10.2 Architecture

### 10.2.1 Layer Position

```
┌─────────────────────────────────────────────────┐
│              Application Layer                   │
│          (UITest Projects, Tests)                │
├─────────────────────────────────────────────────┤
│               Mocking Layer                      │  ◄── WireMock
│          (MockApiServer, Stubs)                  │
├─────────────────────────────────────────────────┤
│             Platform Layer                       │
│        (FlaUI, Appium, Selenium)                 │
├─────────────────────────────────────────────────┤
│               Core Layer                         │
│          (Abstractions, Base)                    │
└─────────────────────────────────────────────────┘
```

### 10.2.2 Components

| Component | Purpose |
|-----------|---------|
| `MockApiServer` | Manages WireMock server lifecycle |
| `ApiStubBuilder` | Fluent builder for stub configuration |
| `MockScenarios` | Pre-built scenarios for common cases |
| `MockResponses` | JSON response templates |

---

## 10.3 MockApiServer

### 10.3.1 Properties

| Property | Type | Description |
|----------|------|-------------|
| `BaseUrl` | `string` | Mock server URL (e.g., `http://localhost:9090`) |
| `IsRunning` | `bool` | Server running state |
| `Port` | `int` | Assigned port number |

### 10.3.2 Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Start()` | `void` | Start the mock server |
| `Stop()` | `void` | Stop and dispose server |
| `Reset()` | `void` | Clear all stubs |
| `Stub(builder)` | `void` | Add stub from builder |
| `VerifyCall(path, times)` | `void` | Verify API was called |

---

## 10.4 Stub Configuration

### 10.4.1 Request Matching

| Matcher | Description | Example |
|---------|-------------|---------|
| `WithPath(path)` | Exact path match | `/api/users` |
| `WithPathPattern(regex)` | Regex path match | `/api/users/\d+` |
| `WithMethod(method)` | HTTP method | `GET`, `POST` |
| `WithHeader(name, value)` | Header match | `Authorization: Bearer *` |
| `WithBody(json)` | Body content match | JSON equality |
| `WithBodyPattern(regex)` | Body regex match | Pattern matching |
| `WithQueryParam(name, value)` | Query parameter | `?id=123` |

### 10.4.2 Response Configuration

| Config | Description | Example |
|--------|-------------|---------|
| `WithStatus(code)` | HTTP status code | `200`, `404`, `500` |
| `WithBody(content)` | Response body | JSON string |
| `WithJsonBody(object)` | Serialize object | Any object |
| `WithHeader(name, value)` | Response header | Content-Type |
| `WithDelay(ms)` | Response delay | Simulate latency |
| `WithFault(type)` | Network fault | Connection reset |

---

## 10.5 Common Scenarios

### 10.5.1 Success Scenarios

| Scenario | Use Case |
|----------|----------|
| `ReturnUser(id)` | Return valid user by ID |
| `ReturnUserList()` | Return paginated user list |
| `ReturnEmpty()` | Return empty collection |
| `CreateSuccess()` | Return 201 Created |
| `UpdateSuccess()` | Return 200 OK |
| `DeleteSuccess()` | Return 204 No Content |

### 10.5.2 Error Scenarios

| Scenario | Use Case |
|----------|----------|
| `ReturnNotFound()` | 404 Not Found |
| `ReturnUnauthorized()` | 401 Unauthorized |
| `ReturnForbidden()` | 403 Forbidden |
| `ReturnBadRequest(errors)` | 400 with validation errors |
| `ReturnServerError()` | 500 Internal Server Error |
| `ReturnServiceUnavailable()` | 503 Service Unavailable |

### 10.5.3 Edge Case Scenarios

| Scenario | Use Case |
|----------|----------|
| `SlowResponse(delay)` | Test timeout handling |
| `ConnectionReset()` | Test connection failures |
| `MalformedResponse()` | Test parsing errors |
| `LargePayload()` | Test memory handling |
| `EmptyBody()` | Test null response handling |

---

## 10.6 Test Categories

### 10.6.1 Mock Test Trait

```csharp
[Trait("Category", "MockedAPITest")]
[Trait("MockApi", "true")]
```

### 10.6.2 Running Mock Tests

```bash
# Run only mocked API tests
dotnet test --filter "MockApi=true"

# Run mock tests with verbose logging
dotnet test --filter "Category=MockedAPITest" -- xunit.diagnosticMessages=true
```

---

## 10.7 Verification

### 10.7.1 Verify Calls

| Method | Description |
|--------|-------------|
| `VerifyCallMade(path)` | At least one call made |
| `VerifyCallMade(path, times)` | Exact number of calls |
| `VerifyNoCallsMade(path)` | No calls to path |
| `VerifyCallWithBody(path, body)` | Call with specific body |
| `VerifyCallWithHeader(path, header)` | Call with header |

### 10.7.2 Request Logging

All requests to mock server are logged:

```
Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
2024-01-15T10:30:45;GetUser_ReturnsData;MockApi;/api/users/1;Stub;GET;200;Passed;
2024-01-15T10:30:46;GetUser_ReturnsData;MockApi;/api/users/1;Verify;1;1;Passed;
```

---

## 10.8 Best Practices

### 10.8.1 DO

- ✅ Start mock server in test setup
- ✅ Reset stubs between tests
- ✅ Use scenario methods for common responses
- ✅ Verify important API calls were made
- ✅ Test error scenarios thoroughly
- ✅ Use realistic response data

### 10.8.2 DON'T

- ❌ Share mock server instances between parallel tests
- ❌ Hardcode ports (use dynamic allocation)
- ❌ Skip stopping the server in cleanup
- ❌ Mock everything (some integration is valuable)
- ❌ Forget to test timeouts and errors

---

## 10.9 Integration with Cloud Testing

When running on cloud providers, mock server can be:

1. **Run locally** - Server on test machine, app configured to use it
2. **Run in container** - Docker container alongside tests
3. **Disabled** - Tests can target real endpoints

```csharp
// Check if mocking is enabled
if (Context.Configuration.UseMockApi)
{
    _mockServer.Start();
    // Configure app to use mock URL
}
```

---

*Next: [Cloud Provider Support](21d11_CloudProviderSupport.md)*

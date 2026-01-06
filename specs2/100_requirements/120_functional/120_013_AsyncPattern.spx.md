# functional AsyncPattern
- **id**: FR-013
- **title**: Asynchronous operation pattern for async-native platforms
- **priority**: high
- **status**: approved
- **tags**: core, async, blazor, playwright

Platforms that are inherently asynchronous (Blazor, Playwright) may implement an async variant of the control and page object APIs.

## capabilities

### AsyncMethodNaming
- **id**: FR-013.1
- **title**: Async method naming convention

Async method variants must follow the `*Async` suffix convention:

| Sync Method | Async Method |
|-------------|--------------|
| Click() | ClickAsync() |
| Enter(text) | EnterAsync(text) |
| GetText() | GetTextAsync() |
| IsVisible() | IsVisibleAsync() |
| WaitVisible() | WaitVisibleAsync() |
| AssertText() | AssertTextAsync() |

All async methods must return `Task` or `Task<T>`.

### AsyncInterfaceParity
- **id**: FR-013.2
- **title**: Async interfaces mirror sync interfaces

For each synchronous interface, an async variant may exist:
- IControlObject → IAsyncControlObject
- IClickableControlObject → IAsyncClickableControlObject
- ITextControlObject → IAsyncTextControlObject
- etc.

Async interfaces must provide the same logical operations as their sync counterparts. Method signatures must match except for:
- Return type wrapped in `Task<T>`
- `Async` suffix on method name

### AsyncBaseClasses
- **id**: FR-013.3
- **title**: Async base class pattern

Platform implementations may provide async base classes:
- AsyncControlObjectBase
- AsyncClickableControlBase
- AsyncTextControlBase
- etc.

Async base classes must follow the same patterns as sync base classes:
- Virtual methods for override
- Logging integration
- Timeout parameter support

### PlatformDeterminesModel
- **id**: FR-013.4
- **title**: Platform determines sync vs async model

Each platform implementation chooses its execution model:

| Platform | Model | Reason |
|----------|-------|--------|
| MAUI/Appium | Sync | Appium WebDriver is synchronous |
| WPF/FlaUI | Sync | FlaUI is synchronous |
| Blazor/Playwright | Async | Playwright is async-native |
| HTML/Selenium | Sync | Selenium WebDriver is synchronous |

Tests written for async platforms must use `async/await` throughout:
```csharp
[Fact]
public async Task LoginTest()
{
    var page = new LoginPage(context);
    await page.UsernameInput.EnterAsync("user@example.com");
    await page.PasswordInput.EnterAsync("password");
    await page.LoginButton.ClickAsync();
    await page.WelcomeLabel.AssertTextAsync("Welcome!");
}
```

### AsyncTestLifecycle
- **id**: FR-013.5
- **title**: Async test lifecycle support

Async platform test bases must support async lifecycle:
- `InitializeAsync()` for async test setup
- `DisposeAsync()` for async test cleanup
- Compatible with xUnit `IAsyncLifetime`

```csharp
public class BlazorTestBase : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await Context.LaunchBrowserAsync();
        await Context.NavigateToAsync(BaseUrl);
    }

    public async Task DisposeAsync()
    {
        await Context.CloseBrowserAsync();
    }
}
```

### MixedModelAvoidance
- **id**: FR-013.6
- **title**: Avoid mixing sync and async in same test

Tests should not mix sync and async control objects. A test project targeting an async platform must use async APIs consistently.

**Avoid:**
```csharp
// Don't mix sync and async
await button.ClickAsync();
var text = label.GetText();  // Sync call in async context
```

**Preferred:**
```csharp
// Consistent async usage
await button.ClickAsync();
var text = await label.GetTextAsync();
```

### CancellationSupport
- **id**: FR-013.7
- **title**: Cancellation token support
- **priority**: medium

Async methods should support `CancellationToken` for operation cancellation:

```csharp
Task ClickAsync(int? timeoutMs = null, CancellationToken cancellationToken = default);
Task<string> GetTextAsync(int? timeoutMs = null, CancellationToken cancellationToken = default);
```

Cancellation enables:
- Test timeout enforcement
- Graceful test abort
- Resource cleanup on cancellation

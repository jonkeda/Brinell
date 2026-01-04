# SPEC-006-003: Blazor Control Hierarchy

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026

---

## 1. Async Base Classes

### AsyncControlObjectBase

Foundation for all Blazor controls (async).

```csharp
public abstract class AsyncControlObjectBase : IAsyncControlObject
{
    protected readonly BlazorTestContext Context;
    
    public ControlLocator Locator { get; }
    public IAsyncPageObject? Page { get; }

    // Primary constructor
    protected AsyncControlObjectBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
        Page = page;
    }

    // String convenience constructor (uses TestId for Blazor)
    protected AsyncControlObjectBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : this(context, By.TestId(testId), page)
    {
    }

    // Logging
    protected void Log(string message)
    {
        Context.Log($"[{GetType().Name}] {Locator}: {message}");
    }

    // Playwright locator
    protected ILocator GetLocator() => ConvertToPlaywright(Locator);

    // Is methods (async)
    public async Task<bool> IsExistsAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var count = await GetLocator().CountAsync();
        return count > 0;
    }

    public async Task<bool> IsVisibleAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().IsVisibleAsync();
    }

    public async Task<bool> IsEnabledAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().IsEnabledAsync();
    }

    public virtual async Task<string> GetTextAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().InnerTextAsync();
    }

    // Wait methods
    public async Task<bool> WaitExistsAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    public async Task<bool> WaitVisibleAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    public async Task<bool> WaitEnabledAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);

    // Check methods
    public async Task CheckExistsAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    public async Task CheckVisibleAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);
    public async Task CheckEnabledAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default);

    // Assert methods
    public async Task AssertExistsAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    public async Task AssertVisibleAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    public async Task AssertTextAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
}
```

---

### AsyncClickableControlBase

Base for clickable controls (async).

```csharp
public abstract class AsyncClickableControlBase : AsyncControlObjectBase, IAsyncClickableControlObject
{
    protected AsyncClickableControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncClickableControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    public virtual async Task ClickAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("ClickAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        await GetLocator().ClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    public virtual async Task DoubleClickAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("DoubleClickAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        await GetLocator().DblClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    public virtual async Task RightClickAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("RightClickAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        await GetLocator().ClickAsync(new() 
        { 
            Button = MouseButton.Right,
            Timeout = timeoutMs ?? DefaultTimeoutMs 
        });
    }

    public virtual async Task HoverAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("HoverAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await GetLocator().HoverAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }
}
```

---

### AsyncTextControlBase

Base for text input controls (async).

```csharp
public abstract class AsyncTextControlBase : AsyncClickableControlBase, IAsyncTextControlObject
{
    protected AsyncTextControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncTextControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    // Focus
    public virtual async Task<bool> IsFocusedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().EvaluateAsync<bool>("el => el === document.activeElement");
    }

    public virtual async Task FocusAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("FocusAsync()");
        await GetLocator().FocusAsync();
    }

    public virtual async Task BlurAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("BlurAsync()");
        await GetLocator().BlurAsync();
    }

    // Text input
    public virtual async Task EnterAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (text is null) return;
        Log($"EnterAsync(\"{text}\")");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        await GetLocator().ClearAsync();
        await GetLocator().FillAsync(text);
    }

    public virtual async Task ClearAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("ClearAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await GetLocator().ClearAsync();
    }

    public virtual async Task ClearAndEnterAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        await ClearAsync(timeoutMs, ct);
        if (text is not null)
        {
            await GetLocator().FillAsync(text);
        }
    }

    public virtual async Task AppendAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (text is null) return;
        Log($"AppendAsync(\"{text}\")");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await GetLocator().PressSequentiallyAsync(text);
    }

    // Read-only
    public virtual async Task<bool> IsReadOnlyAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var attr = await GetLocator().GetAttributeAsync("readonly");
        return attr is not null;
    }

    // Override GetTextAsync for input elements
    public override async Task<string> GetTextAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().InputValueAsync();
    }

    public virtual async Task<int> GetTextLengthAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var text = await GetTextAsync(timeoutMs, ct);
        return text?.Length ?? 0;
    }
}
```

---

## 2. Concrete Controls

### ButtonControl

```csharp
public class ButtonControl : AsyncClickableControlBase
{
    public ButtonControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public ButtonControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    // Inherits all virtual async methods from AsyncClickableControlBase
}
```

### InputControl

```csharp
public class InputControl : AsyncTextControlBase
{
    public InputControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public InputControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    // Inherits all virtual async methods from AsyncTextControlBase
}
```

### LabelControl

```csharp
public class LabelControl : AsyncControlObjectBase
{
    public LabelControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public LabelControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    // Labels are not clickable - only text retrieval
}
```

---

## 3. PageObject Pattern (Async)

### AsyncPageObjectBase

```csharp
public abstract class AsyncPageObjectBase : IAsyncPageObject
{
    protected readonly BlazorTestContext Context;
    
    public abstract string Name { get; }
    protected abstract ControlLocator PageLocator { get; }

    protected AsyncPageObjectBase(BlazorTestContext context)
    {
        Context = context;
    }

    public async Task<bool> IsLoadedAsync(int? timeoutMs = null, CancellationToken ct = default) { ... }
    public async Task<bool> WaitLoadedAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default) { ... }
    
    // Control helpers - use 'new' pattern
    protected ButtonControl Button(string testId) => new(Context, testId, this);
    protected InputControl Input(string testId) => new(Context, testId, this);
    protected LabelControl Label(string testId) => new(Context, testId, this);
}
```

### Example PageObject

```csharp
public class CounterPage : AsyncPageObjectBase
{
    public override string Name => "CounterPage";
    protected override ControlLocator PageLocator => By.TestId("counter-title");

    public CounterPage(BlazorTestContext context) : base(context) { }

    // Controls - using 'new' pattern with TestId
    public ButtonControl IncrementButton => new(Context, "increment-btn", this);
    public ButtonControl ResetButton => new(Context, "reset-btn", this);
    
    public LabelControl CounterTitle => new(Context, "counter-title", this);
    public LabelControl CountDisplay => new(Context, "count-display", this);
}

public class LoginPage : AsyncPageObjectBase
{
    public override string Name => "LoginPage";
    protected override ControlLocator PageLocator => By.TestId("login-form");

    public LoginPage(BlazorTestContext context) : base(context) { }

    public InputControl UsernameInput => new(Context, "username-input", this);
    public InputControl PasswordInput => new(Context, "password-input", this);
    public ButtonControl LoginButton => new(Context, "login-btn", this);
    public LabelControl ErrorMessage => new(Context, "error-message", this);
}
```

---

## 4. Test Pattern (Async)

```csharp
[Fact]
public async Task Counter_ClickIncrement_IncreasesCount()
{
    // Arrange
    var counterPage = new CounterPage(Context);
    await counterPage.WaitLoadedAsync(true);

    // Act
    await counterPage.IncrementButton.ClickAsync();

    // Assert
    await counterPage.CountDisplay.AssertTextContainsAsync("Current count: 1");
}

[Fact]
public async Task Login_ValidCredentials_NavigatesToDashboard()
{
    // Arrange
    var loginPage = new LoginPage(Context);
    await loginPage.WaitLoadedAsync(true);

    // Act
    await loginPage.UsernameInput.EnterAsync("testuser");
    await loginPage.PasswordInput.EnterAsync("password123");
    await loginPage.LoginButton.ClickAsync();

    // Assert
    var dashboard = new DashboardPage(Context);
    await dashboard.WaitLoadedAsync(true);
    await dashboard.WelcomeMessage.AssertTextContainsAsync("Welcome");
}
```

---

## 5. Inheritance Diagram

```
IAsyncControlObject
│
├── IAsyncInteractiveControlObject
│   │
│   └── IAsyncClickableControlObject
│       └── AsyncClickableControlBase (virtual ClickAsync, etc.)
│           └── ButtonControl
│
├── IAsyncFocusableControlObject
│   └── IAsyncTextControlObject
│       └── AsyncTextControlBase (virtual EnterAsync, etc.)
│           ├── InputControl
│           └── TextAreaControl
│
└── AsyncControlObjectBase (IsExistsAsync/WaitAsync/AssertAsync)
    └── LabelControl
```

---

## 6. Locator Strategy for Blazor

Default strategy is `TestId` which maps to `data-testid` attribute:

```html
<button data-testid="increment-btn">+</button>
<input data-testid="username-input" />
```

```csharp
// These are equivalent:
var button = new ButtonControl(context, "increment-btn", page);
var button = new ButtonControl(context, By.TestId("increment-btn"), page);

// For other strategies:
var button = new ButtonControl(context, By.Css("button.submit"), page);
var button = new ButtonControl(context, By.Id("submitBtn"), page);
```

# Brinell Blazor Platform Plan

## Overview

Add `Brinell.Blazor` as a new platform package for testing Blazor applications (Server, WebAssembly, and Hybrid).

**Why Blazor?** Blazor uses component-based rendering with specific DOM patterns that benefit from dedicated tooling beyond generic Selenium/HTML testing.

---

## Architecture Decision

### Option A: Extend Brinell.Html (Recommended)
- Blazor runs in browser, reuse Selenium infrastructure
- Add Blazor-specific component detection and waiting
- New package: `Brinell.Blazor` depends on `Brinell.Html`

### Option B: Standalone Package
- Separate from Html, use Playwright instead of Selenium
- More modern but duplicates infrastructure

**Decision**: Option A - leverage existing Selenium infrastructure

---

## Package Structure

```
src/Brinell.Blazor/
├── Brinell.Blazor.csproj
├── Controls/
│   ├── Base/
│   │   └── BlazorControlBase.cs      # Component-aware base
│   ├── BlazorButtonControl.cs
│   ├── BlazorInputControl.cs
│   ├── BlazorSelectControl.cs
│   ├── EditFormControl.cs            # Blazor EditForm
│   ├── ValidationMessageControl.cs   # Validation display
│   └── NavLinkControl.cs             # Blazor navigation
├── Infrastructure/
│   ├── BlazorTestContext.cs          # Blazor-specific context
│   ├── BlazorWaitService.cs          # SignalR reconnection waits
│   └── ComponentLocator.cs           # Find by @ref, component type
├── Testing/
│   └── BlazorUITestBase.cs           # Test base class
└── Extensions/
    └── BlazorWaitExtensions.cs       # WaitForBlazorReady, etc.
```

---

## Key Features

### 1. Blazor-Specific Waiting
```csharp
// Wait for Blazor circuit/SignalR connection
await Context.WaitForBlazorReady();

// Wait for component render cycle
await Context.WaitForRender();

// Wait for streaming rendering completion
await Context.WaitForStreamingComplete();
```

### 2. Component Locators
```csharp
// Find by Blazor component data attributes
var counter = FindComponent<CounterComponent>("counter-component");

// Find by automation ID (best practice)
var button = FindControl<BlazorButtonControl>("increment-btn");
```

### 3. EditForm Support
```csharp
var form = FindControl<EditFormControl>("login-form");
form.SetField("Email", "user@example.com");
form.SetField("Password", "secret");
form.Submit();

// Assert validation
Assert.False(form.ValidationMessages.Any());
```

### 4. Server vs WASM Detection
```csharp
if (Context.IsBlazorServer)
{
    // Handle SignalR reconnection scenarios
    await Context.WaitForCircuitReconnect();
}
```

---

## Implementation Phases

### Phase 1: Core Infrastructure (3 days)
- [ ] Create `Brinell.Blazor.csproj` with Html dependency
- [ ] Implement `BlazorTestContext` extending `SeleniumTestContext`
- [ ] Add `BlazorWaitService` for circuit/render waits
- [ ] Create `BlazorUITestBase`

### Phase 2: Basic Controls (2 days)
- [ ] Port Html controls with Blazor-aware waiting
- [ ] Add `EditFormControl` for form handling
- [ ] Add `ValidationMessageControl`
- [ ] Add `NavLinkControl`

### Phase 3: Advanced Features (3 days)
- [ ] Component locator by type/data attributes
- [ ] SignalR reconnection handling
- [ ] Streaming rendering support
- [ ] JavaScript interop testing helpers

### Phase 4: Documentation & Samples (2 days)
- [ ] Create `docs/platform-guides/blazor.md`
- [ ] Create sample Blazor app in `samples/Brinell.Samples.Blazor/`
- [ ] Add 10+ example tests

**Total: ~10 days**

---

## Dependencies

```xml
<ItemGroup>
  <PackageReference Include="Selenium.WebDriver" />
  <PackageReference Include="Selenium.Support" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\Brinell.Core\Brinell.Core.csproj" />
  <ProjectReference Include="..\Brinell.Html\Brinell.Html.csproj" />
</ItemGroup>
```

---

## Blazor-Specific Challenges

| Challenge | Solution |
|-----------|----------|
| SignalR disconnection | Detect `blazor-reconnecting` CSS class, wait for reconnect |
| Component re-renders | Wait for `blazor-rendered` attribute changes |
| Streaming rendering | Poll for `blazor-ssr-boundary` completion |
| WASM loading | Wait for `blazor.webassembly.js` initialization |
| Form validation | Query `validation-message` and `field-validation-*` classes |

---

## Test Example

```csharp
[UITest]
[Platform(Platform.Blazor)]
public class CounterTests : BlazorUITestBase
{
    [Fact]
    public async Task ClickButton_IncrementsCounter()
    {
        // Arrange
        await NavigateTo("/counter");
        await WaitForBlazorReady();
        
        var counter = FindControl<LabelControl>("current-count");
        var button = FindControl<BlazorButtonControl>("increment-btn");
        
        // Act
        await button.ClickAsync();
        await WaitForRender();
        
        // Assert
        Assert.Equal("1", counter.Text);
    }
}
```

---

## Success Criteria

- [ ] Blazor Server apps testable with circuit reconnection handling
- [ ] Blazor WASM apps testable with proper initialization waits
- [ ] EditForm validation testing works reliably
- [ ] Sample app with 10+ passing tests
- [ ] Documentation covers Server vs WASM differences
- [ ] CI builds and tests Blazor sample

---

## References

- [Blazor Testing Library](https://bunit.dev/) - Unit testing (different scope)
- [Playwright Blazor Testing](https://playwright.dev/dotnet/) - Alternative approach
- [Selenium Blazor Issues](https://github.com/AdrianWilczynski/Selenol) - Community patterns

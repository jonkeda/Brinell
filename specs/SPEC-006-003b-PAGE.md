# SPEC-006-003b: Page Object Classes

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Parent:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

---

## 1. MAUI Page Classes

### 1.1 PageObjectBase

```csharp
public abstract class PageObjectBase : IPageObject
{
    protected MauiTestContext Context { get; }
    
    protected PageObjectBase(MauiTestContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Page Identity (Example: PageName)

    /// <summary>Page name for logging.</summary>
    public virtual string PageName => GetType().Name;

    /// <summary>AutomationId of page root element.</summary>
    protected virtual string? PageAutomationId => null;

    #endregion

    #region Navigation (Example: WaitForPage)

    public virtual void WaitForPage(int? timeoutMs = null)
    {
        Log($"WaitForPage({PageName})");
        var timeout = timeoutMs ?? Context.DefaultTimeoutMs;
        var locator = PageAutomationId is not null 
            ? By.AutomationId(PageAutomationId) 
            : By.XPath($"//*[@AutomationId='{PageName}']");
        
        Context.Wait.Until(_ => Context.Driver.FindElements(locator.ToAppiumBy()).Any(),
            TimeSpan.FromMilliseconds(timeout));
    }

    public virtual void AssertOnPage(string? message = null, int? timeoutMs = null);
    public virtual bool IsOnPage(int? timeoutMs = null);

    #endregion

    #region Navigation Actions (Example: NavigateTo)

    public virtual void NavigateTo(int? timeoutMs = null)
    {
        Log($"NavigateTo({PageName})");
        // Override in derived classes for specific navigation
    }

    public virtual void GoBack(int? timeoutMs = null);

    #endregion

    #region Control Factory (Example: CreateControl)

    protected virtual T CreateControl<T>(ControlLocator locator) where T : IControlObject
    {
        return (T)Activator.CreateInstance(typeof(T), Context, locator, this)!;
    }

    protected virtual T CreateControl<T>(string automationId) where T : IControlObject
    {
        return CreateControl<T>(By.AutomationId(automationId));
    }

    #endregion

    #region Logging

    protected virtual void Log(string message)
    {
        Context.Logger?.LogInformation("[{PageName}] {Message}", PageName, message);
    }

    #endregion
}
```

### 1.2 BusyPageBase

```csharp
public abstract class BusyPageBase : PageObjectBase, IBusyPage
{
    protected BusyPageBase(MauiTestContext context) : base(context) { }

    #region Busy Indicator

    /// <summary>AutomationId of busy indicator.</summary>
    protected virtual string BusyIndicatorAutomationId => "BusyIndicator";

    private ActivityIndicatorControl? _busyIndicator;
    protected ActivityIndicatorControl BusyIndicator => 
        _busyIndicator ??= CreateControl<ActivityIndicatorControl>(BusyIndicatorAutomationId);

    #endregion

    #region Busy State (Example: IsBusy)

    public virtual bool IsBusy(int? timeoutMs = null)
    {
        return BusyIndicator.IsVisible(timeoutMs) && BusyIndicator.IsRunning(timeoutMs);
    }

    public virtual bool WaitBusy(bool? expected, int? timeoutMs = null);
    public virtual void AssertBusy(bool? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Wait Helpers (Example: WaitUntilNotBusy)

    public virtual void WaitUntilNotBusy(int? timeoutMs = null)
    {
        Log("WaitUntilNotBusy()");
        var timeout = timeoutMs ?? Context.DefaultTimeoutMs * 5; // Longer timeout for busy states
        Context.Wait.Until(_ => !IsBusy(), TimeSpan.FromMilliseconds(timeout));
    }

    public virtual void WaitUntilBusy(int? timeoutMs = null);

    #endregion

    #region Override WaitForPage to account for busy state

    public override void WaitForPage(int? timeoutMs = null)
    {
        base.WaitForPage(timeoutMs);
        WaitUntilNotBusy(timeoutMs);
    }

    #endregion
}
```

### 1.3 ContentPageBase

```csharp
public abstract class ContentPageBase : PageObjectBase
{
    protected ContentPageBase(MauiTestContext context) : base(context) { }

    #region Title (Example: GetTitle)

    public virtual string? GetTitle(int? timeoutMs = null)
    {
        var titleElement = Context.Driver.FindElement(MobileBy.XPath("//TitleBar/TextBlock"));
        return titleElement?.Text;
    }

    public virtual void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Toolbar (Example: GetToolbarItems)

    public virtual IReadOnlyList<string> GetToolbarItems(int? timeoutMs = null)
    {
        var items = Context.Driver.FindElements(MobileBy.XPath("//ToolBar/Button"));
        return items.Select(i => i.Text ?? i.GetAttribute("Name")).ToList();
    }

    public virtual void ClickToolbarItem(string? name, int? timeoutMs = null);

    #endregion
}
```

### 1.4 TabbedPageBase

```csharp
public abstract class TabbedPageBase : PageObjectBase
{
    protected TabbedPageBase(MauiTestContext context) : base(context) { }

    #region Tab Navigation (Example: SelectTab)

    public virtual void SelectTab(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"SelectTab({index})");
        var tabs = Context.Driver.FindElements(MobileBy.XPath("//TabBar/TabItem"));
        if (index < 0 || index >= tabs.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        tabs[index.Value].Click();
    }

    public virtual void SelectTab(string? name, int? timeoutMs = null);
    public virtual int GetSelectedTabIndex(int? timeoutMs = null);
    public virtual string? GetSelectedTabName(int? timeoutMs = null);

    #endregion
}
```

### 1.5 FlyoutPageBase

```csharp
public abstract class FlyoutPageBase : PageObjectBase
{
    protected FlyoutPageBase(MauiTestContext context) : base(context) { }

    #region Flyout Navigation (Example: OpenFlyout)

    public virtual void OpenFlyout(int? timeoutMs = null)
    {
        Log("OpenFlyout()");
        if (!IsFlyoutOpen(timeoutMs))
        {
            // Swipe from left or click hamburger
            Context.Driver.ExecuteScript("mobile: swipe", new Dictionary<string, object>
            {
                ["startX"] = 0, ["startY"] = 300,
                ["endX"] = 200, ["endY"] = 300, ["duration"] = 300
            });
        }
    }

    public virtual void CloseFlyout(int? timeoutMs = null);
    public virtual bool IsFlyoutOpen(int? timeoutMs = null);
    public virtual void SelectFlyoutItem(string? name, int? timeoutMs = null);

    #endregion
}
```

---

## 2. Blazor Page Classes

### 2.1 AsyncPageObjectBase

```csharp
public abstract class AsyncPageObjectBase : IAsyncPageObject
{
    protected BlazorTestContext Context { get; }
    protected IPage Page => Context.Page;

    protected AsyncPageObjectBase(BlazorTestContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Page Identity (Example: PageName)

    /// <summary>Page name for logging.</summary>
    public virtual string PageName => GetType().Name;

    /// <summary>URL path for this page (e.g., "/dashboard").</summary>
    protected virtual string? PagePath => null;

    /// <summary>Test ID of the page root element.</summary>
    protected virtual string? PageTestId => null;

    #endregion

    #region Navigation (Example: WaitForPageAsync)

    public virtual async Task WaitForPageAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"WaitForPageAsync({PageName})");
        var timeout = timeoutMs ?? Context.DefaultTimeoutMs;

        if (PagePath is not null)
        {
            await Page.WaitForURLAsync(url => url.Contains(PagePath), 
                new() { Timeout = timeout });
        }
        else if (PageTestId is not null)
        {
            await Page.Locator($"[data-testid='{PageTestId}']")
                .WaitForAsync(new() { Timeout = timeout });
        }
    }

    public virtual Task AssertOnPageAsync(string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<bool> IsOnPageAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Navigation Actions (Example: NavigateToAsync)

    public virtual async Task NavigateToAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"NavigateToAsync({PageName})");
        if (PagePath is not null)
        {
            await Page.GotoAsync(Context.BaseUrl + PagePath, 
                new() { Timeout = timeoutMs ?? Context.DefaultTimeoutMs });
        }
    }

    public virtual Task GoBackAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task RefreshAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region URL & Title (Example: GetCurrentUrlAsync)

    public virtual async Task<string> GetCurrentUrlAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return Page.Url;
    }

    public virtual Task<string> GetTitleAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task AssertTitleAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task AssertUrlAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Control Factory (Example: CreateControl)

    protected virtual T CreateControl<T>(ControlLocator locator) where T : class
    {
        return (T)Activator.CreateInstance(typeof(T), Context, locator, this)!;
    }

    protected virtual T CreateControl<T>(string testId) where T : class
    {
        return CreateControl<T>(By.TestId(testId));
    }

    #endregion

    #region Logging

    protected virtual void Log(string message)
    {
        Context.Logger?.LogInformation("[{PageName}] {Message}", PageName, message);
    }

    #endregion
}
```

### 2.2 AsyncBusyPageBase

```csharp
public abstract class AsyncBusyPageBase : AsyncPageObjectBase
{
    protected AsyncBusyPageBase(BlazorTestContext context) : base(context) { }

    #region Busy Indicator

    /// <summary>CSS selector for busy/loading indicator.</summary>
    protected virtual string BusyIndicatorSelector => "[data-testid='loading'], .loading, .spinner";

    protected ILocator BusyIndicator => Page.Locator(BusyIndicatorSelector);

    #endregion

    #region Busy State (Example: IsBusyAsync)

    public virtual async Task<bool> IsBusyAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await BusyIndicator.IsVisibleAsync();
    }

    public virtual Task AssertBusyAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Wait Helpers (Example: WaitUntilNotBusyAsync)

    public virtual async Task WaitUntilNotBusyAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("WaitUntilNotBusyAsync()");
        var timeout = timeoutMs ?? Context.DefaultTimeoutMs * 5;
        await BusyIndicator.WaitForAsync(new() 
        { 
            State = WaitForSelectorState.Hidden, 
            Timeout = timeout 
        });
    }

    public virtual Task WaitUntilBusyAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Override WaitForPage to account for busy state

    public override async Task WaitForPageAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await base.WaitForPageAsync(timeoutMs, ct);
        await WaitUntilNotBusyAsync(timeoutMs, ct);
    }

    #endregion
}
```

### 2.3 AsyncFormPageBase

```csharp
public abstract class AsyncFormPageBase : AsyncPageObjectBase
{
    protected AsyncFormPageBase(BlazorTestContext context) : base(context) { }

    #region Form State (Example: IsValidAsync)

    public virtual async Task<bool> IsValidAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var form = Page.Locator("form");
        return await form.EvaluateAsync<bool>("f => f.checkValidity()");
    }

    public virtual Task AssertValidAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Form Actions (Example: SubmitAsync)

    public virtual async Task SubmitAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("SubmitAsync()");
        var submitButton = Page.Locator("button[type='submit'], input[type='submit']").First;
        await submitButton.ClickAsync(new() { Timeout = timeoutMs ?? Context.DefaultTimeoutMs });
    }

    public virtual Task ResetAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Validation Errors (Example: GetValidationErrorsAsync)

    public virtual async Task<IReadOnlyList<string>> GetValidationErrorsAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var errors = Page.Locator(".validation-error, .field-validation-error, [data-valmsg-for]");
        var count = await errors.CountAsync();
        var messages = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var text = await errors.Nth(i).InnerTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
                messages.Add(text);
        }
        return messages;
    }

    public virtual Task AssertNoValidationErrorsAsync(string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.4 AsyncModalPageBase

```csharp
public abstract class AsyncModalPageBase : AsyncPageObjectBase
{
    protected AsyncModalPageBase(BlazorTestContext context) : base(context) { }

    /// <summary>CSS selector for the modal container.</summary>
    protected virtual string ModalSelector => ".modal, [role='dialog']";

    protected ILocator Modal => Page.Locator(ModalSelector);

    #region Modal State (Example: IsOpenAsync)

    public virtual async Task<bool> IsOpenAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await Modal.IsVisibleAsync();
    }

    public virtual Task AssertOpenAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Modal Actions (Example: CloseAsync)

    public virtual async Task CloseAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("CloseAsync()");
        var closeButton = Modal.Locator("button.close, [aria-label='Close'], .modal-close").First;
        await closeButton.ClickAsync(new() { Timeout = timeoutMs ?? Context.DefaultTimeoutMs });
    }

    public virtual Task ConfirmAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task CancelAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Modal Title (Example: GetTitleAsync)

    public override async Task<string> GetTitleAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var title = Modal.Locator(".modal-title, [role='heading'], h1, h2").First;
        return await title.InnerTextAsync();
    }

    #endregion

    #region Wait for Open/Close

    public override async Task WaitForPageAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"WaitForPageAsync({PageName})");
        await Modal.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs ?? Context.DefaultTimeoutMs });
    }

    public virtual async Task WaitForCloseAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("WaitForCloseAsync()");
        await Modal.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = timeoutMs ?? Context.DefaultTimeoutMs });
    }

    #endregion
}
```

---

## 3. Inheritance Summary

```
MAUI:
PageObjectBase : IPageObject
├── BusyPageBase : IBusyPage
├── ContentPageBase
├── TabbedPageBase
└── FlyoutPageBase

Blazor:
AsyncPageObjectBase : IAsyncPageObject
├── AsyncBusyPageBase
├── AsyncFormPageBase
└── AsyncModalPageBase
```

---

## 4. Usage Examples

### MAUI Page Example

```csharp
public class LoginPage : BusyPageBase
{
    public LoginPage(MauiTestContext context) : base(context) { }

    protected override string PageAutomationId => "LoginPage";

    // Controls
    public EntryControl Username => CreateControl<EntryControl>("UsernameEntry");
    public EntryControl Password => CreateControl<EntryControl>("PasswordEntry");
    public ButtonControl LoginButton => CreateControl<ButtonControl>("LoginButton");

    // Actions
    public void Login(string username, string password)
    {
        Username.Enter(username);
        Password.Enter(password);
        LoginButton.Click();
    }
}
```

### Blazor Page Example

```csharp
public class LoginPage : AsyncBusyPageBase
{
    public LoginPage(BlazorTestContext context) : base(context) { }

    protected override string PagePath => "/login";
    protected override string PageTestId => "login-page";

    // Controls
    public InputControl Username => CreateControl<InputControl>("username-input");
    public InputControl Password => CreateControl<InputControl>("password-input");
    public ButtonControl LoginButton => CreateControl<ButtonControl>("login-button");

    // Actions
    public async Task LoginAsync(string username, string password)
    {
        await Username.EnterAsync(username);
        await Password.EnterAsync(password);
        await LoginButton.ClickAsync();
    }
}
```

---

**Index:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

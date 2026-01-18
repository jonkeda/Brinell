namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Shell FlyoutItem control for navigation.
/// Uses XPath with @Name (Title) instead of AccessibilityId because
/// FlyoutItem's AutomationId doesn't propagate to the Windows UI Automation tree.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiFlyoutItemControl<TScope> : MauiControlBase<TScope>, IClickableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly string _title;

    /// <summary>
    /// Creates a new flyout item control using the item's Title for location.
    /// </summary>
    /// <param name="scope">The scope (page) providing element finding.</param>
    /// <param name="title">The Title of the FlyoutItem (becomes @Name in UI tree).</param>
    public MauiFlyoutItemControl(IMauiScope<TScope> scope, string title)
        : base(scope, new Locator(LocatorStrategy.XPath, $"//*[@Name='{title}']"))
    {
        _title = title ?? throw new ArgumentNullException(nameof(title));
    }

    /// <summary>
    /// Gets the title of this flyout item.
    /// </summary>
    public string Title => _title;

    #region IClickableControlObject<TScope> Implementation

    /// <inheritdoc />
    public TScope Click(int? timeoutMs = null)
    {
        Run(nameof(Click), () =>
        {
            var element = FindElement();
            element.Click();
        });
        return ContainingScope;
    }

    /// <inheritdoc />
    public TScope DoubleClick(int? timeoutMs = null)
    {
        Run(nameof(DoubleClick), () =>
        {
            var element = FindElement();
            element.Click();
            element.Click();
        });
        return ContainingScope;
    }

    /// <inheritdoc />
    public TScope RightClick(int? timeoutMs = null)
    {
        Run(nameof(RightClick), () =>
        {
            var element = FindElement();
            var unwrappedElement = element.UnwrapElement();
            var unwrappedDriver = Context.Driver.UnwrapDriver();
            
            var actions = new OpenQA.Selenium.Interactions.Actions(unwrappedDriver);
            actions.ContextClick(unwrappedElement).Perform();
        });
        return ContainingScope;
    }

    /// <inheritdoc />
    public bool? IsClickable()
    {
        var isVisible = IsVisible();
        var isEnabled = IsEnabled();
        
        if (isVisible == null || isEnabled == null)
        {
            return null;
        }
        
        return isVisible.Value && isEnabled.Value;
    }

    /// <inheritdoc />
    public bool WaitClickable(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        return Poll(
            () => IsClickable() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);
    }

    /// <summary>
    /// Asserts the flyout item is clickable.
    /// </summary>
    public TScope AssertClickable(string? message = null, int? timeoutMs = null)
        => AssertClickable(true, message, timeoutMs);

    /// <inheritdoc />
    public TScope AssertClickable(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertClickable), expected, () =>
        {
            WaitClickable(expected, timeoutMs);
            return IsClickable();
        }, message ?? $"Expected flyout item '{_title}' {(expected.Value ? "to be clickable" : "not to be clickable")}.");
    }

    #endregion
}

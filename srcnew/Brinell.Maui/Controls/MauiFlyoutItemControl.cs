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

    #region IClickableControlObject<TScope> Implementation - Public API

    /// <inheritdoc />
    public TScope Click(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Click), timeoutMs, element =>
        {
            ClickCore(element);
        });
    }

    /// <inheritdoc />
    public TScope DoubleClick(int? timeoutMs = null)
    {
        return RunWithElement(nameof(DoubleClick), timeoutMs, element =>
        {
            DoubleClickCore(element);
        });
    }

    /// <inheritdoc />
    public TScope RightClick(int? timeoutMs = null)
    {
        return RunWithElement(nameof(RightClick), timeoutMs, element =>
        {
            RightClickCore(element);
        });
    }

    /// <inheritdoc />
    public bool? IsClickable()
    {
        return IsClickableCore(TryFindElement());
    }

    /// <inheritdoc />
    public bool WaitClickable(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null)
            return expected.Value == false;

        return WaitClickableCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
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

    #region Core Methods (Element-Aware) - Internal Implementation

    /// <summary>
    /// Core implementation of Click using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected void ClickCore(IMauiElement element)
    {
        element.Click();
    }

    /// <summary>
    /// Core implementation of DoubleClick using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected void DoubleClickCore(IMauiElement element)
    {
        element.Click();
        element.Click();
    }

    /// <summary>
    /// Core implementation of RightClick using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected void RightClickCore(IMauiElement element)
    {
        var unwrappedElement = element.UnwrapElement();
        var unwrappedDriver = Context.Driver.UnwrapDriver();

        var actions = new OpenQA.Selenium.Interactions.Actions(unwrappedDriver);
        actions.ContextClick(unwrappedElement).Perform();
    }

    /// <summary>
    /// Checks if element is clickable (visible and enabled) using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if clickable, false if not, null if element not found.</returns>
    protected bool? IsClickableCore(IMauiElement? element)
    {
        var isVisible = IsVisibleCore(element);
        var isEnabled = IsEnabledCore(element);

        if (isVisible == null || isEnabled == null)
            return null;

        return isVisible.Value && isEnabled.Value;
    }

    /// <summary>
    /// Polls clickable state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected clickable state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool WaitClickableCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(element, e => IsClickableCore(e) == expected, timeoutMs);
    }

    #endregion
}

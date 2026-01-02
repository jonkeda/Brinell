using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Exceptions;
using Brinell.Stride.Controls;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Pages;

/// <summary>
/// Base class for all Stride UI page objects.
/// Provides typed access to controls and standard page operations.
/// </summary>
public abstract class StridePageBase : IPageObject
{
    /// <summary>
    /// The test context.
    /// </summary>
    protected readonly StrideTestContext StrideContext;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public string AutomationId { get; }

    /// <inheritdoc />
    public ITestContext Context => StrideContext;

    /// <summary>
    /// Create a new page object.
    /// </summary>
    protected StridePageBase(StrideTestContext context, string automationId = "")
    {
        StrideContext = context ?? throw new ArgumentNullException(nameof(context));
        AutomationId = automationId ?? "";
    }

    #region Control Factory Methods

    /// <summary>
    /// Create a button control.
    /// </summary>
    protected StrideButtonControl Button(string automationId)
        => new(StrideContext, this, automationId);

    /// <summary>
    /// Create a text block control.
    /// </summary>
    protected StrideTextBlockControl TextBlock(string automationId)
        => new(StrideContext, this, automationId);

    /// <summary>
    /// Create an edit text control.
    /// </summary>
    protected StrideEditTextControl EditText(string automationId)
        => new(StrideContext, this, automationId);

    /// <summary>
    /// Create a checkbox control.
    /// </summary>
    protected StrideCheckBoxControl CheckBox(string automationId)
        => new(StrideContext, this, automationId);

    /// <summary>
    /// Create a toggle button control.
    /// </summary>
    protected StrideToggleButtonControl ToggleButton(string automationId)
        => new(StrideContext, this, automationId);

    /// <summary>
    /// Create a slider control.
    /// </summary>
    protected StrideSliderControl Slider(string automationId)
        => new(StrideContext, this, automationId);

    /// <summary>
    /// Create a progress bar control.
    /// </summary>
    protected StrideProgressBarControl ProgressBar(string automationId)
        => new(StrideContext, this, automationId);

    /// <summary>
    /// Create a list box control.
    /// </summary>
    protected StrideListBoxControl ListBox(string automationId)
        => new(StrideContext, this, automationId);

    /// <summary>
    /// Create a combo box control.
    /// </summary>
    protected StrideComboBoxControl ComboBox(string automationId)
        => new(StrideContext, this, automationId);

    /// <summary>
    /// Create an image control.
    /// </summary>
    protected StrideImageControl Image(string automationId)
        => new(StrideContext, this, automationId);

    /// <summary>
    /// Create a panel control.
    /// </summary>
    protected StridePanelControl Panel(string automationId)
        => new(StrideContext, this, automationId);

    #endregion

    #region Page Lifecycle

    /// <inheritdoc />
    public virtual bool IsDisplayed()
    {
        if (!string.IsNullOrEmpty(AutomationId))
        {
            return StrideContext.ElementExists(AutomationId) && StrideContext.ElementIsVisible(AutomationId);
        }
        return true;
    }

    /// <inheritdoc />
    public virtual bool IsReady()
    {
        return IsDisplayed() && !StrideContext.IsGameBusy();
    }

    /// <summary>
    /// Check if page is currently active (alias for IsDisplayed).
    /// </summary>
    public virtual bool IsActive() => IsDisplayed();

    /// <inheritdoc />
    public virtual bool WaitForDisplayed(int? timeoutMs = null)
    {
        return StrideContext.WaitFor(
            () => IsDisplayed(),
            timeoutMs,
            $"page '{Name}' displayed");
    }

    /// <inheritdoc />
    public virtual bool WaitForReady(int? timeoutMs = null)
    {
        return StrideContext.WaitFor(
            () => IsReady(),
            timeoutMs,
            $"page '{Name}' ready");
    }

    /// <summary>
    /// Wait for page to be active (alias for WaitForDisplayed).
    /// </summary>
    public virtual bool WaitActive(int? timeoutMs = null) => WaitForDisplayed(timeoutMs);

    /// <inheritdoc />
    public virtual void CheckDisplayed(int? timeoutMs = null)
    {
        if (!WaitForDisplayed(timeoutMs))
        {
            throw new PageNotDisplayedException($"Page '{Name}' is not displayed.");
        }
    }

    /// <inheritdoc />
    public virtual void CheckReady(int? timeoutMs = null)
    {
        if (!WaitForReady(timeoutMs))
        {
            throw new PageNotReadyException($"Page '{Name}' is not ready.");
        }
    }

    /// <summary>
    /// Check page is active - throws if not (alias for CheckDisplayed).
    /// </summary>
    public virtual void CheckActive(int? timeoutMs = null) => CheckDisplayed(timeoutMs);

    /// <inheritdoc />
    public virtual string? TakeScreenshot(string suffix = "")
    {
        return StrideContext.TakeScreenshot($"{Name}_{suffix}");
    }

    /// <summary>
    /// Navigate to this page (implement in derived classes).
    /// </summary>
    public virtual void Navigate()
    {
        // Default: no-op (override in subclasses)
    }

    #endregion

    #region Input Helpers

    /// <summary>
    /// Click at specific screen coordinates.
    /// </summary>
    protected void ClickAt(int x, int y)
    {
        StrideContext.Input.Click(x, y);
    }

    /// <summary>
    /// Type text using keyboard.
    /// </summary>
    protected void TypeText(string text)
    {
        StrideContext.TypeText(text);
    }

    /// <summary>
    /// Press a key.
    /// </summary>
    protected void PressKey(VirtualKey key)
    {
        StrideContext.PressKey(key);
    }

    /// <summary>
    /// Hold a key for a specified duration.
    /// </summary>
    protected void HoldKey(VirtualKey key, int durationMs)
    {
        StrideContext.HoldKey(key, durationMs);
    }

    /// <summary>
    /// Press Escape key (common for closing dialogs).
    /// </summary>
    protected void PressEscape()
    {
        PressKey(VirtualKey.Escape);
    }

    /// <summary>
    /// Press Enter key (common for confirming).
    /// </summary>
    protected void PressEnter()
    {
        PressKey(VirtualKey.Enter);
    }

    /// <summary>
    /// Press Tab key (common for navigation).
    /// </summary>
    protected void PressTab()
    {
        PressKey(VirtualKey.Tab);
    }

    #endregion

    #region Wait Helpers

    /// <summary>
    /// Wait for a condition.
    /// </summary>
    protected bool WaitFor(Func<bool> condition, int? timeoutMs = null, string? description = null)
    {
        return StrideContext.WaitFor(condition, timeoutMs, description ?? "condition");
    }

    /// <summary>
    /// Wait for an element to exist.
    /// </summary>
    protected bool WaitElementExists(string automationId, int? timeoutMs = null)
    {
        return StrideContext.WaitFor(
            () => StrideContext.ElementExists(automationId),
            timeoutMs,
            $"element '{automationId}' exists");
    }

    /// <summary>
    /// Wait for an element to be visible.
    /// </summary>
    protected bool WaitElementVisible(string automationId, int? timeoutMs = null)
    {
        return StrideContext.WaitFor(
            () => StrideContext.ElementIsVisible(automationId),
            timeoutMs,
            $"element '{automationId}' visible");
    }

    /// <summary>
    /// Wait for an element to disappear.
    /// </summary>
    protected bool WaitElementGone(string automationId, int? timeoutMs = null)
    {
        return StrideContext.WaitFor(
            () => !StrideContext.ElementExists(automationId),
            timeoutMs,
            $"element '{automationId}' gone");
    }

    #endregion
}

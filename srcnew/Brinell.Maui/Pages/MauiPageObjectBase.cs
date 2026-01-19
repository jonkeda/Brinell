using Brinell.Maui.Controls;
using Brinell.Maui.Controls.Buttons;
using Brinell.Maui.Controls.Collection;
using Brinell.Maui.Controls.Container;
using Brinell.Maui.Controls.DateTime;
using Brinell.Maui.Controls.Display;
using Brinell.Maui.Controls.Media;
using Brinell.Maui.Controls.Navigation;
using Brinell.Maui.Controls.Range;
using Brinell.Maui.Controls.Selection;
using Brinell.Maui.Controls.Text;
using Brinell.Maui.Controls.Toggle;

namespace Brinell.Maui.Pages;

/// <summary>
/// Base class for MAUI page objects with fluent method chaining support.
/// Uses CRTP (Curiously Recurring Template Pattern) for strongly-typed fluent returns.
/// Pages delegate element finding to the test context (driver root search).
/// Implements IMauiPage so pages can be used as scopes for child controls.
/// </summary>
/// <typeparam name="TSelf">The concrete page type (CRTP pattern).</typeparam>
public abstract class MauiPageObjectBase<TSelf> : MauiObjectBase, IMauiPage<TSelf>
    where TSelf : MauiPageObjectBase<TSelf>
{
    private readonly IMauiTestContext _context;
    
    /// <summary>
    /// Creates a new page object with the specified context.
    /// </summary>
    /// <param name="context">The MAUI test context.</param>
    protected MauiPageObjectBase(IMauiTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    
    /// <inheritdoc />
    public override IMauiTestContext Context => _context;
    
    /// <summary>
    /// Gets this page as the typed page reference (for fluent chaining).
    /// </summary>
    public TSelf Self => (TSelf)this;
    
    #region IPageObject Implementation
    
    /// <inheritdoc />
    public abstract string Name { get; }
    
    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => _context.DefaultLocatorStrategy;
    
    /// <inheritdoc />
    public abstract bool IsLoaded(int? timeoutMs = null);
    
    /// <inheritdoc />
    public bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        var timeout = timeoutMs ?? _context.Timeouts.PageLoad;
        return Poll(
            () => IsLoaded() == expected.Value,
            timeout);
    }
    
    /// <inheritdoc />
    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return;
        
        if (!WaitLoaded(expected, timeoutMs))
        {
            var actual = IsLoaded();
            throw new PageLoadException(
                message ?? $"Expected page '{Name}' {(expected.Value ? "to be loaded" : "not to be loaded")} but loaded state is {actual}.");
        }
    }
    
    /// <inheritdoc />
    public virtual string? GetTitle(int? timeoutMs = null)
    {
        // Default implementation returns page name
        // Override for platforms that support page titles
        return Name;
    }
    
    /// <inheritdoc />
    public bool WaitTitle(string? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return Poll(
            () => GetTitle() == expected,
            timeout);
    }
    
    /// <inheritdoc />
    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return;
        
        if (!WaitTitle(expected, timeoutMs))
        {
            var actual = GetTitle();
            throw new PageLoadException(
                message ?? $"Expected page title '{expected}' but got '{actual ?? "(null)"}'.");
        }
    }
    
    /// <inheritdoc />
    public void TakeScreenshot(string? filename = null, int? timeoutMs = null)
    {
        var path = filename ?? $"{Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        _context.SaveScreenshot(path);
    }
    
    #endregion
    
    #region IMauiElementScope Implementation
    
    /// <inheritdoc />
    public IPageObject? Page => this;
    
    /// <inheritdoc />
    public bool IsReady(int? timeoutMs = null)
    {
        // For pages, ready means loaded
        return IsLoaded(timeoutMs);
    }
    
    /// <inheritdoc />
    public bool WaitReady(int? timeoutMs = null)
    {
        // For pages, wait ready means wait loaded
        return WaitLoaded(true, timeoutMs);
    }
    
    /// <inheritdoc />
    public IMauiElement? TryFindElement(Locator locator)
    {
        // Delegate to context (searches from driver root)
        return _context.TryFindElement(locator);
    }
    
    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator)
    {
        // Delegate to context (searches from driver root)
        return _context.FindElement(locator);
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
    {
        // Delegate to context (searches from driver root)
        return _context.FindElements(locator);
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a generic control within this page scope.
    /// </summary>
    protected MauiControlBase<TSelf> Control(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a generic control within this page scope.
    /// </summary>
    protected MauiControlBase<TSelf> Control(string locator)
        => new (this, locator);

    /// <summary>
    /// Creates a button control within this page scope.
    /// </summary>
    protected MauiButtonControl<TSelf> Button(Locator locator)
        => new(this, locator);
    
    /// <summary>
    /// Creates a button control within this page scope using the scope default locator.
    /// </summary>
    protected MauiButtonControl<TSelf> Button(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates an entry control within this page scope.
    /// </summary>
    protected MauiEntryControl<TSelf> Entry(Locator locator)
        => new(this, locator);
    
    /// <summary>
    /// Creates an entry control within this page scope using automation ID.
    /// </summary>
    protected MauiEntryControl<TSelf> Entry(string locator)
        => new (this, locator);

    #region Display Controls

    /// <summary>
    /// Creates a label control within this page scope.
    /// </summary>
    protected MauiLabelControl<TSelf> Label(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a label control within this page scope using automation ID.
    /// </summary>
    protected MauiLabelControl<TSelf> Label(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a progress bar control within this page scope.
    /// </summary>
    protected MauiProgressBarControl<TSelf> ProgressBar(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a progress bar control within this page scope using automation ID.
    /// </summary>
    protected MauiProgressBarControl<TSelf> ProgressBar(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates an activity indicator control within this page scope.
    /// </summary>
    protected MauiActivityIndicatorControl<TSelf> ActivityIndicator(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an activity indicator control within this page scope using automation ID.
    /// </summary>
    protected MauiActivityIndicatorControl<TSelf> ActivityIndicator(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates an image control within this page scope.
    /// </summary>
    protected MauiImageControl<TSelf> Image(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an image control within this page scope using automation ID.
    /// </summary>
    protected MauiImageControl<TSelf> Image(string locator)
        => new(this, locator);

    #endregion

    #region Toggle Controls

    /// <summary>
    /// Creates a checkbox control within this page scope.
    /// </summary>
    protected MauiCheckBoxControl<TSelf> CheckBox(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a checkbox control within this page scope using automation ID.
    /// </summary>
    protected MauiCheckBoxControl<TSelf> CheckBox(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a switch control within this page scope.
    /// </summary>
    protected MauiSwitchControl<TSelf> Switch(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a switch control within this page scope using automation ID.
    /// </summary>
    protected MauiSwitchControl<TSelf> Switch(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a radio button control within this page scope.
    /// </summary>
    protected MauiRadioButtonControl<TSelf> RadioButton(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a radio button control within this page scope using automation ID.
    /// </summary>
    protected MauiRadioButtonControl<TSelf> RadioButton(string locator)
        => new(this, locator);

    #endregion

    #region Text Controls

    /// <summary>
    /// Creates an editor control within this page scope.
    /// </summary>
    protected MauiEditorControl<TSelf> Editor(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an editor control within this page scope using automation ID.
    /// </summary>
    protected MauiEditorControl<TSelf> Editor(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a search bar control within this page scope.
    /// </summary>
    protected MauiSearchBarControl<TSelf> SearchBar(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a search bar control within this page scope using automation ID.
    /// </summary>
    protected MauiSearchBarControl<TSelf> SearchBar(string locator)
        => new(this, locator);

    #endregion

    #region Selection Controls

    /// <summary>
    /// Creates a picker control within this page scope.
    /// </summary>
    protected MauiPickerControl<TSelf> Picker(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a picker control within this page scope using automation ID.
    /// </summary>
    protected MauiPickerControl<TSelf> Picker(string locator)
        => new(this, locator);

    #endregion

    #region Range Controls

    /// <summary>
    /// Creates a slider control within this page scope.
    /// </summary>
    protected MauiSliderControl<TSelf> Slider(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a slider control within this page scope using automation ID.
    /// </summary>
    protected MauiSliderControl<TSelf> Slider(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a stepper control within this page scope.
    /// </summary>
    protected MauiStepperControl<TSelf> Stepper(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a stepper control within this page scope using automation ID.
    /// </summary>
    protected MauiStepperControl<TSelf> Stepper(string locator)
        => new(this, locator);

    #endregion

    #region DateTime Controls

    /// <summary>
    /// Creates a date picker control within this page scope.
    /// </summary>
    protected MauiDatePickerControl<TSelf> DatePicker(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a date picker control within this page scope using automation ID.
    /// </summary>
    protected MauiDatePickerControl<TSelf> DatePicker(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a time picker control within this page scope.
    /// </summary>
    protected MauiTimePickerControl<TSelf> TimePicker(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a time picker control within this page scope using automation ID.
    /// </summary>
    protected MauiTimePickerControl<TSelf> TimePicker(string locator)
        => new(this, locator);

    #endregion

    #region Container Controls

    /// <summary>
    /// Creates a scroll view control within this page scope.
    /// </summary>
    protected MauiScrollViewControl<TSelf> ScrollView(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a scroll view control within this page scope using automation ID.
    /// </summary>
    protected MauiScrollViewControl<TSelf> ScrollView(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates an expander control within this page scope.
    /// </summary>
    protected MauiExpanderControl<TSelf> Expander(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an expander control within this page scope using automation ID.
    /// </summary>
    protected MauiExpanderControl<TSelf> Expander(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a refresh view control within this page scope.
    /// </summary>
    protected MauiRefreshViewControl<TSelf> RefreshView(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a refresh view control within this page scope using automation ID.
    /// </summary>
    protected MauiRefreshViewControl<TSelf> RefreshView(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a swipe view control within this page scope.
    /// </summary>
    protected MauiSwipeViewControl<TSelf> SwipeView(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a swipe view control within this page scope using automation ID.
    /// </summary>
    protected MauiSwipeViewControl<TSelf> SwipeView(string locator)
        => new(this, locator);

    #endregion

    // Note: ListView and CollectionView factory methods are not provided here
    // because they require TItem type parameter and item factory function.
    // Use MauiListViewControl<TScope, TItem> and MauiCollectionViewControl<TScope, TItem>
    // directly in page objects for type-safe list control access.

    #region Navigation Controls

    /// <summary>
    /// Creates a menu control within this page scope.
    /// </summary>
    protected MauiMenuControl<TSelf> Menu(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a menu control within this page scope using automation ID.
    /// </summary>
    protected MauiMenuControl<TSelf> Menu(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a toolbar control within this page scope.
    /// </summary>
    protected MauiToolbarControl<TSelf> Toolbar(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a toolbar control within this page scope using automation ID.
    /// </summary>
    protected MauiToolbarControl<TSelf> Toolbar(string locator)
        => new(this, locator);

    #endregion

    #region Media Controls

    /// <summary>
    /// Creates a web view control within this page scope.
    /// </summary>
    protected MauiWebViewControl<TSelf> WebView(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a web view control within this page scope using automation ID.
    /// </summary>
    protected MauiWebViewControl<TSelf> WebView(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a media element control within this page scope.
    /// </summary>
    protected MauiMediaElementControl<TSelf> MediaElement(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a media element control within this page scope using automation ID.
    /// </summary>
    protected MauiMediaElementControl<TSelf> MediaElement(string locator)
        => new(this, locator);

    #endregion

    #region Button Controls

    /// <summary>
    /// Creates an image button control within this page scope.
    /// </summary>
    protected MauiImageButtonControl<TSelf> ImageButton(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an image button control within this page scope using automation ID.
    /// </summary>
    protected MauiImageButtonControl<TSelf> ImageButton(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a link control within this page scope.
    /// </summary>
    protected MauiLinkControl<TSelf> Link(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a link control within this page scope using automation ID.
    /// </summary>
    protected MauiLinkControl<TSelf> Link(string locator)
        => new(this, locator);

    #endregion
    
    #endregion
}

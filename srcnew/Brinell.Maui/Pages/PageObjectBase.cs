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
public abstract class PageObjectBase<TSelf> : ObjectBase, IMauiPage<TSelf>
    where TSelf : PageObjectBase<TSelf>
{
    private readonly IMauiTestContext _context;
    
    /// <summary>
    /// Creates a new page object with the specified context.
    /// </summary>
    /// <param name="context">The MAUI test context.</param>
    protected PageObjectBase(IMauiTestContext context)
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
    public Label<TSelf> BusySentinel => Label("UITest_IsBusy");

    /// <inheritdoc />
    public virtual bool IsLoaded(int? timeoutMs = null)
        => BusySentinel.IsExists();

    /// <summary>
    /// Waits for the page to finish loading.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True when page becomes idle; otherwise false.</returns>
    public bool WaitIdle(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.Timeouts.PageLoad;
        return Poll(() => BusySentinel.GetText() == "False", timeout);
    }

    /// <summary>
    /// Asserts that the page is idle.
    /// </summary>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <exception cref="PageLoadException">Thrown when page does not become idle within timeout.</exception>
    public void AssertIdle(string? message = null, int? timeoutMs = null)
    {
        if (!WaitIdle(timeoutMs))
        {
            var actual = BusySentinel.GetText();
            throw new PageLoadException(
                message ?? $"Page '{Name}' did not become idle within timeout. UITest_IsBusy text: '{actual ?? "(not found)"}'.");
        }
    }
    
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
    /// Creates a button control within this page scope.
    /// </summary>
    protected Button<TSelf> Button(Locator locator)
        => new(this, locator);
    
    /// <summary>
    /// Creates a button control within this page scope using the scope default locator.
    /// </summary>
    protected Button<TSelf> Button(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates an entry control within this page scope.
    /// </summary>
    protected Entry<TSelf> Entry(Locator locator)
        => new(this, locator);
    
    /// <summary>
    /// Creates an entry control within this page scope using automation ID.
    /// </summary>
    protected Entry<TSelf> Entry(string locator)
        => new (this, locator);

    #region Display Controls

    /// <summary>
    /// Creates a label control within this page scope.
    /// </summary>
    protected Label<TSelf> Label(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a label control within this page scope using automation ID.
    /// </summary>
    protected Label<TSelf> Label(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a progress bar control within this page scope.
    /// </summary>
    protected ProgressBar<TSelf> ProgressBar(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a progress bar control within this page scope using automation ID.
    /// </summary>
    protected ProgressBar<TSelf> ProgressBar(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates an activity indicator control within this page scope.
    /// </summary>
    protected ActivityIndicator<TSelf> ActivityIndicator(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an activity indicator control within this page scope using automation ID.
    /// </summary>
    protected ActivityIndicator<TSelf> ActivityIndicator(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates an image control within this page scope.
    /// </summary>
    protected Image<TSelf> Image(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an image control within this page scope using automation ID.
    /// </summary>
    protected Image<TSelf> Image(string locator)
        => new(this, locator);

    #endregion

    #region Toggle Controls

    /// <summary>
    /// Creates a checkbox control within this page scope.
    /// </summary>
    protected CheckBox<TSelf> CheckBox(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a checkbox control within this page scope using automation ID.
    /// </summary>
    protected CheckBox<TSelf> CheckBox(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a switch control within this page scope.
    /// </summary>
    protected Switch<TSelf> Switch(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a switch control within this page scope using automation ID.
    /// </summary>
    protected Switch<TSelf> Switch(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a radio button control within this page scope.
    /// </summary>
    protected RadioButton<TSelf> RadioButton(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a radio button control within this page scope using automation ID.
    /// </summary>
    protected RadioButton<TSelf> RadioButton(string locator)
        => new(this, locator);

    #endregion

    #region Text Controls

    /// <summary>
    /// Creates an editor control within this page scope.
    /// </summary>
    protected Editor<TSelf> Editor(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an editor control within this page scope using automation ID.
    /// </summary>
    protected Editor<TSelf> Editor(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a search bar control within this page scope.
    /// </summary>
    protected SearchBar<TSelf> SearchBar(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a search bar control within this page scope using automation ID.
    /// </summary>
    protected SearchBar<TSelf> SearchBar(string locator)
        => new(this, locator);

    #endregion

    #region Selection Controls

    /// <summary>
    /// Creates a picker control within this page scope.
    /// </summary>
    protected Picker<TSelf> Picker(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a picker control within this page scope using automation ID.
    /// </summary>
    protected Picker<TSelf> Picker(string locator)
        => new(this, locator);

    #endregion

    #region Range Controls

    /// <summary>
    /// Creates a slider control within this page scope.
    /// </summary>
    protected Slider<TSelf> Slider(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a slider control within this page scope using automation ID.
    /// </summary>
    protected Slider<TSelf> Slider(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a stepper control within this page scope.
    /// </summary>
    protected Stepper<TSelf> Stepper(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a stepper control within this page scope using automation ID.
    /// </summary>
    protected Stepper<TSelf> Stepper(string locator)
        => new(this, locator);

    #endregion

    #region DateTime Controls

    /// <summary>
    /// Creates a date picker control within this page scope.
    /// </summary>
    protected DatePicker<TSelf> DatePicker(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a date picker control within this page scope using automation ID.
    /// </summary>
    protected DatePicker<TSelf> DatePicker(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a time picker control within this page scope.
    /// </summary>
    protected TimePicker<TSelf> TimePicker(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a time picker control within this page scope using automation ID.
    /// </summary>
    protected TimePicker<TSelf> TimePicker(string locator)
        => new(this, locator);

    #endregion

    #region Container Controls

    /// <summary>
    /// Creates a scroll view control within this page scope.
    /// </summary>
    protected ScrollView<TSelf> ScrollView(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a grid control within this page scope.
    /// </summary>
    protected Grid<TSelf> Grid(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a grid control within this page scope using automation ID.
    /// </summary>
    protected Grid<TSelf> Grid(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a collection view control within this page scope.
    /// </summary>
    protected CollectionView<TSelf> CollectionView(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a collection view control within this page scope using automation ID.
    /// </summary>
    protected CollectionView<TSelf> CollectionView(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a scroll view control within this page scope using automation ID.
    /// </summary>
    protected ScrollView<TSelf> ScrollView(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates an expander control within this page scope.
    /// </summary>
    protected Expander<TSelf> Expander(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an expander control within this page scope using automation ID.
    /// </summary>
    protected Expander<TSelf> Expander(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a refresh view control within this page scope.
    /// </summary>
    protected RefreshView<TSelf> RefreshView(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a refresh view control within this page scope using automation ID.
    /// </summary>
    protected RefreshView<TSelf> RefreshView(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a swipe view control within this page scope.
    /// </summary>
    protected SwipeView<TSelf> SwipeView(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a swipe view control within this page scope using automation ID.
    /// </summary>
    protected SwipeView<TSelf> SwipeView(string locator)
        => new(this, locator);

    #endregion

    // Note: ListView and CollectionView factory methods are not provided here
    // because they require TItem type parameter and item factory function.
    // Use ListView<TScope, TItem> and CollectionView<TScope, TItem>
    // directly in page objects for type-safe list control access.

    #region Navigation Controls

    /// <summary>
    /// Creates a menu control within this page scope.
    /// </summary>
    protected Menu<TSelf> Menu(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a menu control within this page scope using automation ID.
    /// </summary>
    protected Menu<TSelf> Menu(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a toolbar control within this page scope.
    /// </summary>
    protected Toolbar<TSelf> Toolbar(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a toolbar control within this page scope using automation ID.
    /// </summary>
    protected Toolbar<TSelf> Toolbar(string locator)
        => new(this, locator);

    #endregion

    #region Media Controls

    /// <summary>
    /// Creates a web view control within this page scope.
    /// </summary>
    protected WebView<TSelf> WebView(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a web view control within this page scope using automation ID.
    /// </summary>
    protected WebView<TSelf> WebView(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a media element control within this page scope.
    /// </summary>
    protected MediaElement<TSelf> MediaElement(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a media element control within this page scope using automation ID.
    /// </summary>
    protected MediaElement<TSelf> MediaElement(string locator)
        => new(this, locator);

    #endregion

    #region Button Controls

    /// <summary>
    /// Creates an image button control within this page scope.
    /// </summary>
    protected ImageButton<TSelf> ImageButton(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates an image button control within this page scope using automation ID.
    /// </summary>
    protected ImageButton<TSelf> ImageButton(string locator)
        => new(this, locator);

    /// <summary>
    /// Creates a link control within this page scope.
    /// </summary>
    protected Link<TSelf> Link(Locator locator)
        => new(this, locator);

    /// <summary>
    /// Creates a link control within this page scope using automation ID.
    /// </summary>
    protected Link<TSelf> Link(string locator)
        => new(this, locator);

    #endregion
    
    #endregion
}

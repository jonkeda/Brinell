namespace Brinell.NativeAndroid;

public abstract class NativeAndroidPageObjectBase<TSelf> :
    IPageObject<NativeAndroidElement>,
    INativeAndroidScope<TSelf>
    where TSelf : NativeAndroidPageObjectBase<TSelf>
{
    protected NativeAndroidPageObjectBase(NativeAndroidTestContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public virtual string Name => GetType().Name;

    public NativeAndroidTestContext Context { get; }

    public TSelf Self => (TSelf)this;

    public virtual LocatorStrategy DefaultLocatorStrategy => Context.DefaultLocatorStrategy;

    public IPageObject? Page => this;

    protected virtual Locator? ReadyLocator => null;

    public virtual bool IsLoaded(int? timeoutMs = null)
    {
        var readyLocator = ReadyLocator;
        if (readyLocator is null)
        {
            return true;
        }

        return Context.Driver.TryFindElement(
            readyLocator,
            out var element,
            timeoutMs ?? Context.Timeouts.PageLoad)
            && element is not null
            && element.Visible;
    }

    public virtual bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => IsLoaded(0) == expected.Value, timeoutMs ?? Context.Timeouts.PageLoad);
    }

    public virtual void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return;
        }

        if (!WaitLoaded(expected, timeoutMs))
        {
            throw new PageLoadException(message ?? $"Expected page '{Name}' loaded state to be {expected}.");
        }
    }

    public virtual string? GetTitle(int? timeoutMs = null)
        => Context.Driver.GetCapability("appActivity") ?? Name;

    public virtual bool WaitTitle(string? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(
            () => string.Equals(GetTitle(0), expected, StringComparison.Ordinal),
            timeoutMs ?? Context.Timeouts.PageLoad);
    }

    public virtual void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return;
        }

        var actual = GetTitle(timeoutMs);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new AssertionException(message ?? $"Expected title '{expected}', actual '{actual}'.", expected, actual);
        }
    }

    public virtual void TakeScreenshot(string? filename = null, int? timeoutMs = null)
    {
        var path = filename ?? Path.Combine(
            Environment.CurrentDirectory,
            "artifacts",
            $"{Name}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.png");

        Context.Driver.SaveScreenshot(path);
    }

    public virtual bool IsReady(int? timeoutMs = null) => IsLoaded(timeoutMs);

    public virtual bool WaitReady(int? timeoutMs = null) => WaitLoaded(true, timeoutMs);

    public NativeAndroidElement? TryFindElement(Locator locator)
        => Context.Driver.TryFindElement(locator, out var element, 0) ? element : null;

    public NativeAndroidElement FindElement(Locator locator)
        => Context.Driver.FindElement(locator, Context.Timeouts.ElementFind);

    public IReadOnlyList<NativeAndroidElement> FindElements(Locator locator)
        => Context.Driver.FindElements(locator);

    public AndroidButton<TSelf> Button(Locator locator) => new(locator, Self);

    public AndroidButton<TSelf> Button(string locator) => new(locator, Self);

    public AndroidIconCommandButton<TSelf> IconCommandButton(Locator locator) => new(locator, Self);

    public AndroidIconCommandButton<TSelf> IconCommandButton(string locator) => new(locator, Self);

    public AndroidImageButton<TSelf> ImageButton(Locator locator) => new(locator, Self);

    public AndroidImageButton<TSelf> ImageButton(string locator) => new(locator, Self);

    public AndroidRoundButton<TSelf> RoundButton(Locator locator) => new(locator, Self);

    public AndroidRoundButton<TSelf> RoundButton(string locator) => new(locator, Self);

    public AndroidLink<TSelf> Link(Locator locator) => new(locator, Self);

    public AndroidLink<TSelf> Link(string locator) => new(locator, Self);

    public AndroidText<TSelf> Text(Locator locator) => new(locator, Self);

    public AndroidText<TSelf> Text(string locator) => new(locator, Self);

    public AndroidLabel<TSelf> Label(Locator locator) => new(locator, Self);

    public AndroidLabel<TSelf> Label(string locator) => new(locator, Self);

    public AndroidImage<TSelf> Image(Locator locator) => new(locator, Self);

    public AndroidImage<TSelf> Image(string locator) => new(locator, Self);

    public AndroidProgressBar<TSelf> ProgressBar(Locator locator) => new(locator, Self);

    public AndroidProgressBar<TSelf> ProgressBar(string locator) => new(locator, Self);

    public AndroidActivityIndicator<TSelf> ActivityIndicator(Locator locator) => new(locator, Self);

    public AndroidActivityIndicator<TSelf> ActivityIndicator(string locator) => new(locator, Self);

    public AndroidEditText<TSelf> EditText(Locator locator) => new(locator, Self);

    public AndroidEditText<TSelf> EditText(string locator) => new(locator, Self);

    public AndroidEntry<TSelf> Entry(Locator locator) => new(locator, Self);

    public AndroidEntry<TSelf> Entry(string locator) => new(locator, Self);

    public AndroidEditor<TSelf> Editor(Locator locator) => new(locator, Self);

    public AndroidEditor<TSelf> Editor(string locator) => new(locator, Self);

    public AndroidSearchBar<TSelf> SearchBar(Locator locator) => new(locator, Self);

    public AndroidSearchBar<TSelf> SearchBar(string locator) => new(locator, Self);

    public AndroidCheckBox<TSelf> CheckBox(Locator locator) => new(locator, Self);

    public AndroidCheckBox<TSelf> CheckBox(string locator) => new(locator, Self);

    public AndroidSwitch<TSelf> Switch(Locator locator) => new(locator, Self);

    public AndroidSwitch<TSelf> Switch(string locator) => new(locator, Self);

    public AndroidRadioButton<TSelf> RadioButton(Locator locator) => new(locator, Self);

    public AndroidRadioButton<TSelf> RadioButton(string locator) => new(locator, Self);

    public AndroidSlider<TSelf> Slider(Locator locator) => new(locator, Self);

    public AndroidSlider<TSelf> Slider(string locator) => new(locator, Self);

    public AndroidSeekBar<TSelf> SeekBar(Locator locator) => new(locator, Self);

    public AndroidSeekBar<TSelf> SeekBar(string locator) => new(locator, Self);

    public AndroidStepper<TSelf> Stepper(Locator locator) => new(locator, Self);

    public AndroidStepper<TSelf> Stepper(string locator) => new(locator, Self);

    public AndroidDatePicker<TSelf> DatePicker(Locator locator) => new(locator, Self);

    public AndroidDatePicker<TSelf> DatePicker(string locator) => new(locator, Self);

    public AndroidTimePicker<TSelf> TimePicker(Locator locator) => new(locator, Self);

    public AndroidTimePicker<TSelf> TimePicker(string locator) => new(locator, Self);

    public AndroidPicker<TSelf> Picker(Locator locator) => new(locator, Self);

    public AndroidPicker<TSelf> Picker(string locator) => new(locator, Self);

    public AndroidSpinner<TSelf> Spinner(Locator locator) => new(locator, Self);

    public AndroidSpinner<TSelf> Spinner(string locator) => new(locator, Self);

    public AndroidSelectionList<TSelf> SelectionList(Locator locator) => new(locator, Self);

    public AndroidSelectionList<TSelf> SelectionList(string locator) => new(locator, Self);

    public AndroidSelectionList<TSelf> SelectionList() => new(Self);

    public AndroidRecyclerView<TSelf> RecyclerView(Locator locator) => new(locator, Self);

    public AndroidRecyclerView<TSelf> RecyclerView(string locator) => new(locator, Self);

    public AndroidListView<TSelf> ListView(Locator locator) => new(locator, Self);

    public AndroidListView<TSelf> ListView(string locator) => new(locator, Self);

    public AndroidCollectionView<TSelf> CollectionView(Locator locator) => new(locator, Self);

    public AndroidCollectionView<TSelf> CollectionView(string locator) => new(locator, Self);

    public AndroidCarouselView<TSelf> CarouselView(Locator locator) => new(locator, Self);

    public AndroidCarouselView<TSelf> CarouselView(string locator) => new(locator, Self);

    public AndroidTableView<TSelf> TableView(Locator locator) => new(locator, Self);

    public AndroidTableView<TSelf> TableView(string locator) => new(locator, Self);

    public AndroidViewGroup<TSelf> ViewGroup(Locator locator) => new(locator, Self);

    public AndroidViewGroup<TSelf> ViewGroup(string locator) => new(locator, Self);

    public AndroidGrid<TSelf> Grid(Locator locator) => new(locator, Self);

    public AndroidGrid<TSelf> Grid(string locator) => new(locator, Self);

    public AndroidScrollView<TSelf> ScrollView(Locator locator) => new(locator, Self);

    public AndroidScrollView<TSelf> ScrollView(string locator) => new(locator, Self);

    public AndroidRefreshView<TSelf> RefreshView(Locator locator) => new(locator, Self);

    public AndroidRefreshView<TSelf> RefreshView(string locator) => new(locator, Self);

    public AndroidSwipeView<TSelf> SwipeView(Locator locator) => new(locator, Self);

    public AndroidSwipeView<TSelf> SwipeView(string locator) => new(locator, Self);

    public AndroidExpander<TSelf> Expander(Locator locator) => new(locator, Self);

    public AndroidExpander<TSelf> Expander(string locator) => new(locator, Self);

    public AndroidToolbar<TSelf> Toolbar(Locator locator) => new(locator, Self);

    public AndroidToolbar<TSelf> Toolbar(string locator) => new(locator, Self);

    public AndroidMenu<TSelf> Menu(Locator locator) => new(locator, Self);

    public AndroidMenu<TSelf> Menu(string locator) => new(locator, Self);

    public AndroidTabMenu<TSelf> TabMenu(Locator locator) => new(locator, Self);

    public AndroidTabMenu<TSelf> TabMenu(string locator) => new(locator, Self);

    public AndroidTab<TSelf> Tab(Locator locator) => new(locator, Self);

    public AndroidTab<TSelf> Tab(string locator) => new(locator, Self);

    public AndroidFlyoutItem<TSelf> FlyoutItem(Locator locator) => new(locator, Self);

    public AndroidFlyoutItem<TSelf> FlyoutItem(string locator) => new(locator, Self);

    public AndroidWebView<TSelf> WebView(Locator locator) => new(locator, Self);

    public AndroidWebView<TSelf> WebView(string locator) => new(locator, Self);

    public AndroidMediaElement<TSelf> MediaElement(Locator locator) => new(locator, Self);

    public AndroidMediaElement<TSelf> MediaElement(string locator) => new(locator, Self);

    public AndroidDialog<TSelf> Dialog(Locator locator) => new(locator, Self);

    public AndroidDialog<TSelf> Dialog(string locator) => new(locator, Self);

    public AndroidContentDialog<TSelf> ContentDialog(Locator locator) => new(locator, Self);

    public AndroidContentDialog<TSelf> ContentDialog(string locator) => new(locator, Self);

    public AndroidPermissionDialog<TSelf> PermissionDialog() => new(Self);

    protected bool Poll(Func<bool> condition, int timeoutMs)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeoutMs);
        do
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(Context.Timeouts.PollingInterval);
        }
        while (DateTimeOffset.UtcNow < deadline);

        return condition();
    }
}

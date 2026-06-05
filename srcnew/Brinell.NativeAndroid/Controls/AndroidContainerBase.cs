namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidContainerBase<TParent, TSelf> :
    NativeAndroidControl<TParent>,
    INativeAndroidContainer<TParent, TSelf>
    where TParent : INativeAndroidScope<TParent>
    where TSelf : AndroidContainerBase<TParent, TSelf>
{
    private readonly INativeAndroidScope<TParent> parentScope;
    private NativeAndroidElement? cachedRoot;

    protected AndroidContainerBase(Locator locator, INativeAndroidScope<TParent> parentScope)
        : base(locator, parentScope)
    {
        this.parentScope = parentScope ?? throw new ArgumentNullException(nameof(parentScope));
    }

    protected AndroidContainerBase(string locatorValue, INativeAndroidScope<TParent> parentScope)
        : base(locatorValue, parentScope)
    {
        this.parentScope = parentScope ?? throw new ArgumentNullException(nameof(parentScope));
    }

    public TSelf Self => (TSelf)this;

    public TParent Parent => parentScope.Self;

    public new NativeAndroidTestContext Context => parentScope.Context;

    public LocatorStrategy DefaultLocatorStrategy => parentScope.DefaultLocatorStrategy;

    public new IPageObject? Page => parentScope.Page;

    public NativeAndroidElement ContainerRoot
    {
        get
        {
            if (cachedRoot is not null)
            {
                try
                {
                    _ = cachedRoot.TagName;
                    return cachedRoot;
                }
                catch (WebDriverException)
                {
                    cachedRoot = null;
                }
            }

            cachedRoot = FindElement();
            return cachedRoot;
        }
    }

    public void InvalidateCache()
        => cachedRoot = null;

    public bool IsReady(int? timeoutMs = null)
        => parentScope.IsReady(timeoutMs) && TryGetContainerRoot() is not null;

    public bool WaitReady(int? timeoutMs = null)
        => Poll(() => IsReady(0), timeoutMs ?? Context.Timeouts.DefaultWait);

    public NativeAndroidElement? TryFindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);

        var root = TryGetContainerRoot();
        if (root is null)
        {
            return null;
        }

        return root.TryFindElement(locator, out var element, 0) ? element : null;
    }

    public NativeAndroidElement FindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return ContainerRoot.FindElement(locator, Context.Timeouts.ElementFind);
    }

    public IReadOnlyList<NativeAndroidElement> FindElements(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return TryGetContainerRoot()?.FindElements(locator) ?? Array.Empty<NativeAndroidElement>();
    }

    protected AndroidButton<TSelf> Button(Locator locator) => new(locator, Self);

    protected AndroidButton<TSelf> Button(string locator) => new(locator, Self);

    protected AndroidIconCommandButton<TSelf> IconCommandButton(Locator locator) => new(locator, Self);

    protected AndroidIconCommandButton<TSelf> IconCommandButton(string locator) => new(locator, Self);

    protected AndroidImageButton<TSelf> ImageButton(Locator locator) => new(locator, Self);

    protected AndroidImageButton<TSelf> ImageButton(string locator) => new(locator, Self);

    protected AndroidRoundButton<TSelf> RoundButton(Locator locator) => new(locator, Self);

    protected AndroidRoundButton<TSelf> RoundButton(string locator) => new(locator, Self);

    protected AndroidLink<TSelf> Link(Locator locator) => new(locator, Self);

    protected AndroidLink<TSelf> Link(string locator) => new(locator, Self);

    protected AndroidText<TSelf> Text(Locator locator) => new(locator, Self);

    protected AndroidText<TSelf> Text(string locator) => new(locator, Self);

    protected AndroidLabel<TSelf> Label(Locator locator) => new(locator, Self);

    protected AndroidLabel<TSelf> Label(string locator) => new(locator, Self);

    protected AndroidImage<TSelf> Image(Locator locator) => new(locator, Self);

    protected AndroidImage<TSelf> Image(string locator) => new(locator, Self);

    protected AndroidProgressBar<TSelf> ProgressBar(Locator locator) => new(locator, Self);

    protected AndroidProgressBar<TSelf> ProgressBar(string locator) => new(locator, Self);

    protected AndroidActivityIndicator<TSelf> ActivityIndicator(Locator locator) => new(locator, Self);

    protected AndroidActivityIndicator<TSelf> ActivityIndicator(string locator) => new(locator, Self);

    protected AndroidEditText<TSelf> EditText(Locator locator) => new(locator, Self);

    protected AndroidEditText<TSelf> EditText(string locator) => new(locator, Self);

    protected AndroidEntry<TSelf> Entry(Locator locator) => new(locator, Self);

    protected AndroidEntry<TSelf> Entry(string locator) => new(locator, Self);

    protected AndroidEditor<TSelf> Editor(Locator locator) => new(locator, Self);

    protected AndroidEditor<TSelf> Editor(string locator) => new(locator, Self);

    protected AndroidSearchBar<TSelf> SearchBar(Locator locator) => new(locator, Self);

    protected AndroidSearchBar<TSelf> SearchBar(string locator) => new(locator, Self);

    protected AndroidCheckBox<TSelf> CheckBox(Locator locator) => new(locator, Self);

    protected AndroidCheckBox<TSelf> CheckBox(string locator) => new(locator, Self);

    protected AndroidSwitch<TSelf> Switch(Locator locator) => new(locator, Self);

    protected AndroidSwitch<TSelf> Switch(string locator) => new(locator, Self);

    protected AndroidRadioButton<TSelf> RadioButton(Locator locator) => new(locator, Self);

    protected AndroidRadioButton<TSelf> RadioButton(string locator) => new(locator, Self);

    protected AndroidSlider<TSelf> Slider(Locator locator) => new(locator, Self);

    protected AndroidSlider<TSelf> Slider(string locator) => new(locator, Self);

    protected AndroidSeekBar<TSelf> SeekBar(Locator locator) => new(locator, Self);

    protected AndroidSeekBar<TSelf> SeekBar(string locator) => new(locator, Self);

    protected AndroidStepper<TSelf> Stepper(Locator locator) => new(locator, Self);

    protected AndroidStepper<TSelf> Stepper(string locator) => new(locator, Self);

    protected AndroidDatePicker<TSelf> DatePicker(Locator locator) => new(locator, Self);

    protected AndroidDatePicker<TSelf> DatePicker(string locator) => new(locator, Self);

    protected AndroidTimePicker<TSelf> TimePicker(Locator locator) => new(locator, Self);

    protected AndroidTimePicker<TSelf> TimePicker(string locator) => new(locator, Self);

    protected AndroidPicker<TSelf> Picker(Locator locator) => new(locator, Self);

    protected AndroidPicker<TSelf> Picker(string locator) => new(locator, Self);

    protected AndroidSpinner<TSelf> Spinner(Locator locator) => new(locator, Self);

    protected AndroidSpinner<TSelf> Spinner(string locator) => new(locator, Self);

    protected AndroidSelectionList<TSelf> SelectionList(Locator locator) => new(locator, Self);

    protected AndroidSelectionList<TSelf> SelectionList(string locator) => new(locator, Self);

    protected AndroidRecyclerView<TSelf> RecyclerView(Locator locator) => new(locator, Self);

    protected AndroidRecyclerView<TSelf> RecyclerView(string locator) => new(locator, Self);

    protected AndroidListView<TSelf> ListView(Locator locator) => new(locator, Self);

    protected AndroidListView<TSelf> ListView(string locator) => new(locator, Self);

    protected AndroidCollectionView<TSelf> CollectionView(Locator locator) => new(locator, Self);

    protected AndroidCollectionView<TSelf> CollectionView(string locator) => new(locator, Self);

    protected AndroidCarouselView<TSelf> CarouselView(Locator locator) => new(locator, Self);

    protected AndroidCarouselView<TSelf> CarouselView(string locator) => new(locator, Self);

    protected AndroidTableView<TSelf> TableView(Locator locator) => new(locator, Self);

    protected AndroidTableView<TSelf> TableView(string locator) => new(locator, Self);

    protected AndroidToolbar<TSelf> Toolbar(Locator locator) => new(locator, Self);

    protected AndroidToolbar<TSelf> Toolbar(string locator) => new(locator, Self);

    protected AndroidTab<TSelf> Tab(Locator locator) => new(locator, Self);

    protected AndroidTab<TSelf> Tab(string locator) => new(locator, Self);

    protected AndroidMenu<TSelf> Menu(Locator locator) => new(locator, Self);

    protected AndroidMenu<TSelf> Menu(string locator) => new(locator, Self);

    protected AndroidWebView<TSelf> WebView(Locator locator) => new(locator, Self);

    protected AndroidWebView<TSelf> WebView(string locator) => new(locator, Self);

    protected AndroidMediaElement<TSelf> MediaElement(Locator locator) => new(locator, Self);

    protected AndroidMediaElement<TSelf> MediaElement(string locator) => new(locator, Self);

    private NativeAndroidElement? TryGetContainerRoot()
    {
        try
        {
            return ContainerRoot;
        }
        catch (ElementNotFoundException)
        {
            return null;
        }
        catch (WebDriverException)
        {
            InvalidateCache();
            return null;
        }
    }
}

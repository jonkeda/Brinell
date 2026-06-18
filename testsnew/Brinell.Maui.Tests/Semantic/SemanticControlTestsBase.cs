namespace Brinell.Maui.Tests.Semantic;

public abstract class SemanticControlTestsBase
{
    protected readonly Mock<IMauiTestContext> Context = new();
    protected readonly TestPage Page;

    protected SemanticControlTestsBase()
    {
        Context.Setup(c => c.Timeouts).Returns(new TimeoutSettings
        {
            DefaultWait = 100,
            PageLoad = 100,
            PollingInterval = 1
        });
        Context.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.AutomationId);

        Page = new TestPage(Context.Object);
    }

    protected static Mock<IMauiElement> CreateElement(
        string automationId,
        int x,
        int y,
        int width,
        int height)
    {
        var element = new Mock<IMauiElement>();
        element.Setup(e => e.Visible).Returns(true);
        element.Setup(e => e.Enabled).Returns(true);
        element.Setup(e => e.Rect).Returns(new System.Drawing.Rectangle(x, y, width, height));
        element.Setup(e => e.GetAttribute("AutomationId")).Returns(automationId);
        return element;
    }

    protected static Mock<IMauiElement> CreateInvokableElement(
        string automationId,
        int x,
        int y,
        int width,
        int height)
    {
        var element = CreateElement(automationId, x, y, width, height);
        element.As<IInvokePatternElement>()
            .Setup(e => e.SupportsInvokePattern)
            .Returns(true);
        element.As<IInvokePatternElement>()
            .Setup(e => e.InvokePattern())
            .Returns(true);
        return element;
    }

    protected static Mock<IMauiElement> CreateSelectableElement(
        string automationId,
        int x,
        int y,
        int width,
        int height,
        Action? onSelect = null)
    {
        var element = CreateElement(automationId, x, y, width, height);
        element.As<ISelectionItemPatternElement>()
            .Setup(e => e.SupportsSelectionItemPattern)
            .Returns(true);
        element.As<ISelectionItemPatternElement>()
            .Setup(e => e.SelectItemPattern())
            .Callback(() => onSelect?.Invoke())
            .Returns(true);
        return element;
    }

    protected static Mock<IMauiElement> CreateToggleElement(
        string automationId,
        int x,
        int y,
        int width,
        int height,
        bool initialState)
    {
        var isChecked = initialState;
        var element = CreateElement(automationId, x, y, width, height);
        element.Setup(e => e.Selected).Returns(() => isChecked);
        element.As<ITogglePatternElement>()
            .Setup(e => e.SupportsTogglePattern)
            .Returns(true);
        element.As<ITogglePatternElement>()
            .Setup(e => e.IsTogglePatternChecked())
            .Returns(() => isChecked);
        element.As<ITogglePatternElement>()
            .Setup(e => e.TogglePattern())
            .Callback(() => isChecked = !isChecked)
            .Returns(true);
        element.As<ITogglePatternElement>()
            .Setup(e => e.SetToggleStatePattern(It.IsAny<bool>()))
            .Callback<bool>(value => isChecked = value)
            .Returns(true);
        return element;
    }

    protected static Mock<IMauiElement> CreateLegacyAccessibleElement(
        string automationId,
        int x,
        int y,
        int width,
        int height)
    {
        var element = CreateElement(automationId, x, y, width, height);
        element.As<ILegacyIAccessiblePatternElement>()
            .Setup(e => e.SupportsLegacyIAccessiblePattern)
            .Returns(true);
        element.As<ILegacyIAccessiblePatternElement>()
            .Setup(e => e.DoDefaultActionPattern())
            .Returns(true);
        return element;
    }

    protected sealed class TestPage : PageObjectBase<TestPage>
    {
        public TestPage(IMauiTestContext context)
            : base(context)
        {
        }

        public override string Name => "TestPage";

        public override bool IsLoaded(int? timeoutMs = null) => true;

        public EditableField<TestPage> TestField => new(this, "TestField");

        public Editor<TestPage> Notes => new(this, "Notes");

        public IconCommandButton<TestPage> Save => new(this, "SaveButton");

        public RoundButton<TestPage> Add => new(this, "AddButton");

        public Button<TestPage> PromptOk => new(this, "PromptDialogView_OKButton");

        public Button<TestPage> NativeDialogDelete => new(this, "NativeDialog_Delete");

        public ContentDialog<TestPage> Dialog => new(this);

        public GenericBrowser<TestPage> Browser => new(this);

        public SelectionList<TestPage> List => new(this);

        public TabMenu<TestPage> Tabs => new(this);

        public CheckBox<TestPage> IncludeProblemReports => new(this, "IncludeProblemReports");

        public List<TestPage, TestListItem> TypedList => new(
            this,
            "TestList",
            "Item_",
            (scope, index) => new TestListItem(scope, index));
    }

    protected sealed class TestListItem : ContainerBase<TestPage, TestListItem>
    {
        public TestListItem(IMauiScope<TestPage> scope, int index)
            : base(scope, Locator.ByAutomationId($"Item_{index}"))
        {
        }
    }
}

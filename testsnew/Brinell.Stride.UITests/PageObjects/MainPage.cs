namespace Brinell.Stride.UITests.PageObjects;

/// <summary>
/// Page object for the main sample app page (legacy counter/greeting UI).
/// </summary>
public class MainPage : PageObjectBase<MainPage>
{
    public override string Name => "Main Page";
    public override string AutomationId => "MainPanel";

    public MainPage(IStrideTestContext context) : base(context) { }

    // Controls
    public TextBlock<MainPage> Title => TextBlock("Title");
    public TextBlock<MainPage> CounterDisplay => TextBlock("CounterDisplay");
    public Button<MainPage> IncrementButton => Button("IncrementButton");
    public Button<MainPage> DecrementButton => Button("DecrementButton");
    public Button<MainPage> ResetButton => Button("ResetButton");
    public EditText<MainPage> NameInput => EditText("NameInput");
    public Button<MainPage> GreetButton => Button("GreetButton");
    public TextBlock<MainPage> GreetingDisplay => TextBlock("GreetingDisplay");
    public ToggleButton<MainPage> DarkModeToggle => ToggleButton("DarkModeToggle");
    public Slider<MainPage> VolumeSlider => Slider("VolumeSlider");
    public TextBlock<MainPage> VolumeDisplay => TextBlock("VolumeDisplay");

    // Actions
    public int GetCounterValue()
    {
        var text = CounterDisplay.GetText() ?? "";
        var valueStr = text.Replace("Count: ", "");
        return int.TryParse(valueStr, out var value) ? value : 0;
    }

    public MainPage IncrementCounter()
    {
        var before = GetCounterValue();
        IncrementButton.Click();
        WaitFor(() => GetCounterValue() == before + 1);
        return this;
    }

    public MainPage DecrementCounter()
    {
        var before = GetCounterValue();
        DecrementButton.Click();
        WaitFor(() => GetCounterValue() == before - 1);
        return this;
    }

    public MainPage ResetCounter()
    {
        ResetButton.Click();
        WaitFor(() => GetCounterValue() == 0);
        return this;
    }

    public MainPage Greet(string name)
    {
        NameInput.SetText(name);
        GreetButton.Click();
        WaitFor(() => !string.IsNullOrEmpty(GreetingDisplay.GetText()));
        return this;
    }

    public MainPage SetVolume(double value)
    {
        VolumeSlider.SetValue(value);
        return this;
    }
}

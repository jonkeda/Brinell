using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;
using System.Globalization;
using System.Runtime.CompilerServices;
using Bruinell.Core;
using Bruinell.Core.Locators;
using Bruinell.Maui.Interfaces;
using Microsoft.Maui.Controls;
using MUnit.Inspection;
using MUnit.Interaction;
using MauiElement = Microsoft.Maui.Controls.Element;

namespace Bruinell.Maui.Munit;

/// <summary>
/// One MAUI element of a page rendered by MUnit, as Bruinell's controls ask for it. State is read
/// from the element itself; actions go through MUnit's interactions, so they run the control's own
/// events, commands and bindings and refuse what a user could not do.
/// </summary>
/// <remarks>
/// There is no layout: a visible element reports nominal 1x1 bounds at the origin, so Bruinell's
/// usable-bounds rule holds, and a hidden one reports none. Members a device alone can answer
/// (hover, swipe, pixels) throw <see cref="DeviceOnlyException"/>.
/// </remarks>
public sealed class MunitMauiElement : IMauiElement
{
    private static readonly ConditionalWeakTable<MauiElement, string> Keys = new();
    private static long s_nextKey;

    private readonly MunitMauiDriver _driver;
    private readonly bool _isApp;

    internal MunitMauiElement(MunitMauiDriver driver, MauiElement element, bool isApp = false)
    {
        _driver = driver;
        Element = element;
        _isApp = isApp;
    }

    /// <summary>The MAUI element.</summary>
    public MauiElement Element { get; }

    #region State

    public bool Visible => VisualTree.IsEffectivelyVisible(Element);

    public bool Enabled => VisualTree.IsEffectivelyEnabled(Element);

    public bool Selected => Element switch
    {
        RadioButton radio => radio.IsChecked,
        CheckBox box => box.IsChecked,
        _ => IsSelectedRow(),
    };

    public string? Text => VisualTree.AdapterFor(Element, _driver.TestContext.Adapters).GetText(Element);

    public string? TagName => Element.GetType().Name;

    public Point Location => Point.Empty;

    public Size Size => Rect.Size;

    public Rectangle Rect => Visible ? new Rectangle(0, 0, 1, 1) : Rectangle.Empty;

    public string? GetAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name is "ClassName" or "class")
            return TagName;

        // Any bindable property by its name: "IsToggled", "Placeholder", "Text".
        var field = Element.GetType().GetField(name + "Property",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
        return field?.GetValue(null) is BindableProperty property
            ? Convert.ToString(Element.GetValue(property), CultureInfo.InvariantCulture)
            : null;
    }

    #endregion

    #region Basic actions

    public void Click() => Invoke();

    public void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys)
    {
        var input = Input("type into");
        input.SetText(method == TextInputMethod.SetValue ? text : (input.Text ?? string.Empty) + text);
    }

    public void Clear() => Input("clear").ClearText();

    public void DoubleClick()
        => (Element as View ?? throw new ElementNotInteractableException($"{TagName} cannot be tapped.")).Tap(taps: 2);

    public void RightClick() => throw new DeviceOnlyException(nameof(RightClick));

    public void Hover() => throw new DeviceOnlyException(nameof(Hover));

    public void LongPress(int durationMs = 1000) => throw new DeviceOnlyException(nameof(LongPress));

    // Every element is in the tree and on "screen": there is nothing to scroll to.
    public void ScrollIntoView(int timeoutMs)
    {
    }

    public void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500)
        => throw new DeviceOnlyException(nameof(Swipe));

    #endregion

    #region Child lookup

    public IMauiElement? TryFindElement(Locator locator) => FindElements(locator).FirstOrDefault();

    public IReadOnlyList<IMauiElement> FindElements(Locator locator) => _driver.Find(Element, locator);

    #endregion

    #region Identity

    public string InstanceKey
        => Keys.GetValue(Element, _ => Interlocked.Increment(ref s_nextKey).ToString(CultureInfo.InvariantCulture));

    public string? AutomationId => Element.AutomationId;

    public string? Name => NameOf(Element);

    public bool Focused => Element is VisualElement { IsFocused: true };

    public string? Hint => Element switch
    {
        InputView input => input.Placeholder,
        Picker picker => picker.Title,
        _ => null,
    };

    #endregion

    #region Activation

    public void Invoke()
    {
        switch (Element)
        {
            case Microsoft.Maui.Controls.Button button: button.Click(); break;
            case ImageButton button: button.Click(); break;
            case MenuItem item: item.Click(); break;
            case Switch toggle: toggle.Toggle(); break;
            case CheckBox box: box.Toggle(); break;
            case RadioButton radio: radio.Select(); break;
            case MauiElement row when TrySelectRow(row): break;
            case View view: view.Tap(); break;
            default: throw new ElementNotInteractableException($"{TagName} #{AutomationId} has no primary action.");
        }
    }

    public void Toggle()
    {
        switch (Element)
        {
            case Switch toggle: toggle.Toggle(); break;
            case CheckBox box: box.Toggle(); break;
            default: throw new ElementNotInteractableException($"{TagName} #{AutomationId} cannot be toggled.");
        }
    }

    public void Select() => Invoke();

    public void InvokeToolbarItem(string automationId)
    {
        var item = _driver.CurrentPage.ToolbarItems.FirstOrDefault(candidate => candidate.AutomationId == automationId)
            ?? throw new ElementNotInteractableException($"The page has no toolbar item '{automationId}'.");
        item.Click();
    }

    #endregion

    #region Checked, text field, range

    public bool? Checked => Element switch
    {
        Switch toggle => toggle.IsToggled,
        CheckBox box => box.IsChecked,
        RadioButton radio => radio.IsChecked,
        _ => null,
    };

    public void SetChecked(bool isChecked)
    {
        switch (Element)
        {
            case Switch toggle: toggle.SetToggled(isChecked); break;
            case CheckBox box: box.SetChecked(isChecked); break;
            case RadioButton radio when isChecked: radio.Select(); break;
            default: throw new ElementNotInteractableException($"{TagName} #{AutomationId} has no checked state to set.");
        }
    }

    public bool? IsReadOnly => Element is InputView input ? input.IsReadOnly : null;

    public double? RangeValue => Element switch { Slider s => s.Value, Stepper s => s.Value, ProgressBar p => p.Progress, _ => null };

    public double? RangeMinimum => Element switch { Slider s => s.Minimum, Stepper s => s.Minimum, ProgressBar => 0, _ => null };

    public double? RangeMaximum => Element switch { Slider s => s.Maximum, Stepper s => s.Maximum, ProgressBar => 1, _ => null };

    public double? RangeSmallChange => Element is Stepper stepper ? stepper.Increment : null;

    public void SetRangeValue(double value)
    {
        switch (Element)
        {
            case Slider slider: slider.SlideTo(value); break;
            case Stepper stepper: stepper.StepTo(value); break;
            default: throw new ElementNotInteractableException($"{TagName} #{AutomationId} has no range to set.");
        }
    }

    public void AppendText(string text) => SendKeys(text, TextInputMethod.Keys);

    public void ClearFocus()
    {
    }

    public void Focus()
    {
    }

    public void Submit()
    {
        switch (Element)
        {
            case Entry entry: entry.Complete(); break;
            case SearchBar search: search.Search(); break;
            default: throw new ElementNotInteractableException($"{TagName} #{AutomationId} cannot be submitted.");
        }
    }

    #endregion

    #region Selection, dates and times

    public string? SelectedItemText => Element is Picker ? Text : null;

    public IReadOnlyList<string>? ReadItemTexts() => Element switch
    {
        Picker picker => [.. picker.Items],
        ItemsView => _driver.Read(Element).Children.Select(FirstText).OfType<string>().ToList(),
        _ => null,
    };

    public void SelectIndex(int index)
    {
        switch (Element)
        {
            case Picker picker: picker.SelectIndex(index); break;
            case SelectableItemsView view:
                var items = view.ItemsSource?.Cast<object>().ToList() ?? [];
                if (index < 0 || index >= items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index), index, $"{TagName} #{AutomationId} has {items.Count} items.");
                view.Select(items[index]);
                break;
            case CarouselView carousel: Interactions.Act(carousel, "swipe", () => carousel.Position = index); break;
            default: throw new ElementNotInteractableException($"{TagName} #{AutomationId} has no items to select.");
        }
    }

    public void SelectByText(string text)
    {
        switch (Element)
        {
            case Picker picker: picker.Select(text); break;
            case SelectableItemsView view:
                var row = _driver.Read(view).Children.FirstOrDefault(child => FirstText(child) == text)
                    ?? throw new ElementNotInteractableException($"{TagName} #{AutomationId} shows no row '{text}'.");
                view.Select(row.BindingContext!);
                break;
            default: throw new ElementNotInteractableException($"{TagName} #{AutomationId} has no items to select.");
        }
    }

    public void SetDate(DateTime date)
        => (Element as DatePicker ?? throw new ElementNotInteractableException($"{TagName} is no date picker.")).SetDate(date);

    public void SetTime(TimeSpan time)
        => (Element as TimePicker ?? throw new ElementNotInteractableException($"{TagName} is no time picker.")).SetTime(time);

    #endregion

    #region The app

    public bool? IsFlyoutOpen
        => _isApp
            ? _driver.Window.Page switch { Shell shell => shell.FlyoutIsPresented, FlyoutPage flyout => flyout.IsPresented, _ => false }
            : throw new NotSupportedException("Ask IMauiTestContext.AppElement.");

    public void OpenFlyout() => SetFlyout(true);

    public void CloseFlyout() => SetFlyout(false);

    // MUnit has no alert or action sheet yet (the proposal's Phase 4): none is ever open.
    public AlertContents? ReadAlert() => _isApp ? null : throw new NotSupportedException("Ask IMauiTestContext.AppElement.");

    public IMauiElement? TryFindActiveDialog() => _isApp ? null : throw new NotSupportedException("Ask IMauiTestContext.AppElement.");

    public IMauiElement? TryFindDeclared(string automationId)
        => _isApp
            ? _driver.Find(_driver.Window, Locator.ByAutomationId(automationId)).FirstOrDefault()
            : throw new NotSupportedException("Ask IMauiTestContext.AppElement.");

    #endregion

    #region Web content and forms

    public string? GetDomAttribute(string attributeName) => null;

    public string? GetDomProperty(string propertyName) => null;

    public string? GetCssValue(string propertyName) => null;

    #endregion

    /// <summary>The accessible name: the semantic description, else the automation name.</summary>
    internal static string? NameOf(MauiElement element)
        => element is BindableObject bindable
            ? SemanticProperties.GetDescription(bindable) ?? AutomationProperties.GetName(bindable)
            : null;

    public override string ToString() => $"{TagName} #{AutomationId}";

    private InputView Input(string verb)
        => Element as InputView ?? throw new ElementNotInteractableException($"Cannot {verb} {TagName} #{AutomationId}: it is no text field.");

    private void SetFlyout(bool open)
    {
        if (!_isApp)
            throw new NotSupportedException("Ask IMauiTestContext.AppElement.");

        switch (_driver.Window.Page)
        {
            case Shell shell: _driver.TestContext.Dispatcher.Run(() => shell.FlyoutIsPresented = open); break;
            case FlyoutPage flyout: _driver.TestContext.Dispatcher.Run(() => flyout.IsPresented = open); break;
            default: throw new ElementNotInteractableException("The app has no flyout.");
        }
    }

    private bool IsSelectedRow()
        => Element.Parent is SelectableItemsView view && Element.BindingContext is { } item
           && (Equals(view.SelectedItem, item) || view.SelectedItems?.Contains(item) == true);

    private static bool TrySelectRow(MauiElement row)
    {
        if (row.Parent is not SelectableItemsView { SelectionMode: not SelectionMode.None } view || row.BindingContext is not { } item)
            return false;

        view.Select(item);
        return true;
    }

    private static string? FirstText(VisualNode node)
        => node.DescendantsAndSelf().Select(n => n.Text).FirstOrDefault(text => !string.IsNullOrEmpty(text));
}

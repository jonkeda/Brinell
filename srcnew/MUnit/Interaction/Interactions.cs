using System.Collections;
using Microsoft.Maui.Controls;
using MUnit.Inspection;
using MUnit.Internals;

namespace MUnit.Interaction;

/// <summary>
/// What a user does to a control, done through the control's own entry points so its events,
/// commands and two-way bindings run as on a device. Each runs on the context's dispatcher and
/// returns once the work it queued has run.
/// </summary>
/// <remarks>
/// Every interaction first checks the user could do it: the element is visible and enabled (and an
/// input is not read-only). A button whose command cannot execute is disabled by MAUI itself, so it
/// is refused too.
/// </remarks>
public static class Interactions
{
    #region Buttons and taps

    /// <summary>Clicks the button: Pressed, Released, then Clicked and its Command.</summary>
    public static Button Click(this Button button)
    {
        Act(button, "click", () =>
        {
            button.SendPressed();
            button.SendReleased();
            button.SendClicked();
        });
        return button;
    }

    /// <summary>Clicks the image button: Pressed, Released, then Clicked and its Command.</summary>
    public static ImageButton Click(this ImageButton button)
    {
        Act(button, "click", () =>
        {
            button.SendPressed();
            button.SendReleased();
            button.SendClicked();
        });
        return button;
    }

    /// <summary>Chooses a toolbar or menu item: its Clicked and Command.</summary>
    public static MenuItem Click(this MenuItem item)
    {
        Act(item, "click", () => ((IMenuItemController)item).Activate());
        return item;
    }

    /// <summary>
    /// Taps the view <paramref name="taps"/> times in a row through the recognizers that want that many,
    /// as the platform does: each one's Command and Tapped.
    /// </summary>
    /// <exception cref="ElementNotInteractableException">The view has no recognizer for that many taps.</exception>
    public static TView Tap<TView>(this TView view, int taps = 1) where TView : View
    {
        var recognizers = view.GestureRecognizers.OfType<TapGestureRecognizer>()
            .Where(recognizer => recognizer.NumberOfTapsRequired == taps)
            .ToList();
        if (recognizers.Count == 0)
            throw new ElementNotInteractableException($"Cannot tap {Name(view)}: it has no TapGestureRecognizer for {taps} tap(s).");

        Act(view, "tap", () =>
        {
            foreach (var recognizer in recognizers)
                MauiInternals.SendTapped(recognizer, view);
        });
        return view;
    }

    #endregion

    #region Text input

    /// <summary>
    /// Types <paramref name="text"/> over what the field holds: Text changes (TextChanged, two-way
    /// bindings), cut at MaxLength as the platforms cut it.
    /// </summary>
    public static TInput SetText<TInput>(this TInput input, string? text) where TInput : InputView
    {
        EnsureWritable(input, "type into");
        Act(input, "type into", () =>
            input.Text = text is not null && input.MaxLength >= 0 && text.Length > input.MaxLength
                ? text[..input.MaxLength]
                : text);
        return input;
    }

    /// <summary>Empties the field.</summary>
    public static TInput ClearText<TInput>(this TInput input) where TInput : InputView => input.SetText(string.Empty);

    /// <summary>Presses return in the entry: Completed and ReturnCommand.</summary>
    public static Entry Complete(this Entry entry)
    {
        Act(entry, "complete", entry.SendCompleted);
        return entry;
    }

    /// <summary>Presses the search button: SearchButtonPressed and SearchCommand.</summary>
    public static SearchBar Search(this SearchBar searchBar)
    {
        Act(searchBar, "search", ((ISearchBarController)searchBar).OnSearchButtonPressed);
        return searchBar;
    }

    #endregion

    #region Toggles

    /// <summary>Flips the switch.</summary>
    public static Switch Toggle(this Switch toggle) => toggle.SetToggled(!toggle.IsToggled);

    /// <summary>Sets the switch on or off; already there is no change and raises nothing.</summary>
    public static Switch SetToggled(this Switch toggle, bool on)
    {
        Act(toggle, "toggle", () => toggle.IsToggled = on);
        return toggle;
    }

    /// <summary>Flips the check box.</summary>
    public static CheckBox Toggle(this CheckBox box) => box.SetChecked(!box.IsChecked);

    /// <summary>Checks the check box.</summary>
    public static CheckBox Check(this CheckBox box) => box.SetChecked(true);

    /// <summary>Unchecks the check box.</summary>
    public static CheckBox Uncheck(this CheckBox box) => box.SetChecked(false);

    /// <summary>Sets the check box; already there is no change and raises nothing.</summary>
    public static CheckBox SetChecked(this CheckBox box, bool isChecked)
    {
        Act(box, "check", () => box.IsChecked = isChecked);
        return box;
    }

    /// <summary>Chooses the radio button; its group unchecks the others.</summary>
    public static RadioButton Select(this RadioButton radio)
    {
        Act(radio, "select", () => radio.IsChecked = true);
        return radio;
    }

    #endregion

    #region Ranges, dates and times

    /// <summary>Drags the slider to <paramref name="value"/>: DragStarted, ValueChanged, DragCompleted; MAUI clamps it.</summary>
    public static Slider SlideTo(this Slider slider, double value)
    {
        Act(slider, "slide", () =>
        {
            var controller = (ISliderController)slider;
            controller.SendDragStarted();
            slider.Value = value;
            controller.SendDragCompleted();
        });
        return slider;
    }

    /// <summary>Steps the stepper to <paramref name="value"/>; MAUI clamps it.</summary>
    public static Stepper StepTo(this Stepper stepper, double value)
    {
        Act(stepper, "step", () => stepper.Value = value);
        return stepper;
    }

    /// <summary>Picks a date.</summary>
    /// <exception cref="ElementNotInteractableException">The date is outside MinimumDate and MaximumDate.</exception>
    public static DatePicker SetDate(this DatePicker picker, DateTime date)
    {
        if (date < picker.MinimumDate || date > picker.MaximumDate)
            throw new ElementNotInteractableException(
                $"Cannot pick {date:d} in {Name(picker)}: it allows {picker.MinimumDate:d} to {picker.MaximumDate:d}.");

        Act(picker, "pick a date in", () => picker.Date = date);
        return picker;
    }

    /// <summary>Picks a time.</summary>
    public static TimePicker SetTime(this TimePicker picker, TimeSpan time)
    {
        Act(picker, "pick a time in", () => picker.Time = time);
        return picker;
    }

    #endregion

    #region Selection

    /// <summary>Picks <paramref name="item"/>: one of ItemsSource, or one of Items by its text.</summary>
    /// <exception cref="ElementNotInteractableException">The picker does not offer it.</exception>
    public static Picker Select(this Picker picker, object item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var index = picker.ItemsSource is { } source
            ? IndexOf(source, item)
            : picker.Items.IndexOf(item as string ?? item.ToString() ?? string.Empty);
        if (index < 0)
            throw new ElementNotInteractableException($"Cannot pick '{item}' in {Name(picker)}: it does not offer it.");

        return picker.SelectIndex(index);
    }

    /// <summary>Picks the item at <paramref name="index"/>.</summary>
    public static Picker SelectIndex(this Picker picker, int index)
    {
        var count = picker.ItemsSource is { } source ? source.Cast<object?>().Count() : picker.Items.Count;
        if (index < 0 || index >= count)
            throw new ElementNotInteractableException($"Cannot pick item {index} in {Name(picker)}: it offers {count}.");

        Act(picker, "pick in", () => picker.SelectedIndex = index);
        return picker;
    }

    /// <summary>
    /// Selects <paramref name="item"/>, as tapping its row does: SelectedItem (single) or SelectedItems
    /// (multiple), SelectionChanged and SelectionChangedCommand.
    /// </summary>
    /// <exception cref="ElementNotInteractableException">Selection is off, or the item is not in ItemsSource.</exception>
    public static TView Select<TView>(this TView view, object item) where TView : SelectableItemsView
    {
        if (view.SelectionMode == SelectionMode.None)
            throw new ElementNotInteractableException($"Cannot select in {Name(view)}: its SelectionMode is None.");
        if (view.ItemsSource is not { } source || IndexOf(source, item) < 0)
            throw new ElementNotInteractableException($"Cannot select '{item}' in {Name(view)}: it is not in ItemsSource.");

        Act(view, "select in", () =>
        {
            if (view.SelectionMode == SelectionMode.Single)
                view.SelectedItem = item;
            else if (!view.SelectedItems.Contains(item))
                view.SelectedItems.Add(item);
        });
        return view;
    }

    /// <summary>Clears <paramref name="item"/> from the selection, as tapping a selected row in multiple mode does.</summary>
    public static TView Deselect<TView>(this TView view, object item) where TView : SelectableItemsView
    {
        Act(view, "deselect in", () =>
        {
            if (view.SelectionMode == SelectionMode.Single)
            {
                if (Equals(view.SelectedItem, item))
                    view.SelectedItem = null;
            }
            else
            {
                view.SelectedItems.Remove(item);
            }
        });
        return view;
    }

    /// <summary>Pulls to refresh: IsRefreshing and the Command.</summary>
    public static RefreshView Refresh(this RefreshView view)
    {
        Act(view, "refresh", () => view.IsRefreshing = true);
        return view;
    }

    #endregion

    /// <summary>
    /// Runs <paramref name="action"/> as an interaction with <paramref name="element"/>: refused when a user
    /// could not reach the element, else run on its context's dispatcher with what it queued.
    /// </summary>
    /// <exception cref="ElementNotInteractableException">The element is hidden or disabled.</exception>
    public static void Act(Element element, string verb, Action action)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(action);
        var context = MauiTestContext.Of(element);
        context?.Dispatcher.RunPending();

        if (!VisualTree.IsEffectivelyVisible(element))
            throw new ElementNotInteractableException($"Cannot {verb} {Name(element)}: it is not visible (IsVisible is false on it or an ancestor).");
        if (!VisualTree.IsEffectivelyEnabled(element))
            throw new ElementNotInteractableException($"Cannot {verb} {Name(element)}: it is disabled (IsEnabled is false on it or an ancestor, or its command cannot execute).");

        if (context is null)
            action();
        else
            context.Dispatcher.Run(action);
    }

    private static void EnsureWritable(InputView input, string verb)
    {
        if (input.IsReadOnly)
            throw new ElementNotInteractableException($"Cannot {verb} {Name(input)}: it is read-only.");
    }

    private static int IndexOf(IEnumerable source, object item)
    {
        var index = 0;
        foreach (var candidate in source)
        {
            if (Equals(candidate, item))
                return index;
            index++;
        }

        return -1;
    }

    private static string Name(Element element)
        => element.GetType().Name + (string.IsNullOrEmpty(element.AutomationId) ? "" : $" #{element.AutomationId}");
}

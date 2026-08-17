using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Brinell.Samples.Maui.App.ViewModels;

public class SelectionViewModel : ParentViewModel
{
    private ObservableCollection<string> pickerItems = new();
    private string selectedItem = string.Empty;
    private int selectedIndex = -1;
    private string statusMessage = "Ready. Select an item from the picker.";

    public ObservableCollection<string> PickerItems
    {
        get => pickerItems;
        set => SetProperty(ref pickerItems, value);
    }

    public string SelectedItem
    {
        get => selectedItem;
        set
        {
            if (selectedItem != value)
            {
                selectedItem = value;
                OnPropertyChanged();
                UpdateStatus();
            }
        }
    }

    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            if (selectedIndex != value)
            {
                selectedIndex = value;
                OnPropertyChanged();
                UpdateStatus();
            }
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public ICommand ResetCommand => new RelayCommand(Reset);

    public SelectionViewModel()
    {
        InitializePickerItems();
    }

    private void InitializePickerItems()
    {
        PickerItems.Add("Option 1");
        PickerItems.Add("Option 2");
        PickerItems.Add("Option 3");
        PickerItems.Add("Option 4");
        PickerItems.Add("Option 5");
    }

    private void UpdateStatus()
    {
        if (SelectedIndex >= 0 && SelectedIndex < PickerItems.Count)
        {
            StatusMessage = $"✓ Selected: {SelectedItem} (index {SelectedIndex})";
        }
        else
        {
            StatusMessage = "Ready. Select an item from the picker.";
        }
    }

    private void Reset()
    {
        SelectedIndex = -1;
        SelectedItem = string.Empty;
        StatusMessage = "Ready. Select an item from the picker.";
    }
}
